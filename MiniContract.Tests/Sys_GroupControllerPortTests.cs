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
