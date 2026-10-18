using System.Diagnostics;
using System.Net.Http;
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

public class Mst_OrganizationControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_OrganizationControllerPort _controller;

    public Mst_OrganizationControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_Area trước (Order = 20) rồi Mst_Org (Order = 22), Mst_NNT (Order = 25), Mst_OrgSignConfig (Order = 40), Mst_SignType (Order = 46)
        new Mst_AreaSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_OrgSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_OrgSignConfigSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_SignTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        var config = new Microsoft.Extensions.Configuration.ConfigurationManager();
        config["Biz_NetworkID"] = "1001";

        _controller = new Mst_OrganizationControllerPort(_db, config);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_EmptyBody_ReturnsAllRecords()
    {
        var result = await _controller.Index(null);
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

        // Kiểm tra thứ tự sắp xếp MST asc
        Assert.Equal("0100000001", list[0].GetProperty("MST").GetString());
        Assert.Equal("0100000002", list[1].GetProperty("MST").GetString());
        Assert.Equal("0100000003", list[2].GetProperty("MST").GetString());

        // Kiểm tra ghép bảng Mst_Area
        Assert.Equal("Miền Bắc", list[0].GetProperty("ma_AreaName").GetString());
        Assert.Equal("Miền Nam", list[1].GetProperty("ma_AreaName").GetString());
    }

    [Fact]
    public async Task Index_Paging_PageSize1_ReturnsCorrectPages()
    {
        // Trang 0
        var res0 = await _controller.Index(new Mst_Organization_IndexReq { RecordCount = 1, Page = 0 });
        var ok0 = Assert.IsType<OkObjectResult>(res0);
        var doc0 = JsonDocument.Parse(JsonSerializer.Serialize(ok0.Value));
        var data0 = doc0.RootElement.GetProperty("Data");

        Assert.Equal(3, data0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data0.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data0.GetProperty("PageIndex").GetInt32());
        Assert.Equal(1, data0.GetProperty("PageSize").GetInt32());
        var list0 = data0.GetProperty("DataList");
        Assert.Equal(1, list0.GetArrayLength());
        Assert.Equal("0100000001", list0[0].GetProperty("MST").GetString());

        // Trang 1
        var res1 = await _controller.Index(new Mst_Organization_IndexReq { RecordCount = 1, Page = 1 });
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var data1 = doc1.RootElement.GetProperty("Data");

        Assert.Equal(3, data1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data1.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, data1.GetProperty("PageIndex").GetInt32());
        var list1 = data1.GetProperty("DataList");
        Assert.Equal(1, list1.GetArrayLength());
        Assert.Equal("0100000002", list1[0].GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Index_FilterByOrgID_ReturnsDifferentCounts()
    {
        // Lọc OrgID 7200000001 -> 2 dòng
        var res1 = await _controller.Index(new Mst_Organization_IndexReq { OrgID = "7200000001" });
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var data1 = doc1.RootElement.GetProperty("Data");
        Assert.Equal(2, data1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data1.GetProperty("DataList").GetArrayLength());

        // Lọc OrgID 7200000002 -> 1 dòng
        var res2 = await _controller.Index(new Mst_Organization_IndexReq { OrgID = "7200000002" });
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var data2 = doc2.RootElement.GetProperty("Data");
        Assert.Equal(1, data2.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data2.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByFlagActive_ReturnsMatching()
    {
        // Lọc FlagActive = "1" -> 2 dòng
        var res1 = await _controller.Index(new Mst_Organization_IndexReq { FlagActive = "1" });
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var data1 = doc1.RootElement.GetProperty("Data");
        Assert.Equal(2, data1.GetProperty("ItemCount").GetInt32());

        // Lọc FlagActive = "0" -> 1 dòng
        var res0 = await _controller.Index(new Mst_Organization_IndexReq { FlagActive = "0" });
        var ok0 = Assert.IsType<OkObjectResult>(res0);
        var doc0 = JsonDocument.Parse(JsonSerializer.Serialize(ok0.Value));
        var data0 = doc0.RootElement.GetProperty("Data");
        Assert.Equal(1, data0.GetProperty("ItemCount").GetInt32());
        Assert.Equal("0100000003", data0.GetProperty("DataList")[0].GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Index_FilterByKeyWord_ReturnsMatching()
    {
        var res = await _controller.Index(new Mst_Organization_IndexReq { KeyWord = "Hà Nội" });
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal("0100000001", data.GetProperty("DataList")[0].GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Index_PageSizeZero_ReturnsEmptyListWithTotalCount()
    {
        var res = await _controller.Index(new Mst_Organization_IndexReq { RecordCount = 0 });
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Create_NullBody_FailsWithCheckStrJson()
    {
        var result = await _controller.Create(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Create", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_EmptyBody_FailsWithCheckStrJson()
    {
        var req = new Mst_Organization_CreateReq();
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Create", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_MissingMST_FailsWithInvalidMST()
    {
        var req = new Mst_Organization_CreateReq
        {
            OrgID = "7200000099",
            NNTFullName = "Công ty TNHH Mới"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Create_InvalidMST", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_MissingNNTFullName_FailsWithInvalidNNTFullName()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000099",
            OrgID = "7200000099",
            NNTFullName = ""
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Create_InvalidNNTFullName", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_MissingOrgID_FailsWithInvalidOrgID()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000099",
            NNTFullName = "Công ty TNHH Mới",
            OrgID = ""
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Org_Create_InvalidOrgID", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_ExistingMST_FailsWithNNTExist()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000001",
            OrgID = "7200000099",
            NNTFullName = "Chi nhánh Trùng MST"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTExist", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_ExistingOrgID_FailsWithOrgExist()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000099",
            OrgID = "7200000001",
            NNTFullName = "Chi nhánh Trùng OrgID"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Org_CheckDB_OrgExist", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_NonExistentParent_FailsWithNNTNotFound()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000099",
            OrgID = "7200000099",
            NNTFullName = "Chi nhánh Cha Không Tồn Tại",
            MSTParent = "9999999999"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTNotFound", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_InactiveParent_FailsWithFlagActiveNotMatched()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000099",
            OrgID = "7200000099",
            NNTFullName = "Chi nhánh Cha Bị Khoá",
            MSTParent = "0100000003" // Seed có FlagActive = "0"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_CertMismatch_ReturnsWarningMessage()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000099",
            OrgID = "7200000099",
            NNTFullName = "Chi nhánh Test CKS",
            CANumber = "CA001",
            MSTToken = "0100000088"
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("MST trong chứng thư số không trùng với MST của chi nhánh!", root.GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Create_Success_RoundTrip()
    {
        var req = new Mst_Organization_CreateReq
        {
            MST = "0100000088",
            OrgID = "7200000088",
            NNTFullName = "Chi nhánh Hải Phòng",
            NNTShortName = "CN Hải Phòng",
            MSTParent = "0100000001",
            ProvinceCode = "31",
            DistrictCode = "303",
            AreaCode = "MB",
            Remark = "Tạo mới chi nhánh HP"
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Tạo chi nhánh thành công!", root.GetProperty("Message").GetString());

        // Kiểm tra round-trip ghi vào CSDL cho cả 2 bảng Mst_NNT và Mst_Org
        var nntInDb = await _db.Set<Mst_NNT>().FirstOrDefaultAsync(x => x.MST == "0100000088");
        Assert.NotNull(nntInDb);
        Assert.Equal("7200000088", nntInDb.OrgID);
        Assert.Equal("Chi nhánh Hải Phòng", nntInDb.NNTFullName);
        Assert.Equal("1", nntInDb.FlagActive);
        Assert.Equal("0100000001", nntInDb.MSTParent);

        var orgInDb = await _db.Set<Mst_Org>().FirstOrDefaultAsync(x => x.OrgID == "7200000088");
        Assert.NotNull(orgInDb);
        Assert.Equal("1", orgInDb.FlagActive);
        Assert.Equal("7200000001", orgInDb.OrgParent);
        Assert.Equal("0100000088", orgInDb.MST);
    }

    [Fact]
    public async Task Update_NullBody_ReturnsSuccessWithNullData()
    {
        var result = await _controller.Update(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_EmptyBody_ReturnsSuccessWithNullData()
    {
        var req = new Mst_Organization_UpdateReq();
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_NonExistentMST_ReturnsSuccessWithNullData()
    {
        var req = new Mst_Organization_UpdateReq { MST = "9999999999" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ExistingMST1_ReturnsDetailsAndAncillaryLists()
    {
        var req = new Mst_Organization_UpdateReq { MST = "0100000001" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("Công ty TNHH Giải Pháp Số Hà Nội", data.GetProperty("NNTFullName").GetString());
        Assert.Equal("Miền Bắc", data.GetProperty("ma_AreaName").GetString());

        // Kiểm tra danh sách cấu hình chữ ký số đi kèm (OrgID 7200000001 có 3 bản ghi)
        var signConfigs = data.GetProperty("Lst_Mst_OrgSignConfig");
        Assert.Equal(3, signConfigs.GetArrayLength());

        // Kiểm tra danh sách vùng thị trường active (có 3 bản ghi MB 7200000001, MN 7200000001, MB 7200000002)
        var areas = data.GetProperty("List_Mst_Area");
        Assert.Equal(3, areas.GetArrayLength());
    }

    [Fact]
    public async Task Update_ExistingMST3_ReturnsDifferentSignConfigCount()
    {
        var req = new Mst_Organization_UpdateReq { MST = "0100000003" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("0100000003", data.GetProperty("MST").GetString());
        Assert.Equal("7200000002", data.GetProperty("OrgID").GetString());
        Assert.Equal("Văn phòng Đại diện Miền Trung", data.GetProperty("NNTFullName").GetString());

        // Kiểm tra danh sách cấu hình chữ ký số đi kèm (OrgID 7200000002 có 0 bản ghi)
        var signConfigs = data.GetProperty("Lst_Mst_OrgSignConfig");
        Assert.Equal(0, signConfigs.GetArrayLength());
    }

    [Fact]
    public async Task Edit_NullBody_FailsWithCheckStrJson()
    {
        var result = await _controller.Edit(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Update", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_EmptyBody_FailsWithCheckStrJson()
    {
        var req = new Mst_Organization_EditReq();
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Update", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_MissingMST_ReturnsGateMessage()
    {
        var req = new Mst_Organization_EditReq
        {
            OrgID = "7200000001",
            NNTFullName = "Tên cập nhật không có MST"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Mã chi nhánh trống!", msgs[0].GetString());
    }

    [Fact]
    public async Task Edit_NonExistentMST_ReturnsGateMessage()
    {
        var req = new Mst_Organization_EditReq
        {
            MST = "9999999999",
            OrgID = "7299999999",
            NNTFullName = "Chi nhánh không tồn tại"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Mã chi nhánh '7299999999' không có trong hệ thống!", msgs[0].GetString());
    }

    [Fact]
    public async Task Edit_EmptyNNTFullName_FailsWithInvalidNNTFullName()
    {
        var req = new Mst_Organization_EditReq
        {
            MST = "0100000001",
            NNTFullName = "   "
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Update_InvalidNNTFullName", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_Success_RoundTrip()
    {
        var req = new Mst_Organization_EditReq
        {
            MST = "0100000002",
            NNTFullName = "Chi nhánh TP Hồ Chí Minh - Cập nhật",
            NNTShortName = "CN HCM UPD",
            PresentBy = "Trần Thị B Updated",
            NNTPosition = "Giám Đốc Chi Nhánh",
            ProvinceCode = "79",
            DistrictCode = "760",
            NNTAddress = "Số 100 Nguyễn Huệ, Quận 1, TP Hồ Chí Minh",
            ContactName = "Trần Thị B",
            ContactPhone = "02839999999",
            ContactEmail = "hcm.updated@solution.hn",
            AccNo = "9876543210",
            AccHolder = "Chi nhánh HCM",
            BankName = "Vietcombank",
            SignType = "SERVER",
            FlagActive = "1"
        };

        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Sửa chi nhánh thành công!", msgs[0].GetString());

        // Kiểm tra round-trip ghi lại vào DB
        var nntInDb = await _db.Set<Mst_NNT>().FirstOrDefaultAsync(x => x.MST == "0100000002");
        Assert.NotNull(nntInDb);
        Assert.Equal("Chi nhánh TP Hồ Chí Minh - Cập nhật", nntInDb.NNTFullName);
        Assert.Equal("CN HCM UPD", nntInDb.NNTShortName);
        Assert.Equal("Trần Thị B Updated", nntInDb.PresentBy);
        Assert.Equal("Giám Đốc Chi Nhánh", nntInDb.NNTPosition);
        Assert.Equal("Số 100 Nguyễn Huệ, Quận 1, TP Hồ Chí Minh", nntInDb.NNTAddress);
        Assert.Equal("hcm.updated@solution.hn", nntInDb.ContactEmail);
        Assert.Equal("SERVER", nntInDb.SignType);
    }

    [Fact]
    public async Task Detail_NullBody_ReturnsSuccessWithNullData()
    {
        var result = await _controller.Detail(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_EmptyBody_ReturnsSuccessWithNullData()
    {
        var req = new Mst_Organization_DetailReq();
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_NonExistentMST_ReturnsSuccessWithNullData()
    {
        var req = new Mst_Organization_DetailReq { MST = "9999999999" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ExistingMST1_ReturnsDetailsAndAncillaryLists()
    {
        var req = new Mst_Organization_DetailReq { MST = "0100000001" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("Công ty TNHH Giải Pháp Số Hà Nội", data.GetProperty("NNTFullName").GetString());
        Assert.Equal("Miền Bắc", data.GetProperty("ma_AreaName").GetString());

        // Kiểm tra danh sách cấu hình chữ ký số đi kèm (OrgID 7200000001 có 3 bản ghi)
        var signConfigs = data.GetProperty("Lst_Mst_OrgSignConfig");
        Assert.Equal(3, signConfigs.GetArrayLength());

        // Kiểm tra danh sách vùng thị trường active (có 3 bản ghi)
        var areas = data.GetProperty("List_Mst_Area");
        Assert.Equal(3, areas.GetArrayLength());

        // Kiểm tra danh sách loại ký (NetworkID 1001 có 3 bản ghi)
        var signTypes = data.GetProperty("List_Mst_SignType");
        Assert.Equal(3, signTypes.GetArrayLength());
    }

    [Fact]
    public async Task Detail_ExistingMST3_ReturnsDifferentSignConfigCount()
    {
        var req = new Mst_Organization_DetailReq { MST = "0100000003" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("0100000003", data.GetProperty("MST").GetString());
        Assert.Equal("7200000002", data.GetProperty("OrgID").GetString());
        Assert.Equal("Văn phòng Đại diện Miền Trung", data.GetProperty("NNTFullName").GetString());

        // Kiểm tra danh sách cấu hình chữ ký số đi kèm (OrgID 7200000002 có 0 bản ghi)
        var signConfigs = data.GetProperty("Lst_Mst_OrgSignConfig");
        Assert.Equal(0, signConfigs.GetArrayLength());
    }

    [Fact]
    public async Task Delete_NullBody_FailsWithCheckStrJson()
    {
        var result = await _controller.Delete(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Delete", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_EmptyBody_FailsWithCheckStrJson()
    {
        var req = new Mst_Organization_DeleteReq();
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Delete", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_MissingMST_ReturnsGateMessage()
    {
        var req = new Mst_Organization_DeleteReq
        {
            OrgID = "7200000001"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Mã chi nhánh trống!", msgs[0].GetString());
    }

    [Fact]
    public async Task Delete_MissingOrgID_ReturnsGateMessage()
    {
        var req = new Mst_Organization_DeleteReq
        {
            MST = "0100000001"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Mã chi nhánh trống!", msgs[0].GetString());
    }

    [Fact]
    public async Task Delete_NonExistentMST_ReturnsGateMessage()
    {
        var req = new Mst_Organization_DeleteReq
        {
            MST = "9999999999",
            OrgID = "7299999999"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Mã chi nhánh '7299999999' không có trong hệ thống!", msgs[0].GetString());
    }

    [Fact]
    public async Task Delete_ViaModelJson_Success()
    {
        var newNnt = new Mst_NNT
        {
            MST = "0100000077",
            OrgID = "7200000077",
            NNTFullName = "Chi nhánh Test Model JSON",
            NetworkID = "1001",
            FlagActive = "1"
        };
        _db.Set<Mst_NNT>().Add(newNnt);
        await _db.SaveChangesAsync();

        var jsonModel = JsonSerializer.Serialize(new
        {
            MST = "0100000077",
            OrgID = "7200000077"
        });

        var req = new Mst_Organization_DeleteReq
        {
            Model = jsonModel
        };

        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Xóa chi nhánh '7200000077' thành công!", msgs[0].GetString());

        var inDb = await _db.Set<Mst_NNT>().FirstOrDefaultAsync(x => x.MST == "0100000077");
        Assert.Null(inDb);
    }

    [Fact]
    public async Task Delete_Success_RoundTrip()
    {
        var existing = await _db.Set<Mst_NNT>().FirstOrDefaultAsync(x => x.MST == "0100000003");
        Assert.NotNull(existing);

        var req = new Mst_Organization_DeleteReq
        {
            MST = "0100000003",
            OrgID = "7200000002"
        };

        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Equal("Xóa chi nhánh '7200000002' thành công!", msgs[0].GetString());
        Assert.Equal("/Mst_Organization", root.GetProperty("RedirectUrl").GetString());

        var inDbAfter = await _db.Set<Mst_NNT>().FirstOrDefaultAsync(x => x.MST == "0100000003");
        Assert.Null(inDbAfter);
    }

    [Fact]
    public async Task Export_EmptyBody_ReturnsExcelMultiSheetWithAllRows()
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

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath), $"Exported file does not exist at {physicalPath}");

        using var wb = new XLWorkbook(physicalPath);

        // Sheet 1: Mst_NNT (25 cột tiêu đề, 3 dòng dữ liệu)
        var wsNNT = wb.Worksheet(1);
        Assert.Equal("Mst_NNT", wsNNT.Name);
        Assert.Equal("Mã tổ chức", wsNNT.Cell(1, 1).GetString());
        Assert.Equal("Tên tổ chức", wsNNT.Cell(1, 2).GetString());
        Assert.Equal("Tên viết tắt", wsNNT.Cell(1, 3).GetString());
        Assert.Equal("MST", wsNNT.Cell(1, 4).GetString());
        Assert.Equal("Người đại diện", wsNNT.Cell(1, 5).GetString());
        Assert.Equal("Địa chỉ", wsNNT.Cell(1, 6).GetString());
        Assert.Equal("Người liên hệ", wsNNT.Cell(1, 7).GetString());
        Assert.Equal("Email liên hệ", wsNNT.Cell(1, 8).GetString());
        Assert.Equal("ĐT liên hệ", wsNNT.Cell(1, 9).GetString());
        Assert.Equal("Vùng thị trường", wsNNT.Cell(1, 10).GetString());
        Assert.Equal("Loại tổ chức", wsNNT.Cell(1, 11).GetString());
        Assert.Equal("Mã đại lý", wsNNT.Cell(1, 12).GetString());
        Assert.Equal("Chức vụ", wsNNT.Cell(1, 13).GetString());
        Assert.Equal("Số giấy tờ", wsNNT.Cell(1, 14).GetString());
        Assert.Equal("Loại giấy tờ", wsNNT.Cell(1, 15).GetString());
        Assert.Equal("Quận/Huyện", wsNNT.Cell(1, 16).GetString());
        Assert.Equal("Tỉnh/Thành", wsNNT.Cell(1, 17).GetString());
        Assert.Equal("Website", wsNNT.Cell(1, 18).GetString());
        Assert.Equal("ĐT cố định", wsNNT.Cell(1, 19).GetString());
        Assert.Equal("ĐT di động", wsNNT.Cell(1, 20).GetString());
        Assert.Equal("Fax", wsNNT.Cell(1, 21).GetString());
        Assert.Equal("Số tài khoản", wsNNT.Cell(1, 22).GetString());
        Assert.Equal("Chủ tài khoản", wsNNT.Cell(1, 23).GetString());
        Assert.Equal("Ngân hàng", wsNNT.Cell(1, 24).GetString());
        Assert.Equal("Trạng thái", wsNNT.Cell(1, 25).GetString());
        Assert.Equal(4, wsNNT.LastRowUsed()?.RowNumber()); // 1 header + 3 rows

        // Sheet 2: Mst_OrgCKS (5 cột tiêu đề, 6 dòng dữ liệu: 3 cho NNT1 + 3 cho NNT2)
        var wsCKS = wb.Worksheet(2);
        Assert.Equal("Mst_OrgCKS", wsCKS.Name);
        Assert.Equal("Mã tổ chức", wsCKS.Cell(1, 1).GetString());
        Assert.Equal("Số Seri", wsCKS.Cell(1, 2).GetString());
        Assert.Equal("Tổ chức cấp CTS", wsCKS.Cell(1, 3).GetString());
        Assert.Equal("Thời gian bắt đầu", wsCKS.Cell(1, 4).GetString());
        Assert.Equal("Thời gian kết thúc", wsCKS.Cell(1, 5).GetString());
        Assert.Equal(7, wsCKS.LastRowUsed()?.RowNumber()); // 1 header + 6 rows
    }

    [Fact]
    public async Task Export_FilterByOrgID_7200000002_Returns1NNTAnd0CKS()
    {
        var req = new Mst_Organization_ExportReq { OrgID = "7200000002" };
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

        var wsNNT = wb.Worksheet(1);
        Assert.Equal(2, wsNNT.LastRowUsed()?.RowNumber()); // 1 header + 1 row
        Assert.Equal("0100000003", wsNNT.Cell(2, 4).GetString());

        var wsCKS = wb.Worksheet(2);
        Assert.Equal(1, wsCKS.LastRowUsed()?.RowNumber()); // 1 header + 0 rows
    }

    [Fact]
    public async Task Export_FilterByFlagActive_1_Returns2NNTAnd6CKS()
    {
        var req = new Mst_Organization_ExportReq { FlagActive = "1" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);

        var wsNNT = wb.Worksheet(1);
        Assert.Equal(3, wsNNT.LastRowUsed()?.RowNumber()); // 1 header + 2 rows

        var wsCKS = wb.Worksheet(2);
        Assert.Equal(7, wsCKS.LastRowUsed()?.RowNumber()); // 1 header + 6 rows
    }

    [Fact]
    public async Task Export_NonExistentFilter_ReturnsNoDataTitle()
    {
        var req = new Mst_Organization_ExportReq { KeyWord = "KhongCoTrongCSDL999" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Không có dữ liệu!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
    }

    [Fact]
    public async Task Export_Pagination_Start0_Count1_Returns1Row()
    {
        var req = new Mst_Organization_ExportReq { RecordCount = 1, Page = 0 };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);

        var wsNNT = wb.Worksheet(1);
        Assert.Equal(2, wsNNT.LastRowUsed()?.RowNumber()); // 1 header + 1 row
        Assert.Equal("0100000001", wsNNT.Cell(2, 4).GetString());

        var wsCKS = wb.Worksheet(2);
        Assert.Equal(4, wsCKS.LastRowUsed()?.RowNumber()); // 1 header + 3 CKS
    }

    [Fact]
    public async Task RealServer_Verification_Port8584()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_test_{Guid.NewGuid():N}.db");
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
        var freePort = FreePort.Get(); psi.Environment["PORT"] = freePort.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort}"), Timeout = TimeSpan.FromSeconds(3) };
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

            // Request 1: POST empty body -> 200 OK với tất cả 3 bản ghi
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_organization/index", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ1 (Empty body): {body1}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(3, doc1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(3, doc1.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());

            // Request 2: POST bộ lọc OrgID 7200000001 -> 200 OK với 2 bản ghi
            var req2 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_organization/index", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ2 (OrgID 7200000001): {body2}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            Assert.Equal(2, doc2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, doc2.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());

            // Request 3: POST bộ lọc OrgID 7200000002 -> 200 OK với 1 bản ghi (2 bộ lọc ra số dòng khác nhau)
            var req3 = new StringContent("{\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_organization/index", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ3 (OrgID 7200000002): {body3}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            Assert.Equal(1, doc3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, doc3.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());

            // Request 4: POST phân trang RecordCount=1, Page=0 -> bản ghi 1
            var req4 = new StringContent("{\"RecordCount\":1,\"Page\":0}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_organization/index", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ4 (Page 0, Size 1): {body4}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            Assert.Equal(1, doc4.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());
            Assert.Equal("0100000001", doc4.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("MST").GetString());

            // Request 5: POST phân trang RecordCount=1, Page=1 -> bản ghi 2
            var req5 = new StringContent("{\"RecordCount\":1,\"Page\":1}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_organization/index", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ5 (Page 1, Size 1): {body5}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var doc5 = JsonDocument.Parse(body5);
            Assert.Equal(1, doc5.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());
            Assert.Equal("0100000002", doc5.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("MST").GetString());

            // Request 6: POST /api/mst_organization/create empty body -> 400 Bad Request
            var req6 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_organization/create", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ6 (Create Empty): {body6}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            Assert.False(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Create", doc6.RootElement.GetProperty("ErrorCode").GetString());

            // Request 7: POST /api/mst_organization/create trùng MST -> 400 Bad Request
            var req7 = new StringContent("{\"MST\":\"0100000001\",\"OrgID\":\"7200000099\",\"NNTFullName\":\"Trùng MST\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_organization/create", req7);
            var body7 = await res7.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ7 (Create Duplicate MST): {body7}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res7.StatusCode);
            var doc7 = JsonDocument.Parse(body7);
            Assert.False(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_CheckDB_NNTExist", doc7.RootElement.GetProperty("ErrorCode").GetString());

            // Request 8: POST /api/mst_organization/create thành công -> 200 OK
            var req8 = new StringContent("{\"MST\":\"0100000099\",\"OrgID\":\"7200000099\",\"NNTFullName\":\"Chi nhánh Cần Thơ\",\"MSTParent\":\"0100000001\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_organization/create", req8);
            var body8 = await res8.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ8 (Create Success): {body8}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var doc8 = JsonDocument.Parse(body8);
            Assert.True(doc8.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Tạo chi nhánh thành công!", doc8.RootElement.GetProperty("Message").GetString());

            // Request 9: POST /api/mst_organization/index đọc lại bản ghi vừa tạo -> 200 OK
            var req9 = new StringContent("{\"OrgID\":\"7200000099\"}", Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/mst_organization/index", req9);
            var body9 = await res9.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ9 (Index Verify Created Org): {body9}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res9.StatusCode);
            var doc9 = JsonDocument.Parse(body9);
            Assert.Equal(1, doc9.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal("0100000099", doc9.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("MST").GetString());
            Assert.Equal("Chi nhánh Cần Thơ", doc9.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("NNTFullName").GetString());

            // Request 10: POST /api/mst_organization/update empty body -> 200 OK với Data null
            var req10 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res10 = await http.PostAsync("/api/mst_organization/update", req10);
            var body10 = await res10.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ10 (Update Empty Body): {body10}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res10.StatusCode);
            var doc10 = JsonDocument.Parse(body10);
            Assert.True(doc10.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc10.RootElement.GetProperty("Data").ValueKind);

            // Request 11: POST /api/mst_organization/update non-existent MST -> 200 OK với Data null
            var req11 = new StringContent("{\"MST\":\"9999999999\"}", Encoding.UTF8, "application/json");
            var res11 = await http.PostAsync("/api/mst_organization/update", req11);
            var body11 = await res11.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ11 (Update Non-existent MST): {body11}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res11.StatusCode);
            var doc11 = JsonDocument.Parse(body11);
            Assert.True(doc11.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc11.RootElement.GetProperty("Data").ValueKind);

            // Request 12: POST /api/mst_organization/update MST 0100000001 -> 200 OK với Data chi tiết và 3 CKS
            var req12 = new StringContent("{\"MST\":\"0100000001\"}", Encoding.UTF8, "application/json");
            var res12 = await http.PostAsync("/api/mst_organization/update", req12);
            var body12 = await res12.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ12 (Update MST 0100000001): {body12}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res12.StatusCode);
            var doc12 = JsonDocument.Parse(body12);
            Assert.True(doc12.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0100000001", doc12.RootElement.GetProperty("Data").GetProperty("MST").GetString());
            Assert.Equal("7200000001", doc12.RootElement.GetProperty("Data").GetProperty("OrgID").GetString());
            Assert.Equal(3, doc12.RootElement.GetProperty("Data").GetProperty("Lst_Mst_OrgSignConfig").GetArrayLength());

            // Request 13: POST /api/mst_organization/update MST 0100000003 -> 200 OK với 0 CKS (2 bộ lọc ra số dòng khác nhau)
            var req13 = new StringContent("{\"MST\":\"0100000003\"}", Encoding.UTF8, "application/json");
            var res13 = await http.PostAsync("/api/mst_organization/update", req13);
            var body13 = await res13.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ13 (Update MST 0100000003): {body13}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res13.StatusCode);
            var doc13 = JsonDocument.Parse(body13);
            Assert.True(doc13.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0100000003", doc13.RootElement.GetProperty("Data").GetProperty("MST").GetString());
            Assert.Equal("7200000002", doc13.RootElement.GetProperty("Data").GetProperty("OrgID").GetString());
            Assert.Equal(0, doc13.RootElement.GetProperty("Data").GetProperty("Lst_Mst_OrgSignConfig").GetArrayLength());

            // Request 14: POST /api/mst_organization/edit empty body -> 400 Bad Request
            var req14 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res14 = await http.PostAsync("/api/mst_organization/edit", req14);
            var body14 = await res14.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ14 (Edit Empty Body): {body14}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res14.StatusCode);
            var doc14 = JsonDocument.Parse(body14);
            Assert.False(doc14.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Update", doc14.RootElement.GetProperty("ErrorCode").GetString());

            // Request 15: POST /api/mst_organization/edit missing MST -> 200 OK với Message "Mã chi nhánh trống!"
            var req15 = new StringContent("{\"OrgID\":\"7200000001\",\"NNTFullName\":\"Cập nhật thiếu MST\"}", Encoding.UTF8, "application/json");
            var res15 = await http.PostAsync("/api/mst_organization/edit", req15);
            var body15 = await res15.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ15 (Edit Missing MST): {body15}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res15.StatusCode);
            var doc15 = JsonDocument.Parse(body15);
            Assert.True(doc15.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã chi nhánh trống!", doc15.RootElement.GetProperty("Messages")[0].GetString());

            // Request 16: POST /api/mst_organization/edit non-existent MST -> 200 OK với Message "Mã chi nhánh '...' không có trong hệ thống!"
            var req16 = new StringContent("{\"MST\":\"9999999999\",\"OrgID\":\"7299999999\",\"NNTFullName\":\"Chi nhánh không tồn tại\"}", Encoding.UTF8, "application/json");
            var res16 = await http.PostAsync("/api/mst_organization/edit", req16);
            var body16 = await res16.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ16 (Edit Non-existent MST): {body16}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res16.StatusCode);
            var doc16 = JsonDocument.Parse(body16);
            Assert.True(doc16.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã chi nhánh '7299999999' không có trong hệ thống!", doc16.RootElement.GetProperty("Messages")[0].GetString());

            // Request 17: POST /api/mst_organization/edit empty NNTFullName -> 400 Bad Request
            var req17 = new StringContent("{\"MST\":\"0100000001\",\"NNTFullName\":\"   \"}", Encoding.UTF8, "application/json");
            var res17 = await http.PostAsync("/api/mst_organization/edit", req17);
            var body17 = await res17.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ17 (Edit Empty NNTFullName): {body17}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res17.StatusCode);
            var doc17 = JsonDocument.Parse(body17);
            Assert.False(doc17.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Update_InvalidNNTFullName", doc17.RootElement.GetProperty("ErrorCode").GetString());

            // Request 18: POST /api/mst_organization/edit round-trip cập nhật MST 0100000002 -> 200 OK
            var req18 = new StringContent("{\"MST\":\"0100000002\",\"NNTFullName\":\"Chi nhánh TP Hồ Chí Minh - RealServer\",\"NNTShortName\":\"CN HCM Real\",\"SignType\":\"SERVER\",\"ContactEmail\":\"realserver.hcm@solution.hn\"}", Encoding.UTF8, "application/json");
            var res18 = await http.PostAsync("/api/mst_organization/edit", req18);
            var body18 = await res18.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ18 (Edit Success Round-trip): {body18}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res18.StatusCode);
            var doc18 = JsonDocument.Parse(body18);
            Assert.True(doc18.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Sửa chi nhánh thành công!", doc18.RootElement.GetProperty("Messages")[0].GetString());

            // Request 19: POST /api/mst_organization/update xác minh dữ liệu vừa cập nhật ở Request 18 -> 200 OK
            var req19 = new StringContent("{\"MST\":\"0100000002\"}", Encoding.UTF8, "application/json");
            var res19 = await http.PostAsync("/api/mst_organization/update", req19);
            var body19 = await res19.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ19 (Update Verify Edited Record): {body19}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res19.StatusCode);
            var doc19 = JsonDocument.Parse(body19);
            Assert.True(doc19.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Chi nhánh TP Hồ Chí Minh - RealServer", doc19.RootElement.GetProperty("Data").GetProperty("NNTFullName").GetString());
            Assert.Equal("CN HCM Real", doc19.RootElement.GetProperty("Data").GetProperty("NNTShortName").GetString());
            Assert.Equal("SERVER", doc19.RootElement.GetProperty("Data").GetProperty("SignType").GetString());
            Assert.Equal("realserver.hcm@solution.hn", doc19.RootElement.GetProperty("Data").GetProperty("ContactEmail").GetString());

            // Request 20: POST /api/mst_organization/detail empty body -> 200 OK với Data null
            var req20 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res20 = await http.PostAsync("/api/mst_organization/detail", req20);
            var body20 = await res20.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ20 (Detail Empty Body): {body20}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res20.StatusCode);
            var doc20 = JsonDocument.Parse(body20);
            Assert.True(doc20.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc20.RootElement.GetProperty("Data").ValueKind);

            // Request 21: POST /api/mst_organization/detail non-existent MST -> 200 OK với Data null
            var req21 = new StringContent("{\"MST\":\"9999999999\"}", Encoding.UTF8, "application/json");
            var res21 = await http.PostAsync("/api/mst_organization/detail", req21);
            var body21 = await res21.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ21 (Detail Non-existent MST): {body21}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res21.StatusCode);
            var doc21 = JsonDocument.Parse(body21);
            Assert.True(doc21.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc21.RootElement.GetProperty("Data").ValueKind);

            // Request 22: POST /api/mst_organization/detail MST 0100000001 -> 200 OK với Data chi tiết, 3 CKS, 3 SignType
            var req22 = new StringContent("{\"MST\":\"0100000001\"}", Encoding.UTF8, "application/json");
            var res22 = await http.PostAsync("/api/mst_organization/detail", req22);
            var body22 = await res22.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ22 (Detail MST 0100000001): {body22}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res22.StatusCode);
            var doc22 = JsonDocument.Parse(body22);
            Assert.True(doc22.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0100000001", doc22.RootElement.GetProperty("Data").GetProperty("MST").GetString());
            Assert.Equal("7200000001", doc22.RootElement.GetProperty("Data").GetProperty("OrgID").GetString());
            Assert.Equal(3, doc22.RootElement.GetProperty("Data").GetProperty("Lst_Mst_OrgSignConfig").GetArrayLength());
            Assert.Equal(3, doc22.RootElement.GetProperty("Data").GetProperty("List_Mst_Area").GetArrayLength());
            Assert.Equal(3, doc22.RootElement.GetProperty("Data").GetProperty("List_Mst_SignType").GetArrayLength());

            // Request 23: POST /api/mst_organization/detail MST 0100000003 -> 200 OK với 0 CKS (2 bộ lọc ra số dòng khác nhau)
            var req23 = new StringContent("{\"MST\":\"0100000003\"}", Encoding.UTF8, "application/json");
            var res23 = await http.PostAsync("/api/mst_organization/detail", req23);
            var body23 = await res23.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ23 (Detail MST 0100000003): {body23}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res23.StatusCode);
            var doc23 = JsonDocument.Parse(body23);
            Assert.True(doc23.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0100000003", doc23.RootElement.GetProperty("Data").GetProperty("MST").GetString());
            Assert.Equal("7200000002", doc23.RootElement.GetProperty("Data").GetProperty("OrgID").GetString());
            Assert.Equal(0, doc23.RootElement.GetProperty("Data").GetProperty("Lst_Mst_OrgSignConfig").GetArrayLength());

            // Request 24: POST /api/mst_organization/delete empty body -> 400 Bad Request
            var req24 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res24 = await http.PostAsync("/api/mst_organization/delete", req24);
            var body24 = await res24.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ24 (Delete Empty Body): {body24}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res24.StatusCode);
            var doc24 = JsonDocument.Parse(body24);
            Assert.False(doc24.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_NNT_Delete", doc24.RootElement.GetProperty("ErrorCode").GetString());

            // Request 25: POST /api/mst_organization/delete missing MST -> 200 OK với Message "Mã chi nhánh trống!"
            var req25 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res25 = await http.PostAsync("/api/mst_organization/delete", req25);
            var body25 = await res25.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ25 (Delete Missing MST): {body25}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res25.StatusCode);
            var doc25 = JsonDocument.Parse(body25);
            Assert.False(doc25.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã chi nhánh trống!", doc25.RootElement.GetProperty("Messages")[0].GetString());

            // Request 26: POST /api/mst_organization/delete missing OrgID -> 200 OK với Message "Mã chi nhánh trống!"
            var req26 = new StringContent("{\"MST\":\"0100000001\"}", Encoding.UTF8, "application/json");
            var res26 = await http.PostAsync("/api/mst_organization/delete", req26);
            var body26 = await res26.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ26 (Delete Missing OrgID): {body26}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res26.StatusCode);
            var doc26 = JsonDocument.Parse(body26);
            Assert.False(doc26.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã chi nhánh trống!", doc26.RootElement.GetProperty("Messages")[0].GetString());

            // Request 27: POST /api/mst_organization/delete non-existent MST -> 200 OK với Message "Mã chi nhánh '...' không có trong hệ thống!"
            var req27 = new StringContent("{\"MST\":\"9999999999\",\"OrgID\":\"7299999999\"}", Encoding.UTF8, "application/json");
            var res27 = await http.PostAsync("/api/mst_organization/delete", req27);
            var body27 = await res27.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ27 (Delete Non-existent MST): {body27}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res27.StatusCode);
            var doc27 = JsonDocument.Parse(body27);
            Assert.False(doc27.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã chi nhánh '7299999999' không có trong hệ thống!", doc27.RootElement.GetProperty("Messages")[0].GetString());

            // Request 28: POST /api/mst_organization/delete round-trip xóa bản ghi 0100000099 vừa tạo ở Request 8 -> 200 OK
            var req28 = new StringContent("{\"MST\":\"0100000099\",\"OrgID\":\"7200000099\"}", Encoding.UTF8, "application/json");
            var res28 = await http.PostAsync("/api/mst_organization/delete", req28);
            var body28 = await res28.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ28 (Delete Success Round-trip): {body28}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res28.StatusCode);
            var doc28 = JsonDocument.Parse(body28);
            Assert.True(doc28.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa chi nhánh '7200000099' thành công!", doc28.RootElement.GetProperty("Messages")[0].GetString());

            // Request 29: POST /api/mst_organization/index xác minh bản ghi 0100000099 đã bị xóa -> 200 OK
            var req29 = new StringContent("{\"OrgID\":\"7200000099\"}", Encoding.UTF8, "application/json");
            var res29 = await http.PostAsync("/api/mst_organization/index", req29);
            var body29 = await res29.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ29 (Index Verify Deleted Org): {body29}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res29.StatusCode);
            var doc29 = JsonDocument.Parse(body29);
            Assert.Equal(0, doc29.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, doc29.RootElement.GetProperty("Data").GetProperty("DataList").GetArrayLength());

            // Request 30: POST /api/mst_organization/export empty body -> 200 OK với tất cả 3 bản ghi NNT và 6 bản ghi CKS
            var req30 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res30 = await http.PostAsync("/api/mst_organization/export", req30);
            var body30 = await res30.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ30 (Export Empty Body): {body30}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res30.StatusCode);
            var doc30 = JsonDocument.Parse(body30);
            Assert.True(doc30.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc30.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc30.RootElement.GetProperty("CheckSuccess").GetString());
            Assert.False(string.IsNullOrWhiteSpace(doc30.RootElement.GetProperty("strUrl").GetString()));

            // Request 31: POST /api/mst_organization/export lọc OrgID 7200000002 -> 200 OK (1 NNT, 0 CKS)
            var req31 = new StringContent("{\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var res31 = await http.PostAsync("/api/mst_organization/export", req31);
            var body31 = await res31.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ31 (Export OrgID 7200000002): {body31}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res31.StatusCode);
            var doc31 = JsonDocument.Parse(body31);
            Assert.True(doc31.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc31.RootElement.GetProperty("Title").GetString());

            // Request 32: POST /api/mst_organization/export lọc OrgID 7200000001 -> 200 OK (2 NNT, 6 CKS - 2 bộ lọc ra số dòng khác nhau)
            var req32 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res32 = await http.PostAsync("/api/mst_organization/export", req32);
            var body32 = await res32.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ32 (Export OrgID 7200000001): {body32}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res32.StatusCode);
            var doc32 = JsonDocument.Parse(body32);
            Assert.True(doc32.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc32.RootElement.GetProperty("Title").GetString());

            // Request 33: POST /api/mst_organization/export không khớp bản ghi nào -> 200 OK với Title "Không có dữ liệu!"
            var req33 = new StringContent("{\"KeyWord\":\"KhongCo999\"}", Encoding.UTF8, "application/json");
            var res33 = await http.PostAsync("/api/mst_organization/export", req33);
            var body33 = await res33.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ33 (Export Non-existent): {body33}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res33.StatusCode);
            var doc33 = JsonDocument.Parse(body33);
            Assert.True(doc33.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Không có dữ liệu!", doc33.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc33.RootElement.GetProperty("CheckSuccess").GetString());

            // Request 34: POST /api/mst_organization/export phân trang RecordCount=1, Page=0 -> 200 OK
            var req34 = new StringContent("{\"RecordCount\":1,\"Page\":0}", Encoding.UTF8, "application/json");
            var res34 = await http.PostAsync("/api/mst_organization/export", req34);
            var body34 = await res34.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ34 (Export Pagination Page 0 Size 1): {body34}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res34.StatusCode);
            var doc34 = JsonDocument.Parse(body34);
            Assert.True(doc34.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc34.RootElement.GetProperty("Title").GetString());
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                    await proc.WaitForExitAsync();
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
