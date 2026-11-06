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

// #34 Contract_ContractController/FinishAndSign
// Gate: Contract_ContractController.cs:6767-7014
// BIZ:  zTemp.cs:4485-4638 (WAS_Contract_ContractParty_Finish) -> zTemp.cs:3205-4416 (Contract_ContractParty_FinishX)
public class Contract_ContractFinishAndSignControllerPortTests : IDisposable
{
    private const string ErrPrefix = "Erridn.Skycic.QContract.";

    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractControllerPort _controller;

    public Contract_ContractFinishAndSignControllerPortTests(ITestOutputHelper? output = null)
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

        // #34: KHÔNG thêm dòng vào seeder dùng chung (sẽ đổi số dòng của các test #11/#12/#13).
        // Dựng riêng dữ liệu ca thành công: HD0005 CONFIRMED + party MST 0100000001 CONFIRMED.
        _db.Set<Contract_Contract>().Add(new Contract_Contract
        {
            ContractCode = "HD0005",
            ContractName = "Hợp đồng nguyên tắc cung cấp dịch vụ",
            ContractDateUTC = "2026-05-01 08:00:00",
            CreateDTimeUTC = "2026-05-01 08:00:00",
            ContractStatus = "CONFIRMED",
            ContractFilePath = "HD0005.pdf",
            CurrencyCode = "VND",
            ContractRefNo = "HD0003",
            UserCodeCreate = "U01",
            UserNameCreate = "admin@solution.hn",
            PositionCreate = "Giám đốc",
            CreateBy = "U01",
            ContractNo = "HD0005/2026",
            OrgID = "7200000001"
        });
        _db.Set<Contract_ContractParty>().Add(new Contract_ContractParty
        {
            ContractCode = "HD0005",
            ContractType = "HD_DV",
            CustomerCode = "KH005",
            MST = "0100000001",
            OrgID = "7200000001",
            NetworkID = "1001",
            PartyCode = "P01",
            CustomerCodeSys = "CUST005",
            CustomerName = "Công ty TNHH Giải Pháp Số Hà Nội",
            CustomerEmail = "contact@solution.hn",
            UserToken = "TOKEN-HD0005",
            FlagOrgCreate = "1",
            ContractPartyStatus = "CONFIRMED"
        });
        _db.SaveChanges();

        _controller = new Contract_ContractControllerPort(_db);
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

    // Ca thành công: HD0005 CONFIRMED + party MST 0100000001 CONFIRMED (dữ liệu dựng trong constructor)
    private static Contract_Contract_FinishAndSignReq BuildOkReq(string contractCode = "HD0005", string mst = "0100000001")
        => new()
        {
            Contract_ContractPartyFinish = new Contract_Contract_FinishAndSignParty
            {
                ContractCode = contractCode,
                ContractFinishReasonCode = "REASON01",
                ContractFinishReasonName = "Hết hạn hợp đồng",
                ContractFileSpec = "PHNlY3NwZWM=",
                ContractFileName = "chamdut.pdf",
                FinishFileDesc = "File chấm dứt",
                FinishDescription = "Chấm dứt do hết hạn",
                FinishedDate = "2026-06-01 10:00:00",
                UserToken = "TOKEN-HD0005",
                UserNameSign = "Nguyễn Văn A"
            },
            MST = mst,
            WAUserCode = "U01"
        };

    [Fact]
    public async Task EmptyBody_ReturnsCheckDB_OrganNotFound()
    {
        // Gate :6789-6790 — model rỗng ⇒ ContractCode null ⇒ biz StdParam(null) = null ⇒ CheckDB không thấy
        var res = await _controller.FinishAndSign(null);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNotFound_ReturnsCheckDB_OrganNotFound()
    {
        var req = BuildOkReq("HD9999");
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_OrganNotFound", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractStatusNotConfirmed_ReturnsContractStatusNotMatched()
    {
        // HD0003 = ONPROCESS (ngoài danh sách Confirmed — biz:3309)
        var req = BuildOkReq("HD0003");
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched",
            Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task PartyNotFound_ReturnsPartyCheckDB_OrganNotFound()
    {
        // HD0005 CONFIRMED nhưng MST khác ⇒ Contract_ContractParty_CheckDB không thấy (biz:3338-3345)
        var req = BuildOkReq("HD0005", "9999999999");
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_ContractParty_CheckDB_OrganNotFound", Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractFinishReasonCodeEmpty_ReturnsNotNull()
    {
        var req = BuildOkReq();
        req.Contract_ContractPartyFinish!.ContractFinishReasonCode = "";
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_ContractParty_FinishX_ContractFinishReasonCodeNotNull",
            Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task FinishedDateEmpty_ReturnsNotNull()
    {
        var req = BuildOkReq();
        req.Contract_ContractPartyFinish!.FinishedDate = "";
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_ContractParty_FinishX_FinishedDateNotNull",
            Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractFileSpecEmpty_ReturnsNotNull()
    {
        var req = BuildOkReq();
        req.Contract_ContractPartyFinish!.ContractFileSpec = "";
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_ContractParty_FinishX_ContractFileSpecNotNull",
            Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task UserTokenMismatch_ReturnsInvalid_UserTokenInput()
    {
        // biz:3408-3423 — không có AccessToken ⇒ UserToken phải khớp party.UserToken
        var req = BuildOkReq();
        req.Contract_ContractPartyFinish!.UserToken = "SAI-TOKEN";
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal(ErrPrefix + "Contract_ContractParty_FinishX_Invalid_UserTokenInput",
            Root(res).GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Success_RoundTrip_PartyFinished_And_HistAction()
    {
        var req = BuildOkReq();
        var res = await _controller.FinishAndSign(req);
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal("HD0005", data.GetProperty("ContractCode").GetString());
        Assert.Equal("FINISHED", data.GetProperty("ContractPartyStatus").GetString());
        Assert.Equal("REASON01", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("PartyFinsh", data.GetProperty("HistActionType").GetString());
        Assert.Equal("Kết thúc hợp đồng", data.GetProperty("HistActionDescription").GetString());
        Assert.Equal(1, data.GetProperty("HistActionVersion").GetInt32());
        Assert.Equal("Kết thúc hợp đồng 'HD0005' thành công!", data.GetProperty("Message").GetString());
        Assert.Equal("/Contract_Contract/Detail?contractcode=HD0005", data.GetProperty("RedirectUrl").GetString());

        // Round-trip: đọc lại DB
        var party = await _db.Set<Contract_ContractParty>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0005");
        Assert.Equal("FINISHED", party.ContractPartyStatus);
        Assert.Equal("REASON01", party.ContractFinishReasonCode);
        Assert.Equal("Chấm dứt do hết hạn", party.FinishDescription);

        var contract = await _db.Set<Contract_Contract>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0005");
        Assert.Equal("REASON01", contract.ContractFinishReasonCode);
        Assert.Equal("Hết hạn hợp đồng", contract.ContractFinishReasonName);

        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking().FirstAsync(x => x.ContractCode == "HD0005");
        Assert.Equal("PartyFinsh", hist.FunctionActionType);
        Assert.Equal("Kết thúc hợp đồng", hist.Description);
    }

    [Fact]
    public async Task RealServer_Verification_FreePort()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_fin_{Guid.NewGuid():N}.db");
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

            // Ca 1: body rỗng -> CheckDB OrganNotFound
            var resEmpty = await http.PostAsync("/api/contract_contract/finishandsign",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var txtEmpty = await resEmpty.Content.ReadAsStringAsync();
            Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_OrganNotFound",
                JsonDocument.Parse(txtEmpty).RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[EmptyBody] {txtEmpty}");

            // Ca 2: guard — hợp đồng không tồn tại (HD9999)
            var resNotFound = await http.PostAsync("/api/contract_contract/finishandsign",
                new StringContent(JsonSerializer.Serialize(BuildOkReq("HD9999")), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resNotFound.StatusCode);
            var txtNotFound = await resNotFound.Content.ReadAsStringAsync();
            Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_OrganNotFound",
                JsonDocument.Parse(txtNotFound).RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NotFound] {txtNotFound}");

            // Ca 3: guard — ContractStatus ngoài Confirmed (HD0003 = ONPROCESS, HD0002 = SIGNED)
            foreach (var code in new[] { "HD0003", "HD0002" })
            {
                var resStatus = await http.PostAsync("/api/contract_contract/finishandsign",
                    new StringContent(JsonSerializer.Serialize(BuildOkReq(code)), Encoding.UTF8, "application/json"));
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resStatus.StatusCode);
                var txtStatus = await resStatus.Content.ReadAsStringAsync();
                Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched",
                    JsonDocument.Parse(txtStatus).RootElement.GetProperty("ErrorCode").GetString());
                _output?.WriteLine($"[StatusNotConfirmed {code}] {txtStatus}");
            }

            // Ca 4: ca thành công round-trip chạy ở test in-memory Success_RoundTrip_PartyFinished_And_HistAction
            // (server thật chỉ có seed dùng chung, không có hợp đồng CONFIRMED).
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(); } catch { }
            try { proc.WaitForExit(2000); } catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
