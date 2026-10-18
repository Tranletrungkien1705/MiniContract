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

public class Sys_AccessControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Sys_AccessControllerPort _controller;

    public Sys_AccessControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Sys_Object
        var objSeeder = new Sys_ObjectSeeder();
        objSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Sys_Access
        var accessSeeder = new Sys_AccessSeeder();
        accessSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Sys_AccessControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_NullReq_SuccessWithDefaultMenuAndAccess()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        // Sys_Object gồm 3 menu đang hoạt động (MNU_CONTRACT, MNU_CUSTOMER, MNU_SYSTEM)
        var listObj = data.GetProperty("ListSysObject");
        Assert.Equal(3, listObj.GetArrayLength());
        Assert.Equal("MNU_CONTRACT", listObj[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("MNU_CUSTOMER", listObj[1].GetProperty("ObjectCode").GetString());
        Assert.Equal("MNU_SYSTEM", listObj[2].GetProperty("ObjectCode").GetString());

        // ObjectCode mặc định là menu đầu tiên: MNU_CONTRACT
        Assert.Equal("MNU_CONTRACT", data.GetProperty("ObjectCode").GetString());

        // Sys_Access gồm 2 nhóm được cấp quyền cho MNU_CONTRACT (ADMIN, SALE_MGR)
        var listAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(2, listAccess.GetArrayLength());
        Assert.Equal("ADMIN", listAccess[0].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_CONTRACT", listAccess[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("Quản lý hợp đồng", listAccess[0].GetProperty("so_ObjectName").GetString());
        Assert.Equal("SALE_MGR", listAccess[1].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_CONTRACT", listAccess[1].GetProperty("ObjectCode").GetString());
    }

    [Fact]
    public async Task Index_EmptyReq_SuccessWithDefaultMenuAndAccess()
    {
        var req = new Sys_Access_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        var listObj = data.GetProperty("ListSysObject");
        Assert.Equal(3, listObj.GetArrayLength());
        Assert.Equal("MNU_CONTRACT", data.GetProperty("ObjectCode").GetString());

        var listAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(2, listAccess.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByFlagActiveZero_ReturnsSingleArchivedMenuAndEmptyAccess()
    {
        var req = new Sys_Access_IndexReq { FlagActive = "0" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        // Chỉ có 1 menu ẩn: MNU_ARCHIVED
        var listObj = data.GetProperty("ListSysObject");
        Assert.Equal(1, listObj.GetArrayLength());
        Assert.Equal("MNU_ARCHIVED", listObj[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("MNU_ARCHIVED", data.GetProperty("ObjectCode").GetString());

        // Không có phân quyền nào cho MNU_ARCHIVED
        var listAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(0, listAccess.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByObjectTypeButton_ReturnsSingleButtonAndEmptyAccess()
    {
        var req = new Sys_Access_IndexReq { ObjectType = "BUTTON" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        // Chỉ có 1 object loại BUTTON: BTN_SAVE
        var listObj = data.GetProperty("ListSysObject");
        Assert.Equal(1, listObj.GetArrayLength());
        Assert.Equal("BTN_SAVE", listObj[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("BTN_SAVE", data.GetProperty("ObjectCode").GetString());

        var listAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(0, listAccess.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterBySpecifiedObjectCode_ReturnsAccessForSpecifiedObject()
    {
        var req = new Sys_Access_IndexReq { ObjectCode = "MNU_CUSTOMER" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        // Vẫn lấy đủ 3 menu đang hoạt động
        var listObj = data.GetProperty("ListSysObject");
        Assert.Equal(3, listObj.GetArrayLength());

        // ObjectCode được chọn là MNU_CUSTOMER
        Assert.Equal("MNU_CUSTOMER", data.GetProperty("ObjectCode").GetString());

        // Sys_Access chỉ có 1 nhóm (ADMIN) được phân quyền cho MNU_CUSTOMER
        var listAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(1, listAccess.GetArrayLength());
        Assert.Equal("ADMIN", listAccess[0].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_CUSTOMER", listAccess[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("Quản lý khách hàng", listAccess[0].GetProperty("so_ObjectName").GetString());
    }

    [Fact]
    public async Task Index_FilterByGroupCode_ReturnsFilteredAccess()
    {
        var req = new Sys_Access_IndexReq { GroupCode = "SALE_MGR" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        // ObjectCode mặc định là MNU_CONTRACT
        Assert.Equal("MNU_CONTRACT", data.GetProperty("ObjectCode").GetString());

        // Chỉ có nhóm SALE_MGR
        var listAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(1, listAccess.GetArrayLength());
        Assert.Equal("SALE_MGR", listAccess[0].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_CONTRACT", listAccess[0].GetProperty("ObjectCode").GetString());
    }

    [Fact]
    public async Task Index_NoMatchingSysObject_ReturnsEmptyLists()
    {
        var req = new Sys_Access_IndexReq { ObjectType = "NON_EXISTENT" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        var listObj = data.GetProperty("ListSysObject");
        Assert.Equal(0, listObj.GetArrayLength());
        Assert.Equal("", data.GetProperty("ObjectCode").GetString());

        var listAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(0, listAccess.GetArrayLength());
    }

    [Fact]
    public async Task RealServer_Verification()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_sys_access_{Guid.NewGuid():N}.db");
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

            // 1. Ca body rỗng: POST {} -> 200 OK, ListSysObject=3, ObjectCode="MNU_CONTRACT", ListSysAccess=2
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/sys_access/index", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(3, data1.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal("MNU_CONTRACT", data1.GetProperty("ObjectCode").GetString());
            Assert.Equal(2, data1.GetProperty("ListSysAccess").GetArrayLength());
            Assert.Equal("ADMIN", data1.GetProperty("ListSysAccess")[0].GetProperty("GroupCode").GetString());
            Assert.Equal("Quản lý hợp đồng", data1.GetProperty("ListSysAccess")[0].GetProperty("so_ObjectName").GetString());

            // 2. Ca lọc FlagActive="0" -> 200 OK, ListSysObject=1 (MNU_ARCHIVED), ListSysAccess=0 (khác số dòng ca 1)
            var req2 = new StringContent("{\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/sys_access/index", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(1, data2.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal("MNU_ARCHIVED", data2.GetProperty("ObjectCode").GetString());
            Assert.Equal(0, data2.GetProperty("ListSysAccess").GetArrayLength());

            // 3. Ca lọc ObjectType="BUTTON" -> 200 OK, ListSysObject=1 (BTN_SAVE), ListSysAccess=0
            var req3 = new StringContent("{\"ObjectType\":\"BUTTON\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/sys_access/index", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(1, data3.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal("BTN_SAVE", data3.GetProperty("ObjectCode").GetString());
            Assert.Equal(0, data3.GetProperty("ListSysAccess").GetArrayLength());

            // 4. Ca chỉ định ObjectCode="MNU_CUSTOMER" -> 200 OK, ListSysObject=3, ObjectCode="MNU_CUSTOMER", ListSysAccess=1
            var req4 = new StringContent("{\"ObjectCode\":\"MNU_CUSTOMER\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/sys_access/index", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(3, data4.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal("MNU_CUSTOMER", data4.GetProperty("ObjectCode").GetString());
            Assert.Equal(1, data4.GetProperty("ListSysAccess").GetArrayLength());
            Assert.Equal("ADMIN", data4.GetProperty("ListSysAccess")[0].GetProperty("GroupCode").GetString());
            Assert.Equal("Quản lý khách hàng", data4.GetProperty("ListSysAccess")[0].GetProperty("so_ObjectName").GetString());
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
                if (File.Exists(tempDb)) File.Delete(tempDb);
                if (File.Exists($"{tempDb}-shm")) File.Delete($"{tempDb}-shm");
                if (File.Exists($"{tempDb}-wal")) File.Delete($"{tempDb}-wal");
            }
            catch { }
        }
    }
}
