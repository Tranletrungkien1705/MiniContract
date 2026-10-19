using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class AccountControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly AccountControllerPort _controller;

    public AccountControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy các seeder liên quan đến AccountController/LoginOS
        new Mst_DepartmentSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_UserSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_ObjectSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_GroupSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_AccessSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Sys_UserInGroupSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_Sys_ConfigSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_OrgSignConfigSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_CTSRemoteSupplierSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new AccountControllerPort(_db);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task LoginOS_NullReq_ReturnsFailOrgidParentid()
    {
        var result = await _controller.LoginOS(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.True(p.TryGetProperty("Check.orgid_parentid", out _));
    }

    [Fact]
    public async Task LoginOS_EmptyReq_ReturnsFailOrgidParentid()
    {
        var req = new Account_LoginOSReq();
        var result = await _controller.LoginOS(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.True(p.TryGetProperty("Check.orgid_parentid", out _));
    }

    [Fact]
    public async Task LoginOS_MissingParentId_ReturnsFailParentid()
    {
        var req = new Account_LoginOSReq { OrgId = "7200000001", ParentId = "" };
        var result = await _controller.LoginOS(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.True(p.TryGetProperty("Check.parentid", out _));
    }

    [Fact]
    public async Task LoginOS_MissingOrgId_ReturnsFailOrgid()
    {
        var req = new Account_LoginOSReq { OrgId = "", ParentId = "1001" };
        var result = await _controller.LoginOS(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.True(p.TryGetProperty("Check.orgid", out _));
    }

    [Fact]
    public async Task LoginOS_MissingUserCode_ReturnsFailUserCode()
    {
        var req = new Account_LoginOSReq { OrgId = "7200000001", ParentId = "1001" };
        var result = await _controller.LoginOS(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.True(p.TryGetProperty("Check.UserCode", out _));
    }

    [Fact]
    public async Task LoginOS_NonExistentUser_ReturnsFailUserNotFound()
    {
        var req = new Account_LoginOSReq
        {
            OrgId = "7200000001",
            ParentId = "1001",
            UserCode = "UNKNOWN_USER"
        };
        var result = await _controller.LoginOS(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("UNKNOWN_USER", p.GetProperty("Check.UserNotFound").GetString());
    }

    [Fact]
    public async Task LoginOS_AdminUser_SuccessWithAllObjectsAndConfigs()
    {
        var req = new Account_LoginOSReq
        {
            OrgId = "7200000001",
            ParentId = "1001",
            UserCode = "U01"
        };
        var result = await _controller.LoginOS(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/Dashboard/Index?networkId=1001", root.GetProperty("RedirectUrl").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());

        // Kiểm tra User U01 (Admin)
        var user = data.GetProperty("User");
        Assert.Equal("U01", user.GetProperty("UserCode").GetString());
        Assert.Equal("1", user.GetProperty("FlagSysAdmin").GetString());
        Assert.Equal("DEPT_BGD", user.GetProperty("mdept_DepartmentCode").GetString());

        // Kiểm tra NNT
        var nnt = data.GetProperty("NNT");
        Assert.Equal("0100000001", nnt.GetProperty("MST").GetString());

        // Kiểm tra SysConfig
        var sysConfig = data.GetProperty("SysConfig");
        Assert.Equal("1001", sysConfig.GetProperty("NetworkID").GetString());

        // Kiểm tra OrgSignConfigs: có 2 bản ghi FlagActive="1" cho OrgID 7200000001 (bản ghi thứ 3 có FlagActive="0")
        var orgSignConfigs = data.GetProperty("OrgSignConfigs");
        Assert.Equal(2, orgSignConfigs.GetArrayLength());

        // Kiểm tra CTSRemoteSuppliers: có 2 nhà cung cấp
        var ctsSuppliers = data.GetProperty("CTSRemoteSuppliers");
        Assert.Equal(2, ctsSuppliers.GetArrayLength());

        // Kiểm tra AccessObjects: Admin có toàn bộ 4 Sys_Object đang hoạt động (FlagActive="1")
        var accessObjects = data.GetProperty("AccessObjects");
        Assert.Equal(4, accessObjects.GetArrayLength());
    }

    [Fact]
    public async Task LoginOS_NonAdminUser_SuccessWithRestrictedObjects()
    {
        var req = new Account_LoginOSReq
        {
            OrgId = "7200000001",
            ParentId = "1001",
            UserCode = "U02"
        };
        var result = await _controller.LoginOS(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        var user = data.GetProperty("User");
        Assert.Equal("U02", user.GetProperty("UserCode").GetString());
        Assert.Equal("0", user.GetProperty("FlagSysAdmin").GetString());

        // U02 không phải Admin, số lượng quyền truy cập khác so với Admin
        var accessObjects = data.GetProperty("AccessObjects");
        Assert.True(accessObjects.GetArrayLength() < 6);
    }

    [Fact]
    public async Task LoginOS_FilterDifferentOrg_ReturnsDifferentRowCount()
    {
        // Bộ lọc 1: OrgID = "7200000001" -> 2 bản ghi OrgSignConfigs hoạt động (FlagActive="1")
        var req1 = new Account_LoginOSReq
        {
            OrgId = "7200000001",
            ParentId = "1001",
            UserCode = "U01"
        };
        var res1 = await _controller.LoginOS(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data").GetProperty("OrgSignConfigs").GetArrayLength();
        Assert.Equal(2, count1);

        // Bộ lọc 2: OrgID = "7200000002" -> 0 bản ghi OrgSignConfigs
        var req2 = new Account_LoginOSReq
        {
            OrgId = "7200000002",
            ParentId = "1001",
            UserCode = "U03"
        };
        var res2 = await _controller.LoginOS(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data").GetProperty("OrgSignConfigs").GetArrayLength();
        Assert.Equal(0, count2);

        // 2 bộ lọc cho số dòng khác nhau (3 vs 0)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task LoginOS_LoginByEmail_Success()
    {
        // Khớp với inosuser.Email truyền vào strWhereClause_SysUser từ nguồn (AccountController.cs:330)
        var req = new Account_LoginOSReq
        {
            OrgId = "7200000001",
            ParentId = "1001",
            UserCode = "admin@solution.hn"
        };
        var result = await _controller.LoginOS(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var user = root.GetProperty("Data").GetProperty("User");
        Assert.Equal("U01", user.GetProperty("UserCode").GetString());
        Assert.Equal("admin@solution.hn", user.GetProperty("EMail").GetString());
    }

    [Fact]
    public async Task LoginOS_LoginByHeaderWAUserCode_Success()
    {
        _controller.ControllerContext.HttpContext.Request.Headers["WAUserCode"] = "U01";

        var req = new Account_LoginOSReq
        {
            OrgId = "7200000001",
            ParentId = "1001"
            // Không truyền UserCode trong body, lấy qua header
        };
        var result = await _controller.LoginOS(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var user = root.GetProperty("Data").GetProperty("User");
        Assert.Equal("U01", user.GetProperty("UserCode").GetString());
    }

    [Fact]
    public void LogOff_NullReq_ReturnsOkAndDefaultRedirectUrl()
    {
        var result = _controller.LogOff(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("https://account.inos.vn/Account/LogOff", root.GetProperty("RedirectUrl").GetString());
    }

    [Fact]
    public void LogOff_EmptyReq_ReturnsOkAndDefaultRedirectUrl()
    {
        var req = new Account_LogOffReq();
        var result = _controller.LogOff(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("https://account.inos.vn/Account/LogOff", root.GetProperty("RedirectUrl").GetString());
    }

    [Fact]
    public void LogOff_WithCustomReturnUrl_ReturnsOkAndCustomRedirectUrl()
    {
        var req = new Account_LogOffReq { ReturnUrl = "https://custom.inos.vn/auth/logout" };
        var result = _controller.LogOff(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("https://custom.inos.vn/auth/logout", root.GetProperty("RedirectUrl").GetString());
    }

    [Fact]
    public void LogOff_ClearsCookies_WhenCookiesPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = "accesstoken=dummy_token_123; ASP.NET_SessionId=sess_456";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = _controller.LogOff(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var setCookies = httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("accesstoken=", setCookies);
        Assert.Contains("ASP.NET_SessionId=", setCookies);
    }

    [Fact]
    public async Task RealServer_VerifyAllEndpointsAsync()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_account_{Guid.NewGuid():N}.db");
        var dllPath = typeof(AccountControllerPort).Assembly.Location;
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
                    if (res.IsSuccessStatusCode)
                    {
                        started = true;
                        break;
                    }
                }
                catch { }
            }
            Assert.True(started, "MiniContract server failed to start within 15 seconds.");

            // 1. Ca body rỗng: POST {} -> 400 BadRequest (Account_LoginOS, Check.orgid_parentid)
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/account/loginos", req1);
            Assert.False(res1.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res1.StatusCode);
            var doc1 = JsonDocument.Parse(await res1.Content.ReadAsStringAsync());
            Assert.False(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", doc1.RootElement.GetProperty("ErrorCode").GetString());
            Assert.True(doc1.RootElement.GetProperty("Params").TryGetProperty("Check.orgid_parentid", out _));

            // 2. Ca guard: Thiếu parentid -> 400 BadRequest (Check.parentid)
            var req2 = new StringContent(JsonSerializer.Serialize(new { OrgId = "7200000001", ParentId = "" }), Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/account/loginos", req2);
            Assert.False(res2.IsSuccessStatusCode);
            var doc2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync());
            Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", doc2.RootElement.GetProperty("ErrorCode").GetString());
            Assert.True(doc2.RootElement.GetProperty("Params").TryGetProperty("Check.parentid", out _));

            // 3. Ca guard: User không tồn tại -> 400 BadRequest (Check.UserNotFound)
            var req3 = new StringContent(JsonSerializer.Serialize(new { OrgId = "7200000001", ParentId = "1001", UserCode = "NON_EXISTENT_USER" }), Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/account/loginos", req3);
            Assert.False(res3.IsSuccessStatusCode);
            var doc3 = JsonDocument.Parse(await res3.Content.ReadAsStringAsync());
            Assert.Equal("Erridn.Skycic.QContract.Account_LoginOS", doc3.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("NON_EXISTENT_USER", doc3.RootElement.GetProperty("Params").GetProperty("Check.UserNotFound").GetString());

            // 4. Ca thành công 1: Admin U01 (OrgID 7200000001, ParentID 1001) -> 200 OK
            var req4 = new StringContent(JsonSerializer.Serialize(new { OrgId = "7200000001", ParentId = "1001", UserCode = "U01" }), Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/account/loginos", req4);
            Assert.True(res4.IsSuccessStatusCode);
            var doc4 = JsonDocument.Parse(await res4.Content.ReadAsStringAsync());
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/Dashboard/Index?networkId=1001", doc4.RootElement.GetProperty("RedirectUrl").GetString());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal("1001", data4.GetProperty("NetworkID").GetString());
            Assert.Equal("7200000001", data4.GetProperty("OrgID").GetString());
            Assert.Equal("U01", data4.GetProperty("User").GetProperty("UserCode").GetString());
            Assert.Equal("0100000001", data4.GetProperty("NNT").GetProperty("MST").GetString());
            Assert.Equal(2, data4.GetProperty("OrgSignConfigs").GetArrayLength());
            Assert.Equal(2, data4.GetProperty("CTSRemoteSuppliers").GetArrayLength());
            Assert.Equal(4, data4.GetProperty("AccessObjects").GetArrayLength());

            // 5. Ca thành công 2: Lọc OrgID khác (OrgID 7200000002) -> 200 OK, 0 bản ghi OrgSignConfigs (khác 2 bản ghi ca 4)
            var req5 = new StringContent(JsonSerializer.Serialize(new { OrgId = "7200000002", ParentId = "1001", UserCode = "U03" }), Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/account/loginos", req5);
            Assert.True(res5.IsSuccessStatusCode);
            var doc5 = JsonDocument.Parse(await res5.Content.ReadAsStringAsync());
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal(0, data5.GetProperty("OrgSignConfigs").GetArrayLength());

            // 6. Ca LogOff body rỗng -> 200 OK, RedirectUrl mặc định (https://account.inos.vn/Account/LogOff)
            var req6 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/account/logoff", req6);
            Assert.True(res6.IsSuccessStatusCode);
            var doc6 = JsonDocument.Parse(await res6.Content.ReadAsStringAsync());
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("https://account.inos.vn/Account/LogOff", doc6.RootElement.GetProperty("RedirectUrl").GetString());

            // 7. Ca LogOff với ReturnUrl -> 200 OK, RedirectUrl khớp ReturnUrl
            var req7 = new StringContent(JsonSerializer.Serialize(new { ReturnUrl = "https://myredirect.test/home" }), Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/account/logoff", req7);
            Assert.True(res7.IsSuccessStatusCode);
            var doc7 = JsonDocument.Parse(await res7.Content.ReadAsStringAsync());
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("https://myredirect.test/home", doc7.RootElement.GetProperty("RedirectUrl").GetString());
        }
        finally
        {
            try
            {
                if (!proc.HasExited) proc.Kill(true);
            }
            catch { }
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
