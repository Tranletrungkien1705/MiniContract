using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class Mst_DepartmentControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_DepartmentControllerPort _controller;

    public Mst_DepartmentControllerPortTests()
    {
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
}

