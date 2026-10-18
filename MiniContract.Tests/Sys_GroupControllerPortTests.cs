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
