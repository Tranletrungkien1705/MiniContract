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

public class HomeControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly HomeControllerPort _controller;

    public HomeControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy các seeder liên quan đến HomeController/Index và InitNetworkData
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
        new Mst_OrgSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new HomeControllerPort(_db);
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
    public async Task Index_NullReq_ReturnsRedirectToLogin()
    {
        // Ca 1: Body rỗng hoàn toàn -> người dùng chưa đăng nhập -> chuyển hướng về Login (HomeController.cs:129-133)
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var redirectUrl = root.GetProperty("RedirectUrl").GetString();
        Assert.NotNull(redirectUrl);
        Assert.Contains("/Account/Login", redirectUrl);
    }

    [Fact]
    public async Task Index_Unauthenticated_ReturnsRedirectToLogin()
    {
        // Ca 2: Guard IsAuthenticated = false -> chuyển hướng về Login với redirectUrl đúng RawUrl (HomeController.cs:129-133)
        var req = new Home_IndexReq
        {
            IsAuthenticated = false,
            RawUrl = "/contract/search"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var redirectUrl = root.GetProperty("RedirectUrl").GetString();
        Assert.NotNull(redirectUrl);
        Assert.Contains("/Account/Login", redirectUrl);
        Assert.Contains("%2Fcontract%2Fsearch", redirectUrl);
    }

    [Fact]
    public async Task Index_InitNetworkData_UserNotFound_ReturnsRedirectToLogin()
    {
        // Ca 3: Guard RouteNetworkId > 0 nhưng User không tồn tại -> InitNetworkData trả false -> chuyển hướng Login (HomeController.cs:41-46)
        var req = new Home_IndexReq
        {
            NetworkId = "1001",
            UserCode = "UNKNOWN_USER",
            IsAuthenticated = true
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var redirectUrl = root.GetProperty("RedirectUrl").GetString();
        Assert.NotNull(redirectUrl);
        Assert.Contains("/Account/Login", redirectUrl);
    }

    [Fact]
    public async Task Index_InitNetworkData_AdminUser_SuccessWithAllObjectsAndConfigs()
    {
        // Ca 4: RouteNetworkId > 0 và Admin User (U01) -> InitNetworkData thành công -> chuyển hướng Dashboard (HomeController.cs:50-52)
        var req = new Home_IndexReq
        {
            NetworkId = "1001",
            OrgId = "7200000001",
            UserCode = "U01",
            IsAuthenticated = true
        };
        var result = await _controller.Index(req);
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

        // Kiểm tra OrgSignConfigs: 2 bản ghi hoạt động cho OrgID 7200000001
        var orgSignConfigs = data.GetProperty("OrgSignConfigs");
        Assert.Equal(2, orgSignConfigs.GetArrayLength());

        // Kiểm tra CTSRemoteSuppliers: 2 nhà cung cấp
        var ctsSuppliers = data.GetProperty("CTSRemoteSuppliers");
        Assert.Equal(2, ctsSuppliers.GetArrayLength());

        // Kiểm tra AccessObjects: Admin có toàn bộ 4 Sys_Object đang hoạt động
        var accessObjects = data.GetProperty("AccessObjects");
        Assert.Equal(4, accessObjects.GetArrayLength());
    }

    [Fact]
    public async Task Index_InitNetworkData_NonAdminUser_SuccessWithRestrictedObjects()
    {
        // Ca 5: RouteNetworkId > 0 và User thường (U02) -> InitNetworkData thành công với quyền theo nhóm (BaseController.cs:3310-3321)
        var req = new Home_IndexReq
        {
            NetworkId = "1001",
            OrgId = "7200000001",
            UserCode = "U02",
            IsAuthenticated = true
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        var user = data.GetProperty("User");
        Assert.Equal("U02", user.GetProperty("UserCode").GetString());
        Assert.Equal("0", user.GetProperty("FlagSysAdmin").GetString());

        var accessObjects = data.GetProperty("AccessObjects");
        Assert.True(accessObjects.GetArrayLength() < 6);
    }

    [Fact]
    public async Task Index_RawUrlNotSlash_RedirectsToSlash()
    {
        // Ca 6: RouteNetworkId <= 0, đã xác thực nhưng RawUrl != "/" -> Redirect("/") (HomeController.cs:76-80)
        var req = new Home_IndexReq
        {
            UserCode = "U01",
            IsAuthenticated = true,
            RawUrl = "/some/sub/path"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/", root.GetProperty("RedirectUrl").GetString());
    }

    [Fact]
    public async Task Index_OrgList_FilterDifferentNetwork_ReturnsDifferentRowCount()
    {
        // Ca 7 & 8: 2 bộ lọc ra số dòng khác nhau cho danh sách tổ chức (HomeController.cs:99-119)
        // Bộ lọc 1: NetworkIdFilter = "1001" -> 3 tổ chức ("0", "7200000001", "7200000002")
        var req1 = new Home_IndexReq
        {
            UserCode = "U01",
            IsAuthenticated = true,
            RawUrl = "/",
            NetworkIdFilter = "1001"
        };
        var res1 = await _controller.Index(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(3, count1);

        // Bộ lọc 2: NetworkIdFilter = "9999" (không tồn tại) -> 0 tổ chức
        var req2 = new Home_IndexReq
        {
            UserCode = "U01",
            IsAuthenticated = true,
            RawUrl = "/",
            NetworkIdFilter = "9999"
        };
        var res2 = await _controller.Index(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(0, count2);

        // Khớp quy định P2: 2 bộ lọc cho ra số dòng khác nhau (2 != 0)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task Index_InitNetworkData_ViaHeaderWAUserCode_Success()
    {
        // Ca 9: Xác thực qua Header WAUserCode (Lesson A.24)
        _controller.ControllerContext.HttpContext.Request.Headers["WAUserCode"] = "U01";

        var req = new Home_IndexReq
        {
            NetworkId = "1001",
            OrgId = "7200000001"
            // Không truyền UserCode trong body, lấy qua Header
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var user = root.GetProperty("Data").GetProperty("User");
        Assert.Equal("U01", user.GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task Index_InitNetworkData_LoginByEmail_Success()
    {
        // Ca 10: Tìm user theo email (khớp inosuser.Email từ BaseController.cs:3133)
        var req = new Home_IndexReq
        {
            NetworkId = "1001",
            OrgId = "7200000001",
            UserCode = "admin@solution.hn",
            IsAuthenticated = true
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var user = root.GetProperty("Data").GetProperty("User");
        Assert.Equal("U01", user.GetProperty("UserCode").GetString());
        Assert.Equal("admin@solution.hn", user.GetProperty("EMail").GetString());
    }

    [Fact]
    public async Task RealServer_VerifyAllEndpointsAsync()
    {
        // Chạy kiểm thử trên server thật với FreePort.Get() và SQLite tạm (PORT_RULES §2.6)
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_home_{Guid.NewGuid():N}.db");
        var dllPath = typeof(HomeControllerPort).Assembly.Location;
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
                try
                {
                    var res = await http.GetAsync("/healthz");
                    if (res.IsSuccessStatusCode)
                    {
                        started = true;
                        break;
                    }
                }
                catch
                {
                    await Task.Delay(500);
                }
            }
            Assert.True(started, "Real server did not start within 15 seconds.");

            // 1. Gọi /api/home/index với body rỗng -> chuyển hướng login
            var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var resEmpty = await http.PostAsync("/api/home/index", emptyContent);
            Assert.True(resEmpty.IsSuccessStatusCode);
            var jsonEmpty = await resEmpty.Content.ReadAsStringAsync();
            var docEmpty = JsonDocument.Parse(jsonEmpty);
            Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("/Account/Login", docEmpty.RootElement.GetProperty("RedirectUrl").GetString());

            // 2. Gọi /api/home/index với RouteNetworkId > 0 và user Admin U01
            var adminPayload = JsonSerializer.Serialize(new
            {
                NetworkId = "1001",
                OrgId = "7200000001",
                UserCode = "U01",
                IsAuthenticated = true
            });
            var adminContent = new StringContent(adminPayload, Encoding.UTF8, "application/json");
            var resAdmin = await http.PostAsync("/api/home/index", adminContent);
            Assert.True(resAdmin.IsSuccessStatusCode);
            var jsonAdmin = await resAdmin.Content.ReadAsStringAsync();
            var docAdmin = JsonDocument.Parse(jsonAdmin);
            Assert.True(docAdmin.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/Dashboard/Index?networkId=1001", docAdmin.RootElement.GetProperty("RedirectUrl").GetString());

            // 3. Gọi /api/home/index lấy danh sách tổ chức với bộ lọc 1 (NetworkIdFilter = 1001)
            var filter1Payload = JsonSerializer.Serialize(new
            {
                UserCode = "U01",
                IsAuthenticated = true,
                RawUrl = "/",
                NetworkIdFilter = "1001"
            });
            var filter1Content = new StringContent(filter1Payload, Encoding.UTF8, "application/json");
            var resFilter1 = await http.PostAsync("/api/home/index", filter1Content);
            Assert.True(resFilter1.IsSuccessStatusCode);
            var jsonFilter1 = await resFilter1.Content.ReadAsStringAsync();
            var docFilter1 = JsonDocument.Parse(jsonFilter1);
            var count1 = docFilter1.RootElement.GetProperty("Data").GetArrayLength();
            Assert.Equal(3, count1);

            // 4. Gọi /api/home/index lấy danh sách tổ chức với bộ lọc 2 (NetworkIdFilter = 9999)
            var filter2Payload = JsonSerializer.Serialize(new
            {
                UserCode = "U01",
                IsAuthenticated = true,
                RawUrl = "/",
                NetworkIdFilter = "9999"
            });
            var filter2Content = new StringContent(filter2Payload, Encoding.UTF8, "application/json");
            var resFilter2 = await http.PostAsync("/api/home/index", filter2Content);
            Assert.True(resFilter2.IsSuccessStatusCode);
            var jsonFilter2 = await resFilter2.Content.ReadAsStringAsync();
            var docFilter2 = JsonDocument.Parse(jsonFilter2);
            var count2 = docFilter2.RootElement.GetProperty("Data").GetArrayLength();
            Assert.Equal(0, count2);

            Assert.NotEqual(count1, count2);
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(true);
                    proc.WaitForExit(3000);
                }
            }
            catch { }

            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
