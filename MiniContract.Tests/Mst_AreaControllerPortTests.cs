using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;
using Xunit.Abstractions;

namespace MiniContract.Tests;

public class Mst_AreaControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly TestWebHostEnvironment _env;
    private readonly Mst_AreaControllerPort _controller;
    private readonly ITestOutputHelper? _output;

    public Mst_AreaControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_Area
        var seeder = new Mst_AreaSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

        _env = new TestWebHostEnvironment();
        _controller = new Mst_AreaControllerPort(_db, _env);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
        try
        {
            if (Directory.Exists(_env.WebRootPath))
            {
                Directory.Delete(_env.WebRootPath, true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task Export_WithAllRecords_SuccessAndGeneratesExcel()
    {
        var result = await _controller.Export(new Mst_Area_ExportReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.False(string.IsNullOrEmpty(strUrl));
        Assert.EndsWith(".xlsx", strUrl);

        // Kiểm tra file vật lý đã tạo
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        // Đọc lại file Excel để kiểm tra tiêu đề và dữ liệu
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Area");
        Assert.NotNull(ws);
        Assert.Equal("Mã khu vực", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên khu vực", ws.Cell(1, 2).GetString());
        Assert.Equal("Mô tả", ws.Cell(1, 3).GetString());
        Assert.Equal("Mã khu vực cha", ws.Cell(1, 4).GetString());
        Assert.Equal("Trạng thái", ws.Cell(1, 5).GetString());

        // 4 dòng dữ liệu đã seed
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(5, lastRow); // Dòng 1 tiêu đề + 4 dòng dữ liệu
    }

    [Fact]
    public async Task Export_FilterByOrgID_ReturnsFilteredCount()
    {
        var req = new Mst_Area_ExportReq { OrgID = "7200000001" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        string strUrl = root.GetProperty("strUrl").GetString()!;

        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Area");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(4, lastRow); // Dòng 1 tiêu đề + 3 dòng của Org 7200000001
    }

    [Fact]
    public async Task Export_FilterByOrgAndFlagActive_ReturnsActiveOnly()
    {
        var req = new Mst_Area_ExportReq { OrgID = "7200000001", FlagActive = "1" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        string strUrl = root.GetProperty("strUrl").GetString()!;

        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Area");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(3, lastRow); // Dòng 1 tiêu đề + 2 dòng (MB, MN)
    }

    [Fact]
    public async Task Export_NoMatchingRecords_ReturnsNoData()
    {
        var req = new Mst_Area_ExportReq { OrgID = "NON_EXISTENT" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Không có dữ liệu!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
    }

    [Fact]
    public async Task Export_NegativePageIndexOrZeroCount_ReturnsNoData()
    {
        var req = new Mst_Area_ExportReq { Ft_RecordStart = -1 };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Không có dữ liệu!", root.GetProperty("Title").GetString());
    }

    [Fact]
    public async Task Index_NullOrEmptyBody_ReturnsAllSeededAreasWithDefaultPaging()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(4, list.Count);
        // Thứ tự sắp xếp OrgID asc, AreaCode asc (Customer.cs:380, 420)
        Assert.Equal("7200000001", list[0].GetProperty("OrgID").GetString());
        Assert.Equal("MB", list[0].GetProperty("AreaCode").GetString());
        Assert.Equal("7200000001", list[1].GetProperty("OrgID").GetString());
        Assert.Equal("MN", list[1].GetProperty("AreaCode").GetString());
        Assert.Equal("7200000001", list[2].GetProperty("OrgID").GetString());
        Assert.Equal("MT", list[2].GetProperty("AreaCode").GetString());
        Assert.Equal("7200000002", list[3].GetProperty("OrgID").GetString());
        Assert.Equal("MB", list[3].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByOrgID_ReturnsFilteredItems()
    {
        var req = new Mst_Area_IndexReq { OrgID = "7200000001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(3, list.Count);
        Assert.All(list, item => Assert.Equal("7200000001", item.GetProperty("OrgID").GetString()));
    }

    [Fact]
    public async Task Index_FilterByOrgAndFlagActive_ReturnsActiveOnly()
    {
        var req = new Mst_Area_IndexReq { OrgID = "7200000001", FlagActive = "1" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("MB", list[0].GetProperty("AreaCode").GetString());
        Assert.Equal("MN", list[1].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByAreaCode_ReturnsMatchingCodeAcrossOrgs()
    {
        var req = new Mst_Area_IndexReq { AreaCode = "MB" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("7200000001", list[0].GetProperty("OrgID").GetString());
        Assert.Equal("7200000002", list[1].GetProperty("OrgID").GetString());
    }

    [Fact]
    public async Task Index_FilterByNetworkID_ReturnsMatchingNetwork()
    {
        var req = new Mst_Area_IndexReq { NetworkID = "1002" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Single(list);
        Assert.Equal("7200000002", list[0].GetProperty("OrgID").GetString());
        Assert.Equal("MB", list[0].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_NoMatchingRecords_ReturnsEmptyListAndZeroCounts()
    {
        var req = new Mst_Area_IndexReq { OrgID = "NON_EXISTENT" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task Index_Paging_FirstPageAndSecondPage_CalculatesCorrectOffsets()
    {
        // Trang 0, kích thước 2
        var req0 = new Mst_Area_IndexReq { Page = 0, RecordCount = 2 };
        var result0 = await _controller.Index(req0);
        var doc0 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result0).Value));
        var data0 = doc0.RootElement.GetProperty("Data");

        Assert.Equal(4, data0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data0.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data0.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data0.GetProperty("PageSize").GetInt32());
        var list0 = data0.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list0.Count);
        Assert.Equal("MB", list0[0].GetProperty("AreaCode").GetString());
        Assert.Equal("MN", list0[1].GetProperty("AreaCode").GetString());

        // Trang 1, kích thước 2
        var req1 = new Mst_Area_IndexReq { Page = 1, RecordCount = 2 };
        var result1 = await _controller.Index(req1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result1).Value));
        var data1 = doc1.RootElement.GetProperty("Data");

        Assert.Equal(4, data1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data1.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, data1.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data1.GetProperty("PageSize").GetInt32());
        var list1 = data1.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list1.Count);
        Assert.Equal("MT", list1[0].GetProperty("AreaCode").GetString());
        Assert.Equal("MB", list1[1].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_Paging_PageBeyondTotal_ReturnsEmptyList()
    {
        var req = new Mst_Area_IndexReq { Page = 10, RecordCount = 2 };
        var result = await _controller.Index(req);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32()); // Empty list -> PageCount = 0 (gate Mst_AreaController.cs:48, 92)
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task Index_NegativePageIndexOrZeroCount_ReturnsEmptyDataList()
    {
        var req = new Mst_Area_IndexReq { Ft_RecordStart = -1 };
        var result = await _controller.Index(req);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task Detail_EmptyBodyOrNull_ReturnsNullData()
    {
        var resNull = await _controller.Detail(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docNull.RootElement.GetProperty("Data").ValueKind);

        var resEmpty = await _controller.Detail(new Mst_Area_DetailReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Data").ValueKind);

        var resBlank = await _controller.Detail(new Mst_Area_DetailReq { AreaCode = "   " });
        var okBlank = Assert.IsType<OkObjectResult>(resBlank);
        var docBlank = JsonDocument.Parse(JsonSerializer.Serialize(okBlank.Value));
        Assert.True(docBlank.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docBlank.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ValidAreaCode_Org1_ReturnsItemAndParentList()
    {
        var req = new Mst_Area_DetailReq { AreaCode = "MB", OrgID = "7200000001" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("MB", data.GetProperty("AreaCode").GetString());
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("Miền Bắc", data.GetProperty("AreaName").GetString());
        Assert.Equal("Khu vực Miền Bắc", data.GetProperty("AreaDesc").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("1", data.GetProperty("AreaLevel").GetString());

        // Danh sách vùng cha ListMst_AreaExt gồm 3 vùng của Org 7200000001
        var extList = data.GetProperty("ListMst_AreaExt").EnumerateArray().ToList();
        Assert.Equal(3, extList.Count);
    }

    [Fact]
    public async Task Detail_ValidAreaCode_Org2_ReturnsItemAndParentList()
    {
        // 2 bộ lọc ra kết quả khác nhau: MB thuộc Org 7200000002
        var req = new Mst_Area_DetailReq { AreaCode = "MB", OrgID = "7200000002" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("7200000002", data.GetProperty("OrgID").GetString());
        Assert.Equal("MB", data.GetProperty("AreaCode").GetString());
        Assert.Equal("1002", data.GetProperty("NetworkID").GetString());
        Assert.Equal("Miền Bắc Org2", data.GetProperty("AreaName").GetString());
        Assert.Equal("Khu vực Miền Bắc Org2", data.GetProperty("AreaDesc").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());

        var extList = data.GetProperty("ListMst_AreaExt").EnumerateArray().ToList();
        Assert.Single(extList);
        Assert.Equal("7200000002", extList[0].GetProperty("OrgID").GetString());
    }

    [Fact]
    public async Task Detail_ValidAreaCode_MT_ReturnsInactiveItem()
    {
        var req = new Mst_Area_DetailReq { AreaCode = "MT", OrgID = "7200000001" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("MT", data.GetProperty("AreaCode").GetString());
        Assert.Equal("Miền Trung", data.GetProperty("AreaName").GetString());
        Assert.Equal("0", data.GetProperty("FlagActive").GetString());
    }

    [Fact]
    public async Task Detail_NonExistentAreaCode_ReturnsNullData()
    {
        var req = new Mst_Area_DetailReq { AreaCode = "NON_EXISTENT", OrgID = "7200000001" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_NetworkIDMismatch_ReturnsNullData()
    {
        var req = new Mst_Area_DetailReq { AreaCode = "MB", OrgID = "7200000001", NetworkID = "9999" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_WithModelString_ReturnsItem()
    {
        var req = new Mst_Area_DetailReq
        {
            Model = JsonSerializer.Serialize(new Mst_Area_DetailReq { AreaCode = "MN", OrgID = "7200000001" })
        };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("MN", data.GetProperty("AreaCode").GetString());
        Assert.Equal("Miền Nam", data.GetProperty("AreaName").GetString());
    }

    // =========================================================================================
    // Endpoint Update (Gate: Mst_AreaController.cs:451-548 Update(string areaCode))
    // =========================================================================================

    [Fact]
    public async Task Update_EmptyBodyOrNull_ReturnsNullData()
    {
        var resNull = await _controller.Update(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docNull.RootElement.GetProperty("Data").ValueKind);

        var resEmpty = await _controller.Update(new Mst_Area_UpdateReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Data").ValueKind);

        var resBlank = await _controller.Update(new Mst_Area_UpdateReq { AreaCode = "   " });
        var okBlank = Assert.IsType<OkObjectResult>(resBlank);
        var docBlank = JsonDocument.Parse(JsonSerializer.Serialize(okBlank.Value));
        Assert.True(docBlank.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docBlank.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ValidAreaCode_Org1_ReturnsItemAndParentList()
    {
        var req = new Mst_Area_UpdateReq { AreaCode = "MB", OrgID = "7200000001" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("MB", data.GetProperty("AreaCode").GetString());
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("Miền Bắc", data.GetProperty("AreaName").GetString());
        Assert.Equal("Khu vực Miền Bắc", data.GetProperty("AreaDesc").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("1", data.GetProperty("AreaLevel").GetString());

        // Danh sách vùng cha ListMst_AreaExt gồm 3 vùng của Org 7200000001
        var extList = data.GetProperty("ListMst_AreaExt").EnumerateArray().ToList();
        Assert.Equal(3, extList.Count);
    }

    [Fact]
    public async Task Update_ValidAreaCode_Org2_ReturnsItemAndParentList()
    {
        // 2 bộ lọc ra kết quả khác nhau: MB thuộc Org 7200000002
        var req = new Mst_Area_UpdateReq { AreaCode = "MB", OrgID = "7200000002" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("7200000002", data.GetProperty("OrgID").GetString());
        Assert.Equal("MB", data.GetProperty("AreaCode").GetString());
        Assert.Equal("1002", data.GetProperty("NetworkID").GetString());
        Assert.Equal("Miền Bắc Org2", data.GetProperty("AreaName").GetString());
        Assert.Equal("Khu vực Miền Bắc Org2", data.GetProperty("AreaDesc").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());

        var extList = data.GetProperty("ListMst_AreaExt").EnumerateArray().ToList();
        Assert.Single(extList);
        Assert.Equal("7200000002", extList[0].GetProperty("OrgID").GetString());
    }

    [Fact]
    public async Task Update_ValidAreaCode_MT_ReturnsInactiveItem()
    {
        var req = new Mst_Area_UpdateReq { AreaCode = "MT", OrgID = "7200000001" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("MT", data.GetProperty("AreaCode").GetString());
        Assert.Equal("Miền Trung", data.GetProperty("AreaName").GetString());
        Assert.Equal("0", data.GetProperty("FlagActive").GetString());
    }

    [Fact]
    public async Task Update_NonExistentAreaCode_ReturnsNullData()
    {
        var req = new Mst_Area_UpdateReq { AreaCode = "NON_EXISTENT", OrgID = "7200000001" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_NetworkIDMismatch_ReturnsNullData()
    {
        var req = new Mst_Area_UpdateReq { AreaCode = "MB", OrgID = "7200000001", NetworkID = "9999" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_WithModelString_ReturnsItem()
    {
        var req = new Mst_Area_UpdateReq
        {
            Model = JsonSerializer.Serialize(new Mst_Area_UpdateReq { AreaCode = "MN", OrgID = "7200000001" })
        };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("MN", data.GetProperty("AreaCode").GetString());
        Assert.Equal("Miền Nam", data.GetProperty("AreaName").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Port()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_area_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
        }
        Assert.True(File.Exists(dllPath), $"Cannot find MiniContract.dll at {dllPath}");

        var port = FreePort.Get();
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(dllPath),
        };
        psi.Environment["PORT"] = port.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(5) };
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
            Assert.True(started, $"MiniContract server failed to start within 15 seconds on port {port}.");

            // Request 1: POST empty body -> 200 OK, returns all 4 seeded records
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_area/index", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ1 (Empty body): {body1}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(4, data1.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data1.GetProperty("PageCount").GetInt32());
            Assert.Equal(4, data1.GetProperty("DataList").GetArrayLength());

            // Request 2: POST Filter OrgID = 7200000001 -> 200 OK, returns 3 records
            var req2 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_area/index", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ2 (OrgID 7200000001): {body2}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(3, data2.GetProperty("ItemCount").GetInt32());
            Assert.Equal(3, data2.GetProperty("DataList").GetArrayLength());

            // Request 3: POST Filter OrgID = 7200000001 AND FlagActive = 1 -> 200 OK, returns 2 records (MB, MN)
            var req3 = new StringContent("{\"OrgID\":\"7200000001\",\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_area/index", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ3 (OrgID + FlagActive): {body3}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(2, data3.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data3.GetProperty("DataList").GetArrayLength());

            // Request 4: POST Filter AreaCode = MB -> 200 OK, returns 2 records across both orgs
            var req4 = new StringContent("{\"AreaCode\":\"MB\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_area/index", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ4 (AreaCode MB): {body4}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(2, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data4.GetProperty("DataList").GetArrayLength());

            // Request 5: POST Filter NetworkID = 1002 -> 200 OK, returns 1 record
            var req5 = new StringContent("{\"NetworkID\":\"1002\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_area/index", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ5 (NetworkID 1002): {body5}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var doc5 = JsonDocument.Parse(body5);
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal(1, data5.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data5.GetProperty("DataList").GetArrayLength());

            // Request 6: POST Pagination Page = 0, Recordcount = 2 -> 200 OK, returns 2 records, PageCount = 2
            var req6 = new StringContent("{\"Page\":0,\"Recordcount\":2}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_area/index", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ6 (Pagination): {body6}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(4, data6.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data6.GetProperty("PageCount").GetInt32());
            Assert.Equal(2, data6.GetProperty("DataList").GetArrayLength());

            // Request 7: POST Non-existent Org -> 200 OK, returns empty DataList
            var req7 = new StringContent("{\"OrgID\":\"NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_area/index", req7);
            var body7 = await res7.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ7 (Non-existent): {body7}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res7.StatusCode);
            var doc7 = JsonDocument.Parse(body7);
            var data7 = doc7.RootElement.GetProperty("Data");
            Assert.Equal(0, data7.GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, data7.GetProperty("PageCount").GetInt32());
            Assert.Equal(0, data7.GetProperty("DataList").GetArrayLength());

            // ================== Kiểm thử endpoint Detail qua server thật ==================

            // Request 8: Detail với body rỗng -> 200 OK, Data = null (gate trả View(new Mst_Area()))
            var reqDet8 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resDet8 = await http.PostAsync("/api/mst_area/detail", reqDet8);
            var bodyDet8 = await resDet8.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ8 Detail (Empty body): {bodyDet8}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet8.StatusCode);
            var docDet8 = JsonDocument.Parse(bodyDet8);
            Assert.True(docDet8.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docDet8.RootElement.GetProperty("Data").ValueKind);

            // Request 9: Detail AreaCode = MB, OrgID = 7200000001 -> 200 OK, trả về MB Org 1 kèm ListMst_AreaExt 3 bản ghi
            var reqDet9 = new StringContent("{\"AreaCode\":\"MB\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var resDet9 = await http.PostAsync("/api/mst_area/detail", reqDet9);
            var bodyDet9 = await resDet9.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ9 Detail (MB Org1): {bodyDet9}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet9.StatusCode);
            var docDet9 = JsonDocument.Parse(bodyDet9);
            Assert.True(docDet9.RootElement.GetProperty("Success").GetBoolean());
            var dataDet9 = docDet9.RootElement.GetProperty("Data");
            Assert.Equal("MB", dataDet9.GetProperty("AreaCode").GetString());
            Assert.Equal("7200000001", dataDet9.GetProperty("OrgID").GetString());
            Assert.Equal("1001", dataDet9.GetProperty("NetworkID").GetString());
            Assert.Equal("Miền Bắc", dataDet9.GetProperty("AreaName").GetString());
            Assert.Equal(3, dataDet9.GetProperty("ListMst_AreaExt").GetArrayLength());

            // Request 10: Detail AreaCode = MB, OrgID = 7200000002 -> 200 OK, trả về MB Org 2 kèm ListMst_AreaExt 1 bản ghi
            var reqDet10 = new StringContent("{\"AreaCode\":\"MB\",\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var resDet10 = await http.PostAsync("/api/mst_area/detail", reqDet10);
            var bodyDet10 = await resDet10.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ10 Detail (MB Org2): {bodyDet10}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet10.StatusCode);
            var docDet10 = JsonDocument.Parse(bodyDet10);
            Assert.True(docDet10.RootElement.GetProperty("Success").GetBoolean());
            var dataDet10 = docDet10.RootElement.GetProperty("Data");
            Assert.Equal("MB", dataDet10.GetProperty("AreaCode").GetString());
            Assert.Equal("7200000002", dataDet10.GetProperty("OrgID").GetString());
            Assert.Equal("1002", dataDet10.GetProperty("NetworkID").GetString());
            Assert.Equal("Miền Bắc Org2", dataDet10.GetProperty("AreaName").GetString());
            Assert.Equal(1, dataDet10.GetProperty("ListMst_AreaExt").GetArrayLength());

            // Request 11: Detail AreaCode = NON_EXISTENT -> 200 OK, Data = null
            var reqDet11 = new StringContent("{\"AreaCode\":\"NON_EXISTENT\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var resDet11 = await http.PostAsync("/api/mst_area/detail", reqDet11);
            var bodyDet11 = await resDet11.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ11 Detail (Non-existent): {bodyDet11}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet11.StatusCode);
            var docDet11 = JsonDocument.Parse(bodyDet11);
            Assert.True(docDet11.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docDet11.RootElement.GetProperty("Data").ValueKind);

            // Request 12: Detail với chuỗi Model serialize JSON
            var reqDet12 = new StringContent("{\"Model\":\"{\\\"AreaCode\\\":\\\"MN\\\",\\\"OrgID\\\":\\\"7200000001\\\"}\"}", Encoding.UTF8, "application/json");
            var resDet12 = await http.PostAsync("/api/mst_area/detail", reqDet12);
            var bodyDet12 = await resDet12.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ12 Detail (Model string): {bodyDet12}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet12.StatusCode);
            var docDet12 = JsonDocument.Parse(bodyDet12);
            Assert.True(docDet12.RootElement.GetProperty("Success").GetBoolean());
            var dataDet12 = docDet12.RootElement.GetProperty("Data");
            Assert.Equal("MN", dataDet12.GetProperty("AreaCode").GetString());
            Assert.Equal("Miền Nam", dataDet12.GetProperty("AreaName").GetString());

            // ================== Kiểm thử endpoint Update qua server thật ==================

            // Request 13: Update với body rỗng -> 200 OK, Data = null (gate trả View(new Mst_Area()))
            var reqUpd13 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resUpd13 = await http.PostAsync("/api/mst_area/update", reqUpd13);
            var bodyUpd13 = await resUpd13.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ13 Update (Empty body): {bodyUpd13}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpd13.StatusCode);
            var docUpd13 = JsonDocument.Parse(bodyUpd13);
            Assert.True(docUpd13.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docUpd13.RootElement.GetProperty("Data").ValueKind);

            // Request 14: Update AreaCode = MB, OrgID = 7200000001 -> 200 OK, trả về MB Org 1 kèm ListMst_AreaExt 3 bản ghi
            var reqUpd14 = new StringContent("{\"AreaCode\":\"MB\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var resUpd14 = await http.PostAsync("/api/mst_area/update", reqUpd14);
            var bodyUpd14 = await resUpd14.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ14 Update (MB Org1): {bodyUpd14}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpd14.StatusCode);
            var docUpd14 = JsonDocument.Parse(bodyUpd14);
            Assert.True(docUpd14.RootElement.GetProperty("Success").GetBoolean());
            var dataUpd14 = docUpd14.RootElement.GetProperty("Data");
            Assert.Equal("MB", dataUpd14.GetProperty("AreaCode").GetString());
            Assert.Equal("7200000001", dataUpd14.GetProperty("OrgID").GetString());
            Assert.Equal("1001", dataUpd14.GetProperty("NetworkID").GetString());
            Assert.Equal("Miền Bắc", dataUpd14.GetProperty("AreaName").GetString());
            Assert.Equal(3, dataUpd14.GetProperty("ListMst_AreaExt").GetArrayLength());

            // Request 15: Update AreaCode = MB, OrgID = 7200000002 -> 200 OK, trả về MB Org 2 kèm ListMst_AreaExt 1 bản ghi
            var reqUpd15 = new StringContent("{\"AreaCode\":\"MB\",\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var resUpd15 = await http.PostAsync("/api/mst_area/update", reqUpd15);
            var bodyUpd15 = await resUpd15.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ15 Update (MB Org2): {bodyUpd15}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpd15.StatusCode);
            var docUpd15 = JsonDocument.Parse(bodyUpd15);
            Assert.True(docUpd15.RootElement.GetProperty("Success").GetBoolean());
            var dataUpd15 = docUpd15.RootElement.GetProperty("Data");
            Assert.Equal("MB", dataUpd15.GetProperty("AreaCode").GetString());
            Assert.Equal("7200000002", dataUpd15.GetProperty("OrgID").GetString());
            Assert.Equal("1002", dataUpd15.GetProperty("NetworkID").GetString());
            Assert.Equal("Miền Bắc Org2", dataUpd15.GetProperty("AreaName").GetString());
            Assert.Equal(1, dataUpd15.GetProperty("ListMst_AreaExt").GetArrayLength());

            // Request 16: Update AreaCode = NON_EXISTENT -> 200 OK, Data = null
            var reqUpd16 = new StringContent("{\"AreaCode\":\"NON_EXISTENT\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var resUpd16 = await http.PostAsync("/api/mst_area/update", reqUpd16);
            var bodyUpd16 = await resUpd16.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ16 Update (Non-existent): {bodyUpd16}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpd16.StatusCode);
            var docUpd16 = JsonDocument.Parse(bodyUpd16);
            Assert.True(docUpd16.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docUpd16.RootElement.GetProperty("Data").ValueKind);

            // Request 17: Update với chuỗi Model serialize JSON
            var reqUpd17 = new StringContent("{\"Model\":\"{\\\"AreaCode\\\":\\\"MN\\\",\\\"OrgID\\\":\\\"7200000001\\\"}\"}", Encoding.UTF8, "application/json");
            var resUpd17 = await http.PostAsync("/api/mst_area/update", reqUpd17);
            var bodyUpd17 = await resUpd17.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ17 Update (Model string): {bodyUpd17}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpd17.StatusCode);
            var docUpd17 = JsonDocument.Parse(bodyUpd17);
            Assert.True(docUpd17.RootElement.GetProperty("Success").GetBoolean());
            var dataUpd17 = docUpd17.RootElement.GetProperty("Data");
            Assert.Equal("MN", dataUpd17.GetProperty("AreaCode").GetString());
            Assert.Equal("Miền Nam", dataUpd17.GetProperty("AreaName").GetString());
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
            }
            catch { }
        }
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "test_wwwroot_" + Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MiniContract.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
    }
}
