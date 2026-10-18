using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class Sys_GroupControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Sys_GroupControllerPort _controller;

    public Sys_GroupControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Sys_Group
        var groupSeeder = new Sys_GroupSeeder();
        groupSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Sys_User
        var userSeeder = new Sys_UserSeeder();
        userSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Sys_UserInGroup
        var userInGroupSeeder = new Sys_UserInGroupSeeder();
        userInGroupSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Sys_Object
        var objectSeeder = new Sys_ObjectSeeder();
        objectSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Sys_Access
        var accessSeeder = new Sys_AccessSeeder();
        accessSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Sys_GroupControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_NullReq_SuccessWithAllGroups()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        // Sys_Group gồm 5 nhóm theo thứ tự GroupCode asc
        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(5, list.GetArrayLength());
        Assert.Equal("GRP_ACCOUNTANT", list[0].GetProperty("GroupCode").GetString());
        Assert.Equal("GRP_ADMIN", list[1].GetProperty("GroupCode").GetString());
        Assert.Equal("GRP_INACTIVE", list[2].GetProperty("GroupCode").GetString());
        Assert.Equal("GRP_MANAGER", list[3].GetProperty("GroupCode").GetString());
        Assert.Equal("GRP_STAFF", list[4].GetProperty("GroupCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByGroupName_ReturnsSingleMatchingGroup()
    {
        var req = new Sys_Group_IndexReq { GroupName = "Quản lý" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("GRP_MANAGER", list[0].GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Quản lý hợp đồng", list[0].GetProperty("GroupName").GetString());
    }

    [Fact]
    public async Task Index_FilterByGroupName_MatchesAll()
    {
        var req = new Sys_Group_IndexReq { GroupName = "Nhóm" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(5, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByGroupName_NoMatch_ReturnsEmpty()
    {
        var req = new Sys_Group_IndexReq { GroupName = "KHONG_CO_NHOM_NAY" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_Pagination_Page0_PageSize2()
    {
        var req = new Sys_Group_IndexReq { Page = 0, RecordCount = 2 };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("GRP_ACCOUNTANT", list[0].GetProperty("GroupCode").GetString());
        Assert.Equal("GRP_ADMIN", list[1].GetProperty("GroupCode").GetString());
    }

    [Fact]
    public async Task Index_Pagination_Page1_PageSize2()
    {
        var req = new Sys_Group_IndexReq { Page = 1, RecordCount = 2 };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("GRP_INACTIVE", list[0].GetProperty("GroupCode").GetString());
        Assert.Equal("GRP_MANAGER", list[1].GetProperty("GroupCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByFlagActive()
    {
        var req = new Sys_Group_IndexReq { FlagActive = "0" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("GRP_INACTIVE", list[0].GetProperty("GroupCode").GetString());
    }

    [Fact]
    public async Task GetDetailData_NullReq_ReturnsAllMappings()
    {
        var result = await _controller.GetDetailData(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetArrayLength());

        // Kiểm tra phần tử đầu tiên theo GroupCode asc (GRP_ACCOUNTANT - U03) có kèm thông tin join Sys_User và Sys_Group
        var first = data[0];
        Assert.Equal("GRP_ACCOUNTANT", first.GetProperty("GroupCode").GetString());
        Assert.Equal("U03", first.GetProperty("UserCode").GetString());
        Assert.Equal("dev@solution.hn", first.GetProperty("su_UserName").GetString());
        Assert.Equal("Nhóm Kế toán", first.GetProperty("sg_GroupName").GetString());
    }

    [Fact]
    public async Task GetDetailData_FilterByGroupCode_AdminGroup_ReturnsTwoUsers()
    {
        var req = new Sys_Group_GetDetailDataReq { GroupCode = "GRP_ADMIN" };
        var result = await _controller.GetDetailData(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(2, data.GetArrayLength());
        Assert.Equal("U01", data[0].GetProperty("UserCode").GetString());
        Assert.Equal("admin@solution.hn", data[0].GetProperty("su_UserName").GetString());
        Assert.Equal("U02", data[1].GetProperty("UserCode").GetString());
        Assert.Equal("tester@solution.hn", data[1].GetProperty("su_UserName").GetString());
    }

    [Fact]
    public async Task GetDetailData_FilterByGroupCode_ManagerGroup_ReturnsOneUser()
    {
        var req = new Sys_Group_GetDetailDataReq { GroupCode = "GRP_MANAGER" };
        var result = await _controller.GetDetailData(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("U02", data[0].GetProperty("UserCode").GetString());
        Assert.Equal("tester@solution.hn", data[0].GetProperty("su_UserName").GetString());
    }

    [Fact]
    public async Task GetDetailData_FilterByGroupCode_StaffGroup_ReturnsEmpty()
    {
        // GRP_STAFF có trong Sys_Group nhưng chưa có user nào trong Sys_UserInGroup -> trả []
        var req = new Sys_Group_GetDetailDataReq { GroupCode = "GRP_STAFF" };
        var result = await _controller.GetDetailData(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task GetDetailData_FilterByGroupCode_NonExistentGroup_ReturnsEmpty()
    {
        var req = new Sys_Group_GetDetailDataReq { GroupCode = "GRP_NONEXISTENT" };
        var result = await _controller.GetDetailData(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task Create_NullReq_ReturnsCheckStrJsonFail()
    {
        var result = await _controller.Create(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Create", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_EmptyReq_ReturnsCheckStrJsonFail()
    {
        var result = await _controller.Create(new Sys_Group_CreateReq());
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Create", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_EmptyGroupCode_ReturnsInvalidGroupCode()
    {
        var result = await _controller.Create(new Sys_Group_CreateReq { GroupCode = "", GroupName = "Nhóm mới" });
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Create_InvalidGroupCode", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_ExistingGroupCode_ReturnsGroupCodeExist()
    {
        var result = await _controller.Create(new Sys_Group_CreateReq { GroupCode = "GRP_ADMIN", GroupName = "Nhóm Quản Trị Trùng" });
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeExist", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_EmptyGroupName_ReturnsInvalidGroupName()
    {
        var result = await _controller.Create(new Sys_Group_CreateReq { GroupCode = "GRP_NEW_01", GroupName = "   " });
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Create_InvalidGroupName", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_UserNotFound_ReturnsUserCodeNotFound()
    {
        var result = await _controller.Create(new Sys_Group_CreateReq
        {
            GroupCode = "GRP_NEW_01",
            GroupName = "Nhóm hợp lệ",
            WAUserCode = "USER_NOT_EXIST"
        });
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_UserCodeNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_UserInactive_ReturnsFlagActiveNotMatched()
    {
        _db.Set<Sys_User>().Add(new Sys_User
        {
            UserCode = "U_INACTIVE_TEST",
            UserName = "Người dùng ngừng hoạt động",
            FlagActive = "0",
            MST = "0100000001"
        });
        await _db.SaveChangesAsync();

        var result = await _controller.Create(new Sys_Group_CreateReq
        {
            GroupCode = "GRP_NEW_02",
            GroupName = "Nhóm hợp lệ",
            WAUserCode = "U_INACTIVE_TEST"
        });
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_FlagActiveNotMatched", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_ValidReq_SuccessAndRoundTrip()
    {
        var req = new Sys_Group_CreateReq
        {
            GroupCode = "grp_test_roundtrip",
            GroupName = "Nhóm Kiểm Thử RoundTrip",
            WAUserCode = "U01"
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("GRP_TEST_ROUNDTRIP", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Kiểm Thử RoundTrip", data.GetProperty("GroupName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
        Assert.Equal("U01", data.GetProperty("LogLUBy").GetString());

        // Đọc lại từ DB (round-trip)
        var saved = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_TEST_ROUNDTRIP");
        Assert.NotNull(saved);
        Assert.Equal("Nhóm Kiểm Thử RoundTrip", saved.GroupName);
        Assert.Equal("1", saved.FlagActive);
        Assert.Equal("0100000001", saved.MST);
    }

    [Fact]
    public async Task Update_NullReq_ReturnsNullData()
    {
        var result = await _controller.Update(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_EmptyReq_ReturnsNullData()
    {
        var result = await _controller.Update(new Sys_Group_UpdateReq());
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_NonExistentGroup_ReturnsNullData()
    {
        var req = new Sys_Group_UpdateReq { GroupCode = "GRP_NONEXISTENT" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ValidGroupCode_AdminGroup_ReturnsAdmin()
    {
        var req = new Sys_Group_UpdateReq { GroupCode = "GRP_ADMIN" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_ADMIN", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Quản trị hệ thống", data.GetProperty("GroupName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Update_ValidGroupCode_ManagerGroup_ReturnsManager()
    {
        var req = new Sys_Group_UpdateReq { GroupCode = "grp_manager" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_MANAGER", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Quản lý hợp đồng", data.GetProperty("GroupName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Update_ValidGroupCode_WithMatchingMst_ReturnsGroup()
    {
        var req = new Sys_Group_UpdateReq { GroupCode = "GRP_INACTIVE", MST = "0100000002" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_INACTIVE", data.GetProperty("GroupCode").GetString());
        Assert.Equal("0", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000002", data.GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Update_ValidGroupCode_WithMismatchedMst_ReturnsNull()
    {
        var req = new Sys_Group_UpdateReq { GroupCode = "GRP_INACTIVE", MST = "0100000001" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ViaModelJson_ReturnsGroup()
    {
        var req = new Sys_Group_UpdateReq { Model = "{\"GroupCode\":\"GRP_STAFF\"}" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_STAFF", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Nhân viên kinh doanh", data.GetProperty("GroupName").GetString());
    }

    [Fact]
    public async Task Update_ViaRawCodeInModel_ReturnsGroup()
    {
        var req = new Sys_Group_UpdateReq { Model = "GRP_ACCOUNTANT" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_ACCOUNTANT", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Kế toán", data.GetProperty("GroupName").GetString());
    }

    [Fact]
    public async Task Edit_NullReq_ReturnsEmptyGroupCodeMessage()
    {
        var result = await _controller.Edit(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Edit_EmptyReq_ReturnsEmptyGroupCodeMessage()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq());
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Edit_EmptyGroupCode_ReturnsEmptyGroupCodeMessage()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq { GroupCode = "   ", GroupName = "Nhóm mới" });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Edit_NonExistentGroupCode_ReturnsNotFoundMessage()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq { GroupCode = "GRP_NONEXISTENT", GroupName = "Nhóm mới" });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng 'GRP_NONEXISTENT' không có trong hệ thống!", messages[0].GetString());
    }

    [Fact]
    public async Task Edit_EmptyGroupName_ReturnsInvalidGroupName()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq { GroupCode = "GRP_ADMIN", GroupName = "" });
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Update_InvalidGroupName", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_WhitespaceGroupName_ReturnsInvalidGroupName()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq { GroupCode = "GRP_ADMIN", GroupName = "   " });
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Update_InvalidGroupName", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_ValidReq_SuccessAndRoundTrip()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq
        {
            GroupCode = "grp_manager",
            GroupName = "Nhóm Quản lý hợp đồng ĐÃ SỬA",
            FlagActive = "1"
        });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Sửa nhóm người dùng thành công!", messages[0].GetString());

        var updated = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_MANAGER");
        Assert.NotNull(updated);
        Assert.Equal("Nhóm Quản lý hợp đồng ĐÃ SỬA", updated.GroupName);
        Assert.Equal("1", updated.FlagActive);
    }

    [Fact]
    public async Task Edit_ViaModelJson_SuccessAndRoundTrip()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq
        {
            Model = "{\"GroupCode\":\"GRP_STAFF\",\"GroupName\":\"Nhóm Nhân viên kinh doanh MỚI\",\"FlagActive\":\"1\"}"
        });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var staff = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_STAFF");
        Assert.NotNull(staff);
        Assert.Equal("Nhóm Nhân viên kinh doanh MỚI", staff.GroupName);
    }

    [Fact]
    public async Task Edit_ChangeFlagActive_Success()
    {
        var result = await _controller.Edit(new Sys_Group_EditReq
        {
            GroupCode = "GRP_INACTIVE",
            GroupName = "Nhóm Kích Hoạt Lại",
            FlagActive = "1"
        });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var inactive = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_INACTIVE");
        Assert.NotNull(inactive);
        Assert.Equal("1", inactive.FlagActive);
        Assert.Equal("Nhóm Kích Hoạt Lại", inactive.GroupName);
    }

    [Fact]
    public async Task Detail_NullReq_ReturnsNullData()
    {
        var result = await _controller.Detail(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_EmptyReq_ReturnsNullData()
    {
        var result = await _controller.Detail(new Sys_Group_DetailReq());
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_NonExistentGroup_ReturnsNullData()
    {
        var req = new Sys_Group_DetailReq { GroupCode = "GRP_NONEXISTENT" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ValidGroupCode_AdminGroup_ReturnsAdmin()
    {
        var req = new Sys_Group_DetailReq { GroupCode = "GRP_ADMIN" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_ADMIN", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Quản trị hệ thống", data.GetProperty("GroupName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Detail_ValidGroupCode_ManagerGroup_ReturnsManager()
    {
        var req = new Sys_Group_DetailReq { GroupCode = "grp_manager" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_MANAGER", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Quản lý hợp đồng", data.GetProperty("GroupName").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000001", data.GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Detail_ValidGroupCode_WithMatchingMst_ReturnsGroup()
    {
        var req = new Sys_Group_DetailReq { GroupCode = "GRP_INACTIVE", MST = "0100000002" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_INACTIVE", data.GetProperty("GroupCode").GetString());
        Assert.Equal("0", data.GetProperty("FlagActive").GetString());
        Assert.Equal("0100000002", data.GetProperty("MST").GetString());
    }

    [Fact]
    public async Task Detail_ValidGroupCode_WithMismatchedMst_ReturnsNull()
    {
        var req = new Sys_Group_DetailReq { GroupCode = "GRP_INACTIVE", MST = "0100000001" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ViaModelJson_ReturnsGroup()
    {
        var req = new Sys_Group_DetailReq { Model = "{\"GroupCode\":\"GRP_STAFF\"}" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_STAFF", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Nhân viên kinh doanh", data.GetProperty("GroupName").GetString());
    }

    [Fact]
    public async Task Detail_ViaRawCodeInModel_ReturnsGroup()
    {
        var req = new Sys_Group_DetailReq { Model = "GRP_ACCOUNTANT" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP_ACCOUNTANT", data.GetProperty("GroupCode").GetString());
        Assert.Equal("Nhóm Kế toán", data.GetProperty("GroupName").GetString());
    }

    [Fact]
    public async Task Delete_NullReq_ReturnsEmptyGroupCodeMessage()
    {
        var result = await _controller.Delete(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Delete_EmptyReq_ReturnsEmptyGroupCodeMessage()
    {
        var result = await _controller.Delete(new Sys_Group_DeleteReq());
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Delete_EmptyGroupCode_ReturnsEmptyGroupCodeMessage()
    {
        var result = await _controller.Delete(new Sys_Group_DeleteReq { GroupCode = "   " });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Delete_NonExistentGroupCode_ReturnsNotFoundMessage()
    {
        var result = await _controller.Delete(new Sys_Group_DeleteReq { GroupCode = "GRP_NONEXISTENT" });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Mã nhóm người dùng 'GRP_NONEXISTENT' không có trong hệ thống!", messages[0].GetString());
    }

    [Fact]
    public async Task Delete_ValidGroupCode_DeletesGroupAndUsersInGroup_RoundTrip()
    {
        // GRP_ACCOUNTANT có 1 user (U03) trong Sys_UserInGroup
        var beforeGroup = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_ACCOUNTANT");
        Assert.NotNull(beforeGroup);
        var beforeUsers = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == "GRP_ACCOUNTANT").ToListAsync();
        Assert.Single(beforeUsers);

        var result = await _controller.Delete(new Sys_Group_DeleteReq { GroupCode = "grp_accountant" });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var messages = doc.RootElement.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Xóa nhóm người dùng 'grp_accountant' thành công!", messages[0].GetString());

        // Kiểm tra round-trip: Sys_Group và Sys_UserInGroup đều đã bị xóa
        var afterGroup = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_ACCOUNTANT");
        Assert.Null(afterGroup);
        var afterUsers = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == "GRP_ACCOUNTANT").ToListAsync();
        Assert.Empty(afterUsers);
    }

    [Fact]
    public async Task Delete_ViaModelJson_SuccessAndRoundTrip()
    {
        var beforeGroup = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_STAFF");
        Assert.NotNull(beforeGroup);

        var result = await _controller.Delete(new Sys_Group_DeleteReq { Model = "{\"GroupCode\":\"GRP_STAFF\"}" });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var afterGroup = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_STAFF");
        Assert.Null(afterGroup);
    }

    [Fact]
    public async Task Delete_ViaRawCodeInModel_SuccessAndRoundTrip()
    {
        // GRP_MANAGER có user U02 trong Sys_UserInGroup
        var beforeGroup = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_MANAGER");
        Assert.NotNull(beforeGroup);
        var beforeUsers = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == "GRP_MANAGER").ToListAsync();
        Assert.Single(beforeUsers);

        var result = await _controller.Delete(new Sys_Group_DeleteReq { Model = "GRP_MANAGER" });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var afterGroup = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_MANAGER");
        Assert.Null(afterGroup);
        var afterUsers = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == "GRP_MANAGER").ToListAsync();
        Assert.Empty(afterUsers);
    }

    [Fact]
    public async Task GetSysUser_NullReq_ReturnsAllActiveUsersAndAllGroupMappings()
    {
        var result = await _controller.GetSysUser(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        var listSysUser = data.GetProperty("ListSysUser");
        var listUserInGroup = data.GetProperty("ListUserInGroup");

        Assert.Equal(3, listSysUser.GetArrayLength());
        Assert.Equal(4, listUserInGroup.GetArrayLength());

        // Kiểm tra mật khẩu được che
        for (int i = 0; i < listSysUser.GetArrayLength(); i++)
        {
            Assert.Equal("*********", listSysUser[i].GetProperty("UserPassword").GetString());
        }

        // Top-level properties
        Assert.Equal(3, root.GetProperty("ListSysUser").GetArrayLength());
        Assert.Equal(4, root.GetProperty("ListUserInGroup").GetArrayLength());
    }

    [Fact]
    public async Task GetSysUser_FilterByGroupCode_AdminGroup_ReturnsTwoMappings()
    {
        var req = new Sys_Group_GetSysUserReq { GroupCode = "GRP_ADMIN" };
        var result = await _controller.GetSysUser(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP_ADMIN", data.GetProperty("GroupCode").GetString());

        var listSysUser = data.GetProperty("ListSysUser");
        Assert.Equal(3, listSysUser.GetArrayLength());

        var listUserInGroup = data.GetProperty("ListUserInGroup");
        Assert.Equal(2, listUserInGroup.GetArrayLength());
        Assert.Equal("U01", listUserInGroup[0].GetProperty("UserCode").GetString());
        Assert.Equal("U02", listUserInGroup[1].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task GetSysUser_FilterByGroupCode_ManagerGroup_ReturnsOneMapping()
    {
        var req = new Sys_Group_GetSysUserReq { GroupCode = "grp_manager" };
        var result = await _controller.GetSysUser(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP_MANAGER", data.GetProperty("GroupCode").GetString());

        var listUserInGroup = data.GetProperty("ListUserInGroup");
        Assert.Equal(1, listUserInGroup.GetArrayLength());
        Assert.Equal("U02", listUserInGroup[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task GetSysUser_FilterByGroupCode_StaffGroup_ReturnsEmptyMappings()
    {
        var req = new Sys_Group_GetSysUserReq { GroupCode = "GRP_STAFF" };
        var result = await _controller.GetSysUser(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP_STAFF", data.GetProperty("GroupCode").GetString());

        var listSysUser = data.GetProperty("ListSysUser");
        Assert.Equal(3, listSysUser.GetArrayLength());

        var listUserInGroup = data.GetProperty("ListUserInGroup");
        Assert.Equal(0, listUserInGroup.GetArrayLength());
    }

    [Fact]
    public async Task GetSysUser_FilterByGroupCode_NonExistentGroup_ReturnsEmptyMappings()
    {
        var req = new Sys_Group_GetSysUserReq { GroupCode = "GRP_NONEXISTENT" };
        var result = await _controller.GetSysUser(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP_NONEXISTENT", data.GetProperty("GroupCode").GetString());

        var listUserInGroup = data.GetProperty("ListUserInGroup");
        Assert.Equal(0, listUserInGroup.GetArrayLength());
    }

    [Fact]
    public async Task GetSysUser_ViaModelJson_Success()
    {
        var req = new Sys_Group_GetSysUserReq { Model = "{\"GroupCode\":\"GRP_ADMIN\"}" };
        var result = await _controller.GetSysUser(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP_ADMIN", data.GetProperty("GroupCode").GetString());
        Assert.Equal(2, data.GetProperty("ListUserInGroup").GetArrayLength());
    }

    [Fact]
    public async Task GetSysUser_ViaRawCodeInModel_Success()
    {
        var req = new Sys_Group_GetSysUserReq { Model = "GRP_ACCOUNTANT" };
        var result = await _controller.GetSysUser(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP_ACCOUNTANT", data.GetProperty("GroupCode").GetString());
        Assert.Equal(1, data.GetProperty("ListUserInGroup").GetArrayLength());
        Assert.Equal("U03", data.GetProperty("ListUserInGroup")[0].GetProperty("UserCode").GetString());
    }

    [Fact]
    public async Task GetSysUser_FilterByFlagActive_Inactive_ReturnsEmptySysUser()
    {
        var req = new Sys_Group_GetSysUserReq { FlagActive = "0" };
        var result = await _controller.GetSysUser(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetProperty("ListSysUser").GetArrayLength());
    }

    [Fact]
    public async Task SaveUserInGroup_NullReq_ReturnsNoUserSelectedTitle()
    {
        var result = await _controller.SaveUserInGroup(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Chưa chọn người dùng gán vào nhóm!", doc.RootElement.GetProperty("Title").GetString());
    }

    [Fact]
    public async Task SaveUserInGroup_EmptyReq_ReturnsNoUserSelectedTitle()
    {
        var result = await _controller.SaveUserInGroup(new Sys_Group_SaveUserInGroupReq());
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Chưa chọn người dùng gán vào nhóm!", doc.RootElement.GetProperty("Title").GetString());
    }

    [Fact]
    public async Task SaveUserInGroup_InvalidModel_ReturnsNoUserSelectedTitle()
    {
        var result = await _controller.SaveUserInGroup(new Sys_Group_SaveUserInGroupReq { Model = "invalid json {{" });
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Chưa chọn người dùng gán vào nhóm!", doc.RootElement.GetProperty("Title").GetString());
    }

    [Fact]
    public async Task SaveUserInGroup_GroupCodeNotFound_ReturnsBadRequest()
    {
        var req = new Sys_Group_SaveUserInGroupReq
        {
            GroupCode = "GRP_NONEXISTENT",
            Lst_Sys_UserInGroup = new List<Sys_UserInGroup> { new() { UserCode = "U01" } }
        };
        var result = await _controller.SaveUserInGroup(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveUserInGroup_UserCodeNotFound_ReturnsBadRequest()
    {
        var req = new Sys_Group_SaveUserInGroupReq
        {
            GroupCode = "GRP_ADMIN",
            WAUserCode = "USER_NONEXISTENT",
            Lst_Sys_UserInGroup = new List<Sys_UserInGroup> { new() { UserCode = "U01" } }
        };
        var result = await _controller.SaveUserInGroup(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_UserCodeNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveUserInGroup_UserCodeInactive_ReturnsBadRequest()
    {
        _db.Set<Sys_User>().Add(new Sys_User
        {
            UserCode = "U_INACTIVE_FOR_SAVE",
            UserName = "Người dùng không hoạt động",
            FlagActive = "0",
            MST = "0100000001"
        });
        await _db.SaveChangesAsync();

        var req = new Sys_Group_SaveUserInGroupReq
        {
            GroupCode = "GRP_ADMIN",
            WAUserCode = "U_INACTIVE_FOR_SAVE",
            Lst_Sys_UserInGroup = new List<Sys_UserInGroup> { new() { UserCode = "U01" } }
        };
        var result = await _controller.SaveUserInGroup(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_FlagActiveNotMatched", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task SaveUserInGroup_ValidReq_AssignUsers_SuccessAndRoundTrip()
    {
        // GRP_STAFF ban đầu không có user nào
        var beforeUsers = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == "GRP_STAFF").ToListAsync();
        Assert.Empty(beforeUsers);

        var req = new Sys_Group_SaveUserInGroupReq
        {
            GroupCode = "grp_staff",
            Lst_Sys_UserInGroup = new List<Sys_UserInGroup>
            {
                new() { UserCode = "u01" },
                new() { UserCode = "u03" }
            }
        };

        var result = await _controller.SaveUserInGroup(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Gán người dùng vào nhóm thành công!", doc.RootElement.GetProperty("Title").GetString());

        var afterUsers = await _db.Set<Sys_UserInGroup>()
            .Where(x => x.GroupCode == "GRP_STAFF")
            .OrderBy(x => x.UserCode)
            .ToListAsync();
        Assert.Equal(2, afterUsers.Count);
        Assert.Equal("U01", afterUsers[0].UserCode);
        Assert.Equal("U03", afterUsers[1].UserCode);
        Assert.Equal("0100000001", afterUsers[0].MST);
        Assert.Equal("U01", afterUsers[0].LogLUBy);
    }

    [Fact]
    public async Task SaveUserInGroup_ValidReq_ClearUsers_SuccessAndRoundTrip()
    {
        // GRP_ADMIN ban đầu có 2 user (U01, U02)
        var beforeUsers = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == "GRP_ADMIN").ToListAsync();
        Assert.Equal(2, beforeUsers.Count);

        var req = new Sys_Group_SaveUserInGroupReq
        {
            GroupCode = "GRP_ADMIN",
            Lst_Sys_UserInGroup = new List<Sys_UserInGroup>()
        };

        var result = await _controller.SaveUserInGroup(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa người dùng khỏi nhóm thành công!", doc.RootElement.GetProperty("Title").GetString());

        var afterUsers = await _db.Set<Sys_UserInGroup>().Where(x => x.GroupCode == "GRP_ADMIN").ToListAsync();
        Assert.Empty(afterUsers);
    }

    [Fact]
    public async Task SaveUserInGroup_ViaModelJson_SuccessAndRoundTrip()
    {
        var modelJson = "{\"Sys_Group\":{\"GroupCode\":\"GRP_MANAGER\"},\"Lst_Sys_UserInGroup\":[{\"UserCode\":\"U01\"},{\"UserCode\":\"U02\"}]}";
        var req = new Sys_Group_SaveUserInGroupReq { Model = modelJson };

        var result = await _controller.SaveUserInGroup(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Gán người dùng vào nhóm thành công!", doc.RootElement.GetProperty("Title").GetString());

        var afterUsers = await _db.Set<Sys_UserInGroup>()
            .Where(x => x.GroupCode == "GRP_MANAGER")
            .OrderBy(x => x.UserCode)
            .ToListAsync();
        Assert.Equal(2, afterUsers.Count);
        Assert.Equal("U01", afterUsers[0].UserCode);
        Assert.Equal("U02", afterUsers[1].UserCode);
    }

    [Fact]
    public async Task GetSysObject_NullReq_ReturnsAllActiveObjectsAndAllAccesses()
    {
        var result = await _controller.GetSysObject(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var data = doc.RootElement.GetProperty("Data");
        var listSysObject = data.GetProperty("ListSysObject");
        // 4 đối tượng hoạt động có ObjectType != "FUNC" (BTN_SAVE, MNU_CONTRACT, MNU_CUSTOMER, MNU_SYSTEM)
        Assert.Equal(4, listSysObject.GetArrayLength());
        Assert.Equal("BTN_SAVE", listSysObject[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("MNU_CONTRACT", listSysObject[1].GetProperty("ObjectCode").GetString());
        Assert.Equal("MNU_CUSTOMER", listSysObject[2].GetProperty("ObjectCode").GetString());
        Assert.Equal("MNU_SYSTEM", listSysObject[3].GetProperty("ObjectCode").GetString());

        var listSysAccess = data.GetProperty("ListSysAccess");
        // 4 bản ghi phân quyền (3 của ADMIN, 1 của SALE_MGR)
        Assert.Equal(4, listSysAccess.GetArrayLength());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("GroupCode").ValueKind);
    }

    [Fact]
    public async Task GetSysObject_EmptyReq_ReturnsAllActiveObjectsAndAllAccesses()
    {
        var req = new Sys_Group_GetSysObjectReq();
        var result = await _controller.GetSysObject(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var data = doc.RootElement.GetProperty("Data");
        var listSysObject = data.GetProperty("ListSysObject");
        Assert.Equal(4, listSysObject.GetArrayLength());

        var listSysAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(4, listSysAccess.GetArrayLength());
    }

    [Fact]
    public async Task GetSysObject_FilterByGroupCode_Admin_Returns3Accesses()
    {
        var req = new Sys_Group_GetSysObjectReq { GroupCode = "admin" };
        var result = await _controller.GetSysObject(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("ADMIN", data.GetProperty("GroupCode").GetString());

        var listSysObject = data.GetProperty("ListSysObject");
        Assert.Equal(4, listSysObject.GetArrayLength());

        var listSysAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(3, listSysAccess.GetArrayLength());
        Assert.Equal("ADMIN", listSysAccess[0].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_CONTRACT", listSysAccess[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("Quản lý hợp đồng", listSysAccess[0].GetProperty("so_ObjectName").GetString());

        Assert.Equal("ADMIN", listSysAccess[1].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_CUSTOMER", listSysAccess[1].GetProperty("ObjectCode").GetString());
        Assert.Equal("Quản lý khách hàng", listSysAccess[1].GetProperty("so_ObjectName").GetString());

        Assert.Equal("ADMIN", listSysAccess[2].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_SYSTEM", listSysAccess[2].GetProperty("ObjectCode").GetString());
        Assert.Equal("Quản trị hệ thống", listSysAccess[2].GetProperty("so_ObjectName").GetString());
    }

    [Fact]
    public async Task GetSysObject_FilterByGroupCode_SaleMgr_Returns1Access()
    {
        var req = new Sys_Group_GetSysObjectReq { GroupCode = "sale_mgr" };
        var result = await _controller.GetSysObject(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("SALE_MGR", data.GetProperty("GroupCode").GetString());

        var listSysObject = data.GetProperty("ListSysObject");
        Assert.Equal(4, listSysObject.GetArrayLength());

        var listSysAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(1, listSysAccess.GetArrayLength());
        Assert.Equal("SALE_MGR", listSysAccess[0].GetProperty("GroupCode").GetString());
        Assert.Equal("MNU_CONTRACT", listSysAccess[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("Quản lý hợp đồng", listSysAccess[0].GetProperty("so_ObjectName").GetString());
    }

    [Fact]
    public async Task GetSysObject_FilterByGroupCode_NonExistent_Returns0Access()
    {
        var req = new Sys_Group_GetSysObjectReq { GroupCode = "GRP_NONEXISTENT" };
        var result = await _controller.GetSysObject(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("GRP_NONEXISTENT", data.GetProperty("GroupCode").GetString());

        var listSysObject = data.GetProperty("ListSysObject");
        Assert.Equal(4, listSysObject.GetArrayLength());

        var listSysAccess = data.GetProperty("ListSysAccess");
        Assert.Equal(0, listSysAccess.GetArrayLength());
    }

    [Fact]
    public async Task GetSysObject_ExcludesFuncObjectType()
    {
        // Thêm một đối tượng có ObjectType = "FUNC" vào DB
        _db.Set<Sys_Object>().Add(new Sys_Object
        {
            ObjectCode = "FNC_UNIT_TEST",
            ObjectName = "Hàm kiểm thử",
            ObjectType = "FUNC",
            FlagActive = "1"
        });
        await _db.SaveChangesAsync();

        var result = await _controller.GetSysObject(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var listSysObject = doc.RootElement.GetProperty("Data").GetProperty("ListSysObject");
        // Vẫn chỉ có 4 đối tượng, FNC_UNIT_TEST bị loại bỏ do ObjectType != "FUNC"
        Assert.Equal(4, listSysObject.GetArrayLength());
        for (int i = 0; i < listSysObject.GetArrayLength(); i++)
        {
            Assert.NotEqual("FNC_UNIT_TEST", listSysObject[i].GetProperty("ObjectCode").GetString());
            Assert.NotEqual("FUNC", listSysObject[i].GetProperty("ObjectType").GetString());
        }
    }

    [Fact]
    public async Task GetSysObject_FilterByFlagActiveZero_ReturnsSingleArchivedObject()
    {
        var req = new Sys_Group_GetSysObjectReq { FlagActive = "0" };
        var result = await _controller.GetSysObject(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var listSysObject = doc.RootElement.GetProperty("Data").GetProperty("ListSysObject");
        Assert.Equal(1, listSysObject.GetArrayLength());
        Assert.Equal("MNU_ARCHIVED", listSysObject[0].GetProperty("ObjectCode").GetString());
        Assert.Equal("0", listSysObject[0].GetProperty("FlagActive").GetString());
    }

    [Fact]
    public async Task GetSysObject_ViaModelJson_Success()
    {
        var req = new Sys_Group_GetSysObjectReq { Model = "{\"GroupCode\":\"ADMIN\"}" };
        var result = await _controller.GetSysObject(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("ADMIN", data.GetProperty("GroupCode").GetString());
        Assert.Equal(3, data.GetProperty("ListSysAccess").GetArrayLength());
    }

    [Fact]
    public async Task Export_NullReq_ReturnsAllGroupsExcel()
    {
        var result = await _controller.Export(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        var strUrl = root.GetProperty("strUrl").GetString();
        Assert.NotNull(strUrl);
        Assert.Contains("/TempFiles/", strUrl);
        Assert.EndsWith(".xlsx", strUrl);
    }

    [Fact]
    public async Task Export_FilterByGroupName_SingleMatch()
    {
        var req = new Sys_Group_ExportReq { GroupName = "Quản lý" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        Assert.NotNull(root.GetProperty("strUrl").GetString());
    }

    [Fact]
    public async Task Export_FilterByGroupName_NoMatch_ReturnsNoData()
    {
        var req = new Sys_Group_ExportReq { GroupName = "KHONG_CO_NHOM_NAY" };
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
    public async Task Export_FilterByFlagActive()
    {
        var req = new Sys_Group_ExportReq { FlagActive = "0" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
    }

    [Fact]
    public async Task Export_ViaModelJson_Success()
    {
        var req = new Sys_Group_ExportReq { Model = "{\"GroupName\":\"Quản lý\"}" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
    }

    private static IFormFile CreateExcelFormFile(string fileName, string[] headers, List<string?[]> dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sys_Group");
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
        }
        for (int r = 0; r < dataRows.Count; r++)
        {
            for (int c = 0; c < dataRows[r].Length; c++)
            {
                var val = dataRows[r][c];
                if (val != null)
                {
                    ws.Cell(r + 2, c + 1).Value = val;
                }
            }
        }
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return new FormFile(ms, 0, ms.Length, "file", fileName);
    }

    private static byte[] CreateExcelBytes(string[] headers, List<string?[]> dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sys_Group");
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
        }
        for (int r = 0; r < dataRows.Count; r++)
        {
            for (int c = 0; c < dataRows[r].Length; c++)
            {
                var val = dataRows[r][c];
                if (val != null)
                {
                    ws.Cell(r + 2, c + 1).Value = val;
                }
            }
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task Import_NullFile_ReturnsFail_InvalidFile()
    {
        var res = await _controller.Import(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không hợp lệ!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_EmptyFile_ReturnsFail_InvalidFile()
    {
        var emptyFile = new FormFile(new MemoryStream(Array.Empty<byte>()), 0, 0, "file", "test.xlsx");
        var res = await _controller.Import(emptyFile);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không hợp lệ!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidExtension_ReturnsFail_InvalidFile()
    {
        var content = Encoding.UTF8.GetBytes("not excel");
        var txtFile = new FormFile(new MemoryStream(content), 0, content.Length, "file", "test.txt");
        var res = await _controller.Import(txtFile);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không hợp lệ!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_EmptyDataRows_ReturnsFail_NoData()
    {
        var file = CreateExcelFormFile("empty.xlsx", new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" }, new List<string?[]>());
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không có dữ liệu!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidColumnCount_ReturnsFail_InvalidFile()
    {
        var file = CreateExcelFormFile("3cols.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng", "Cột thừa" },
            new List<string?[]> { new string?[] { "GRP_NEW", "Nhóm Mới", "Thừa" } });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không hợp lệ!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_EmptyGroupCode_ReturnsFail()
    {
        var file = CreateExcelFormFile("empty_code.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]> { new string?[] { "", "Nhóm thiếu mã" } });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã nhóm người dùng không được trống!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_EmptyGroupName_ReturnsFail()
    {
        var file = CreateExcelFormFile("empty_name.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]> { new string?[] { "GRP_NONAME", "" } });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Tên nhóm người dùng không được trống!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_DuplicateInExcel_ReturnsFail()
    {
        var file = CreateExcelFormFile("duplicate.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]>
            {
                new string?[] { "GRP_DUP", "Nhóm Lặp 1" },
                new string?[] { "GRP_DUP", "Nhóm Lặp 2" }
            });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã nhóm người dùng 'GRP_DUP' bị lặp trong file excel!", root.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_GroupCodeAlreadyExistsInDB_ReturnsFail()
    {
        var file = CreateExcelFormFile("exist.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]> { new string?[] { "GRP_ADMIN", "Nhóm Trùng Admin" } });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeExist", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("GRP_ADMIN", root.GetProperty("Params").GetProperty("Check.GroupCodeExist").GetString());
    }

    [Fact]
    public async Task Import_UserNotFound_ReturnsFail()
    {
        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.Headers["WAUserCode"] = "USER_NOT_EXIST";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var file = CreateExcelFormFile("valid.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]> { new string?[] { "GRP_NEW_01", "Nhóm Mới 01" } });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_UserCodeNotFound", root.GetProperty("ErrorCode").GetString());

        _controller.ControllerContext = new ControllerContext();
    }

    [Fact]
    public async Task Import_UserInactive_ReturnsFail()
    {
        _db.Set<Sys_User>().Add(new Sys_User
        {
            UserCode = "U_INACTIVE_FOR_IMP",
            UserName = "Người dùng ngưng hoạt động",
            FlagActive = "0",
            MST = "0100000001"
        });
        await _db.SaveChangesAsync();

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.Headers["WAUserCode"] = "U_INACTIVE_FOR_IMP";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var file = CreateExcelFormFile("valid.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]> { new string?[] { "GRP_NEW_02", "Nhóm Mới 02" } });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_FlagActiveNotMatched", root.GetProperty("ErrorCode").GetString());

        _controller.ControllerContext = new ControllerContext();
    }

    [Fact]
    public async Task Import_Success_SingleRow_RoundTrip()
    {
        var file = CreateExcelFormFile("single.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]> { new string?[] { "GRP_IMP_01", "Nhóm Import Đơn" } });
        var res = await _controller.Import(file);
        var okResult = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã nhập dữ liệu excel thành công!", root.GetProperty("Title").GetString());

        // Đọc lại từ DB xác minh round-trip
        var saved = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_IMP_01");
        Assert.NotNull(saved);
        Assert.Equal("Nhóm Import Đơn", saved.GroupName);
        Assert.Equal("1", saved.FlagActive);
        Assert.Equal("0100000001", saved.MST);
        Assert.Equal("U01", saved.LogLUBy);
    }

    [Fact]
    public async Task Import_Success_MultiRow_RoundTrip()
    {
        var file = CreateExcelFormFile("multi.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]>
            {
                new string?[] { "GRP_IMP_02", "Nhóm Import Hai" },
                new string?[] { "GRP_IMP_03", "Nhóm Import Ba" }
            });
        var res = await _controller.Import(file);
        var okResult = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        var g2 = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_IMP_02");
        Assert.NotNull(g2);
        Assert.Equal("Nhóm Import Hai", g2.GroupName);

        var g3 = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_IMP_03");
        Assert.NotNull(g3);
        Assert.Equal("Nhóm Import Ba", g3.GroupName);
    }

    [Fact]
    public async Task Import_PartialImport_WhenSecondRowFails_FirstRowCommitted()
    {
        var file = CreateExcelFormFile("partial.xlsx",
            new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" },
            new List<string?[]>
            {
                new string?[] { "GRP_IMP_PART1", "Nhóm Hợp Lệ Trước" },
                new string?[] { "GRP_ADMIN", "Nhóm Trùng Bị Lỗi" }
            });
        var res = await _controller.Import(file);
        var badResult = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeExist", root.GetProperty("ErrorCode").GetString());

        // Kiểm tra dòng 1 GRP_IMP_PART1 đã được commit thành công (partial import theo LESSON A.9)
        var part1 = await _db.Set<Sys_Group>().FirstOrDefaultAsync(x => x.GroupCode == "GRP_IMP_PART1");
        Assert.NotNull(part1);
        Assert.Equal("Nhóm Hợp Lệ Trước", part1.GroupName);
    }

    [Fact]
    public async Task RealServer_Verification()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_sys_group_{Guid.NewGuid():N}.db");
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

            // 1. Ca body rỗng: POST {} -> 200 OK, ItemCount=5, DataList=5
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/sys_group/index", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(5, data1.GetProperty("ItemCount").GetInt32());
            Assert.Equal(5, data1.GetProperty("DataList").GetArrayLength());

            // 2. Ca lọc GroupName="Quản lý" -> 200 OK, ItemCount=1, DataList=1 (khác số dòng ca 1)
            var req2 = new StringContent("{\"GroupName\":\"Quản lý\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/sys_group/index", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(1, data2.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data2.GetProperty("DataList").GetArrayLength());
            Assert.Equal("GRP_MANAGER", data2.GetProperty("DataList")[0].GetProperty("GroupCode").GetString());

            // 3. Ca lọc FlagActive="0" -> 200 OK, ItemCount=1, DataList=1 (GRP_INACTIVE)
            var req3 = new StringContent("{\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/sys_group/index", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(1, data3.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data3.GetProperty("DataList").GetArrayLength());
            Assert.Equal("GRP_INACTIVE", data3.GetProperty("DataList")[0].GetProperty("GroupCode").GetString());

            // 4. Ca lọc không tồn tại: GroupName="KHONG_CO" -> 200 OK, ItemCount=0, DataList=0
            var req4 = new StringContent("{\"GroupName\":\"KHONG_CO\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/sys_group/index", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(0, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, data4.GetProperty("DataList").GetArrayLength());

            // 5. Ca GetDetailData body rỗng: POST /api/sys_group/getdetaildata {} -> 200 OK, 4 dòng
            var req5 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/sys_group/getdetaildata", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal(4, data5.GetArrayLength());

            // 6. Ca GetDetailData lọc GroupCode="GRP_ADMIN" -> 200 OK, 2 dòng (U01, U02)
            var req6 = new StringContent("{\"GroupCode\":\"GRP_ADMIN\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/sys_group/getdetaildata", req6);
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(2, data6.GetArrayLength());
            Assert.Equal("U01", data6[0].GetProperty("UserCode").GetString());
            Assert.Equal("U02", data6[1].GetProperty("UserCode").GetString());

            // 7. Ca GetDetailData lọc GroupCode="GRP_MANAGER" -> 200 OK, 1 dòng (U02) - khác số dòng ca 6
            var req7 = new StringContent("{\"GroupCode\":\"GRP_MANAGER\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/sys_group/getdetaildata", req7);
            Assert.Equal(System.Net.HttpStatusCode.OK, res7.StatusCode);
            var body7 = await res7.Content.ReadAsStringAsync();
            var doc7 = JsonDocument.Parse(body7);
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            var data7 = doc7.RootElement.GetProperty("Data");
            Assert.Equal(1, data7.GetArrayLength());
            Assert.Equal("U02", data7[0].GetProperty("UserCode").GetString());

            // 8. Ca GetDetailData lọc nhóm không có user: GroupCode="GRP_STAFF" -> 200 OK, 0 dòng []
            var req8 = new StringContent("{\"GroupCode\":\"GRP_STAFF\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/sys_group/getdetaildata", req8);
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var body8 = await res8.Content.ReadAsStringAsync();
            var doc8 = JsonDocument.Parse(body8);
            Assert.True(doc8.RootElement.GetProperty("Success").GetBoolean());
            var data8 = doc8.RootElement.GetProperty("Data");
            Assert.Equal(0, data8.GetArrayLength());

            // 9. Ca GetDetailData lọc nhóm không tồn tại: GroupCode="GRP_NONEXISTENT" -> 200 OK, 0 dòng []
            var req9 = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\"}", Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/sys_group/getdetaildata", req9);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9.StatusCode);
            var body9 = await res9.Content.ReadAsStringAsync();
            var doc9 = JsonDocument.Parse(body9);
            Assert.True(doc9.RootElement.GetProperty("Success").GetBoolean());
            var data9 = doc9.RootElement.GetProperty("Data");
            Assert.Equal(0, data9.GetArrayLength());

            // 9a. Ca GetSysUser body rỗng: POST /api/sys_group/getsysuser {} -> 200 OK, ListSysUser=3, ListUserInGroup=4
            var req9a = new StringContent("{}", Encoding.UTF8, "application/json");
            var res9a = await http.PostAsync("/api/sys_group/getsysuser", req9a);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9a.StatusCode);
            var body9a = await res9a.Content.ReadAsStringAsync();
            var doc9a = JsonDocument.Parse(body9a);
            Assert.True(doc9a.RootElement.GetProperty("Success").GetBoolean());
            var data9a = doc9a.RootElement.GetProperty("Data");
            var listSysUser9a = data9a.GetProperty("ListSysUser");
            var listUserInGroup9a = data9a.GetProperty("ListUserInGroup");
            Assert.Equal(3, listSysUser9a.GetArrayLength());
            Assert.Equal(4, listUserInGroup9a.GetArrayLength());
            // Kiểm tra mật khẩu được che bằng "*********"
            for (int i = 0; i < listSysUser9a.GetArrayLength(); i++)
            {
                Assert.Equal("*********", listSysUser9a[i].GetProperty("UserPassword").GetString());
            }

            // 9b. Ca GetSysUser lọc GroupCode="GRP_ADMIN" -> 200 OK, ListSysUser=3, ListUserInGroup=2 (U01, U02)
            var req9b = new StringContent("{\"GroupCode\":\"GRP_ADMIN\"}", Encoding.UTF8, "application/json");
            var res9b = await http.PostAsync("/api/sys_group/getsysuser", req9b);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9b.StatusCode);
            var body9b = await res9b.Content.ReadAsStringAsync();
            var doc9b = JsonDocument.Parse(body9b);
            Assert.True(doc9b.RootElement.GetProperty("Success").GetBoolean());
            var data9b = doc9b.RootElement.GetProperty("Data");
            Assert.Equal("GRP_ADMIN", data9b.GetProperty("GroupCode").GetString());
            Assert.Equal(3, data9b.GetProperty("ListSysUser").GetArrayLength());
            Assert.Equal(2, data9b.GetProperty("ListUserInGroup").GetArrayLength());
            Assert.Equal("U01", data9b.GetProperty("ListUserInGroup")[0].GetProperty("UserCode").GetString());
            Assert.Equal("U02", data9b.GetProperty("ListUserInGroup")[1].GetProperty("UserCode").GetString());

            // 9c. Ca GetSysUser lọc GroupCode="GRP_MANAGER" (bộ lọc B khác số dòng ca 9b) -> 200 OK, ListSysUser=3, ListUserInGroup=1 (U02)
            var req9c = new StringContent("{\"GroupCode\":\"GRP_MANAGER\"}", Encoding.UTF8, "application/json");
            var res9c = await http.PostAsync("/api/sys_group/getsysuser", req9c);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9c.StatusCode);
            var body9c = await res9c.Content.ReadAsStringAsync();
            var doc9c = JsonDocument.Parse(body9c);
            Assert.True(doc9c.RootElement.GetProperty("Success").GetBoolean());
            var data9c = doc9c.RootElement.GetProperty("Data");
            Assert.Equal("GRP_MANAGER", data9c.GetProperty("GroupCode").GetString());
            Assert.Equal(3, data9c.GetProperty("ListSysUser").GetArrayLength());
            Assert.Equal(1, data9c.GetProperty("ListUserInGroup").GetArrayLength());
            Assert.Equal("U02", data9c.GetProperty("ListUserInGroup")[0].GetProperty("UserCode").GetString());

            // 9d. Ca GetSysUser lọc nhóm không có user: GroupCode="GRP_STAFF" -> 200 OK, ListSysUser=3, ListUserInGroup=0 []
            var req9d = new StringContent("{\"GroupCode\":\"GRP_STAFF\"}", Encoding.UTF8, "application/json");
            var res9d = await http.PostAsync("/api/sys_group/getsysuser", req9d);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9d.StatusCode);
            var body9d = await res9d.Content.ReadAsStringAsync();
            var doc9d = JsonDocument.Parse(body9d);
            Assert.True(doc9d.RootElement.GetProperty("Success").GetBoolean());
            var data9d = doc9d.RootElement.GetProperty("Data");
            Assert.Equal("GRP_STAFF", data9d.GetProperty("GroupCode").GetString());
            Assert.Equal(3, data9d.GetProperty("ListSysUser").GetArrayLength());
            Assert.Equal(0, data9d.GetProperty("ListUserInGroup").GetArrayLength());

            // 9e. Ca GetSysUser lọc nhóm không tồn tại: GroupCode="GRP_NONEXISTENT" -> 200 OK, ListSysUser=3, ListUserInGroup=0 []
            var req9e = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\"}", Encoding.UTF8, "application/json");
            var res9e = await http.PostAsync("/api/sys_group/getsysuser", req9e);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9e.StatusCode);
            var body9e = await res9e.Content.ReadAsStringAsync();
            var doc9e = JsonDocument.Parse(body9e);
            Assert.True(doc9e.RootElement.GetProperty("Success").GetBoolean());
            var data9e = doc9e.RootElement.GetProperty("Data");
            Assert.Equal("GRP_NONEXISTENT", data9e.GetProperty("GroupCode").GetString());
            Assert.Equal(3, data9e.GetProperty("ListSysUser").GetArrayLength());
            Assert.Equal(0, data9e.GetProperty("ListUserInGroup").GetArrayLength());

            // 9f. Ca GetSysUser lọc FlagActive="0" -> 200 OK, ListSysUser=0 []
            var req9f = new StringContent("{\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json");
            var res9f = await http.PostAsync("/api/sys_group/getsysuser", req9f);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9f.StatusCode);
            var body9f = await res9f.Content.ReadAsStringAsync();
            var doc9f = JsonDocument.Parse(body9f);
            Assert.True(doc9f.RootElement.GetProperty("Success").GetBoolean());
            var data9f = doc9f.RootElement.GetProperty("Data");
            Assert.Equal(0, data9f.GetProperty("ListSysUser").GetArrayLength());

            // 9g. Ca GetSysUser qua Model JSON string -> 200 OK, ListUserInGroup=2
            var req9g = new StringContent("{\"Model\":\"{\\\"GroupCode\\\":\\\"GRP_ADMIN\\\"}\"}", Encoding.UTF8, "application/json");
            var res9g = await http.PostAsync("/api/sys_group/getsysuser", req9g);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9g.StatusCode);
            var body9g = await res9g.Content.ReadAsStringAsync();
            var doc9g = JsonDocument.Parse(body9g);
            Assert.True(doc9g.RootElement.GetProperty("Success").GetBoolean());
            var data9g = doc9g.RootElement.GetProperty("Data");
            Assert.Equal("GRP_ADMIN", data9g.GetProperty("GroupCode").GetString());
            Assert.Equal(2, data9g.GetProperty("ListUserInGroup").GetArrayLength());

            // 9h. Ca GetSysUser qua query string ?groupcode=GRP_ACCOUNTANT -> 200 OK, ListUserInGroup=1
            var req9h = new StringContent("{}", Encoding.UTF8, "application/json");
            var res9h = await http.PostAsync("/api/sys_group/getsysuser?groupcode=GRP_ACCOUNTANT", req9h);
            Assert.Equal(System.Net.HttpStatusCode.OK, res9h.StatusCode);
            var body9h = await res9h.Content.ReadAsStringAsync();
            var doc9h = JsonDocument.Parse(body9h);
            Assert.True(doc9h.RootElement.GetProperty("Success").GetBoolean());
            var data9h = doc9h.RootElement.GetProperty("Data");
            Assert.Equal("GRP_ACCOUNTANT", data9h.GetProperty("GroupCode").GetString());
            Assert.Equal(1, data9h.GetProperty("ListUserInGroup").GetArrayLength());
            Assert.Equal("U03", data9h.GetProperty("ListUserInGroup")[0].GetProperty("UserCode").GetString());

            // 10. Ca Create body rỗng -> 400 Bad Request, Erridn.Skycic.QContract.Sys_Group_Create
            var req10 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res10 = await http.PostAsync("/api/sys_group/create", req10);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res10.StatusCode);
            var body10 = await res10.Content.ReadAsStringAsync();
            var doc10 = JsonDocument.Parse(body10);
            Assert.False(doc10.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Create", doc10.RootElement.GetProperty("ErrorCode").GetString());

            // 11. Ca Create GroupCode rỗng -> 400 Bad Request, Erridn.Skycic.QContract.Sys_Group_Create_InvalidGroupCode
            var req11 = new StringContent("{\"GroupCode\":\"\", \"GroupName\":\"Nhóm Test\"}", Encoding.UTF8, "application/json");
            var res11 = await http.PostAsync("/api/sys_group/create", req11);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res11.StatusCode);
            var body11 = await res11.Content.ReadAsStringAsync();
            var doc11 = JsonDocument.Parse(body11);
            Assert.False(doc11.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Create_InvalidGroupCode", doc11.RootElement.GetProperty("ErrorCode").GetString());

            // 12. Ca Create GroupCode trùng -> 400 Bad Request, Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeExist
            var req12 = new StringContent("{\"GroupCode\":\"GRP_ADMIN\", \"GroupName\":\"Trùng Admin\"}", Encoding.UTF8, "application/json");
            var res12 = await http.PostAsync("/api/sys_group/create", req12);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res12.StatusCode);
            var body12 = await res12.Content.ReadAsStringAsync();
            var doc12 = JsonDocument.Parse(body12);
            Assert.False(doc12.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeExist", doc12.RootElement.GetProperty("ErrorCode").GetString());

            // 13. Ca Create GroupName rỗng -> 400 Bad Request, Erridn.Skycic.QContract.Sys_Group_Create_InvalidGroupName
            var req13 = new StringContent("{\"GroupCode\":\"GRP_NEW_SERVER_01\", \"GroupName\":\"\"}", Encoding.UTF8, "application/json");
            var res13 = await http.PostAsync("/api/sys_group/create", req13);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res13.StatusCode);
            var body13 = await res13.Content.ReadAsStringAsync();
            var doc13 = JsonDocument.Parse(body13);
            Assert.False(doc13.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Create_InvalidGroupName", doc13.RootElement.GetProperty("ErrorCode").GetString());

            // 14. Ca Create User không tồn tại -> 400 Bad Request, Erridn.Skycic.QContract.Sys_User_CheckDB_UserCodeNotFound
            var req14 = new StringContent("{\"GroupCode\":\"GRP_NEW_SERVER_02\", \"GroupName\":\"Tên hợp lệ\", \"WAUserCode\":\"USER_KHONG_TON_TAI\"}", Encoding.UTF8, "application/json");
            var res14 = await http.PostAsync("/api/sys_group/create", req14);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res14.StatusCode);
            var body14 = await res14.Content.ReadAsStringAsync();
            var doc14 = JsonDocument.Parse(body14);
            Assert.False(doc14.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_UserCodeNotFound", doc14.RootElement.GetProperty("ErrorCode").GetString());

            // 15. Ca Create thành công round-trip: POST tạo mới -> 200 OK -> đọc lại thấy đúng
            var req15 = new StringContent("{\"GroupCode\":\"GRP_REAL_CREATED\", \"GroupName\":\"Nhóm Tạo Mới Thật Sự\"}", Encoding.UTF8, "application/json");
            var res15 = await http.PostAsync("/api/sys_group/create", req15);
            Assert.Equal(System.Net.HttpStatusCode.OK, res15.StatusCode);
            var body15 = await res15.Content.ReadAsStringAsync();
            var doc15 = JsonDocument.Parse(body15);
            Assert.True(doc15.RootElement.GetProperty("Success").GetBoolean());
            var createdData = doc15.RootElement.GetProperty("Data");
            Assert.Equal("GRP_REAL_CREATED", createdData.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Tạo Mới Thật Sự", createdData.GetProperty("GroupName").GetString());
            Assert.Equal("1", createdData.GetProperty("FlagActive").GetString());

            // Đọc lại qua /api/sys_group/index để xác minh round-trip
            var req15Read = new StringContent("{\"GroupCode\":\"GRP_REAL_CREATED\"}", Encoding.UTF8, "application/json");
            var res15Read = await http.PostAsync("/api/sys_group/index", req15Read);
            Assert.Equal(System.Net.HttpStatusCode.OK, res15Read.StatusCode);
            var body15Read = await res15Read.Content.ReadAsStringAsync();
            var doc15Read = JsonDocument.Parse(body15Read);
            Assert.True(doc15Read.RootElement.GetProperty("Success").GetBoolean());
            var data15Read = doc15Read.RootElement.GetProperty("Data");
            Assert.Equal(1, data15Read.GetProperty("ItemCount").GetInt32());
            var itemCreated = data15Read.GetProperty("DataList")[0];
            Assert.Equal("GRP_REAL_CREATED", itemCreated.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Tạo Mới Thật Sự", itemCreated.GetProperty("GroupName").GetString());
            Assert.Equal("1", itemCreated.GetProperty("FlagActive").GetString());

            // 16. Ca Update body rỗng: POST /api/sys_group/update {} -> 200 OK, Success=true, Data=null
            var req16 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res16 = await http.PostAsync("/api/sys_group/update", req16);
            Assert.Equal(System.Net.HttpStatusCode.OK, res16.StatusCode);
            var body16 = await res16.Content.ReadAsStringAsync();
            var doc16 = JsonDocument.Parse(body16);
            Assert.True(doc16.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc16.RootElement.GetProperty("Data").ValueKind);

            // 17. Ca Update nhóm không tồn tại: POST /api/sys_group/update {"GroupCode":"GRP_NONEXISTENT"} -> 200 OK, Success=true, Data=null
            var req17 = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\"}", Encoding.UTF8, "application/json");
            var res17 = await http.PostAsync("/api/sys_group/update", req17);
            Assert.Equal(System.Net.HttpStatusCode.OK, res17.StatusCode);
            var body17 = await res17.Content.ReadAsStringAsync();
            var doc17 = JsonDocument.Parse(body17);
            Assert.True(doc17.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc17.RootElement.GetProperty("Data").ValueKind);

            // 18. Ca Update nhóm GRP_ADMIN (Bộ lọc A) -> 200 OK, Success=true, Data=GRP_ADMIN
            var req18 = new StringContent("{\"GroupCode\":\"GRP_ADMIN\"}", Encoding.UTF8, "application/json");
            var res18 = await http.PostAsync("/api/sys_group/update", req18);
            Assert.Equal(System.Net.HttpStatusCode.OK, res18.StatusCode);
            var body18 = await res18.Content.ReadAsStringAsync();
            var doc18 = JsonDocument.Parse(body18);
            Assert.True(doc18.RootElement.GetProperty("Success").GetBoolean());
            var data18 = doc18.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data18.ValueKind);
            Assert.Equal("GRP_ADMIN", data18.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Quản trị hệ thống", data18.GetProperty("GroupName").GetString());
            Assert.Equal("1", data18.GetProperty("FlagActive").GetString());

            // 19. Ca Update nhóm GRP_MANAGER (Bộ lọc B, ra kết quả khác ca 18) -> 200 OK, Success=true, Data=GRP_MANAGER
            var req19 = new StringContent("{\"GroupCode\":\"GRP_MANAGER\"}", Encoding.UTF8, "application/json");
            var res19 = await http.PostAsync("/api/sys_group/update", req19);
            Assert.Equal(System.Net.HttpStatusCode.OK, res19.StatusCode);
            var body19 = await res19.Content.ReadAsStringAsync();
            var doc19 = JsonDocument.Parse(body19);
            Assert.True(doc19.RootElement.GetProperty("Success").GetBoolean());
            var data19 = doc19.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data19.ValueKind);
            Assert.Equal("GRP_MANAGER", data19.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Quản lý hợp đồng", data19.GetProperty("GroupName").GetString());
            Assert.Equal("1", data19.GetProperty("FlagActive").GetString());

            // 20. Ca Update nhóm GRP_INACTIVE kèm MST đúng 0100000002 -> 200 OK, Success=true, Data=GRP_INACTIVE
            var req20 = new StringContent("{\"GroupCode\":\"GRP_INACTIVE\", \"MST\":\"0100000002\"}", Encoding.UTF8, "application/json");
            var res20 = await http.PostAsync("/api/sys_group/update", req20);
            Assert.Equal(System.Net.HttpStatusCode.OK, res20.StatusCode);
            var body20 = await res20.Content.ReadAsStringAsync();
            var doc20 = JsonDocument.Parse(body20);
            Assert.True(doc20.RootElement.GetProperty("Success").GetBoolean());
            var data20 = doc20.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data20.ValueKind);
            Assert.Equal("GRP_INACTIVE", data20.GetProperty("GroupCode").GetString());
            Assert.Equal("0", data20.GetProperty("FlagActive").GetString());

            // 21. Ca Update nhóm GRP_INACTIVE kèm MST không khớp 0100000001 -> 200 OK, Success=true, Data=null
            var req21 = new StringContent("{\"GroupCode\":\"GRP_INACTIVE\", \"MST\":\"0100000001\"}", Encoding.UTF8, "application/json");
            var res21 = await http.PostAsync("/api/sys_group/update", req21);
            Assert.Equal(System.Net.HttpStatusCode.OK, res21.StatusCode);
            var body21 = await res21.Content.ReadAsStringAsync();
            var doc21 = JsonDocument.Parse(body21);
            Assert.True(doc21.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc21.RootElement.GetProperty("Data").ValueKind);

            // 22. Ca Update qua Model JSON string -> 200 OK, Success=true, Data=GRP_STAFF
            var req22 = new StringContent("{\"Model\":\"{\\\"GroupCode\\\":\\\"GRP_STAFF\\\"}\"}", Encoding.UTF8, "application/json");
            var res22 = await http.PostAsync("/api/sys_group/update", req22);
            Assert.Equal(System.Net.HttpStatusCode.OK, res22.StatusCode);
            var body22 = await res22.Content.ReadAsStringAsync();
            var doc22 = JsonDocument.Parse(body22);
            Assert.True(doc22.RootElement.GetProperty("Success").GetBoolean());
            var data22 = doc22.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data22.ValueKind);
            Assert.Equal("GRP_STAFF", data22.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Nhân viên kinh doanh", data22.GetProperty("GroupName").GetString());

            // 23. Ca Update qua query string ?groupcode=GRP_ACCOUNTANT -> 200 OK, Success=true, Data=GRP_ACCOUNTANT
            var req23 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res23 = await http.PostAsync("/api/sys_group/update?groupcode=GRP_ACCOUNTANT", req23);
            Assert.Equal(System.Net.HttpStatusCode.OK, res23.StatusCode);
            var body23 = await res23.Content.ReadAsStringAsync();
            var doc23 = JsonDocument.Parse(body23);
            Assert.True(doc23.RootElement.GetProperty("Success").GetBoolean());
            var data23 = doc23.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data23.ValueKind);
            Assert.Equal("GRP_ACCOUNTANT", data23.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Kế toán", data23.GetProperty("GroupName").GetString());

            // 24. Ca Edit body rỗng: POST /api/sys_group/edit {} -> 200 OK, Success=true, Messages=["Mã nhóm người dùng trống!"]
            var req24 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res24 = await http.PostAsync("/api/sys_group/edit", req24);
            Assert.Equal(System.Net.HttpStatusCode.OK, res24.StatusCode);
            var body24 = await res24.Content.ReadAsStringAsync();
            var doc24 = JsonDocument.Parse(body24);
            Assert.True(doc24.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã nhóm người dùng trống!", doc24.RootElement.GetProperty("Messages")[0].GetString());

            // 25. Ca Edit GroupCode không tồn tại: POST /api/sys_group/edit {"GroupCode":"GRP_NONEXISTENT","GroupName":"Nhóm mới"} -> 200 OK, Success=true, Messages=["Mã nhóm người dùng 'GRP_NONEXISTENT' không có trong hệ thống!"]
            var req25 = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\",\"GroupName\":\"Nhóm mới\"}", Encoding.UTF8, "application/json");
            var res25 = await http.PostAsync("/api/sys_group/edit", req25);
            Assert.Equal(System.Net.HttpStatusCode.OK, res25.StatusCode);
            var body25 = await res25.Content.ReadAsStringAsync();
            var doc25 = JsonDocument.Parse(body25);
            Assert.True(doc25.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã nhóm người dùng 'GRP_NONEXISTENT' không có trong hệ thống!", doc25.RootElement.GetProperty("Messages")[0].GetString());

            // 26. Ca Edit GroupName rỗng: POST /api/sys_group/edit {"GroupCode":"GRP_ADMIN","GroupName":""} -> 400 Bad Request, Erridn.Skycic.QContract.Sys_Group_Update_InvalidGroupName
            var req26 = new StringContent("{\"GroupCode\":\"GRP_ADMIN\",\"GroupName\":\"\"}", Encoding.UTF8, "application/json");
            var res26 = await http.PostAsync("/api/sys_group/edit", req26);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res26.StatusCode);
            var body26 = await res26.Content.ReadAsStringAsync();
            var doc26 = JsonDocument.Parse(body26);
            Assert.False(doc26.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Update_InvalidGroupName", doc26.RootElement.GetProperty("ErrorCode").GetString());

            // 27. Ca Edit thành công round-trip: POST /api/sys_group/edit {"GroupCode":"GRP_ACCOUNTANT","GroupName":"Nhóm Kế toán ĐÃ SỬA","FlagActive":"1"} -> 200 OK, Success=true
            var req27 = new StringContent("{\"GroupCode\":\"GRP_ACCOUNTANT\",\"GroupName\":\"Nhóm Kế toán ĐÃ SỬA\",\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var res27 = await http.PostAsync("/api/sys_group/edit", req27);
            Assert.Equal(System.Net.HttpStatusCode.OK, res27.StatusCode);
            var body27 = await res27.Content.ReadAsStringAsync();
            var doc27 = JsonDocument.Parse(body27);
            Assert.True(doc27.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Sửa nhóm người dùng thành công!", doc27.RootElement.GetProperty("Messages")[0].GetString());

            // 28. Đọc lại qua /api/sys_group/update để xác minh round-trip
            var req28 = new StringContent("{\"GroupCode\":\"GRP_ACCOUNTANT\"}", Encoding.UTF8, "application/json");
            var res28 = await http.PostAsync("/api/sys_group/update", req28);
            Assert.Equal(System.Net.HttpStatusCode.OK, res28.StatusCode);
            var body28 = await res28.Content.ReadAsStringAsync();
            var doc28 = JsonDocument.Parse(body28);
            Assert.True(doc28.RootElement.GetProperty("Success").GetBoolean());
            var data28 = doc28.RootElement.GetProperty("Data");
            Assert.Equal("GRP_ACCOUNTANT", data28.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Kế toán ĐÃ SỬA", data28.GetProperty("GroupName").GetString());

            // 29. Ca Edit qua Model JSON string: POST /api/sys_group/edit {"Model":"{\"GroupCode\":\"GRP_INACTIVE\",\"GroupName\":\"Nhóm Tái Kích Hoạt\",\"FlagActive\":\"1\"}"}
            var req29 = new StringContent("{\"Model\":\"{\\\"GroupCode\\\":\\\"GRP_INACTIVE\\\",\\\"GroupName\\\":\\\"Nhóm Tái Kích Hoạt\\\",\\\"FlagActive\\\":\\\"1\\\"}\"}", Encoding.UTF8, "application/json");
            var res29 = await http.PostAsync("/api/sys_group/edit", req29);
            Assert.Equal(System.Net.HttpStatusCode.OK, res29.StatusCode);
            var body29 = await res29.Content.ReadAsStringAsync();
            var doc29 = JsonDocument.Parse(body29);
            Assert.True(doc29.RootElement.GetProperty("Success").GetBoolean());

            // 30. Đọc lại qua /api/sys_group/update xác minh FlagActive="1" và GroupName="Nhóm Tái Kích Hoạt"
            var req30 = new StringContent("{\"GroupCode\":\"GRP_INACTIVE\"}", Encoding.UTF8, "application/json");
            var res30 = await http.PostAsync("/api/sys_group/update", req30);
            Assert.Equal(System.Net.HttpStatusCode.OK, res30.StatusCode);
            var body30 = await res30.Content.ReadAsStringAsync();
            var doc30 = JsonDocument.Parse(body30);
            Assert.True(doc30.RootElement.GetProperty("Success").GetBoolean());
            var data30 = doc30.RootElement.GetProperty("Data");
            Assert.Equal("GRP_INACTIVE", data30.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Tái Kích Hoạt", data30.GetProperty("GroupName").GetString());
            Assert.Equal("1", data30.GetProperty("FlagActive").GetString());

            // 31. Ca Detail body rỗng: POST /api/sys_group/detail {} -> 200 OK, Success=true, Data=null
            var req31 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res31 = await http.PostAsync("/api/sys_group/detail", req31);
            Assert.Equal(System.Net.HttpStatusCode.OK, res31.StatusCode);
            var body31 = await res31.Content.ReadAsStringAsync();
            var doc31 = JsonDocument.Parse(body31);
            Assert.True(doc31.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc31.RootElement.GetProperty("Data").ValueKind);

            // 32. Ca Detail nhóm không tồn tại: POST /api/sys_group/detail {"GroupCode":"GRP_NONEXISTENT"} -> 200 OK, Success=true, Data=null
            var req32 = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\"}", Encoding.UTF8, "application/json");
            var res32 = await http.PostAsync("/api/sys_group/detail", req32);
            Assert.Equal(System.Net.HttpStatusCode.OK, res32.StatusCode);
            var body32 = await res32.Content.ReadAsStringAsync();
            var doc32 = JsonDocument.Parse(body32);
            Assert.True(doc32.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc32.RootElement.GetProperty("Data").ValueKind);

            // 33. Ca Detail nhóm GRP_ADMIN (Bộ lọc A) -> 200 OK, Success=true, Data=GRP_ADMIN
            var req33 = new StringContent("{\"GroupCode\":\"GRP_ADMIN\"}", Encoding.UTF8, "application/json");
            var res33 = await http.PostAsync("/api/sys_group/detail", req33);
            Assert.Equal(System.Net.HttpStatusCode.OK, res33.StatusCode);
            var body33 = await res33.Content.ReadAsStringAsync();
            var doc33 = JsonDocument.Parse(body33);
            Assert.True(doc33.RootElement.GetProperty("Success").GetBoolean());
            var data33 = doc33.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data33.ValueKind);
            Assert.Equal("GRP_ADMIN", data33.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Quản trị hệ thống", data33.GetProperty("GroupName").GetString());
            Assert.Equal("1", data33.GetProperty("FlagActive").GetString());

            // 34. Ca Detail nhóm GRP_MANAGER (Bộ lọc B, ra kết quả khác ca 33) -> 200 OK, Success=true, Data=GRP_MANAGER
            var req34 = new StringContent("{\"GroupCode\":\"GRP_MANAGER\"}", Encoding.UTF8, "application/json");
            var res34 = await http.PostAsync("/api/sys_group/detail", req34);
            Assert.Equal(System.Net.HttpStatusCode.OK, res34.StatusCode);
            var body34 = await res34.Content.ReadAsStringAsync();
            var doc34 = JsonDocument.Parse(body34);
            Assert.True(doc34.RootElement.GetProperty("Success").GetBoolean());
            var data34 = doc34.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data34.ValueKind);
            Assert.Equal("GRP_MANAGER", data34.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Quản lý hợp đồng", data34.GetProperty("GroupName").GetString());
            Assert.Equal("1", data34.GetProperty("FlagActive").GetString());

            // 35. Ca Detail nhóm GRP_INACTIVE kèm MST đúng 0100000002 -> 200 OK, Success=true, Data=GRP_INACTIVE
            var req35 = new StringContent("{\"GroupCode\":\"GRP_INACTIVE\", \"MST\":\"0100000002\"}", Encoding.UTF8, "application/json");
            var res35 = await http.PostAsync("/api/sys_group/detail", req35);
            Assert.Equal(System.Net.HttpStatusCode.OK, res35.StatusCode);
            var body35 = await res35.Content.ReadAsStringAsync();
            var doc35 = JsonDocument.Parse(body35);
            Assert.True(doc35.RootElement.GetProperty("Success").GetBoolean());
            var data35 = doc35.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data35.ValueKind);
            Assert.Equal("GRP_INACTIVE", data35.GetProperty("GroupCode").GetString());
            Assert.Equal("0100000002", data35.GetProperty("MST").GetString());

            // 36. Ca Detail nhóm GRP_INACTIVE kèm MST không khớp 0100000001 -> 200 OK, Success=true, Data=null
            var req36 = new StringContent("{\"GroupCode\":\"GRP_INACTIVE\", \"MST\":\"0100000001\"}", Encoding.UTF8, "application/json");
            var res36 = await http.PostAsync("/api/sys_group/detail", req36);
            Assert.Equal(System.Net.HttpStatusCode.OK, res36.StatusCode);
            var body36 = await res36.Content.ReadAsStringAsync();
            var doc36 = JsonDocument.Parse(body36);
            Assert.True(doc36.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc36.RootElement.GetProperty("Data").ValueKind);

            // 37. Ca Detail qua Model JSON string -> 200 OK, Success=true, Data=GRP_STAFF
            var req37 = new StringContent("{\"Model\":\"{\\\"GroupCode\\\":\\\"GRP_STAFF\\\"}\"}", Encoding.UTF8, "application/json");
            var res37 = await http.PostAsync("/api/sys_group/detail", req37);
            Assert.Equal(System.Net.HttpStatusCode.OK, res37.StatusCode);
            var body37 = await res37.Content.ReadAsStringAsync();
            var doc37 = JsonDocument.Parse(body37);
            Assert.True(doc37.RootElement.GetProperty("Success").GetBoolean());
            var data37 = doc37.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data37.ValueKind);
            Assert.Equal("GRP_STAFF", data37.GetProperty("GroupCode").GetString());

            // 38. Ca Detail qua query string ?groupcode=GRP_ACCOUNTANT -> 200 OK, Success=true, Data=GRP_ACCOUNTANT
            var req38 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res38 = await http.PostAsync("/api/sys_group/detail?groupcode=GRP_ACCOUNTANT", req38);
            Assert.Equal(System.Net.HttpStatusCode.OK, res38.StatusCode);
            var body38 = await res38.Content.ReadAsStringAsync();
            var doc38 = JsonDocument.Parse(body38);
            Assert.True(doc38.RootElement.GetProperty("Success").GetBoolean());
            var data38 = doc38.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data38.ValueKind);
            Assert.Equal("GRP_ACCOUNTANT", data38.GetProperty("GroupCode").GetString());

            // 39. Ca Delete body rỗng: POST /api/sys_group/delete {} -> 200 OK, Success=false, Messages=["Mã nhóm người dùng trống!"]
            var req39 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res39 = await http.PostAsync("/api/sys_group/delete", req39);
            Assert.Equal(System.Net.HttpStatusCode.OK, res39.StatusCode);
            var body39 = await res39.Content.ReadAsStringAsync();
            var doc39 = JsonDocument.Parse(body39);
            Assert.False(doc39.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã nhóm người dùng trống!", doc39.RootElement.GetProperty("Messages")[0].GetString());

            // 40. Ca Delete GroupCode rỗng: POST /api/sys_group/delete {"GroupCode":""} -> 200 OK, Success=false, Messages=["Mã nhóm người dùng trống!"]
            var req40 = new StringContent("{\"GroupCode\":\"\"}", Encoding.UTF8, "application/json");
            var res40 = await http.PostAsync("/api/sys_group/delete", req40);
            Assert.Equal(System.Net.HttpStatusCode.OK, res40.StatusCode);
            var body40 = await res40.Content.ReadAsStringAsync();
            var doc40 = JsonDocument.Parse(body40);
            Assert.False(doc40.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã nhóm người dùng trống!", doc40.RootElement.GetProperty("Messages")[0].GetString());

            // 41. Ca Delete GroupCode không tồn tại: POST /api/sys_group/delete {"GroupCode":"GRP_NONEXISTENT"} -> 200 OK, Success=false, Messages=["Mã nhóm người dùng 'GRP_NONEXISTENT' không có trong hệ thống!"]
            var req41 = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\"}", Encoding.UTF8, "application/json");
            var res41 = await http.PostAsync("/api/sys_group/delete", req41);
            Assert.Equal(System.Net.HttpStatusCode.OK, res41.StatusCode);
            var body41 = await res41.Content.ReadAsStringAsync();
            var doc41 = JsonDocument.Parse(body41);
            Assert.False(doc41.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã nhóm người dùng 'GRP_NONEXISTENT' không có trong hệ thống!", doc41.RootElement.GetProperty("Messages")[0].GetString());

            // 42. Ca Delete thành công round-trip: Xóa nhóm GRP_REAL_CREATED vừa tạo ở ca 15
            var req42 = new StringContent("{\"GroupCode\":\"GRP_REAL_CREATED\"}", Encoding.UTF8, "application/json");
            var res42 = await http.PostAsync("/api/sys_group/delete", req42);
            Assert.Equal(System.Net.HttpStatusCode.OK, res42.StatusCode);
            var body42 = await res42.Content.ReadAsStringAsync();
            var doc42 = JsonDocument.Parse(body42);
            Assert.True(doc42.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa nhóm người dùng 'GRP_REAL_CREATED' thành công!", doc42.RootElement.GetProperty("Messages")[0].GetString());

            // Đọc lại qua /api/sys_group/index để xác minh nhóm GRP_REAL_CREATED đã bị xóa hoàn toàn (round-trip)
            var req42Read = new StringContent("{\"GroupCode\":\"GRP_REAL_CREATED\"}", Encoding.UTF8, "application/json");
            var res42Read = await http.PostAsync("/api/sys_group/index", req42Read);
            Assert.Equal(System.Net.HttpStatusCode.OK, res42Read.StatusCode);
            var body42Read = await res42Read.Content.ReadAsStringAsync();
            var doc42Read = JsonDocument.Parse(body42Read);
            Assert.True(doc42Read.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, doc42Read.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 43. Ca Delete qua Model JSON string: Xóa nhóm GRP_INACTIVE
            var req43 = new StringContent("{\"Model\":\"{\\\"GroupCode\\\":\\\"GRP_INACTIVE\\\"}\"}", Encoding.UTF8, "application/json");
            var res43 = await http.PostAsync("/api/sys_group/delete", req43);
            Assert.Equal(System.Net.HttpStatusCode.OK, res43.StatusCode);
            var body43 = await res43.Content.ReadAsStringAsync();
            var doc43 = JsonDocument.Parse(body43);
            Assert.True(doc43.RootElement.GetProperty("Success").GetBoolean());

            // 44. Ca Delete qua query string ?groupcode=GRP_ACCOUNTANT
            var req44 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res44 = await http.PostAsync("/api/sys_group/delete?groupcode=GRP_ACCOUNTANT", req44);
            Assert.Equal(System.Net.HttpStatusCode.OK, res44.StatusCode);
            var body44 = await res44.Content.ReadAsStringAsync();
            var doc44 = JsonDocument.Parse(body44);
            Assert.True(doc44.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa nhóm người dùng 'GRP_ACCOUNTANT' thành công!", doc44.RootElement.GetProperty("Messages")[0].GetString());

            // 45. Ca SaveUserInGroup body rỗng: POST /api/sys_group/saveuseringroup {} -> 200 OK, Success=true, Title="Chưa chọn người dùng gán vào nhóm!"
            var req45 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res45 = await http.PostAsync("/api/sys_group/saveuseringroup", req45);
            Assert.Equal(System.Net.HttpStatusCode.OK, res45.StatusCode);
            var body45 = await res45.Content.ReadAsStringAsync();
            var doc45 = JsonDocument.Parse(body45);
            Assert.True(doc45.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Chưa chọn người dùng gán vào nhóm!", doc45.RootElement.GetProperty("Title").GetString());

            // 46. Ca SaveUserInGroup nhóm không tồn tại: POST /api/sys_group/saveuseringroup {"GroupCode":"GRP_NONEXISTENT", "Lst_Sys_UserInGroup":[{"UserCode":"U01"}]} -> 400 Bad Request, Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeNotFound
            var req46 = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\", \"Lst_Sys_UserInGroup\":[{\"UserCode\":\"U01\"}]}", Encoding.UTF8, "application/json");
            var res46 = await http.PostAsync("/api/sys_group/saveuseringroup", req46);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res46.StatusCode);
            var body46 = await res46.Content.ReadAsStringAsync();
            var doc46 = JsonDocument.Parse(body46);
            Assert.False(doc46.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeNotFound", doc46.RootElement.GetProperty("ErrorCode").GetString());

            // 47. Ca SaveUserInGroup user thực hiện không tồn tại: POST /api/sys_group/saveuseringroup {"GroupCode":"GRP_STAFF", "WAUserCode":"USER_NOT_EXIST", "Lst_Sys_UserInGroup":[{"UserCode":"U01"}]} -> 400 Bad Request, Erridn.Skycic.QContract.Sys_User_CheckDB_UserCodeNotFound
            var req47 = new StringContent("{\"GroupCode\":\"GRP_STAFF\", \"WAUserCode\":\"USER_NOT_EXIST\", \"Lst_Sys_UserInGroup\":[{\"UserCode\":\"U01\"}]}", Encoding.UTF8, "application/json");
            var res47 = await http.PostAsync("/api/sys_group/saveuseringroup", req47);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res47.StatusCode);
            var body47 = await res47.Content.ReadAsStringAsync();
            var doc47 = JsonDocument.Parse(body47);
            Assert.False(doc47.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_User_CheckDB_UserCodeNotFound", doc47.RootElement.GetProperty("ErrorCode").GetString());

            // 48. Ca SaveUserInGroup gán người dùng thành công round-trip: Gán U01, U03 vào GRP_STAFF
            var req48 = new StringContent("{\"GroupCode\":\"GRP_STAFF\", \"Lst_Sys_UserInGroup\":[{\"UserCode\":\"U01\"},{\"UserCode\":\"U03\"}]}", Encoding.UTF8, "application/json");
            var res48 = await http.PostAsync("/api/sys_group/saveuseringroup", req48);
            Assert.Equal(System.Net.HttpStatusCode.OK, res48.StatusCode);
            var body48 = await res48.Content.ReadAsStringAsync();
            var doc48 = JsonDocument.Parse(body48);
            Assert.True(doc48.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Gán người dùng vào nhóm thành công!", doc48.RootElement.GetProperty("Title").GetString());

            // Đọc lại qua /api/sys_group/getsysuser xác minh round-trip gán thành công 2 user
            var req48Read = new StringContent("{\"GroupCode\":\"GRP_STAFF\"}", Encoding.UTF8, "application/json");
            var res48Read = await http.PostAsync("/api/sys_group/getsysuser", req48Read);
            Assert.Equal(System.Net.HttpStatusCode.OK, res48Read.StatusCode);
            var body48Read = await res48Read.Content.ReadAsStringAsync();
            var doc48Read = JsonDocument.Parse(body48Read);
            Assert.True(doc48Read.RootElement.GetProperty("Success").GetBoolean());
            var listUser48 = doc48Read.RootElement.GetProperty("Data").GetProperty("ListUserInGroup");
            Assert.Equal(2, listUser48.GetArrayLength());
            Assert.Equal("U01", listUser48[0].GetProperty("UserCode").GetString());
            Assert.Equal("U03", listUser48[1].GetProperty("UserCode").GetString());

            // 49. Ca SaveUserInGroup xoá người dùng khỏi nhóm thành công round-trip: Xoá hết user khỏi GRP_STAFF
            var req49 = new StringContent("{\"GroupCode\":\"GRP_STAFF\", \"Lst_Sys_UserInGroup\":[]}", Encoding.UTF8, "application/json");
            var res49 = await http.PostAsync("/api/sys_group/saveuseringroup", req49);
            Assert.Equal(System.Net.HttpStatusCode.OK, res49.StatusCode);
            var body49 = await res49.Content.ReadAsStringAsync();
            var doc49 = JsonDocument.Parse(body49);
            Assert.True(doc49.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa người dùng khỏi nhóm thành công!", doc49.RootElement.GetProperty("Title").GetString());

            // Đọc lại qua /api/sys_group/getsysuser xác minh round-trip xoá thành công (0 user)
            var req49Read = new StringContent("{\"GroupCode\":\"GRP_STAFF\"}", Encoding.UTF8, "application/json");
            var res49Read = await http.PostAsync("/api/sys_group/getsysuser", req49Read);
            Assert.Equal(System.Net.HttpStatusCode.OK, res49Read.StatusCode);
            var body49Read = await res49Read.Content.ReadAsStringAsync();
            var doc49Read = JsonDocument.Parse(body49Read);
            Assert.True(doc49Read.RootElement.GetProperty("Success").GetBoolean());
            var listUser49 = doc49Read.RootElement.GetProperty("Data").GetProperty("ListUserInGroup");
            Assert.Equal(0, listUser49.GetArrayLength());

            // 50. Ca SaveUserInGroup qua Model JSON string: Gán U02 vào GRP_STAFF
            var req50 = new StringContent("{\"Model\":\"{\\\"Sys_Group\\\":{\\\"GroupCode\\\":\\\"GRP_STAFF\\\"},\\\"Lst_Sys_UserInGroup\\\":[{\\\"UserCode\\\":\\\"U02\\\"}]}\"}", Encoding.UTF8, "application/json");
            var res50 = await http.PostAsync("/api/sys_group/saveuseringroup", req50);
            Assert.Equal(System.Net.HttpStatusCode.OK, res50.StatusCode);
            var body50 = await res50.Content.ReadAsStringAsync();
            var doc50 = JsonDocument.Parse(body50);
            Assert.True(doc50.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Gán người dùng vào nhóm thành công!", doc50.RootElement.GetProperty("Title").GetString());

            // Đọc lại qua /api/sys_group/getsysuser xác minh có 1 user (U02)
            var req50Read = new StringContent("{\"GroupCode\":\"GRP_STAFF\"}", Encoding.UTF8, "application/json");
            var res50Read = await http.PostAsync("/api/sys_group/getsysuser", req50Read);
            Assert.Equal(System.Net.HttpStatusCode.OK, res50Read.StatusCode);
            var body50Read = await res50Read.Content.ReadAsStringAsync();
            var doc50Read = JsonDocument.Parse(body50Read);
            Assert.True(doc50Read.RootElement.GetProperty("Success").GetBoolean());
            var listUser50 = doc50Read.RootElement.GetProperty("Data").GetProperty("ListUserInGroup");
            Assert.Equal(1, listUser50.GetArrayLength());
            Assert.Equal("U02", listUser50[0].GetProperty("UserCode").GetString());

            // 51. Ca GetSysObject body rỗng: POST /api/sys_group/getsysobject {} -> 200 OK, ListSysObject=4, ListSysAccess=4
            var req51 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res51 = await http.PostAsync("/api/sys_group/getsysobject", req51);
            Assert.Equal(System.Net.HttpStatusCode.OK, res51.StatusCode);
            var body51 = await res51.Content.ReadAsStringAsync();
            var doc51 = JsonDocument.Parse(body51);
            Assert.True(doc51.RootElement.GetProperty("Success").GetBoolean());
            var data51 = doc51.RootElement.GetProperty("Data");
            Assert.Equal(4, data51.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal(4, data51.GetProperty("ListSysAccess").GetArrayLength());

            // 52. Ca GetSysObject lọc GroupCode="ADMIN" (Bộ lọc A) -> 200 OK, ListSysObject=4, ListSysAccess=3
            var req52 = new StringContent("{\"GroupCode\":\"ADMIN\"}", Encoding.UTF8, "application/json");
            var res52 = await http.PostAsync("/api/sys_group/getsysobject", req52);
            Assert.Equal(System.Net.HttpStatusCode.OK, res52.StatusCode);
            var body52 = await res52.Content.ReadAsStringAsync();
            var doc52 = JsonDocument.Parse(body52);
            Assert.True(doc52.RootElement.GetProperty("Success").GetBoolean());
            var data52 = doc52.RootElement.GetProperty("Data");
            Assert.Equal("ADMIN", data52.GetProperty("GroupCode").GetString());
            Assert.Equal(4, data52.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal(3, data52.GetProperty("ListSysAccess").GetArrayLength());
            Assert.Equal("ADMIN", data52.GetProperty("ListSysAccess")[0].GetProperty("GroupCode").GetString());
            Assert.Equal("Quản lý hợp đồng", data52.GetProperty("ListSysAccess")[0].GetProperty("so_ObjectName").GetString());

            // 53. Ca GetSysObject lọc GroupCode="SALE_MGR" (Bộ lọc B, ra kết quả khác ca 52) -> 200 OK, ListSysObject=4, ListSysAccess=1
            var req53 = new StringContent("{\"GroupCode\":\"SALE_MGR\"}", Encoding.UTF8, "application/json");
            var res53 = await http.PostAsync("/api/sys_group/getsysobject", req53);
            Assert.Equal(System.Net.HttpStatusCode.OK, res53.StatusCode);
            var body53 = await res53.Content.ReadAsStringAsync();
            var doc53 = JsonDocument.Parse(body53);
            Assert.True(doc53.RootElement.GetProperty("Success").GetBoolean());
            var data53 = doc53.RootElement.GetProperty("Data");
            Assert.Equal("SALE_MGR", data53.GetProperty("GroupCode").GetString());
            Assert.Equal(4, data53.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal(1, data53.GetProperty("ListSysAccess").GetArrayLength());
            Assert.Equal("SALE_MGR", data53.GetProperty("ListSysAccess")[0].GetProperty("GroupCode").GetString());
            Assert.Equal("MNU_CONTRACT", data53.GetProperty("ListSysAccess")[0].GetProperty("ObjectCode").GetString());

            // 54. Ca GetSysObject lọc nhóm chưa được gán quyền: GroupCode="GRP_STAFF" -> 200 OK, ListSysObject=4, ListSysAccess=0
            var req54 = new StringContent("{\"GroupCode\":\"GRP_STAFF\"}", Encoding.UTF8, "application/json");
            var res54 = await http.PostAsync("/api/sys_group/getsysobject", req54);
            Assert.Equal(System.Net.HttpStatusCode.OK, res54.StatusCode);
            var body54 = await res54.Content.ReadAsStringAsync();
            var doc54 = JsonDocument.Parse(body54);
            Assert.True(doc54.RootElement.GetProperty("Success").GetBoolean());
            var data54 = doc54.RootElement.GetProperty("Data");
            Assert.Equal("GRP_STAFF", data54.GetProperty("GroupCode").GetString());
            Assert.Equal(4, data54.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal(0, data54.GetProperty("ListSysAccess").GetArrayLength());

            // 55. Ca GetSysObject lọc nhóm không tồn tại: GroupCode="GRP_NONEXISTENT" -> 200 OK, ListSysObject=4, ListSysAccess=0
            var req55 = new StringContent("{\"GroupCode\":\"GRP_NONEXISTENT\"}", Encoding.UTF8, "application/json");
            var res55 = await http.PostAsync("/api/sys_group/getsysobject", req55);
            Assert.Equal(System.Net.HttpStatusCode.OK, res55.StatusCode);
            var body55 = await res55.Content.ReadAsStringAsync();
            var doc55 = JsonDocument.Parse(body55);
            Assert.True(doc55.RootElement.GetProperty("Success").GetBoolean());
            var data55 = doc55.RootElement.GetProperty("Data");
            Assert.Equal("GRP_NONEXISTENT", data55.GetProperty("GroupCode").GetString());
            Assert.Equal(4, data55.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal(0, data55.GetProperty("ListSysAccess").GetArrayLength());

            // 56. Ca GetSysObject qua Model JSON string -> 200 OK, ListSysAccess=3
            var req56 = new StringContent("{\"Model\":\"{\\\"GroupCode\\\":\\\"ADMIN\\\"}\"}", Encoding.UTF8, "application/json");
            var res56 = await http.PostAsync("/api/sys_group/getsysobject", req56);
            Assert.Equal(System.Net.HttpStatusCode.OK, res56.StatusCode);
            var body56 = await res56.Content.ReadAsStringAsync();
            var doc56 = JsonDocument.Parse(body56);
            Assert.True(doc56.RootElement.GetProperty("Success").GetBoolean());
            var data56 = doc56.RootElement.GetProperty("Data");
            Assert.Equal("ADMIN", data56.GetProperty("GroupCode").GetString());
            Assert.Equal(3, data56.GetProperty("ListSysAccess").GetArrayLength());

            // 57. Ca GetSysObject qua query string ?groupcode=SALE_MGR -> 200 OK, ListSysAccess=1
            var req57 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res57 = await http.PostAsync("/api/sys_group/getsysobject?groupcode=SALE_MGR", req57);
            Assert.Equal(System.Net.HttpStatusCode.OK, res57.StatusCode);
            var body57 = await res57.Content.ReadAsStringAsync();
            var doc57 = JsonDocument.Parse(body57);
            Assert.True(doc57.RootElement.GetProperty("Success").GetBoolean());
            var data57 = doc57.RootElement.GetProperty("Data");
            Assert.Equal("SALE_MGR", data57.GetProperty("GroupCode").GetString());
            Assert.Equal(1, data57.GetProperty("ListSysAccess").GetArrayLength());

            // 58. Ca GetSysObject lọc FlagActive="0" -> 200 OK, ListSysObject=1 (MNU_ARCHIVED), ListSysAccess=0
            var req58 = new StringContent("{\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json");
            var res58 = await http.PostAsync("/api/sys_group/getsysobject", req58);
            Assert.Equal(System.Net.HttpStatusCode.OK, res58.StatusCode);
            var body58 = await res58.Content.ReadAsStringAsync();
            var doc58 = JsonDocument.Parse(body58);
            Assert.True(doc58.RootElement.GetProperty("Success").GetBoolean());
            var data58 = doc58.RootElement.GetProperty("Data");
            Assert.Equal(1, data58.GetProperty("ListSysObject").GetArrayLength());
            Assert.Equal("MNU_ARCHIVED", data58.GetProperty("ListSysObject")[0].GetProperty("ObjectCode").GetString());

            // 59. Ca Export body rỗng: POST /api/sys_group/export {} -> 200 OK, Success=true, Title="Đã xuất excel thành công!", CheckSuccess="1", strUrl có đuôi .xlsx
            var req59 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res59 = await http.PostAsync("/api/sys_group/export", req59);
            Assert.Equal(System.Net.HttpStatusCode.OK, res59.StatusCode);
            var body59 = await res59.Content.ReadAsStringAsync();
            var doc59 = JsonDocument.Parse(body59);
            Assert.True(doc59.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc59.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc59.RootElement.GetProperty("CheckSuccess").GetString());
            var url59 = doc59.RootElement.GetProperty("strUrl").GetString();
            Assert.NotNull(url59);
            Assert.Contains("/TempFiles/", url59);
            Assert.EndsWith(".xlsx", url59);

            // 60. Ca Export lọc GroupName="Quản lý" (Bộ lọc A) -> 200 OK, Success=true, Title="Đã xuất excel thành công!"
            var req60 = new StringContent("{\"GroupName\":\"Quản lý\"}", Encoding.UTF8, "application/json");
            var res60 = await http.PostAsync("/api/sys_group/export", req60);
            Assert.Equal(System.Net.HttpStatusCode.OK, res60.StatusCode);
            var body60 = await res60.Content.ReadAsStringAsync();
            var doc60 = JsonDocument.Parse(body60);
            Assert.True(doc60.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc60.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc60.RootElement.GetProperty("CheckSuccess").GetString());
            Assert.NotNull(doc60.RootElement.GetProperty("strUrl").GetString());

            // 61. Ca Export lọc GroupName="Nhóm" (Bộ lọc B, ra kết quả khác ca 60) -> 200 OK, Success=true
            var req61 = new StringContent("{\"GroupName\":\"Nhóm\"}", Encoding.UTF8, "application/json");
            var res61 = await http.PostAsync("/api/sys_group/export", req61);
            Assert.Equal(System.Net.HttpStatusCode.OK, res61.StatusCode);
            var body61 = await res61.Content.ReadAsStringAsync();
            var doc61 = JsonDocument.Parse(body61);
            Assert.True(doc61.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc61.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc61.RootElement.GetProperty("CheckSuccess").GetString());

            // 62. Ca Export lọc không có dữ liệu: GroupName="KHONG_CO_NHOM_NAY" -> 200 OK, Success=true, Title="Không có dữ liệu!", CheckSuccess="1"
            var req62 = new StringContent("{\"GroupName\":\"KHONG_CO_NHOM_NAY\"}", Encoding.UTF8, "application/json");
            var res62 = await http.PostAsync("/api/sys_group/export", req62);
            Assert.Equal(System.Net.HttpStatusCode.OK, res62.StatusCode);
            var body62 = await res62.Content.ReadAsStringAsync();
            var doc62 = JsonDocument.Parse(body62);
            Assert.True(doc62.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Không có dữ liệu!", doc62.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc62.RootElement.GetProperty("CheckSuccess").GetString());
            Assert.False(doc62.RootElement.TryGetProperty("strUrl", out _));

            // 63. Ca Export qua query string ?groupname=Quản%20trị -> 200 OK, Success=true
            var req63 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res63 = await http.PostAsync("/api/sys_group/export?groupname=Quản%20trị", req63);
            Assert.Equal(System.Net.HttpStatusCode.OK, res63.StatusCode);
            var body63 = await res63.Content.ReadAsStringAsync();
            var doc63 = JsonDocument.Parse(body63);
            Assert.True(doc63.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc63.RootElement.GetProperty("Title").GetString());

            // 64. Ca Export qua Model JSON string -> 200 OK, Success=true (lọc "kinh doanh" khớp GRP_STAFF)
            var req64 = new StringContent("{\"Model\":\"{\\\"GroupName\\\":\\\"kinh doanh\\\"}\"}", Encoding.UTF8, "application/json");
            var res64 = await http.PostAsync("/api/sys_group/export", req64);
            Assert.Equal(System.Net.HttpStatusCode.OK, res64.StatusCode);
            var body64 = await res64.Content.ReadAsStringAsync();
            var doc64 = JsonDocument.Parse(body64);
            Assert.True(doc64.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc64.RootElement.GetProperty("Title").GetString());

            // 65. Ca Import body rỗng: POST /api/sys_group/import rỗng -> 400 Bad Request, "File excel import không hợp lệ!"
            var res65 = await http.PostAsync("/api/sys_group/import", new MultipartFormDataContent());
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res65.StatusCode);
            var body65 = await res65.Content.ReadAsStringAsync();
            var doc65 = JsonDocument.Parse(body65);
            Assert.False(doc65.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_Import", doc65.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("File excel import không hợp lệ!", doc65.RootElement.GetProperty("Params").GetProperty("Message").GetString());

            // 66. Ca Import file không có dữ liệu: POST /api/sys_group/import file 0 dòng -> 400 Bad Request, "File excel import không có dữ liệu!"
            var emptyBytes = CreateExcelBytes(new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" }, new List<string?[]>());
            using var form66 = new MultipartFormDataContent();
            var content66 = new ByteArrayContent(emptyBytes);
            content66.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form66.Add(content66, "file", "empty.xlsx");
            var res66 = await http.PostAsync("/api/sys_group/import", form66);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res66.StatusCode);
            var body66 = await res66.Content.ReadAsStringAsync();
            var doc66 = JsonDocument.Parse(body66);
            Assert.False(doc66.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("File excel import không có dữ liệu!", doc66.RootElement.GetProperty("Params").GetProperty("Message").GetString());

            // 67. Ca Import file sai số cột (3 cột): POST /api/sys_group/import -> 400 Bad Request, "File excel import không hợp lệ!"
            var invalidColBytes = CreateExcelBytes(new[] { "Mã nhóm người dùng", "Tên nhóm người dùng", "Thừa" }, new List<string?[]> { new string?[] { "GRP_X", "Nhóm X", "Thừa" } });
            using var form67 = new MultipartFormDataContent();
            var content67 = new ByteArrayContent(invalidColBytes);
            content67.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form67.Add(content67, "file", "invalid_cols.xlsx");
            var res67 = await http.PostAsync("/api/sys_group/import", form67);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res67.StatusCode);
            var body67 = await res67.Content.ReadAsStringAsync();
            var doc67 = JsonDocument.Parse(body67);
            Assert.False(doc67.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("File excel import không hợp lệ!", doc67.RootElement.GetProperty("Params").GetProperty("Message").GetString());

            // 68. Ca Import file có mã nhóm bị lặp: POST /api/sys_group/import -> 400 Bad Request
            var dupBytes = CreateExcelBytes(new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" }, new List<string?[]>
            {
                new string?[] { "GRP_REAL_DUP", "Nhóm Lặp 1" },
                new string?[] { "GRP_REAL_DUP", "Nhóm Lặp 2" }
            });
            using var form68 = new MultipartFormDataContent();
            var content68 = new ByteArrayContent(dupBytes);
            content68.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form68.Add(content68, "file", "duplicate.xlsx");
            var res68 = await http.PostAsync("/api/sys_group/import", form68);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res68.StatusCode);
            var body68 = await res68.Content.ReadAsStringAsync();
            var doc68 = JsonDocument.Parse(body68);
            Assert.False(doc68.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã nhóm người dùng 'GRP_REAL_DUP' bị lặp trong file excel!", doc68.RootElement.GetProperty("Params").GetProperty("Message").GetString());

            // 69. Ca Import file có mã nhóm đã tồn tại trong DB (GRP_ADMIN) -> 400 Bad Request
            var existBytes = CreateExcelBytes(new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" }, new List<string?[]>
            {
                new string?[] { "GRP_ADMIN", "Nhóm Admin Trùng" }
            });
            using var form69 = new MultipartFormDataContent();
            var content69 = new ByteArrayContent(existBytes);
            content69.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form69.Add(content69, "file", "exist.xlsx");
            var res69 = await http.PostAsync("/api/sys_group/import", form69);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res69.StatusCode);
            var body69 = await res69.Content.ReadAsStringAsync();
            var doc69 = JsonDocument.Parse(body69);
            Assert.False(doc69.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Sys_Group_CheckDB_GroupCodeExist", doc69.RootElement.GetProperty("ErrorCode").GetString());

            // 70. Ca Import thành công round-trip -> 200 OK -> đọc lại qua /api/sys_group/index thấy nhóm mới
            var validBytes = CreateExcelBytes(new[] { "Mã nhóm người dùng", "Tên nhóm người dùng" }, new List<string?[]>
            {
                new string?[] { "GRP_REAL_IMP_OK", "Nhóm Import Server Thật" }
            });
            using var form70 = new MultipartFormDataContent();
            var content70 = new ByteArrayContent(validBytes);
            content70.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form70.Add(content70, "file", "valid.xlsx");
            var res70 = await http.PostAsync("/api/sys_group/import", form70);
            Assert.Equal(System.Net.HttpStatusCode.OK, res70.StatusCode);
            var body70 = await res70.Content.ReadAsStringAsync();
            var doc70 = JsonDocument.Parse(body70);
            Assert.True(doc70.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã nhập dữ liệu excel thành công!", doc70.RootElement.GetProperty("Title").GetString());

            // Đọc lại qua /api/sys_group/index để xác minh round-trip
            var req70Read = new StringContent("{\"GroupCode\":\"GRP_REAL_IMP_OK\"}", Encoding.UTF8, "application/json");
            var res70Read = await http.PostAsync("/api/sys_group/index", req70Read);
            Assert.Equal(System.Net.HttpStatusCode.OK, res70Read.StatusCode);
            var body70Read = await res70Read.Content.ReadAsStringAsync();
            var doc70Read = JsonDocument.Parse(body70Read);
            Assert.True(doc70Read.RootElement.GetProperty("Success").GetBoolean());
            var data70Read = doc70Read.RootElement.GetProperty("Data");
            Assert.Equal(1, data70Read.GetProperty("ItemCount").GetInt32());
            var itemImported = data70Read.GetProperty("DataList")[0];
            Assert.Equal("GRP_REAL_IMP_OK", itemImported.GetProperty("GroupCode").GetString());
            Assert.Equal("Nhóm Import Server Thật", itemImported.GetProperty("GroupName").GetString());
            Assert.Equal("1", itemImported.GetProperty("FlagActive").GetString());
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
