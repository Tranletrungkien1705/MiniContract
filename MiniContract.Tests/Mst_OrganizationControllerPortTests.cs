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

        // Chạy seeder Mst_Area trước (Order = 20) rồi Mst_Org (Order = 22) và Mst_NNT (Order = 25)
        new Mst_AreaSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_OrgSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();

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
    public async Task RealServer_Verification_Port8583()
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
        psi.Environment["PORT"] = "8583";
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8583"), Timeout = TimeSpan.FromSeconds(3) };
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
