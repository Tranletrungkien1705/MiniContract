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

public class ESignControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly ESignControllerPort _controller;

    public ESignControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Contract_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractPartySeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_Contract_HistActionSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new ESignControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private static JsonElement Root(IActionResult result)
    {
        if (result is OkObjectResult ok)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;
        }
        if (result is BadRequestObjectResult bad)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(bad.Value)).RootElement;
        }
        throw new InvalidOperationException($"Unexpected result type: {result?.GetType()}");
    }

    [Fact]
    public async Task Cancel_EmptyBody_ReturnsFailCheckJson()
    {
        // Body null
        var resultNull = await _controller.Contract_ContractParty_Cancel(null);
        var badNull = Assert.IsType<BadRequestObjectResult>(resultNull);
        var rootNull = Root(badNull);
        Assert.False(rootNull.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Contract_ContractParty_Cancel", rootNull.GetProperty("ErrorCode").GetString());

        // Body rỗng
        var resultEmpty = await _controller.Contract_ContractParty_Cancel(new ESign_Contract_ContractParty_CancelReq());
        var badEmpty = Assert.IsType<BadRequestObjectResult>(resultEmpty);
        var rootEmpty = Root(badEmpty);
        Assert.False(rootEmpty.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Contract_ContractParty_Cancel", rootEmpty.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Cancel_ContractNotFound_ReturnsFailOrganNotFound()
    {
        // Guard 1: Contract_Contract_CheckDB -> không tìm thấy hợp đồng
        var req = new ESign_Contract_ContractParty_CancelReq
        {
            ContractCode = "HD9999",
            MST = "0100109108"
        };
        var result = await _controller.Contract_ContractParty_Cancel(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var root = Root(bad);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Contract_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD9999", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
    }

    [Fact]
    public async Task Cancel_ContractStatusNotMatched_ReturnsFailStatusNotMatched()
    {
        // Guard 2: Contract_Contract_CheckDB -> trạng thái không phải ONPROCESS (HD0002 có trạng thái SIGNED)
        var req = new ESign_Contract_ContractParty_CancelReq
        {
            ContractCode = "HD0002",
            MST = "0100109107"
        };
        var result = await _controller.Contract_ContractParty_Cancel(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var root = Root(bad);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD0002", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
        Assert.Equal("ONPROCESS", root.GetProperty("Params").GetProperty("Check.strContractListToCheck").GetString());
        Assert.Equal("SIGNED", root.GetProperty("Params").GetProperty("DB.ContractStatus").GetString());
    }

    [Fact]
    public async Task Cancel_PartyNotFound_ReturnsFailPartyOrganNotFound()
    {
        // Guard 3: Contract_ContractParty_CheckDB -> không tìm thấy đối tác với MST tương ứng
        var req = new ESign_Contract_ContractParty_CancelReq
        {
            ContractCode = "HD0003",
            MST = "0000000000"
        };
        var result = await _controller.Contract_ContractParty_Cancel(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var root = Root(bad);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Contract_ContractParty_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD0003", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
        Assert.Equal("0000000000", root.GetProperty("Params").GetProperty("Check.MST").GetString());
    }

    [Fact]
    public async Task Cancel_Success_RoundTrip()
    {
        // Round-trip: Hủy hợp đồng HD0003 thành công
        var req = new ESign_Contract_ContractParty_CancelReq
        {
            ContractCode = "HD0003",
            MST = "0100109108",
            WAUserCode = "signer@solution.hn"
        };

        var result = await _controller.Contract_ContractParty_Cancel(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var root = Root(ok);
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Hủy hợp đồng thành công", root.GetProperty("Messages")[0].GetString());
        Assert.Equal("", root.GetProperty("RedirectUrl").GetString());

        // Đọc lại Contract_Contract kiểm tra dữ liệu thật
        var updatedContract = await _db.Set<Contract_Contract>().FirstOrDefaultAsync(x => x.ContractCode == "HD0003");
        Assert.NotNull(updatedContract);
        Assert.Equal("CANCELED", updatedContract.ContractStatus);
        Assert.NotNull(updatedContract.CancelDTimeUTC);
        Assert.Equal("SIGNER@SOLUTION.HN", updatedContract.LogLUBy);

        // Đọc lại Contract_ContractParty kiểm tra dữ liệu thật
        var updatedParty = await _db.Set<Contract_ContractParty>().FirstOrDefaultAsync(x => x.ContractCode == "HD0003" && x.MST == "0100109108");
        Assert.NotNull(updatedParty);
        Assert.Equal("CANCELED", updatedParty.ContractPartyStatus);
        Assert.NotNull(updatedParty.CancelDTimeUTC);
        Assert.Equal("SIGNER@SOLUTION.HN", updatedParty.CancelBy);
        Assert.Equal("SIGNER@SOLUTION.HN", updatedParty.LogLUBy);

        // Đọc lại Contract_Contract_HistAction kiểm tra bản ghi lịch sử
        var hist = await _db.Set<Contract_Contract_HistAction>().FirstOrDefaultAsync(x => x.ContractCode == "HD0003" && x.Version == 1);
        Assert.NotNull(hist);
        Assert.Equal("SIGNER@SOLUTION.HN", hist.UserCode);
        Assert.Equal("Công ty CP DEF", hist.UserName);
        Assert.Equal("Hủy hợp đồng", hist.Description);
        Assert.Equal("Contract_ContractParty_CancelX", hist.FunctionName);
        Assert.Equal("PartyCancel", hist.FunctionActionType);
        Assert.NotNull(hist.LogLUDTimeUTC);
        Assert.Equal("SIGNER@SOLUTION.HN", hist.LogLUBy);

        // Hủy lại lần 2 -> báo lỗi trạng thái đã CANCELED không còn là ONPROCESS
        var resultSecond = await _controller.Contract_ContractParty_Cancel(req);
        var badSecond = Assert.IsType<BadRequestObjectResult>(resultSecond);
        var rootSecond = Root(badSecond);
        Assert.False(rootSecond.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched", rootSecond.GetProperty("ErrorCode").GetString());
        Assert.Equal("CANCELED", rootSecond.GetProperty("Params").GetProperty("DB.ContractStatus").GetString());
    }

    [Fact]
    public async Task Cancel_ModelAndUserString_Success()
    {
        // Chuẩn bị dữ liệu mẫu mới HD_TEST_01 để thử nghiệm gọi kiểu MVC gate
        _db.Set<Contract_Contract>().Add(new Contract_Contract
        {
            ContractCode = "HD_TEST_01",
            ContractStatus = "ONPROCESS",
            ContractDateUTC = "2026-04-01 00:00:00"
        });
        _db.Set<Contract_ContractParty>().Add(new Contract_ContractParty
        {
            ContractCode = "HD_TEST_01",
            MST = "0109999999",
            OrgID = "7200000001",
            CustomerCodeSys = "CUST_TEST",
            CustomerName = "Công ty CP Model Test",
            ContractPartyStatus = "ONPROCESS"
        });
        await _db.SaveChangesAsync();

        var req = new ESign_Contract_ContractParty_CancelReq
        {
            Model = "{\"ContractCode\":\"HD_TEST_01\"}",
            User = "{\"MST\":\"0109999999\",\"OrgID\":\"7200000001\",\"NetworkID\":\"1001\"}",
            Email = "tester@model.com"
        };

        var result = await _controller.Contract_ContractParty_Cancel(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var root = Root(ok);
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Hủy hợp đồng thành công", root.GetProperty("Messages")[0].GetString());

        var contract = await _db.Set<Contract_Contract>().FirstOrDefaultAsync(x => x.ContractCode == "HD_TEST_01");
        Assert.NotNull(contract);
        Assert.Equal("CANCELED", contract.ContractStatus);
    }

    [Fact]
    public async Task RealServer_Verification_FreePort()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_esign_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        }
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
        }
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
                try
                {
                    var res = await http.GetAsync("/healthz");
                    if (res.IsSuccessStatusCode) { started = true; break; }
                }
                catch { }
            }
            Assert.True(started, "MiniContract server failed to start within 15 seconds.");

            // Ca 1: Body rỗng -> HTTP 400
            var resEmpty = await http.PostAsync("/api/esign/contract_contractparty_cancel",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(bodyEmpty);
            Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Contract_ContractParty_Cancel", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[RealServer EmptyBody] StatusCode={(int)resEmpty.StatusCode} Body={bodyEmpty}");

            // Ca 2: Guard ContractNotFound -> HTTP 400
            var resNotFound = await http.PostAsync("/api/esign/contract_contractparty_cancel",
                new StringContent("{\"ContractCode\":\"HD_NONE\",\"MST\":\"0100109108\"}", Encoding.UTF8, "application/json"));
            var bodyNotFound = await resNotFound.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resNotFound.StatusCode);
            var docNotFound = JsonDocument.Parse(bodyNotFound);
            Assert.Equal("Erridn.Skycic.QContract.Contract_Contract_CheckDB_OrganNotFound", docNotFound.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[RealServer ContractNotFound] StatusCode={(int)resNotFound.StatusCode} Body={bodyNotFound}");

            // Ca 3: Guard ContractStatusNotMatched -> HTTP 400 (HD0002 có ContractStatus = SIGNED)
            var resWrongStatus = await http.PostAsync("/api/esign/contract_contractparty_cancel",
                new StringContent("{\"ContractCode\":\"HD0002\",\"MST\":\"0100109107\"}", Encoding.UTF8, "application/json"));
            var bodyWrongStatus = await resWrongStatus.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resWrongStatus.StatusCode);
            var docWrongStatus = JsonDocument.Parse(bodyWrongStatus);
            Assert.Equal("Erridn.Skycic.QContract.Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched", docWrongStatus.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[RealServer StatusNotMatched] StatusCode={(int)resWrongStatus.StatusCode} Body={bodyWrongStatus}");

            // Ca 4: Guard PartyNotFound -> HTTP 400 (HD0003 có MST khác)
            var resPartyNotFound = await http.PostAsync("/api/esign/contract_contractparty_cancel",
                new StringContent("{\"ContractCode\":\"HD0003\",\"MST\":\"9999999999\"}", Encoding.UTF8, "application/json"));
            var bodyPartyNotFound = await resPartyNotFound.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resPartyNotFound.StatusCode);
            var docPartyNotFound = JsonDocument.Parse(bodyPartyNotFound);
            Assert.Equal("Erridn.Skycic.QContract.Contract_ContractParty_CheckDB_OrganNotFound", docPartyNotFound.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[RealServer PartyNotFound] StatusCode={(int)resPartyNotFound.StatusCode} Body={bodyPartyNotFound}");

            // Ca 5: Round-trip Thành công -> HTTP 200
            var resSuccess = await http.PostAsync("/api/esign/contract_contractparty_cancel",
                new StringContent("{\"ContractCode\":\"HD0003\",\"MST\":\"0100109108\",\"WAUserCode\":\"verifier@solution.hn\"}", Encoding.UTF8, "application/json"));
            var bodySuccess = await resSuccess.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resSuccess.StatusCode);
            var docSuccess = JsonDocument.Parse(bodySuccess);
            Assert.True(docSuccess.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Hủy hợp đồng thành công", docSuccess.RootElement.GetProperty("Messages")[0].GetString());
            _output?.WriteLine($"[RealServer Success] StatusCode={(int)resSuccess.StatusCode} Body={bodySuccess}");

            // Ca 6: Hủy lại HD0003 lần nữa -> HTTP 400 vì ContractStatus đã đổi sang CANCELED
            var resSecond = await http.PostAsync("/api/esign/contract_contractparty_cancel",
                new StringContent("{\"ContractCode\":\"HD0003\",\"MST\":\"0100109108\",\"WAUserCode\":\"verifier@solution.hn\"}", Encoding.UTF8, "application/json"));
            var bodySecond = await resSecond.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resSecond.StatusCode);
            var docSecond = JsonDocument.Parse(bodySecond);
            Assert.Equal("Erridn.Skycic.QContract.Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched", docSecond.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[RealServer SecondCancel] StatusCode={(int)resSecond.StatusCode} Body={bodySecond}");
        }
        finally
        {
            try { proc.Kill(true); } catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
