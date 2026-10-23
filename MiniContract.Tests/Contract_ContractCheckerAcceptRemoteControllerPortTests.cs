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

public class Contract_ContractCheckerAcceptRemoteControllerPortTests : IDisposable
{
    private const string ErrPrefix = "Erridn.Skycic.QContract.";

    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Contract_ContractCheckerAcceptRemoteControllerPort _controller;

    public Contract_ContractCheckerAcceptRemoteControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        new Mst_ContractTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_UserSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_CheckerSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractElementSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Contract_ContractCheckerAcceptRemoteControllerPort(_db);
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

    private static Contract_Contract_ContractCheckerAcceptRemoteReq BuildReq(string contractCode, string userCode, string waUserCode = "PORT")
        => new()
        {
            ContractCode = contractCode,
            UserCode = userCode,
            WAUserCode = waUserCode,
            Model = new Contract_Contract_ContractCheckerAcceptRemoteModel
            {
                ContractCode = contractCode,
                ContractFileName = "HD0001.pdf",
                FileList = new()
                {
                    new Contract_Contract_ContractCheckerAcceptRemoteFile { FileName = "HD0001.pdf", FilePath = "tmp/HD0001.pdf" }
                }
            }
        };

    [Fact]
    public async Task EmptyBody_ReturnsCheckStrJson()
    {
        var res = await _controller.Contract_Checker_Accept_Remote(null);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Checker_Accept", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task EmptyContractCode_ReturnsCheckStrJson()
    {
        var req = new Contract_Contract_ContractCheckerAcceptRemoteReq
        {
            ContractCode = "",
            UserCode = "U01",
            Model = new Contract_Contract_ContractCheckerAcceptRemoteModel { ContractCode = "" }
        };
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Checker_Accept", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ContractNotFound_ReturnsOrganNotFound()
    {
        var req = BuildReq("HD9999", "U01");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD9999", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
    }

    [Fact]
    public async Task ContractStatusNotPending_ReturnsStatusNotMatched()
    {
        // HD0002 có ContractStatus = "SIGNED" trong seed
        var req = BuildReq("HD0002", "U01");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD0002", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
        Assert.Equal("PENDING", root.GetProperty("Params").GetProperty("Check.strContractListToCheck").GetString());
    }

    [Fact]
    public async Task SysUserNotFound_ReturnsUserCodeNotFound()
    {
        var req = BuildReq("HD0001", "U_NONEXISTENT");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Sys_User_CheckDB_UserCodeNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("U_NONEXISTENT", root.GetProperty("Params").GetProperty("Check.UserCodeNotFound").GetString());
    }

    [Fact]
    public async Task SysUserInactive_ReturnsFlagActiveNotMatched()
    {
        _db.Set<Sys_User>().Add(new Sys_User
        {
            UserCode = "U_INACTIVE",
            UserName = "Người dùng bị khoá",
            FlagActive = "0"
        });
        await _db.SaveChangesAsync();

        var req = BuildReq("HD0001", "U_INACTIVE");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Sys_User_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("U_INACTIVE", root.GetProperty("Params").GetProperty("Check.UserCodeError").GetString());
        Assert.Equal("0", root.GetProperty("Params").GetProperty("Check.FlagActiveCurrent").GetString());
    }

    [Fact]
    public async Task CheckerNotFound_ReturnsCheckerNotFound()
    {
        // U03 tồn tại trong Sys_User nhưng không có trong Contract_Checker cho HD0001
        var req = BuildReq("HD0001", "U03");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Checker_CheckDB_CheckerNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD0001", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
        Assert.Equal("U03", root.GetProperty("Params").GetProperty("Check.UserCode").GetString());
    }

    [Fact]
    public async Task CheckerInactive_ReturnsFlagActiveNotMatched()
    {
        // Thêm contract HD_PENDING_INACTIVE với status PENDING
        _db.Set<Contract_Contract>().Add(new Contract_Contract
        {
            ContractCode = "HD_PENDING_INACTIVE",
            ContractStatus = "PENDING"
        });
        _db.Set<Contract_Checker>().Add(new Contract_Checker
        {
            ContractCode = "HD_PENDING_INACTIVE",
            UserCode = "U01",
            FlagChecker = "1",
            FlagCheck = "0",
            FlagActive = "0" // Không hoạt động
        });
        await _db.SaveChangesAsync();

        var req = BuildReq("HD_PENDING_INACTIVE", "U01");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Checker_CheckDB_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD_PENDING_INACTIVE", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
        Assert.Equal("U01", root.GetProperty("Params").GetProperty("Check.UserCode").GetString());
        Assert.Equal("0", root.GetProperty("Params").GetProperty("DB.FlagActive").GetString());
    }

    [Fact]
    public async Task CheckerNotFlagChecker_ReturnsInvalidChecker()
    {
        // U02 có FlagChecker = "0" (người ký, không phải người kiểm tra)
        var req = BuildReq("HD0001", "U02");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<BadRequestObjectResult>(res);
        var root = Root(res);
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal(ErrPrefix + "Contract_Checker_Accept_InvalidChecker", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("HD0001", root.GetProperty("Params").GetProperty("Check.ContractCode").GetString());
        Assert.Equal("U02", root.GetProperty("Params").GetProperty("Check.UserCode").GetString());
        Assert.Equal("0", root.GetProperty("Params").GetProperty("Check.FlagChecker").GetString());
    }

    [Fact]
    public async Task Success_RoundTrip_UpdatesCheckerAndHistAction()
    {
        var req = BuildReq("HD0001", "U01", waUserCode: "U01");
        var res = await _controller.Contract_Checker_Accept_Remote(req);
        Assert.IsType<OkObjectResult>(res);
        var root = Root(res);
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/Contract_Contract/Detail?contractcode=HD0001", root.GetProperty("RedirectUrl").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("HD0001", data.GetProperty("ContractCode").GetString());
        Assert.Equal("U01", data.GetProperty("UserCode").GetString());
        Assert.Equal("1", data.GetProperty("FlagCheck").GetString());
        Assert.Equal(1, data.GetProperty("HistActionVersion").GetInt32());
        Assert.Equal("CHECK", data.GetProperty("HistActionType").GetString());

        // Kiểm tra Contract_Checker được cập nhật trong DB
        var checker = await _db.Set<Contract_Checker>().AsNoTracking()
            .FirstAsync(x => x.ContractCode == "HD0001" && x.UserCode == "U01");
        Assert.Equal("1", checker.FlagCheck);
        Assert.False(string.IsNullOrEmpty(checker.CheckDTimeUTC));
        Assert.Equal("U01", checker.LogLUBy);

        // Kiểm tra Contract_Contract_HistAction được thêm vào DB
        var hist = await _db.Set<Contract_Contract_HistAction>().AsNoTracking()
            .FirstAsync(x => x.ContractCode == "HD0001");
        Assert.Equal(1, hist.Version);
        Assert.Equal("U01", hist.UserCode);
        Assert.Equal("admin@solution.hn", hist.UserName);
        Assert.Equal("Kiểm tra hợp đồng", hist.Description);
        Assert.Equal("Contract_Checker_Accept", hist.FunctionName);
        Assert.Equal("CHECK", hist.FunctionActionType);
    }

    [Fact]
    public async Task RealServer_Verification_FreePort()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_cca_{Guid.NewGuid():N}.db");
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

            // 1. Ca body rỗng
            var resEmpty = await http.PostAsync("/api/contract_contract/contract_checker_accept_remote",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var txtEmpty = await resEmpty.Content.ReadAsStringAsync();
            var docEmpty = JsonDocument.Parse(txtEmpty);
            Assert.Equal(ErrPrefix + "Contract_Checker_Accept", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[EmptyBody] {txtEmpty}");

            // 2. Ca ContractCode không tồn tại
            var bodyMissing = JsonSerializer.Serialize(BuildReq("HD9999", "U01"));
            var resMissing = await http.PostAsync("/api/contract_contract/contract_checker_accept_remote",
                new StringContent(bodyMissing, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resMissing.StatusCode);
            var txtMissing = await resMissing.Content.ReadAsStringAsync();
            var docMissing = JsonDocument.Parse(txtMissing);
            Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_OrganNotFound", docMissing.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[NotFound] {txtMissing}");

            // 3. Ca ContractStatus không phải PENDING (HD0002 = SIGNED)
            var bodyStatus = JsonSerializer.Serialize(BuildReq("HD0002", "U01"));
            var resStatus = await http.PostAsync("/api/contract_contract/contract_checker_accept_remote",
                new StringContent(bodyStatus, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resStatus.StatusCode);
            var txtStatus = await resStatus.Content.ReadAsStringAsync();
            var docStatus = JsonDocument.Parse(txtStatus);
            Assert.Equal(ErrPrefix + "Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched", docStatus.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[StatusSIGNED] {txtStatus}");

            // 4. Ca Checker không phải FlagChecker = 1 (U02 = 0)
            var bodyInvalidChecker = JsonSerializer.Serialize(BuildReq("HD0001", "U02"));
            var resInvalidChecker = await http.PostAsync("/api/contract_contract/contract_checker_accept_remote",
                new StringContent(bodyInvalidChecker, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resInvalidChecker.StatusCode);
            var txtInvalidChecker = await resInvalidChecker.Content.ReadAsStringAsync();
            var docInvalidChecker = JsonDocument.Parse(txtInvalidChecker);
            Assert.Equal(ErrPrefix + "Contract_Checker_Accept_InvalidChecker", docInvalidChecker.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[InvalidChecker] {txtInvalidChecker}");

            // 5. Ca thành công round-trip (HD0001, U01)
            var bodyOk = JsonSerializer.Serialize(BuildReq("HD0001", "U01", "U01"));
            var resOk = await http.PostAsync("/api/contract_contract/contract_checker_accept_remote",
                new StringContent(bodyOk, Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.OK, resOk.StatusCode);
            var txtOk = await resOk.Content.ReadAsStringAsync();
            var docOk = JsonDocument.Parse(txtOk);
            Assert.True(docOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("1", docOk.RootElement.GetProperty("Data").GetProperty("FlagCheck").GetString());
            _output?.WriteLine($"[Success] {txtOk}");
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(true); } catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
