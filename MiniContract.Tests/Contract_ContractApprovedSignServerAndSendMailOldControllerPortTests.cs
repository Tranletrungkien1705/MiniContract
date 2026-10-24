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

public class Contract_ContractApprovedSignServerAndSendMailOldControllerPortTests : IDisposable
{
    private const string ErrPrefix = "Erridn.Skycic.QContract.";

    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractApprovedSignServerAndSendMailOldControllerPort _controller;

    public Contract_ContractApprovedSignServerAndSendMailOldControllerPortTests(ITestOutputHelper? output = null)
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
        new Contract_Contract_HistActionSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_ContractApprovedSignServerAndSendMailOldControllerPort(_db);
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

    // Gate :4026, 4066 - fileName LUON tu FileList[0].FileName
    private static Contract_Contract_ApprovedSignServerAndSendMailOldReq BuildBody(
        string contractCode, string fileName, string flagOrgCreate = "1")
        => new()
        {
            ContractCode = contractCode,
            FileList = new()
            {
                new Contract_Contract_ApprovedSignServerAndSendMailOldFile
                {
                    FileName = fileName,
                    FilePath = "tmp/" + fileName
                }
            },
            FlagOrgCreate = flagOrgCreate,
            FlagElement = "0",
            FlagCheckOTP = "0"
        };

    private static object BuildRemoteBody(string contractCode, string fileName, string flagOrgCreate = "1")
        => new { Model = JsonSerializer.Serialize(BuildBody(contractCode, fileName, flagOrgCreate)) };

    [Fact]
    public async Task EmptyBody_ReturnsCheckStrJson()
    {
        var res = await _controller.ApprovedSignServerAndSendMail_Old(null);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task MissingContractCode_ReturnsCheckStrJson()
    {
        var req = new Contract_Contract_ApprovedSignServerAndSendMailOldReq { ContractCode = null };
        var res = await _controller.ApprovedSignServerAndSendMail_Old(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNotFound_ReturnsInvalid_UserApproved()
    {
        var res = await _controller.ApprovedSignServerAndSendMail_Old(BuildBody("HD9999", "hd9999.pdf"));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractStatusNotInList_ReturnsInvalid_UserApproved()
    {
        // HD0002 co ContractStatus = SIGNED -> ngoai danh sach {PENDING, ONPROCESS}
        var res = await _controller.ApprovedSignServerAndSendMail_Old(BuildBody("HD0002", "hd0002.pdf"));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task FileListEmpty_ReturnsInvalidContractFileName()
    {
        var res = await _controller.ApprovedSignServerAndSendMail_Old(BuildBody("HD0003", ""));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_InvalidContractFileName", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNoNull_ReturnsContractNoIsNotNull()
    {
        // HD0001 co ContractNo = null
        var res = await _controller.ApprovedSignServerAndSendMail_Old(BuildBody("HD0001", "hd0001.pdf"));
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_Approved_ContractNoIsNotNull", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Success_FlagOrgCreate_One_UsesApprovedXFunctionName_AndFileListFileName()
    {
        var req = BuildBody("HD0003", "hd0003_signed.pdf", "1");
        var res = await _controller.ApprovedSignServerAndSendMail_Old(req);
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

        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking()
            .Where(x => x.ContractCode == "HD0003").OrderByDescending(x => x.Version).FirstAsync();
        Assert.Equal("APPROVED", hist.FunctionActionType);
        Assert.Equal("Contract_Contract_ApprovedX", hist.FunctionName);
    }

    [Fact]
    public async Task Success_FlagOrgCreate_Zero_UsesPartySignXFunctionName_StillFileListFileName()
    {
        // DIEM KHAC #25: _Old LUON lay fileName tu FileList[0].FileName, ke ca nhanh PartySign
        var req = BuildBody("HD0003", "hd0003_party.pdf", "0");
        var res = await _controller.ApprovedSignServerAndSendMail_Old(req);
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("0", root.GetProperty("Data").GetProperty("FlagOrgCreate").GetString());

        var saved = await _db.Set<Contract_Contract>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0003");
        Assert.Equal("hd0003_party.pdf", saved.ContractFilePath);

        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking()
            .Where(x => x.ContractCode == "HD0003").OrderByDescending(x => x.Version).FirstAsync();
        Assert.Equal("PartySign", hist.FunctionActionType);
        Assert.Equal("Contract_Contract_PartySignX", hist.FunctionName);
    }

    [Fact]
    public async Task RealServer_Verification_FreePort()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_asssmold_{Guid.NewGuid():N}.db");
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

            var resEmpty = await http.PostAsync("/api/contract_contract/approvedsignserverandsendmail_old",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var txtEmpty = await resEmpty.Content.ReadAsStringAsync();
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved", JsonDocument.Parse(txtEmpty).RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[EmptyBody] {txtEmpty}");

            var resMissing = await http.PostAsync("/api/contract_contract/approvedsignserverandsendmail_old",
                new StringContent(JsonSerializer.Serialize(BuildRemoteBody("HD9999", "hd9999.pdf")), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resMissing.StatusCode);
            var txtMissing = await resMissing.Content.ReadAsStringAsync();
            Assert.Equal(ErrPrefix + "Contract_Contract_Approved_Invalid_UserApproved", JsonDocument.Parse(txtMissing).RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NotFound] {txtMissing}");

            var resOk = await http.PostAsync("/api/contract_contract/approvedsignserverandsendmail_old",
                new StringContent(JsonSerializer.Serialize(BuildRemoteBody("HD0003", "hd0003_signed.pdf", "1")), Encoding.UTF8, "application/json"));
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
