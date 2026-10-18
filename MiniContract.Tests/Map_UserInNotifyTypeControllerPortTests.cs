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
    public async Task DoSearch_NullReq_ReturnsAllRecords()
    {
        var result = await _controller.DoSearch(null);
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
    }

    [Fact]
    public async Task DoSearch_EmptyReq_ReturnsAllRecords()
    {
        var req = new Map_UserInNotifyType_DoSearchReq();
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_FilterByMST_0100000001_Returns3Records()
    {
        // MST 0100000001 có U01 (2 loại thông báo) và U02 (1 loại thông báo) -> tổng 3 bản ghi
        var req = new Map_UserInNotifyType_DoSearchReq { MST = "0100000001" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_FilterByMST_0100000002_Returns1Record()
    {
        // MST 0100000002 chỉ có U03 -> tổng 1 bản ghi
        var req = new Map_UserInNotifyType_DoSearchReq { MST = "0100000002" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_FilterByEmail_ReturnsMatchingRecord()
    {
        var req = new Map_UserInNotifyType_DoSearchReq { Email = "tester" };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var listMap = data.GetProperty("Lst_Map_UserInNotifyType");
        Assert.Equal(1, listMap.GetArrayLength());
        Assert.Equal("U02", listMap[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByNonExistentMST_ReturnsEmpty()
    {
        var req = new Map_UserInNotifyType_DoSearchReq { MST = "9999999999" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_Pagination_CorrectPageCountAndItems()
    {
        var req = new Map_UserInNotifyType_DoSearchReq { Page = 0, PageSize = 2 };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(2, data.GetProperty("Lst_Map_UserInNotifyType").GetArrayLength());

        // Trang thứ 2
        var reqPage2 = new Map_UserInNotifyType_DoSearchReq { Page = 1, PageSize = 2 };
        var result2 = await _controller.DoSearch(reqPage2);
        var okResult2 = Assert.IsType<OkObjectResult>(result2);

        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(okResult2.Value));
        var data2 = doc2.RootElement.GetProperty("Data");
        Assert.Equal(2, data2.GetProperty("Lst_Map_UserInNotifyType").GetArrayLength());
    }

    [Fact]
    public async Task SaveData_NullBody_ReturnsInvoiceTblInvalid()
    {
        var result = await _controller.SaveData(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Map_UserInNotifyType", root.GetProperty("Params").GetProperty("Check.TableName").GetString());
    }

    [Fact]
    public async Task SaveData_EmptyObject_ReturnsInvoiceTblInvalid()
    {
        var docIn = JsonDocument.Parse("{}");
        var result = await _controller.SaveData(docIn.RootElement);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveData_EmptyList_ReturnsInvoiceTblInvalid()
    {
        var docIn = JsonDocument.Parse("[]");
        var result = await _controller.SaveData(docIn.RootElement);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveData_TableNotFound_ReturnsInvoiceTblNotFound()
    {
        var docIn = JsonDocument.Parse("{\"UnknownTable\": []}");
        var result = await _controller.SaveData(docIn.RootElement);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Map_UserInNotifyType_SaveX_Input_InvoiceTblNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Map_UserInNotifyType", root.GetProperty("Params").GetProperty("Check.TableName").GetString());
    }

    [Fact]
    public async Task SaveData_InsertAndUpsert_SuccessRoundTrip()
    {
        // Ban đầu có 4 bản ghi: U01-NT01 (FlagNotify=1), U01-NT02 (1), U02-NT01 (1), U03-NT03 (0)
        // Thực hiện cập nhật U01-NT01 thành FlagNotify=0 và thêm mới U01-NT03 FlagNotify=1
        var jsonInput = JsonSerializer.Serialize(new
        {
            Lst_Map_UserInNotifyType = new[]
            {
                new { UserCode = "u01", NotifyType = "nt01", FlagNotify = "0" },
                new { UserCode = "u01", NotifyType = "nt03", FlagNotify = "1" }
            }
        });
        var docIn = JsonDocument.Parse(jsonInput);

        var result = await _controller.SaveData(docIn.RootElement);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Phân quyền thông báo thành công!", doc.RootElement.GetProperty("Title").GetString());

        // Kiểm tra round-trip đọc lại qua Index
        var indexRes = await _controller.Index(new Map_UserInNotifyType_IndexReq { UserCode = "U01" });
        var okIndex = Assert.IsType<OkObjectResult>(indexRes);
        var docIndex = JsonDocument.Parse(JsonSerializer.Serialize(okIndex.Value));
        var data = docIndex.RootElement.GetProperty("Data");

        // U01 giờ có 3 bản ghi: NT01, NT02, NT03
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());

        // Kiểm tra FlagNotify của NT01 đã thành 0
        var list = data.GetProperty("Lst_Map_UserInNotifyType");
        var nt01 = false;
        var nt03 = false;
        foreach (var item in list.EnumerateArray())
        {
            if (item.GetProperty("NotifyType").GetString() == "NT01")
            {
                Assert.Equal("0", item.GetProperty("FlagNotify").GetString());
                nt01 = true;
            }
            if (item.GetProperty("NotifyType").GetString() == "NT03")
            {
                Assert.Equal("1", item.GetProperty("FlagNotify").GetString());
                nt03 = true;
            }
        }
        Assert.True(nt01);
        Assert.True(nt03);

        // Tổng số bản ghi trong bảng tăng từ 4 lên 5
        var allRes = await _controller.Index(null);
        var docAll = JsonDocument.Parse(JsonSerializer.Serialize(((OkObjectResult)allRes).Value));
        Assert.Equal(5, docAll.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task SaveData_DeleteWithFlagIsDelete1_SuccessRoundTrip()
    {
        // Xoá bản ghi U03 - NT03 với FlagIsDelete = "1"
        var jsonInput = JsonSerializer.Serialize(new
        {
            FlagIsDelete = "1",
            Lst_Map_UserInNotifyType = new[]
            {
                new { UserCode = "u03", NotifyType = "nt03" }
            }
        });
        var docIn = JsonDocument.Parse(jsonInput);

        var result = await _controller.SaveData(docIn.RootElement);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        // Đọc lại thấy U03 không còn bản ghi nào
        var indexRes = await _controller.Index(new Map_UserInNotifyType_IndexReq { UserCode = "U03" });
        var docIndex = JsonDocument.Parse(JsonSerializer.Serialize(((OkObjectResult)indexRes).Value));
        Assert.Equal(0, docIndex.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

        // Tổng số bản ghi trong bảng giảm từ 4 xuống 3
        var allRes = await _controller.Index(null);
        var docAll = JsonDocument.Parse(JsonSerializer.Serialize(((OkObjectResult)allRes).Value));
        Assert.Equal(3, docAll.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task SaveData_StringModelFormat_Success()
    {
        // Định dạng chuỗi { "model": "[{...}]" } theo đúng gate SaveData(string model)
        var innerJson = JsonSerializer.Serialize(new[]
        {
            new { UserCode = "u02", NotifyType = "nt02", FlagNotify = "1" }
        });
        var jsonInput = JsonSerializer.Serialize(new { model = innerJson });
        var docIn = JsonDocument.Parse(jsonInput);

        var result = await _controller.SaveData(docIn.RootElement);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        // Kiểm tra round-trip U02 - NT02 đã được tạo
        var indexRes = await _controller.Index(new Map_UserInNotifyType_IndexReq { UserCode = "U02", NotifyType = "NT02" });
        var docIndex = JsonDocument.Parse(JsonSerializer.Serialize(((OkObjectResult)indexRes).Value));
        Assert.Equal(1, docIndex.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
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

            // 6. Kiểm tra action DoSearch: POST /api/map_userinnotifytype/dosearch với body rỗng -> ItemCount=4
            var reqDs1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resDs1 = await http.PostAsync("/api/map_userinnotifytype/dosearch", reqDs1);
            Assert.True(resDs1.IsSuccessStatusCode);
            var docDs1 = JsonDocument.Parse(await resDs1.Content.ReadAsStringAsync());
            Assert.True(docDs1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(4, docDs1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 7. Kiểm tra action DoSearch: POST /api/map_userinnotifytype/dosearch với MST = 0100000001 -> ItemCount=3
            var reqDs2 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000001" }), Encoding.UTF8, "application/json");
            var resDs2 = await http.PostAsync("/api/map_userinnotifytype/dosearch", reqDs2);
            Assert.True(resDs2.IsSuccessStatusCode);
            var docDs2 = JsonDocument.Parse(await resDs2.Content.ReadAsStringAsync());
            Assert.Equal(3, docDs2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 8. Kiểm tra action DoSearch: POST /api/map_userinnotifytype/dosearch với MST = 0100000002 -> ItemCount=1
            var reqDs3 = new StringContent(JsonSerializer.Serialize(new { MST = "0100000002" }), Encoding.UTF8, "application/json");
            var resDs3 = await http.PostAsync("/api/map_userinnotifytype/dosearch", reqDs3);
            Assert.True(resDs3.IsSuccessStatusCode);
            var docDs3 = JsonDocument.Parse(await resDs3.Content.ReadAsStringAsync());
            Assert.Equal(1, docDs3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 9. Kiểm tra action DoSearch: POST /api/map_userinnotifytype/dosearch với Email = tester -> ItemCount=1
            var reqDs4 = new StringContent(JsonSerializer.Serialize(new { Email = "tester" }), Encoding.UTF8, "application/json");
            var resDs4 = await http.PostAsync("/api/map_userinnotifytype/dosearch", reqDs4);
            Assert.True(resDs4.IsSuccessStatusCode);
            var docDs4 = JsonDocument.Parse(await resDs4.Content.ReadAsStringAsync());
            Assert.Equal(1, docDs4.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 10. Kiểm tra action DoSearch: POST /api/map_userinnotifytype/dosearch với MST = 9999999999 -> ItemCount=0, PageCount=0
            var reqDs5 = new StringContent(JsonSerializer.Serialize(new { MST = "9999999999" }), Encoding.UTF8, "application/json");
            var resDs5 = await http.PostAsync("/api/map_userinnotifytype/dosearch", reqDs5);
            Assert.True(resDs5.IsSuccessStatusCode);
            var docDs5 = JsonDocument.Parse(await resDs5.Content.ReadAsStringAsync());
            Assert.Equal(0, docDs5.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, docDs5.RootElement.GetProperty("Data").GetProperty("PageCount").GetInt32());

            // 11. Kiểm tra action SaveData: Guard body rỗng POST {} -> 400 BadRequest (Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid)
            var reqSd1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resSd1 = await http.PostAsync("/api/map_userinnotifytype/savedata", reqSd1);
            Assert.False(resSd1.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resSd1.StatusCode);
            var docSd1 = JsonDocument.Parse(await resSd1.Content.ReadAsStringAsync());
            Assert.False(docSd1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid", docSd1.RootElement.GetProperty("ErrorCode").GetString());

            // 12. Kiểm tra action SaveData: Guard bảng không tồn tại -> 400 BadRequest (Map_UserInNotifyType_SaveX_Input_InvoiceTblNotFound)
            var reqSd2 = new StringContent(JsonSerializer.Serialize(new { UnknownTable = new object[0] }), Encoding.UTF8, "application/json");
            var resSd2 = await http.PostAsync("/api/map_userinnotifytype/savedata", reqSd2);
            Assert.False(resSd2.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resSd2.StatusCode);
            var docSd2 = JsonDocument.Parse(await resSd2.Content.ReadAsStringAsync());
            Assert.False(docSd2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Map_UserInNotifyType_SaveX_Input_InvoiceTblNotFound", docSd2.RootElement.GetProperty("ErrorCode").GetString());

            // 13. Kiểm tra action SaveData: Round-trip GHI thêm mới bản ghi U01-NT03 -> 200 OK
            var reqSd3 = new StringContent(JsonSerializer.Serialize(new
            {
                Lst_Map_UserInNotifyType = new[]
                {
                    new { UserCode = "U01", NotifyType = "NT03", FlagNotify = "1" }
                }
            }), Encoding.UTF8, "application/json");
            var resSd3 = await http.PostAsync("/api/map_userinnotifytype/savedata", reqSd3);
            Assert.True(resSd3.IsSuccessStatusCode);
            var docSd3 = JsonDocument.Parse(await resSd3.Content.ReadAsStringAsync());
            Assert.True(docSd3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Phân quyền thông báo thành công!", docSd3.RootElement.GetProperty("Title").GetString());

            // Đọc lại qua Index để xác minh round-trip U01 giờ có 3 bản ghi
            var reqVerifyU01 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U01" }), Encoding.UTF8, "application/json");
            var resVerifyU01 = await http.PostAsync("/api/map_userinnotifytype/index", reqVerifyU01);
            Assert.True(resVerifyU01.IsSuccessStatusCode);
            var docVerifyU01 = JsonDocument.Parse(await resVerifyU01.Content.ReadAsStringAsync());
            Assert.Equal(3, docVerifyU01.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 14. Kiểm tra action SaveData: Round-trip XOÁ bản ghi U03-NT03 với FlagIsDelete = 1 -> 200 OK
            var reqSd4 = new StringContent(JsonSerializer.Serialize(new
            {
                FlagIsDelete = "1",
                Lst_Map_UserInNotifyType = new[]
                {
                    new { UserCode = "U03", NotifyType = "NT03" }
                }
            }), Encoding.UTF8, "application/json");
            var resSd4 = await http.PostAsync("/api/map_userinnotifytype/savedata", reqSd4);
            Assert.True(resSd4.IsSuccessStatusCode);
            var docSd4 = JsonDocument.Parse(await resSd4.Content.ReadAsStringAsync());
            Assert.True(docSd4.RootElement.GetProperty("Success").GetBoolean());

            // Đọc lại qua Index để xác minh round-trip U03 giờ có 0 bản ghi
            var reqVerifyU03 = new StringContent(JsonSerializer.Serialize(new { UserCode = "U03" }), Encoding.UTF8, "application/json");
            var resVerifyU03 = await http.PostAsync("/api/map_userinnotifytype/index", reqVerifyU03);
            Assert.True(resVerifyU03.IsSuccessStatusCode);
            var docVerifyU03 = JsonDocument.Parse(await resVerifyU03.Content.ReadAsStringAsync());
            Assert.Equal(0, docVerifyU03.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
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
