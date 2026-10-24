using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;
using Xunit.Abstractions;

namespace MiniContract.Tests;

public class Contract_ContractApprovedSignRemoteAndSendMailControllerPortTests : IDisposable
{
    private const string ErrPrefix = "Erridn.Skycic.QContract.";

    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractApprovedSignRemoteAndSendMailControllerPort _controller;

    public Contract_ContractApprovedSignRemoteAndSendMailControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Mst_ContractTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractPartySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractElementSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_ContractApprovedSignRemoteAndSendMailControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private static JsonElement Root(IActionResult result)
    {
        object? val = result switch
        {
            OkObjectResult ok => ok.Value,
            BadRequestObjectResult bad => bad.Value,
            _ => throw new Xunit.Sdk.XunitException($"Unexpected result type {result.GetType().Name}")
        };
        return JsonDocument.Parse(JsonSerializer.Serialize(val)).RootElement;
    }

    private static Contract_Contract_ApprovedSignRemoteAndSendMailReq BuildReq(
        string contractCode, string fileName, string flagOrgCreate = "1")
    {
        var model = new
        {
            FileList = new[] { new { FileName = fileName, FilePath = "/tmp/" + fileName } },
            ContractCode = contractCode,
            ContractFileName = fileName,
            FlagOrgCreate = flagOrgCreate,
            FlagElement = "0",
            FlagCheckOTP = "0"
        };
        var json = JsonSerializer.Serialize(model);
        using var doc = JsonDocument.Parse(json);
        return new Contract_Contract_ApprovedSignRemoteAndSendMailReq { Model = doc.RootElement.Clone() };
    }

    [Fact]
    public async Task EmptyBody_ReturnsCheckStrJson()
    {
        var res = await _controller.ApprovedSignRemoteAndSendMail(null);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task MissingContractCode_ReturnsCheckStrJson()
    {
        var req = new Contract_Contract_ApprovedSignRemoteAndSendMailReq();
        var res = await _controller.ApprovedSignRemoteAndSendMail(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNotFound_ReturnsInvalid_UserApproved()
    {
        var res = await _controller.ApprovedSignRemoteAndSendMail(BuildReq("HD9999", "hd9999.pdf"));
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ApprovedPath_StatusSIGNED_ReturnsInvalid_UserApproved()
    {
        var res = await _controller.ApprovedSignRemoteAndSendMail(BuildReq("HD0002", "hd0002.pdf", "1"));
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task PartySignPath_StatusPENDING_ReturnsInvalid_UserApproved()
    {
        // PartySign path (flagOrgCreate != "1") chỉ chấp nhận ONPROCESS (biz:35211-35235, FlagCreatePartnerSignFirst null)
        var res = await _controller.ApprovedSignRemoteAndSendMail(BuildReq("HD0004", "hd0004.pdf", "0"));
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractFileNameEmpty_ReturnsInvalidContractFileName()
    {
        var res = await _controller.ApprovedSignRemoteAndSendMail(BuildReq("HD0003", ""));
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_InvalidContractFileName", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNoNull_ReturnsContractNoIsNotNull()
    {
        var res = await _controller.ApprovedSignRemoteAndSendMail(BuildReq("HD0001", "hd0001.pdf"));
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_ContractNoIsNotNull", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Success_ApprovedPath_WritesApprovedHistAction()
    {
        var res = await _controller.ApprovedSignRemoteAndSendMail(BuildReq("HD0003", "hd0003_signed.pdf", "1"));
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal("HD0003", data.GetProperty("ContractCode").GetString());
        Assert.Equal("CONFIRMED", data.GetProperty("ContractStatus").GetString());
        Assert.Equal("APPROVED", data.GetProperty("HistActionType").GetString());
        Assert.Equal(1, data.GetProperty("HistActionVersion").GetInt32());
        Assert.True(data.GetProperty("SendResultFound").GetBoolean());

        var saved = await _db.Set<Contract_Contract>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("CONFIRMED", saved.ContractStatus);
        Assert.Equal("hd0003_signed.pdf", saved.ContractFilePath);

        var party = await _db.Set<Contract_ContractParty>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("CONFIRMED", party.ContractPartyStatus);

        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("APPROVED", hist.FunctionActionType);
        Assert.Equal("Contract_Contract_ApprovedX", hist.FunctionName);
    }

    [Fact]
    public async Task Success_PartySignPath_WritesPartySignHistAction()
    {
        var res = await _controller.ApprovedSignRemoteAndSendMail(BuildReq("HD0003", "hd0003_signed.pdf", "0"));
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("0", root.GetProperty("Data").GetProperty("FlagOrgCreate").GetString());

        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("PartySign", hist.FunctionActionType);
        Assert.Equal("Contract_Contract_PartySignX", hist.FunctionName);
    }

    [Fact]
    public async Task RealServer_Verification_FreePort()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_asrsm_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath)) dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath)) dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
        Assert.True(File.Exists(dllPath), $"Cannot find MiniContract.dll at {dllPath}");

        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(dllPath),
        };
        var freePort = FreePort.Get();
        psi.Environment["PORT"] = freePort.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort}"), Timeout = TimeSpan.FromSeconds(5) };
            bool started = false;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(500);
                try { var res = await http.GetAsync("/healthz"); if (res.IsSuccessStatusCode) { started = true; break; } }
                catch { }
            }
            Assert.True(started, "MiniContract server failed to start within 15 seconds.");

            // 1. Body rỗng → Contract_Contract_Approved (Check.strJson)
            var resEmpty = await http.PostAsync("/api/contract_contract/approvedsignremoteandsendmail",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var txtEmpty = await resEmpty.Content.ReadAsStringAsync();
            var docEmpty = JsonDocument.Parse(txtEmpty);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[EmptyBody] {txtEmpty}");

            // 2. Không tồn tại → Invalid_UserApproved
            var bodyMissing = JsonSerializer.Serialize(new
            {
                Model = new { FileList = new[] { new { FileName = "hd9999.pdf", FilePath = "/tmp/x.pdf" } }, ContractCode = "HD9999", FlagOrgCreate = "1" }
            });
            var resMissing = await http.PostAsync("/api/contract_contract/approvedsignremoteandsendmail",
                new StringContent(bodyMissing, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resMissing.StatusCode);
            var txtMissing = await resMissing.Content.ReadAsStringAsync();
            var docMissing = JsonDocument.Parse(txtMissing);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", docMissing.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NotFound] {txtMissing}");

            // 3. PartySign path + PENDING → Invalid_UserApproved (khác Approved path)
            var bodyPending = JsonSerializer.Serialize(new
            {
                Model = new { FileList = new[] { new { FileName = "hd0004.pdf", FilePath = "/tmp/y.pdf" } }, ContractCode = "HD0004", FlagOrgCreate = "0" }
            });
            var resPending = await http.PostAsync("/api/contract_contract/approvedsignremoteandsendmail",
                new StringContent(bodyPending, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resPending.StatusCode);
            var txtPending = await resPending.Content.ReadAsStringAsync();
            var docPending = JsonDocument.Parse(txtPending);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", docPending.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[PartySign-PENDING] {txtPending}");

            // 4. HD0003 ONPROCESS nhưng ContractNo null → ContractNoIsNotNull
            var bodyNoContractNo = JsonSerializer.Serialize(new
            {
                Model = new { FileList = new[] { new { FileName = "hd0003_signed.pdf", FilePath = "/tmp/z.pdf" } }, ContractCode = "HD0003", FlagOrgCreate = "1" }
            });
            // HD0003 trong seeder có ContractNo="HD0003/2026" → không vướng guard này. Dùng HD0001 (PENDING, ContractNo null).
            bodyNoContractNo = JsonSerializer.Serialize(new
            {
                Model = new { FileList = new[] { new { FileName = "hd0001.pdf", FilePath = "/tmp/z.pdf" } }, ContractCode = "HD0001", FlagOrgCreate = "1" }
            });
            var resNo = await http.PostAsync("/api/contract_contract/approvedsignremoteandsendmail",
                new StringContent(bodyNoContractNo, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resNo.StatusCode);
            var txtNo = await resNo.Content.ReadAsStringAsync();
            var docNo = JsonDocument.Parse(txtNo);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_ContractNoIsNotNull", docNo.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NoContractNo] {txtNo}");

            // 5. HD0003 (ONPROCESS, ContractNo có) + Approved path → OK, round-trip qua DoSearch
            var bodyOk = JsonSerializer.Serialize(new
            {
                Model = new { FileList = new[] { new { FileName = "hd0003_signed.pdf", FilePath = "/tmp/ok.pdf" } }, ContractCode = "HD0003", FlagOrgCreate = "1" }
            });
            var resOk = await http.PostAsync("/api/contract_contract/approvedsignremoteandsendmail",
                new StringContent(bodyOk, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.OK, resOk.StatusCode);
            var txtOk = await resOk.Content.ReadAsStringAsync();
            var docOk = JsonDocument.Parse(txtOk);
            Assert.True(docOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("CONFIRMED", docOk.RootElement.GetProperty("Data").GetProperty("ContractStatus").GetString());
            Assert.Equal("APPROVED", docOk.RootElement.GetProperty("Data").GetProperty("HistActionType").GetString());
            _output?.WriteLine($"[Success-Approved] {txtOk}");

            var resDs = await http.PostAsync("/api/contract_contract/dosearch",
                new StringContent("{\"ContractNo\":\"HD0003/2026\",\"RecordCount\":10}", Encoding.UTF8, "application/json"));
            var txtDs = await resDs.Content.ReadAsStringAsync();
            var docDs = JsonDocument.Parse(txtDs);
            var list = docDs.RootElement.GetProperty("Data").GetProperty("DataList");
            Assert.Equal(1, list.GetArrayLength());
            Assert.Equal("CONFIRMED", list[0].GetProperty("ContractStatus").GetString());
            Assert.Equal("hd0003_signed.pdf", list[0].GetProperty("ContractFilePath").GetString());
            _output?.WriteLine($"[RoundTrip-Approved] {txtDs}");
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(); } catch { }
            try { proc.WaitForExit(2000); } catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
