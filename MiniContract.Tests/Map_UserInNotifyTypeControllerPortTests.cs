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

namespace MiniContract.Tests;

public class Map_UserInNotifyTypeControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Map_UserInNotifyTypeControllerPort _controller;

    public Map_UserInNotifyTypeControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Seed Mst_NNT
        var nntSeeder = new Mst_NNTSeeder();
        nntSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Sys_User
        var userSeeder = new Sys_UserSeeder();
        userSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Mst_NotifyType
        var notifyTypeSeeder = new Mst_NotifyTypeSeeder();
        notifyTypeSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Mst_ManageNotify
        var manageNotifySeeder = new Mst_ManageNotifySeeder();
        manageNotifySeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Seed Map_UserInNotifyType
        var mapSeeder = new Map_UserInNotifyTypeSeeder();
        mapSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Map_UserInNotifyTypeControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_NullReq_ReturnsAllRecords()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());

        var listMap = data.GetProperty("Lst_Map_UserInNotifyType");
        Assert.Equal(4, listMap.GetArrayLength());

        var listNotifyType = data.GetProperty("Lst_Mst_NotifyType");
        Assert.Equal(3, listNotifyType.GetArrayLength());

        var listManageNotify = data.GetProperty("Lst_Mst_ManageNotify");
        Assert.Equal(3, listManageNotify.GetArrayLength());

        var listNNT = data.GetProperty("ListMst_NNT");
        Assert.True(listNNT.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Index_EmptyReq_ReturnsAllRecords()
    {
        var req = new Map_UserInNotifyType_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());

        var summary = data.GetProperty("MySummaryTable");
        Assert.Equal(4, summary.GetProperty("MyCount").GetInt32());
    }

    [Fact]
    public async Task Index_FilterByMST_0100000001_Returns3Records()
    {
        // MST 0100000001 có U01 (2 loại thông báo) và U02 (1 loại thông báo) -> tổng 3 bản ghi
        var req = new Map_UserInNotifyType_IndexReq { MST = "0100000001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        var listMap = data.GetProperty("Lst_Map_UserInNotifyType");
        Assert.Equal(3, listMap.GetArrayLength());

        var listNotify = data.GetProperty("Lst_Mst_NotifyType");
        Assert.Equal(2, listNotify.GetArrayLength()); // NT01, NT02

        var listManage = data.GetProperty("Lst_Mst_ManageNotify");
        Assert.Equal(2, listManage.GetArrayLength()); // U01, U02
    }

    [Fact]
    public async Task Index_FilterByMST_0100000002_Returns1Record()
    {
        // MST 0100000002 chỉ có U03 -> tổng 1 bản ghi
        var req = new Map_UserInNotifyType_IndexReq { MST = "0100000002" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var listMap = data.GetProperty("Lst_Map_UserInNotifyType");
        Assert.Equal(1, listMap.GetArrayLength());
        Assert.Equal("U03", listMap[0].GetProperty("UserCode").GetString());
        Assert.Equal("NT03", listMap[0].GetProperty("NotifyType").GetString());
    }

    [Fact]
    public async Task Index_FilterByEmail_ReturnsMatchingRecord()
    {
        var req = new Map_UserInNotifyType_IndexReq { Email = "tester" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var listMap = data.GetProperty("Lst_Map_UserInNotifyType");
        Assert.Equal(1, listMap.GetArrayLength());
        Assert.Equal("U02", listMap[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByNonExistentMST_ReturnsEmpty()
    {
        var req = new Map_UserInNotifyType_IndexReq { MST = "9999999999" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("Lst_Map_UserInNotifyType").GetArrayLength());
        Assert.Equal(0, data.GetProperty("Lst_Mst_NotifyType").GetArrayLength());
        Assert.Equal(0, data.GetProperty("Lst_Mst_ManageNotify").GetArrayLength());
    }

    [Fact]
    public async Task Index_Pagination_CorrectPageCountAndItems()
    {
        var req = new Map_UserInNotifyType_IndexReq { Page = 0, PageSize = 2 };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(2, data.GetProperty("Lst_Map_UserInNotifyType").GetArrayLength());

        // Trang thứ 2
        var reqPage2 = new Map_UserInNotifyType_IndexReq { Page = 1, PageSize = 2 };
        var result2 = await _controller.Index(reqPage2);
        var okResult2 = Assert.IsType<OkObjectResult>(result2);

        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(okResult2.Value));
        var data2 = doc2.RootElement.GetProperty("Data");
        Assert.Equal(2, data2.GetProperty("Lst_Map_UserInNotifyType").GetArrayLength());
    }

    [Fact]
    public async Task RealServer_Verification_Port_MapUserInNotifyType()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_map_userinnotifytype_{Guid.NewGuid():N}.db");
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

            // 1. Ca body rỗng: POST {} -> 200 OK, ItemCount=4
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/map_userinnotifytype/index", req1);
            Assert.True(res1.IsSuccessStatusCode);
            var json1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(json1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(4, doc1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 2. Ca bộ lọc 1: MST = 0100000001 -> ItemCount=3
            var req2 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001" }), Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/map_userinnotifytype/index", req2);
            Assert.True(res2.IsSuccessStatusCode);
            var doc2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync());
            Assert.Equal(3, doc2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 3. Ca bộ lọc 2: MST = 0100000002 -> ItemCount=1
            var req3 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000002" }), Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/map_userinnotifytype/index", req3);
            Assert.True(res3.IsSuccessStatusCode);
            var doc3 = JsonDocument.Parse(await res3.Content.ReadAsStringAsync());
            Assert.Equal(1, doc3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 4. Ca bộ lọc 3: Email = "tester" -> ItemCount=1
            var req4 = new StringContent(JsonSerializer.Serialize(new { Email = "tester" }), Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/map_userinnotifytype/index", req4);
            Assert.True(res4.IsSuccessStatusCode);
            var doc4 = JsonDocument.Parse(await res4.Content.ReadAsStringAsync());
            Assert.Equal(1, doc4.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 5. Ca bộ lọc không tồn tại: MST = 9999999999 -> ItemCount=0, PageCount=0
            var req5 = new StringContent(JsonSerializer.Serialize(new { MST = "9999999999" }), Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/map_userinnotifytype/index", req5);
            Assert.True(res5.IsSuccessStatusCode);
            var doc5 = JsonDocument.Parse(await res5.Content.ReadAsStringAsync());
            Assert.Equal(0, doc5.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, doc5.RootElement.GetProperty("Data").GetProperty("PageCount").GetInt32());
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
