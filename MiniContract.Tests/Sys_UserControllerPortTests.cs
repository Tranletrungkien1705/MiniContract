using System.Diagnostics;
using System.IO;
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

namespace MiniContract.Tests;

public class Sys_UserControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Sys_UserControllerPort _controller;

    public Sys_UserControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Seed Mst_Org
        var orgSeeder = new Mst_OrgSeeder();
        orgSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Mst_NNT
        var nntSeeder = new Mst_NNTSeeder();
        nntSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Mst_Department
        var deptSeeder = new Mst_DepartmentSeeder();
        deptSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Sys_User
        var userSeeder = new Sys_UserSeeder();
        userSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Sys_UserControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_NullReq_ReturnsAllUsersWithMaskedPassword()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());

        // Kiểm tra UserCode sắp xếp asc: U01, U02, U03
        var u01 = list[0];
        Assert.Equal("U01", u01.GetProperty("UserCode").GetString());
        Assert.Equal("admin@solution.hn", u01.GetProperty("UserName").GetString());
        Assert.Equal("admin@solution.hn", u01.GetProperty("EMail").GetString());
        Assert.Equal("0901234567", u01.GetProperty("PhoneNo").GetString());
        Assert.Equal("0100000001", u01.GetProperty("MST").GetString());
        // Mật khẩu phải được che bằng Default_PasswordMask ("*********") theo System.cs:6816
        Assert.Equal("*********", u01.GetProperty("UserPassword").GetString());
        // Cột nối từ Mst_Department theo System.cs:6837-6838
        Assert.Equal("DEPT_BGD", u01.GetProperty("mdept_DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", u01.GetProperty("mdept_DepartmentName").GetString());
        // OrgID nối từ Mst_NNT theo System.cs:6839
        Assert.Equal("7200000001", u01.GetProperty("OrgID").GetString());

        var u02 = list[1];
        Assert.Equal("U02", u02.GetProperty("UserCode").GetString());
        Assert.Equal("*********", u02.GetProperty("UserPassword").GetString());
        Assert.Equal("DEPT_KT", u02.GetProperty("mdept_DepartmentCode").GetString());
        Assert.Equal("Phòng Kế Toán", u02.GetProperty("mdept_DepartmentName").GetString());

        var u03 = list[2];
        Assert.Equal("U03", u03.GetProperty("UserCode").GetString());
        Assert.Equal("*********", u03.GetProperty("UserPassword").GetString());
    }

    [Fact]
    public async Task Index_EmptyReq_ReturnsAllUsers()
    {
        var req = new Sys_User_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByUserNameKeyword_Returns1Record()
    {
        // Bộ lọc 1: username = "admin" -> 1 dòng (U01)
        var req = new Sys_User_IndexReq { UserName = "admin" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("U01", list[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByCommonDomainKeyword_Returns3Records()
    {
        // Bộ lọc 2: username = "solution.hn" -> 3 dòng (U01, U02, U03) khác số dòng với bộ lọc 1 (1 dòng)
        var req = new Sys_User_IndexReq { UserName = "solution.hn" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByPhoneNo_Returns1Record()
    {
        // Tìm kiếm theo số điện thoại: strWhereClause_SysUserName tìm trên UserName/PhoneNo/EMail
        var req = new Sys_User_IndexReq { UserName = "0902345678" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("U02", list[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByMST_ReturnsDifferentCounts()
    {
        // Lọc theo MST: 0100000001 gồm U01, U02 -> 2 bản ghi
        var req1 = new Sys_User_IndexReq { MST = "0100000001" };
        var result1 = await _controller.Index(req1);
        var okResult1 = Assert.IsType<OkObjectResult>(result1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(okResult1.Value));
        Assert.Equal(2, doc1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

        // Lọc theo MST: 0100000002 gồm U03 -> 1 bản ghi
        var req2 = new Sys_User_IndexReq { MST = "0100000002" };
        var result2 = await _controller.Index(req2);
        var okResult2 = Assert.IsType<OkObjectResult>(result2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(okResult2.Value));
        Assert.Equal(1, doc2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Index_Pagination_SkipAndTake()
    {
        // Trang 0, kích thước 1 -> lấy U01, tổng 3, số trang 3
        var reqPage0 = new Sys_User_IndexReq { Page = 0, PageSize = 1 };
        var result0 = await _controller.Index(reqPage0);
        var okResult0 = Assert.IsType<OkObjectResult>(result0);
        var doc0 = JsonDocument.Parse(JsonSerializer.Serialize(okResult0.Value));
        var data0 = doc0.RootElement.GetProperty("Data");
        Assert.Equal(3, data0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data0.GetProperty("PageCount").GetInt32());
        var list0 = data0.GetProperty("DataList");
        Assert.Equal(1, list0.GetArrayLength());
        Assert.Equal("U01", list0[0].GetProperty("UserCode").GetString());

        // Trang 1, kích thước 1 -> lấy U02
        var reqPage1 = new Sys_User_IndexReq { Page = 1, PageSize = 1 };
        var result1 = await _controller.Index(reqPage1);
        var okResult1 = Assert.IsType<OkObjectResult>(result1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(okResult1.Value));
        var data1 = doc1.RootElement.GetProperty("Data");
        var list1 = data1.GetProperty("DataList");
        Assert.Equal(1, list1.GetArrayLength());
        Assert.Equal("U02", list1[0].GetProperty("UserCode").GetString());

        // Kích thước 0 -> DataList rỗng, ItemCount = 3, PageCount = 0 (LESSONS_AGENTS A.7)
        var reqPageZero = new Sys_User_IndexReq { PageSize = 0 };
        var resultZero = await _controller.Index(reqPageZero);
        var okResultZero = Assert.IsType<OkObjectResult>(resultZero);
        var docZero = JsonDocument.Parse(JsonSerializer.Serialize(okResultZero.Value));
        var dataZero = docZero.RootElement.GetProperty("Data");
        Assert.Equal(3, dataZero.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_Guard_InvalidWAUserCode_ReturnsSys_User_BizInvalidUserAbility()
    {
        // Guard kiểm tra quyền xem của người dùng: UserCode không tồn tại
        // ném TError.ErridQContract.Sys_User_BizInvalidUserAbility (System.Mix.cs:74)
        var req = new Sys_User_IndexReq { WAUserCode = "INVALID_USER" };
        var result = await _controller.Index(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Index_ValidWAUserCode_ReturnsVisibleUsers()
    {
        // Người dùng hợp lệ U01 có MST 0100000001 -> xem được người dùng thuộc MST của mình (U01, U02)
        var req = new Sys_User_IndexReq { WAUserCode = "U01" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("U01", list[0].GetProperty("UserCode").GetString());
        Assert.Equal("U02", list[1].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task RealServer_VerifyAllEndpointsAsync()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_sys_user_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
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

            // 1. Ca body rỗng: POST {} -> 200 OK, ItemCount=3, mật khẩu đã che *********
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/sys_user/index", req1);
            Assert.True(res1.IsSuccessStatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(3, data1.GetProperty("ItemCount").GetInt32());
            var list1 = data1.GetProperty("DataList");
            Assert.Equal(3, list1.GetArrayLength());
            Assert.Equal("U01", list1[0].GetProperty("UserCode").GetString());
            Assert.Equal("*********", list1[0].GetProperty("UserPassword").GetString());
            Assert.Equal("DEPT_BGD", list1[0].GetProperty("mdept_DepartmentCode").GetString());
            Assert.Equal("Ban Giám Đốc", list1[0].GetProperty("mdept_DepartmentName").GetString());
            Assert.Equal("7200000001", list1[0].GetProperty("OrgID").GetString());

            // 2. Ca guard: WAUserCode không tồn tại -> 400 BadRequest (Sys_User_BizInvalidUserAbility)
            var req2 = new StringContent(JsonSerializer.Serialize(new { WAUserCode = "INVALID_USER" }), Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/sys_user/index", req2);
            Assert.False(res2.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res2.StatusCode);
            var doc2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync());
            Assert.False(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_BizInvalidUserAbility", doc2.RootElement.GetProperty("ErrorCode").GetString());

            // 3. Ca bộ lọc 1: UserName = admin -> 1 dòng
            var req3 = new StringContent(JsonSerializer.Serialize(new { UserName = "admin" }), Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/sys_user/index", req3);
            Assert.True(res3.IsSuccessStatusCode);
            var doc3 = JsonDocument.Parse(await res3.Content.ReadAsStringAsync());
            Assert.Equal(1, doc3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal("U01", doc3.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("UserCode").GetString());

            // 4. Ca bộ lọc 2: UserName = solution.hn -> 3 dòng (khác số dòng với bộ lọc 1)
            var req4 = new StringContent(JsonSerializer.Serialize(new { UserName = "solution.hn" }), Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/sys_user/index", req4);
            Assert.True(res4.IsSuccessStatusCode);
            var doc4 = JsonDocument.Parse(await res4.Content.ReadAsStringAsync());
            Assert.Equal(3, doc4.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 5. Ca bộ lọc MST: 0100000001 -> 2 dòng (U01, U02)
            var req5 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001" }), Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/sys_user/index", req5);
            Assert.True(res5.IsSuccessStatusCode);
            var doc5 = JsonDocument.Parse(await res5.Content.ReadAsStringAsync());
            Assert.Equal(2, doc5.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 6. Ca bộ lọc không tồn tại: UserName = nonexistent -> 0 dòng
            var req6 = new StringContent(JsonSerializer.Serialize(new { UserName = "nonexistent" }), Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/sys_user/index", req6);
            Assert.True(res6.IsSuccessStatusCode);
            var doc6 = JsonDocument.Parse(await res6.Content.ReadAsStringAsync());
            Assert.Equal(0, doc6.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, doc6.RootElement.GetProperty("Data").GetProperty("PageCount").GetInt32());
            Assert.Equal(0, doc6.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());

            // 7. Ca action alias search: POST /api/sys_user/search với body rỗng -> ItemCount=3
            var req7 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/sys_user/search", req7);
            Assert.True(res7.IsSuccessStatusCode);
            var doc7 = JsonDocument.Parse(await res7.Content.ReadAsStringAsync());
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(3, doc7.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
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

            try
            {
                if (File.Exists(tempDb))
                {
                    File.Delete(tempDb);
                }
            }
            catch { }
        }
    }
}
