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

public class Contract_ContractApprovedAndSignServerOldControllerPortTests : IDisposable
{
    private const string ErrPrefix = "Erridn.Skycic.QContract.";

    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractApprovedAndSignServerOldControllerPort _controller;

    public Contract_ContractApprovedAndSignServerOldControllerPortTests(ITestOutputHelper? output = null)
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
        new Contract_ContractTBodySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractElementSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_ContractApprovedAndSignServerOldControllerPort(_db);
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

    // Gate :2493 — nhánh Approved (FlagOrgCreate=="1") lấy fileName từ FileList[0].FileName.
    private static Contract_Contract_ApprovedAndSignServerOldReq BuildApprovedReq(string contractCode, string fileName)
        => new()
        {
            ContractCode = contractCode,
            FileList = new()
            {
                new Contract_Contract_ApprovedAndSignServerOldFile { FileName = fileName, FilePath = "tmp/" + fileName }
            },
            FlagOrgCreate = "1",
            FlagElement = "0",
            FlagCheckOTP = "0"
        };

    // Gate :2576 — nhánh PartySign (FlagOrgCreate!="1") lấy fileName từ ContractFileName cấp cao nhất.
    private static Contract_Contract_ApprovedAndSignServerOldReq BuildPartySignReq(string contractCode, string fileName)
        => new()
        {
            ContractCode = contractCode,
            ContractFileName = fileName,
            FlagOrgCreate = "0",
            FlagElement = "0",
            FlagCheckOTP = "0"
        };

    [Fact]
    public async Task EmptyBody_ReturnsCheckStrJson()
    {
        var res = await _controller.ApprovedAndSignServer_Old(null);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task MissingContractCode_ReturnsCheckStrJson()
    {
        var req = new Contract_Contract_ApprovedAndSignServerOldReq { ContractCode = null };
        var res = await _controller.ApprovedAndSignServer_Old(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNotFound_ReturnsInvalid_UserApproved()
    {
        var req = BuildApprovedReq("HD9999", "hd9999.pdf");
        var res = await _controller.ApprovedAndSignServer_Old(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractStatusNotInList_ReturnsInvalid_UserApproved()
    {
        var req = BuildApprovedReq("HD0002", "hd0002.pdf");
        var res = await _controller.ApprovedAndSignServer_Old(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractFileNameEmpty_ReturnsInvalidContractFileName()
    {
        var req = BuildApprovedReq("HD0003", "");
        var res = await _controller.ApprovedAndSignServer_Old(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_InvalidContractFileName", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNoNull_ReturnsContractNoIsNotNull()
    {
        var req = BuildApprovedReq("HD0001", "hd0001.pdf");
        var res = await _controller.ApprovedAndSignServer_Old(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_ContractNoIsNotNull", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Success_FlagOrgCreate_One_UsesApprovedXFunctionName()
    {
        var req = BuildApprovedReq("HD0003", "hd0003_signed.pdf");
        var res = await _controller.ApprovedAndSignServer_Old(req);
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal("HD0003", data.GetProperty("ContractCode").GetString());
        Assert.Equal("CONFIRMED", data.GetProperty("ContractStatus").GetString());
        Assert.Equal("APPROVED", data.GetProperty("HistActionType").GetString());
        Assert.Equal(1, data.GetProperty("HistActionVersion").GetInt32());

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
    public async Task Success_FlagOrgCreate_Zero_UsesPartySignXFunctionName_And_TopLevelFileName()
    {
        var req = BuildPartySignReq("HD0003", "hd0003_party.pdf");
        var res = await _controller.ApprovedAndSignServer_Old(req);
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("0", root.GetProperty("Data").GetProperty("FlagOrgCreate").GetString());

        var saved = await _db.Set<Contract_Contract>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("hd0003_party.pdf", saved.ContractFilePath);

        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("Contract_Contract_PartySignX", hist.FunctionName);
    }

    [Fact]
    public async Task RealServer_Verification_FreePort()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_assold_{Guid.NewGuid():N}.db");
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

            var resEmpty = await http.PostAsync("/api/contract_contract/approvedandsignserver_old",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var txtEmpty = await resEmpty.Content.ReadAsStringAsync();
            var docEmpty = JsonDocument.Parse(txtEmpty);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[EmptyBody] {txtEmpty}");

            var bodyMissing = JsonSerializer.Serialize(BuildApprovedReq("HD9999", "hd9999.pdf"));
            var resMissing = await http.PostAsync("/api/contract_contract/approvedandsignserver_old",
                new StringContent(bodyMissing, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resMissing.StatusCode);
            var txtMissing = await resMissing.Content.ReadAsStringAsync();
            var docMissing = JsonDocument.Parse(txtMissing);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", docMissing.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NotFound] {txtMissing}");

            var bodyStatus = JsonSerializer.Serialize(BuildApprovedReq("HD0002", "hd0002.pdf"));
            var resStatus = await http.PostAsync("/api/contract_contract/approvedandsignserver_old",
                new StringContent(bodyStatus, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resStatus.StatusCode);
            var txtStatus = await resStatus.Content.ReadAsStringAsync();
            var docStatus = JsonDocument.Parse(txtStatus);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", docStatus.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[StatusSIGNED] {txtStatus}");

            var bodyFileName = JsonSerializer.Serialize(BuildApprovedReq("HD0003", ""));
            var resFileName = await http.PostAsync("/api/contract_contract/approvedandsignserver_old",
                new StringContent(bodyFileName, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resFileName.StatusCode);
            var txtFileName = await resFileName.Content.ReadAsStringAsync();
            var docFileName = JsonDocument.Parse(txtFileName);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_InvalidContractFileName", docFileName.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NoFileName] {txtFileName}");

            var bodyNo = JsonSerializer.Serialize(BuildApprovedReq("HD0001", "hd0001.pdf"));
            var resNo = await http.PostAsync("/api/contract_contract/approvedandsignserver_old",
                new StringContent(bodyNo, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resNo.StatusCode);
            var txtNo = await resNo.Content.ReadAsStringAsync();
            var docNo = JsonDocument.Parse(txtNo);
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_ContractNoIsNotNull", docNo.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NoContractNo] {txtNo}");

            var bodyOk = JsonSerializer.Serialize(BuildApprovedReq("HD0003", "hd0003_signed.pdf"));
            var resOk = await http.PostAsync("/api/contract_contract/approvedandsignserver_old",
                new StringContent(bodyOk, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.OK, resOk.StatusCode);
            var txtOk = await resOk.Content.ReadAsStringAsync();
            var docOk = JsonDocument.Parse(txtOk);
            Assert.True(docOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("CONFIRMED", docOk.RootElement.GetProperty("Data").GetProperty("ContractStatus").GetString());
            _output?.WriteLine($"[Success] {txtOk}");

            var resDs = await http.PostAsync("/api/contract_contract/dosearch",
                new StringContent("{\"ContractNo\":\"HD0003/2026\",\"RecordCount\":10}", Encoding.UTF8, "application/json"));
            var txtDs = await resDs.Content.ReadAsStringAsync();
            var docDs = JsonDocument.Parse(txtDs);
            var list = docDs.RootElement.GetProperty("Data").GetProperty("DataList");
            Assert.Equal(1, list.GetArrayLength());
            Assert.Equal("CONFIRMED", list[0].GetProperty("ContractStatus").GetString());
            _output?.WriteLine($"[RoundTrip] {txtDs}");
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(); } catch { }
            try { proc.WaitForExit(2000); } catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
