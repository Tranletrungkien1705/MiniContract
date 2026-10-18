using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;
using Xunit.Abstractions;

namespace MiniContract.Tests;

public class Mst_DepartmentControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_DepartmentControllerPort _controller;
    private readonly ITestOutputHelper? _output;

    public Mst_DepartmentControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_Department
        var seeder = new Mst_DepartmentSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Mst_NNT (để kiểm tra MST khi tạo phòng ban)
        var nntSeeder = new Mst_NNTSeeder();
        nntSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_DepartmentControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_WithDefaultOrEmptyBody_ReturnsAllSeededItems()
    {
        var result = await _controller.Index(new Mst_Department_IndexReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
        // Sắp xếp theo DepartmentCode asc (Master.cs:36701, 36740)
        Assert.Equal("DEPT_BGD", list[0].GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_KD_INACT", list[1].GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_KT", list[2].GetProperty("DepartmentCode").GetString());
    }

    [Fact]
    public async Task Index_WithInitSearch_ReturnsEmptyDataList()
    {
        // Khi init == "search", gate không gọi backend và trả danh sách rỗng (Mst_DepartmentController.cs:39, 75-78)
        var result = await _controller.Index(new Mst_Department_IndexReq { Init = "search" });
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: FlagActive = "1" -> 2 bản ghi hoạt động
        var reqActive = new Mst_Department_IndexReq { FlagActive = "1" };
        var resActive = await _controller.Index(reqActive);
        var okActive = Assert.IsType<OkObjectResult>(resActive);
        var docActive = JsonDocument.Parse(JsonSerializer.Serialize(okActive.Value));
        var dataActive = docActive.RootElement.GetProperty("Data");
        Assert.Equal(2, dataActive.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataActive.GetProperty("DataList").GetArrayLength());

        // Bộ lọc 2: DepartmentCode = "DEPT_BGD" -> 1 bản ghi
        var reqCode = new Mst_Department_IndexReq { DepartmentCode = "DEPT_BGD" };
        var resCode = await _controller.Index(reqCode);
        var okCode = Assert.IsType<OkObjectResult>(resCode);
        var docCode = JsonDocument.Parse(JsonSerializer.Serialize(okCode.Value));
        var dataCode = docCode.RootElement.GetProperty("Data");
        Assert.Equal(1, dataCode.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, dataCode.GetProperty("DataList").GetArrayLength());
        Assert.Equal("DEPT_BGD", dataCode.GetProperty("DataList")[0].GetProperty("DepartmentCode").GetString());

        // Hai bộ lọc trả về số dòng khác nhau (2 != 1)
        Assert.NotEqual(dataActive.GetProperty("ItemCount").GetInt32(), dataCode.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Index_FilterByDepartmentName_Contains_ReturnsMatched()
    {
        var req = new Mst_Department_IndexReq { DepartmentName = "Kế Toán" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("DataList").GetArrayLength());
        Assert.Equal("DEPT_KT", data.GetProperty("DataList")[0].GetProperty("DepartmentCode").GetString());
    }

    [Fact]
    public async Task Index_Pagination_ReturnsCorrectPageAndCount()
    {
        // Trang 0, kích thước 2 -> lấy 2 bản ghi đầu
        var reqPage0 = new Mst_Department_IndexReq { RecordCount = 2, Page = 0 };
        var resPage0 = await _controller.Index(reqPage0);
        var okPage0 = Assert.IsType<OkObjectResult>(resPage0);
        var docPage0 = JsonDocument.Parse(JsonSerializer.Serialize(okPage0.Value));
        var dataPage0 = docPage0.RootElement.GetProperty("Data");

        Assert.Equal(3, dataPage0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataPage0.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageSize").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("DataList").GetArrayLength());
        Assert.Equal("DEPT_BGD", dataPage0.GetProperty("DataList")[0].GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_KD_INACT", dataPage0.GetProperty("DataList")[1].GetProperty("DepartmentCode").GetString());

        // Trang 1, kích thước 2 -> lấy bản ghi thứ 3
        var reqPage1 = new Mst_Department_IndexReq { RecordCount = 2, Page = 1 };
        var resPage1 = await _controller.Index(reqPage1);
        var okPage1 = Assert.IsType<OkObjectResult>(resPage1);
        var docPage1 = JsonDocument.Parse(JsonSerializer.Serialize(okPage1.Value));
        var dataPage1 = docPage1.RootElement.GetProperty("Data");

        Assert.Equal(3, dataPage1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataPage1.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, dataPage1.GetProperty("PageIndex").GetInt32());
        Assert.Equal(1, dataPage1.GetProperty("DataList").GetArrayLength());
        Assert.Equal("DEPT_KT", dataPage1.GetProperty("DataList")[0].GetProperty("DepartmentCode").GetString());
    }

    [Fact]
    public async Task Index_RecordCountZeroOrNegative_ReturnsEmptyList()
    {
        // RecordCount = 0 -> DataList rỗng, PageCount = 0, ItemCount vẫn là 3 (LESSON A.7, A.12)
        var reqZero = new Mst_Department_IndexReq { RecordCount = 0 };
        var resZero = await _controller.Index(reqZero);
        var okZero = Assert.IsType<OkObjectResult>(resZero);
        var docZero = JsonDocument.Parse(JsonSerializer.Serialize(okZero.Value));
        var dataZero = docZero.RootElement.GetProperty("Data");

        Assert.Equal(3, dataZero.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByMST_ReturnsMatchingDepartments()
    {
        var req = new Mst_Department_IndexReq { MST = "0101234567" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Create_EmptyBody_ReturnsFail_Mst_Department_Create()
    {
        var result = await _controller.Create(new Mst_Department_CreateReq());
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_Create", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strJson", out var p));
        Assert.Equal(JsonValueKind.Null, p.ValueKind);
    }

    [Fact]
    public async Task Create_NullBody_ReturnsFail_Mst_Department_Create()
    {
        var result = await _controller.Create(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_Create", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_InvalidDepartmentCode_ReturnsFail()
    {
        var req = new Mst_Department_CreateReq { DepartmentCode = "" };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_Create_InvalidDepartmentCode", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_DepartmentCodeExists_ReturnsFail()
    {
        var req = new Mst_Department_CreateReq { DepartmentCode = "DEPT_BGD" };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_CheckDB_DepartmentExist", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("DEPT_BGD", root.GetProperty("Params").GetProperty("Check.DepartmentCode").GetString());
    }

    [Fact]
    public async Task Create_MSTNotFound_ReturnsFail()
    {
        var req = new Mst_Department_CreateReq
        {
            DepartmentCode = "DEPT_NEW",
            MST = "9999999999"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("9999999999", root.GetProperty("Params").GetProperty("Check.MST").GetString());
    }

    [Fact]
    public async Task Create_MSTInactive_ReturnsFail()
    {
        var req = new Mst_Department_CreateReq
        {
            DepartmentCode = "DEPT_NEW",
            MST = "0100000003" // seeded inactive in Mst_NNTSeeder
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("0100000003", root.GetProperty("Params").GetProperty("Check.MST").GetString());
        Assert.Equal("0", root.GetProperty("Params").GetProperty("DB.FlagActive").GetString());
    }

    [Fact]
    public async Task Create_DepartmentCodeParentNotFound_ReturnsFail()
    {
        var req = new Mst_Department_CreateReq
        {
            DepartmentCode = "DEPT_NEW",
            MST = "0100000001",
            DepartmentCodeParent = "DEPT_NONEXIST"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_CheckDB_DepartmentNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("DEPT_NONEXIST", root.GetProperty("Params").GetProperty("Check.DepartmentCode").GetString());
    }

    [Fact]
    public async Task Create_DepartmentCodeParentInactive_ReturnsFail()
    {
        var req = new Mst_Department_CreateReq
        {
            DepartmentCode = "DEPT_NEW",
            MST = "0100000001",
            DepartmentCodeParent = "DEPT_KD_INACT" // seeded inactive in Mst_DepartmentSeeder
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("DEPT_KD_INACT", root.GetProperty("Params").GetProperty("Check.DepartmentCode").GetString());
        Assert.Equal("0", root.GetProperty("Params").GetProperty("DB.FlagActive").GetString());
    }

    [Fact]
    public async Task Create_InvalidDepartmentName_ReturnsFail()
    {
        var req = new Mst_Department_CreateReq
        {
            DepartmentCode = "DEPT_NEW",
            MST = "0100000001",
            DepartmentName = ""
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_Create_InvalidDepartmentName", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_Success_RoundTrip()
    {
        var req = new Mst_Department_CreateReq
        {
            DepartmentCode = "DEPT_MKT",
            DepartmentName = "Phòng Marketing",
            MST = "0100000001",
            DepartmentCodeParent = "DEPT_BGD"
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/Mst_Department", root.GetProperty("RedirectUrl").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("DEPT_MKT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Phòng Marketing", data.GetProperty("DepartmentName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCodeParent").GetString());
        Assert.Equal("DEPT_BGD.DEPT_MKT", data.GetProperty("DepartmentBUCode").GetString());
        Assert.Equal("2", data.GetProperty("DepartmentLevel").GetString());

        // Đọc lại qua Index để xác minh round-trip
        var indexRes = await _controller.Index(new Mst_Department_IndexReq { DepartmentCode = "DEPT_MKT" });
        var okIndex = Assert.IsType<OkObjectResult>(indexRes);
        var indexDoc = JsonDocument.Parse(JsonSerializer.Serialize(okIndex.Value));
        var indexData = indexDoc.RootElement.GetProperty("Data");

        Assert.Equal(1, indexData.GetProperty("ItemCount").GetInt32());
        Assert.Equal("DEPT_MKT", indexData.GetProperty("DataList")[0].GetProperty("DepartmentCode").GetString());
        Assert.Equal("Phòng Marketing", indexData.GetProperty("DataList")[0].GetProperty("DepartmentName").GetString());
    }

    [Fact]
    public async Task Create_WithModelJson_Success_RoundTrip()
    {
        var modelJson = JsonSerializer.Serialize(new
        {
            DepartmentCode = "DEPT_HR",
            DepartmentName = "Phòng Nhân Sự",
            MST = "0100000001",
            DepartmentCodeParent = "DEPT_BGD"
        });

        var req = new Mst_Department_CreateReq { Model = modelJson };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("DEPT_HR", data.GetProperty("DepartmentCode").GetString());

        // Đọc lại qua Index
        var indexRes = await _controller.Index(new Mst_Department_IndexReq { DepartmentCode = "DEPT_HR" });
        var okIndex = Assert.IsType<OkObjectResult>(indexRes);
        var indexDoc = JsonDocument.Parse(JsonSerializer.Serialize(okIndex.Value));
        var indexData = indexDoc.RootElement.GetProperty("Data");

        Assert.Equal(1, indexData.GetProperty("ItemCount").GetInt32());
        Assert.Equal("DEPT_HR", indexData.GetProperty("DataList")[0].GetProperty("DepartmentCode").GetString());
    }

    [Fact]
    public async Task Update_WithNullOrEmptyBody_ReturnsNullData()
    {
        // Body null
        var resNull = await _controller.Update(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docNull.RootElement.GetProperty("Data").ValueKind);

        // Body rỗng
        var resEmpty = await _controller.Update(new Mst_Department_UpdateReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Data").ValueKind);

        // DepartmentCode chuỗi trắng
        var resWhite = await _controller.Update(new Mst_Department_UpdateReq { DepartmentCode = "   " });
        var okWhite = Assert.IsType<OkObjectResult>(resWhite);
        var docWhite = JsonDocument.Parse(JsonSerializer.Serialize(okWhite.Value));
        Assert.True(docWhite.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docWhite.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_WithNonExistentDepartmentCode_ReturnsNullData()
    {
        var res = await _controller.Update(new Mst_Department_UpdateReq { DepartmentCode = "DEPT_NOT_FOUND" });
        var okRes = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_WithValidDepartmentCode_ReturnsDetailAndActiveLookups()
    {
        var req = new Mst_Department_UpdateReq { DepartmentCode = "DEPT_BGD" };
        var res = await _controller.Update(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", data.GetProperty("DepartmentName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());

        // Kiểm tra danh sách phòng ban active (ViewBag.ListMst_Department): chỉ có 2 bản ghi active (DEPT_BGD, DEPT_KT)
        var listDept = data.GetProperty("ListMst_Department");
        Assert.Equal(2, listDept.GetArrayLength());
        Assert.Equal("DEPT_BGD", listDept[0].GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_KT", listDept[1].GetProperty("DepartmentCode").GetString());

        // Kiểm tra danh sách NNT active (ViewBag.ListMst_NNT): chỉ có 2 bản ghi active (0100000001, 0100000002)
        var listNNT = data.GetProperty("ListMst_NNT");
        Assert.Equal(2, listNNT.GetArrayLength());
        Assert.Equal("0100000001", listNNT[0].GetProperty("MST").GetString());
        Assert.Equal("0100000002", listNNT[1].GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Update_WithModelJson_ReturnsCorrectDetail()
    {
        var modelJson = JsonSerializer.Serialize(new { DepartmentCode = "DEPT_KT" });
        var req = new Mst_Department_UpdateReq { Model = modelJson };
        var res = await _controller.Update(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("DEPT_KT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Phòng Kế Toán", data.GetProperty("DepartmentName").GetString());
        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCodeParent").GetString());
    }

    [Fact]
    public async Task Update_CaseInsensitiveDepartmentCode_ReturnsDetail()
    {
        var req = new Mst_Department_UpdateReq { DepartmentCode = "dept_bgd" };
        var res = await _controller.Update(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", data.GetProperty("DepartmentName").GetString());
    }

    [Fact]
    public async Task Update_WhenInactiveDepartment_StillReturnsItselfWithActiveLookups()
    {
        // Khi departmentcode là phòng ban không active (DEPT_KD_INACT), bản ghi vẫn được trả về để hiển thị
        // Nhưng danh sách ListMst_Department và ListMst_NNT vẫn chỉ gồm các bản ghi active
        var req = new Mst_Department_UpdateReq { DepartmentCode = "DEPT_KD_INACT" };
        var res = await _controller.Update(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("DEPT_KD_INACT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("0", data.GetProperty("FlagActive").GetString());

        var listDept = data.GetProperty("ListMst_Department");
        Assert.Equal(2, listDept.GetArrayLength());
        var listNNT = data.GetProperty("ListMst_NNT");
        Assert.Equal(2, listNNT.GetArrayLength());
    }

    [Fact]
    public async Task Edit_NullBody_ReturnsFail_Mst_Department_Update()
    {
        var result = await _controller.Edit(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_Update", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strJson", out var p));
        Assert.Equal(JsonValueKind.Null, p.ValueKind);
    }

    [Fact]
    public async Task Edit_EmptyBodyOrModel_ReturnsOk_DepartmentEmptyMessage()
    {
        // 1. Body rỗng không có thông tin
        var resEmpty = await _controller.Edit(new Mst_Department_EditReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Bộ phận phòng ban trống!", docEmpty.RootElement.GetProperty("Messages")[0].GetString());

        // 2. Chuỗi Model rỗng
        var resModelEmpty = await _controller.Edit(new Mst_Department_EditReq { Model = "" });
        var okModelEmpty = Assert.IsType<OkObjectResult>(resModelEmpty);
        var docModelEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okModelEmpty.Value));
        Assert.True(docModelEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Bộ phận phòng ban trống!", docModelEmpty.RootElement.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Edit_NonExistentDepartmentCode_ReturnsOk_DepartmentNotFoundMessage()
    {
        var req = new Mst_Department_EditReq
        {
            DepartmentCode = "DEPT_NONEXIST",
            DepartmentName = "Phòng không tồn tại",
            MST = "0100000001"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã bộ phận phòng ban 'DEPT_NONEXIST' không có trong hệ thống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Edit_MSTNotFound_ReturnsFail_Mst_NNT_CheckDB_NNTNotFound()
    {
        var req = new Mst_Department_EditReq
        {
            DepartmentCode = "DEPT_KT",
            DepartmentName = "Phòng Kế Toán",
            MST = "9999999999"
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("9999999999", root.GetProperty("Params").GetProperty("Check.MST").GetString());
    }

    [Fact]
    public async Task Edit_MSTInactive_ReturnsFail_Mst_NNT_CheckDB_FlagActiveNotMatched()
    {
        var req = new Mst_Department_EditReq
        {
            DepartmentCode = "DEPT_KT",
            DepartmentName = "Phòng Kế Toán",
            MST = "0100000003" // Seeded inactive (FlagActive = "0") in Mst_NNTSeeder
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("0100000003", root.GetProperty("Params").GetProperty("Check.MST").GetString());
        Assert.Equal("0", root.GetProperty("Params").GetProperty("DB.FlagActive").GetString());
    }

    [Fact]
    public async Task Edit_EmptyDepartmentName_ReturnsFail_Mst_Department_UpdateX_InvalidDepartmentName()
    {
        var req = new Mst_Department_EditReq
        {
            DepartmentCode = "DEPT_KT",
            DepartmentName = "",
            MST = "0100000001"
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Department_UpdateX_InvalidDepartmentName", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_Success_RoundTrip()
    {
        var req = new Mst_Department_EditReq
        {
            DepartmentCode = "DEPT_KT",
            DepartmentName = "Phòng Kế Toán Đã Sửa",
            MST = "0100000001",
            FlagActive = "1"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Sửa bộ phận phòng ban thành công!", root.GetProperty("Messages")[0].GetString());
        Assert.Equal("/Mst_Department", root.GetProperty("RedirectUrl").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("DEPT_KT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Phòng Kế Toán Đã Sửa", data.GetProperty("DepartmentName").GetString());
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());

        // Đọc lại từ DB để xác minh round-trip
        var deptInDb = await _db.Set<Mst_Department>().FirstAsync(x => x.DepartmentCode == "DEPT_KT");
        Assert.Equal("Phòng Kế Toán Đã Sửa", deptInDb.DepartmentName);
        Assert.Equal("0100000001", deptInDb.MST);
        Assert.Equal("1", deptInDb.FlagActive);
    }

    [Fact]
    public async Task Edit_WithModelJson_Success_RoundTrip()
    {
        var modelJson = JsonSerializer.Serialize(new
        {
            DepartmentCode = "DEPT_KD_INACT",
            DepartmentName = "Phòng Kinh Doanh Kích Hoạt",
            MST = "0100000002",
            FlagActive = "1"
        });

        var req = new Mst_Department_EditReq { Model = modelJson };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Sửa bộ phận phòng ban thành công!", root.GetProperty("Messages")[0].GetString());

        // Đọc lại từ DB xác minh chuyển từ inactive "0" sang active "1" và tên mới
        var deptInDb = await _db.Set<Mst_Department>().FirstAsync(x => x.DepartmentCode == "DEPT_KD_INACT");
        Assert.Equal("Phòng Kinh Doanh Kích Hoạt", deptInDb.DepartmentName);
        Assert.Equal("0100000002", deptInDb.MST);
        Assert.Equal("1", deptInDb.FlagActive);
    }

    [Fact]
    public async Task Detail_WithNullOrEmptyBody_ReturnsNullData()
    {
        // Body null (Gate: Mst_DepartmentController.cs:361, 417)
        var resNull = await _controller.Detail(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docNull.RootElement.GetProperty("Data").ValueKind);

        // Body rỗng
        var resEmpty = await _controller.Detail(new Mst_Department_DetailReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Data").ValueKind);

        // DepartmentCode chuỗi trắng
        var resWhite = await _controller.Detail(new Mst_Department_DetailReq { DepartmentCode = "   " });
        var okWhite = Assert.IsType<OkObjectResult>(resWhite);
        var docWhite = JsonDocument.Parse(JsonSerializer.Serialize(okWhite.Value));
        Assert.True(docWhite.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docWhite.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_WithNonExistentDepartmentCode_ReturnsNullData()
    {
        var res = await _controller.Detail(new Mst_Department_DetailReq { DepartmentCode = "DEPT_NOT_FOUND" });
        var okRes = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_WithValidDepartmentCode_ReturnsDetailAndActiveLookups()
    {
        var req = new Mst_Department_DetailReq { DepartmentCode = "DEPT_BGD" };
        var res = await _controller.Detail(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var data = root.GetProperty("Data");
        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", data.GetProperty("DepartmentName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0101234567", data.GetProperty("MST").GetString());

        // Kiểm tra danh sách phòng ban active (ViewBag.ListMst_Department): chỉ có 2 bản ghi active (DEPT_BGD, DEPT_KT)
        var listDept = data.GetProperty("ListMst_Department");
        Assert.Equal(2, listDept.GetArrayLength());
        Assert.Equal("DEPT_BGD", listDept[0].GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_KT", listDept[1].GetProperty("DepartmentCode").GetString());

        // Kiểm tra danh sách NNT active (ViewBag.ListMst_NNT): chỉ có 2 bản ghi active (0100000001, 0100000002)
        var listNNT = data.GetProperty("ListMst_NNT");
        Assert.Equal(2, listNNT.GetArrayLength());
        Assert.Equal("0100000001", listNNT[0].GetProperty("MST").GetString());
        Assert.Equal("0100000002", listNNT[1].GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Detail_WithModelJson_ReturnsCorrectDetail()
    {
        var modelJson = JsonSerializer.Serialize(new { DepartmentCode = "DEPT_KT" });
        var req = new Mst_Department_DetailReq { Model = modelJson };
        var res = await _controller.Detail(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("DEPT_KT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Phòng Kế Toán", data.GetProperty("DepartmentName").GetString());
        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCodeParent").GetString());
    }

    [Fact]
    public async Task Detail_CaseInsensitiveDepartmentCode_ReturnsDetail()
    {
        var req = new Mst_Department_DetailReq { DepartmentCode = "dept_bgd" };
        var res = await _controller.Detail(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("DEPT_BGD", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("Ban Giám Đốc", data.GetProperty("DepartmentName").GetString());
    }

    [Fact]
    public async Task Detail_WhenInactiveDepartment_StillReturnsItselfWithActiveLookups()
    {
        // Khi departmentcode là phòng ban không active (DEPT_KD_INACT), bản ghi vẫn được trả về để xem chi tiết
        // Nhưng danh sách ListMst_Department và ListMst_NNT vẫn chỉ gồm các bản ghi active (Mst_DepartmentController.cs:398, 407)
        var req = new Mst_Department_DetailReq { DepartmentCode = "DEPT_KD_INACT" };
        var res = await _controller.Detail(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("DEPT_KD_INACT", data.GetProperty("DepartmentCode").GetString());
        Assert.Equal("0", data.GetProperty("FlagActive").GetString());

        var listDept = data.GetProperty("ListMst_Department");
        Assert.Equal(2, listDept.GetArrayLength());
        var listNNT = data.GetProperty("ListMst_NNT");
        Assert.Equal(2, listNNT.GetArrayLength());
    }

    [Fact]
    public async Task Detail_TwoDifferentDepartmentCodes_ReturnDifferentResults()
    {
        // Bộ lọc 1: DEPT_BGD
        var req1 = new Mst_Department_DetailReq { DepartmentCode = "DEPT_BGD" };
        var res1 = await _controller.Detail(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var data1 = doc1.RootElement.GetProperty("Data");

        // Bộ lọc 2: DEPT_KT
        var req2 = new Mst_Department_DetailReq { DepartmentCode = "DEPT_KT" };
        var res2 = await _controller.Detail(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var data2 = doc2.RootElement.GetProperty("Data");

        // So sánh 2 kết quả khác nhau
        Assert.NotEqual(data1.GetProperty("DepartmentCode").GetString(), data2.GetProperty("DepartmentCode").GetString());
        Assert.NotEqual(data1.GetProperty("DepartmentName").GetString(), data2.GetProperty("DepartmentName").GetString());
        Assert.Equal("DEPT_BGD", data1.GetProperty("DepartmentCode").GetString());
        Assert.Equal("DEPT_KT", data2.GetProperty("DepartmentCode").GetString());
    }

    [Fact]
    public async Task Delete_WithNullOrEmptyBody_ReturnsOk_DepartmentEmptyMessage()
    {
        // 1. Body null (Gate: Mst_DepartmentController.cs:430, 480-482)
        var resNull = await _controller.Delete(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Bộ phận phòng ban trống!", docNull.RootElement.GetProperty("Messages")[0].GetString());

        // 2. Body rỗng
        var resEmpty = await _controller.Delete(new Mst_Department_DeleteReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Bộ phận phòng ban trống!", docEmpty.RootElement.GetProperty("Messages")[0].GetString());

        // 3. DepartmentCode chuỗi trắng
        var resWhite = await _controller.Delete(new Mst_Department_DeleteReq { DepartmentCode = "   " });
        var okWhite = Assert.IsType<OkObjectResult>(resWhite);
        var docWhite = JsonDocument.Parse(JsonSerializer.Serialize(okWhite.Value));
        Assert.True(docWhite.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Bộ phận phòng ban trống!", docWhite.RootElement.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Delete_WithNonExistentDepartmentCode_ReturnsOk_DepartmentNotFoundMessage()
    {
        // Mã không tồn tại (Gate: Mst_DepartmentController.cs:474-476)
        var req = new Mst_Department_DeleteReq { DepartmentCode = "DEPT_NONEXIST" };
        var res = await _controller.Delete(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã bộ phận phong ban'DEPT_NONEXIST' không có trong hệ thống!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task Delete_Success_RoundTrip()
    {
        // Tạo trước phòng ban để xóa
        _db.Set<Mst_Department>().Add(new Mst_Department
        {
            DepartmentCode = "DEPT_DEL_TEST",
            DepartmentName = "Phòng Chuẩn Bị Xóa",
            MST = "0101234567",
            FlagActive = "1"
        });
        await _db.SaveChangesAsync();

        // Kiểm tra tồn tại trước khi xóa
        var beforeDel = await _db.Set<Mst_Department>().FirstOrDefaultAsync(x => x.DepartmentCode == "DEPT_DEL_TEST");
        Assert.NotNull(beforeDel);

        // Gọi action Delete (Gate: Mst_DepartmentController.cs:466-471)
        var req = new Mst_Department_DeleteReq { DepartmentCode = "DEPT_DEL_TEST" };
        var res = await _controller.Delete(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa bộ phận phòng ban thành công!", root.GetProperty("Messages")[0].GetString());
        Assert.Equal("/Mst_Department", root.GetProperty("RedirectUrl").GetString());

        // Đọc lại từ DB để xác minh round-trip xóa thật
        var afterDel = await _db.Set<Mst_Department>().FirstOrDefaultAsync(x => x.DepartmentCode == "DEPT_DEL_TEST");
        Assert.Null(afterDel);
    }

    [Fact]
    public async Task Delete_WithModelJson_Success_RoundTrip()
    {
        // Tạo trước phòng ban
        _db.Set<Mst_Department>().Add(new Mst_Department
        {
            DepartmentCode = "DEPT_DEL_JSON",
            DepartmentName = "Phòng Xóa Bằng Model JSON",
            MST = "0101234567",
            FlagActive = "1"
        });
        await _db.SaveChangesAsync();

        var modelJson = JsonSerializer.Serialize(new { DepartmentCode = "DEPT_DEL_JSON" });
        var req = new Mst_Department_DeleteReq { Model = modelJson };
        var res = await _controller.Delete(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa bộ phận phòng ban thành công!", root.GetProperty("Messages")[0].GetString());

        // Xác minh không còn trong DB
        var afterDel = await _db.Set<Mst_Department>().FirstOrDefaultAsync(x => x.DepartmentCode == "DEPT_DEL_JSON");
        Assert.Null(afterDel);
    }

    [Fact]
    public async Task Delete_CaseInsensitiveDepartmentCode_Success_RoundTrip()
    {
        // Tạo phòng ban với mã hoa
        _db.Set<Mst_Department>().Add(new Mst_Department
        {
            DepartmentCode = "DEPT_DEL_CASE",
            DepartmentName = "Phòng Xóa Phân Biệt Hoa Thường",
            MST = "0101234567",
            FlagActive = "1"
        });
        await _db.SaveChangesAsync();

        // Gửi mã viết thường
        var req = new Mst_Department_DeleteReq { DepartmentCode = "dept_del_case" };
        var res = await _controller.Delete(req);
        var okRes = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okRes.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa bộ phận phòng ban thành công!", root.GetProperty("Messages")[0].GetString());

        // Xác minh đã bị xóa
        var afterDel = await _db.Set<Mst_Department>().FirstOrDefaultAsync(x => x.DepartmentCode == "DEPT_DEL_CASE");
        Assert.Null(afterDel);
    }

    [Fact]
    public async Task Export_EmptyBody_ReturnsExcelSuccessWith3Rows()
    {
        var result = await _controller.Export(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        var strUrl = root.GetProperty("strUrl").GetString();
        Assert.False(string.IsNullOrWhiteSpace(strUrl));

        // Kiểm tra file vật lý đã được tạo và đọc nội dung
        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath), $"Exported file does not exist at {physicalPath}");

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal("Mst_Department", ws.Name);
        Assert.Equal("Mã bộ phận (*)", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên bộ phận (*)", ws.Cell(1, 2).GetString());
        Assert.Equal("Bộ phận quản lý (*)", ws.Cell(1, 3).GetString());
        Assert.Equal("Mã đơn vị (*)", ws.Cell(1, 4).GetString());
        Assert.Equal("Trạng thái", ws.Cell(1, 5).GetString());

        // 3 dòng dữ liệu từ seeder (1 header + 3 rows)
        Assert.Equal(4, ws.LastRowUsed()?.RowNumber());
    }

    [Fact]
    public async Task Export_FilterByFlagActive1_Returns2Rows()
    {
        var req = new Mst_Department_ExportReq { FlagActive = "1" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal(3, ws.LastRowUsed()?.RowNumber()); // 1 header + 2 rows
    }

    [Fact]
    public async Task Export_FilterByDepartmentCode_DEPT_BGD_Returns1Row()
    {
        var req = new Mst_Department_ExportReq { DepartmentCode = "DEPT_BGD" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal(2, ws.LastRowUsed()?.RowNumber()); // 1 header + 1 row
        Assert.Equal("DEPT_BGD", ws.Cell(2, 1).GetString());
        Assert.Equal("Ban Giám Đốc", ws.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task Export_FilterNonExistent_ReturnsNoDataMessage()
    {
        var req = new Mst_Department_ExportReq { DepartmentCode = "DEPT_NONEXIST" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Không có dữ liệu!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        Assert.False(root.TryGetProperty("strUrl", out _));
    }

    [Fact]
    public async Task Export_Pagination_Start0_Count1_Returns1Row()
    {
        var req = new Mst_Department_ExportReq
        {
            Ft_RecordStart = 0,
            Ft_RecordCount = 1
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal(2, ws.LastRowUsed()?.RowNumber()); // 1 header + 1 row
        Assert.Equal("DEPT_BGD", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Port()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_department_{Guid.NewGuid():N}.db");
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

            // Ca 1: POST empty body -> 200 OK, Success = true, Data = null
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_department/detail", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc1.RootElement.GetProperty("Data").ValueKind);

            // Ca 2: POST mã không tồn tại -> 200 OK, Success = true, Data = null
            var req2 = new StringContent("{\"DepartmentCode\":\"DEPT_NONEXISTENT\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_department/detail", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").ValueKind);

            // Ca 3: POST mã hợp lệ DEPT_BGD -> 200 OK, Success = true, trả về chi tiết và danh sách lookup
            var req3 = new StringContent("{\"DepartmentCode\":\"DEPT_BGD\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_department/detail", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal("DEPT_BGD", data3.GetProperty("DepartmentCode").GetString());
            Assert.Equal("Ban Giám Đốc", data3.GetProperty("DepartmentName").GetString());
            Assert.Equal(2, data3.GetProperty("ListMst_Department").GetArrayLength());
            Assert.Equal(2, data3.GetProperty("ListMst_NNT").GetArrayLength());

            // Ca 4: POST mã khác DEPT_KT -> 200 OK, trả về đúng phòng ban Kế Toán
            var req4 = new StringContent("{\"DepartmentCode\":\"DEPT_KT\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_department/detail", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal("DEPT_KT", data4.GetProperty("DepartmentCode").GetString());
            Assert.Equal("Phòng Kế Toán", data4.GetProperty("DepartmentName").GetString());

            // Ca 5: POST /api/mst_department/delete với body rỗng -> 200 OK, Success = true, Bộ phận phòng ban trống!
            var req5 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_department/delete", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 5 POST /api/mst_department/delete (empty body)] Status: {(int)res5.StatusCode} Response: {body5}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Bộ phận phòng ban trống!", doc5.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 6: POST /api/mst_department/delete với mã không tồn tại -> 200 OK, Success = true, thông báo không có trong hệ thống
            var req6 = new StringContent("{\"DepartmentCode\":\"DEPT_SRV_NONEXIST\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_department/delete", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 6 POST /api/mst_department/delete (non-existent code)] Status: {(int)res6.StatusCode} Response: {body6}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã bộ phận phong ban'DEPT_SRV_NONEXIST' không có trong hệ thống!", doc6.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 7: POST /api/mst_department/create tạo mới DEPT_SRV_DEL, sau đó xóa và xác minh round-trip
            var createJson = JsonSerializer.Serialize(new
            {
                DepartmentCode = "DEPT_SRV_DEL",
                DepartmentName = "Phòng Xóa Server Thật",
                MST = "0100000001",
                DepartmentCodeParent = "DEPT_BGD"
            });
            var reqCreate = new StringContent(createJson, Encoding.UTF8, "application/json");
            var resCreate = await http.PostAsync("/api/mst_department/create", reqCreate);
            var bodyCreate = await resCreate.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 7a POST /api/mst_department/create] Status: {(int)resCreate.StatusCode} Response: {bodyCreate}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resCreate.StatusCode);

            // Xóa DEPT_SRV_DEL qua endpoint delete
            var reqDel = new StringContent("{\"DepartmentCode\":\"DEPT_SRV_DEL\"}", Encoding.UTF8, "application/json");
            var resDel = await http.PostAsync("/api/mst_department/delete", reqDel);
            var bodyDel = await resDel.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 7b POST /api/mst_department/delete] Status: {(int)resDel.StatusCode} Response: {bodyDel}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resDel.StatusCode);
            var docDel = JsonDocument.Parse(bodyDel);
            Assert.True(docDel.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa bộ phận phòng ban thành công!", docDel.RootElement.GetProperty("Messages")[0].GetString());

            // Đọc lại qua detail -> Data = null (round-trip đã xóa thành công trên server thật)
            var reqVerify = new StringContent("{\"DepartmentCode\":\"DEPT_SRV_DEL\"}", Encoding.UTF8, "application/json");
            var resVerify = await http.PostAsync("/api/mst_department/detail", reqVerify);
            var bodyVerify = await resVerify.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 7c POST /api/mst_department/detail (after delete)] Status: {(int)resVerify.StatusCode} Response: {bodyVerify}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resVerify.StatusCode);
            var docVerify = JsonDocument.Parse(bodyVerify);
            Assert.True(docVerify.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docVerify.RootElement.GetProperty("Data").ValueKind);

            // Ca 8: POST /api/mst_department/export với body rỗng -> 200 OK, Success = true, Title = "Đã xuất excel thành công!", strUrl
            var req8 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_department/export", req8);
            var body8 = await res8.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 8 POST /api/mst_department/export (empty body)] Status: {(int)res8.StatusCode} Response: {body8}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var doc8 = JsonDocument.Parse(body8);
            Assert.True(doc8.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc8.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc8.RootElement.GetProperty("CheckSuccess").GetString());
            var exportUrl = doc8.RootElement.GetProperty("strUrl").GetString();
            Assert.False(string.IsNullOrWhiteSpace(exportUrl));

            // Ca 9: POST /api/mst_department/export với mã không tồn tại -> 200 OK, Success = true, Title = "Không có dữ liệu!"
            var req9 = new StringContent("{\"DepartmentCode\":\"DEPT_SRV_NONEXIST\"}", Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/mst_department/export", req9);
            var body9 = await res9.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 9 POST /api/mst_department/export (non-existent filter)] Status: {(int)res9.StatusCode} Response: {body9}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res9.StatusCode);
            var doc9 = JsonDocument.Parse(body9);
            Assert.True(doc9.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Không có dữ liệu!", doc9.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc9.RootElement.GetProperty("CheckSuccess").GetString());

            // Ca 10: POST /api/mst_department/export với 2 bộ lọc ra số dòng khác nhau
            // Bộ lọc active -> xuất 2 bản ghi
            var req10a = new StringContent("{\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var res10a = await http.PostAsync("/api/mst_department/export", req10a);
            var body10a = await res10a.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 10a POST /api/mst_department/export (FlagActive=1)] Status: {(int)res10a.StatusCode} Response: {body10a}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res10a.StatusCode);
            var doc10a = JsonDocument.Parse(body10a);
            Assert.True(doc10a.RootElement.GetProperty("Success").GetBoolean());
            var url10a = doc10a.RootElement.GetProperty("strUrl").GetString();

            // Bộ lọc DepartmentCode = DEPT_BGD -> xuất 1 bản ghi
            var req10b = new StringContent("{\"DepartmentCode\":\"DEPT_BGD\"}", Encoding.UTF8, "application/json");
            var res10b = await http.PostAsync("/api/mst_department/export", req10b);
            var body10b = await res10b.Content.ReadAsStringAsync();
            _output?.WriteLine($"[Ca 10b POST /api/mst_department/export (DepartmentCode=DEPT_BGD)] Status: {(int)res10b.StatusCode} Response: {body10b}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res10b.StatusCode);
            var doc10b = JsonDocument.Parse(body10b);
            Assert.True(doc10b.RootElement.GetProperty("Success").GetBoolean());
            var url10b = doc10b.RootElement.GetProperty("strUrl").GetString();

            // Xác minh 2 tệp excel khác nhau được tạo ra
            Assert.NotEqual(url10a, url10b);
        }
        finally
        {
            try { proc.Kill(); } catch { }
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}

