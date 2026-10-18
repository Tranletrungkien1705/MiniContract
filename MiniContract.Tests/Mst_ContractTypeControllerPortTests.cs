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

public class Mst_ContractTypeControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_ContractTypeControllerPort _controller;

    public Mst_ContractTypeControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_ContractType
        var seeder = new Mst_ContractTypeSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Mst_SubmissionForm
        var sfSeeder = new Mst_SubmissionFormSeeder();
        sfSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_ContractTypeControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_EmptyBody_ReturnsAllSeededItems()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(5, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByNetworkIDAndFlagActive_Returns3Items()
    {
        var req = new Mst_ContractType_IndexReq
        {
            NetworkID = "1001",
            FlagActive = "1"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByNetworkID_1002_Returns1Item()
    {
        var req = new Mst_ContractType_IndexReq
        {
            NetworkID = "1002"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("HD_VAY", list[0].GetProperty("ContractType").GetString());
    }

    [Fact]
    public async Task Index_FilterNonExistent_Returns0ItemsAndPageCount0()
    {
        var req = new Mst_ContractType_IndexReq
        {
            ContractType = "NOT_EXIST"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_Pagination_Page0_Size2()
    {
        var req = new Mst_ContractType_IndexReq
        {
            Page = 0,
            RecordCount = 2
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        // Sắp xếp NetworkID asc, sau đó ContractType asc: HD_DV, HD_KT
        Assert.Equal("HD_DV", list[0].GetProperty("ContractType").GetString());
        Assert.Equal("HD_KT", list[1].GetProperty("ContractType").GetString());
    }

    [Fact]
    public async Task Index_Pagination_Page1_Size2()
    {
        var req = new Mst_ContractType_IndexReq
        {
            Page = 1,
            RecordCount = 2
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageIndex").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("HD_LD", list[0].GetProperty("ContractType").GetString());
        Assert.Equal("HD_MB", list[1].GetProperty("ContractType").GetString());
    }

    [Fact]
    public async Task Index_Pagination_Page2_Size2()
    {
        var req = new Mst_ContractType_IndexReq
        {
            Page = 2,
            RecordCount = 2
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(2, data.GetProperty("PageIndex").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("HD_VAY", list[0].GetProperty("ContractType").GetString());
    }

    [Fact]
    public async Task Index_Pagination_PageSizeZero_ReturnsEmptyListAndPageCount0()
    {
        var req = new Mst_ContractType_IndexReq
        {
            PageSize = 0
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_Pagination_NegativeRecordStart_ReturnsEmptyList()
    {
        var req = new Mst_ContractType_IndexReq
        {
            Ft_RecordStart = -1,
            Ft_RecordCount = 10
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByKeyWord_ReturnsMatchingItem()
    {
        var req = new Mst_ContractType_IndexReq
        {
            KeyWord = "lao động"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("HD_LD", list[0].GetProperty("ContractType").GetString());
    }

    [Fact]
    public async Task Create_NullBody_ReturnsCheckStrJson()
    {
        var result = await _controller.Create(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Create", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strJson", out _));
    }

    [Fact]
    public async Task Create_EmptyBody_ReturnsCheckStrJson()
    {
        var result = await _controller.Create(new Mst_ContractType_CreateReq());
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Create", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_InvalidNetworkID_ReturnsInvalidNetworkID()
    {
        var req = new Mst_ContractType_CreateReq
        {
            NetworkID = "",
            ContractType = "HD_NEW",
            ContractTypeName = "Hợp đồng mới"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Create_InvalidNetworkID", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strNetworkID", out _));
    }

    [Fact]
    public async Task Create_EmptyContractType_ReturnsCheckContractType()
    {
        var req = new Mst_ContractType_CreateReq
        {
            NetworkID = "1001",
            ContractType = "",
            ContractTypeName = "Hợp đồng không mã"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Create", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.ContractType", out _));
    }

    [Fact]
    public async Task Create_DuplicatePrimaryKey_ReturnsOrganExist()
    {
        var req = new Mst_ContractType_CreateReq
        {
            NetworkID = "1001",
            ContractType = "HD_KT", // Đã có trong seed
            ContractTypeName = "Trùng hợp đồng kinh tế"
        };
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_CheckDB_OrganExist", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("1001", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("HD_KT", p.GetProperty("Check.ContractType").GetString());
    }

    [Fact]
    public async Task Create_Success_RoundTrip()
    {
        var req = new Mst_ContractType_CreateReq
        {
            NetworkID = "1001",
            ContractType = "HD_NEW",
            ContractTypeName = "Hợp đồng thử nghiệm",
            Description = "Mô tả thử nghiệm",
            OrgID = "7200000001"
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/Mst_ContractType", root.GetProperty("RedirectUrl").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("HD_NEW", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng thử nghiệm", data.GetProperty("ContractTypeName").GetString());
        Assert.Equal("Mô tả thử nghiệm", data.GetProperty("Description").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
        Assert.Equal("port", data.GetProperty("LogLUBy").GetString());

        // Kiểm tra đọc lại qua DB
        var saved = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_NEW");
        Assert.NotNull(saved);
        Assert.Equal("Hợp đồng thử nghiệm", saved.ContractTypeName);

        // Kiểm tra đọc lại qua Index
        var indexResult = await _controller.Index(new Mst_ContractType_IndexReq { ContractType = "HD_NEW" });
        var okIndex = Assert.IsType<OkObjectResult>(indexResult);
        var docIndex = JsonDocument.Parse(JsonSerializer.Serialize(okIndex.Value));
        var dataIndex = docIndex.RootElement.GetProperty("Data");
        Assert.Equal(1, dataIndex.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Create_WithModelString_Success()
    {
        var req = new Mst_ContractType_CreateReq
        {
            Model = "{\"NetworkID\":\"1001\",\"ContractType\":\"HD_FROM_JSON\",\"ContractTypeName\":\"Hợp đồng từ JSON\",\"Description\":\"Từ form MVC\"}"
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("HD_FROM_JSON", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng từ JSON", data.GetProperty("ContractTypeName").GetString());

        var saved = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_FROM_JSON");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Update_WithValidContractType_ReturnsItemAndSubmissionForms()
    {
        var req = new Mst_ContractType_UpdateReq { ContractType = "HD_KT" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("HD_KT", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng kinh tế", data.GetProperty("ContractTypeName").GetString());

        var subForms = data.GetProperty("List_Mst_SubmissionForm");
        Assert.Equal(3, subForms.GetArrayLength());
    }

    [Fact]
    public async Task Update_WithContractTypeLowercaseAlias_ReturnsItem()
    {
        var json = "{\"contracttype\":\"HD_LD\"}";
        var req = JsonSerializer.Deserialize<Mst_ContractType_UpdateReq>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("HD_LD", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng lao động", data.GetProperty("ContractTypeName").GetString());
    }

    [Fact]
    public async Task Update_WithModelProperty_ReturnsItem()
    {
        var req = new Mst_ContractType_UpdateReq
        {
            Model = JsonSerializer.Serialize(new Mst_ContractType_UpdateReq { ContractType = "HD_DV" })
        };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("HD_DV", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng dịch vụ", data.GetProperty("ContractTypeName").GetString());
    }

    [Fact]
    public async Task Update_WithNonExistentContractType_ReturnsNullData()
    {
        var req = new Mst_ContractType_UpdateReq { ContractType = "NOT_EXIST" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_WithEmptyBodyOrNullReq_ReturnsFirstItem()
    {
        var resNull = await _controller.Update(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("HD_DV", docNull.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());

        var resEmpty = await _controller.Update(new Mst_ContractType_UpdateReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("HD_DV", docEmpty.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());
    }

    [Fact]
    public async Task Update_WithNetworkIDFilter_FiltersCorrectly()
    {
        // NetworkID = 1002 và HD_VAY -> tìm thấy
        var req1 = new Mst_ContractType_UpdateReq { NetworkID = "1002", ContractType = "HD_VAY" };
        var res1 = await _controller.Update(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("HD_VAY", doc1.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());

        // NetworkID = 1001 và HD_VAY -> không khớp NetworkID -> Data null
        var req2 = new Mst_ContractType_UpdateReq { NetworkID = "1001", ContractType = "HD_VAY" };
        var res2 = await _controller.Update(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task RealServer_Verification_Port8589()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_ct_{Guid.NewGuid():N}.db");
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
        psi.Environment["PORT"] = "8589";
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8589"), Timeout = TimeSpan.FromSeconds(3) };
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

            // 1. Index: Kiểm tra 2 bộ lọc ra số dòng khác nhau
            // Bộ lọc 1: NetworkID = "1001" -> 4 bản ghi
            var reqIdx1 = new StringContent("{\"NetworkID\":\"1001\"}", Encoding.UTF8, "application/json");
            var resIdx1 = await http.PostAsync("/api/mst_contracttype/index", reqIdx1);
            var bodyIdx1 = await resIdx1.Content.ReadAsStringAsync();
            var docIdx1 = JsonDocument.Parse(bodyIdx1);
            Assert.Equal(System.Net.HttpStatusCode.OK, resIdx1.StatusCode);
            Assert.Equal(4, docIdx1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // Bộ lọc 2: NetworkID = "1002" -> 1 bản ghi
            var reqIdx2 = new StringContent("{\"NetworkID\":\"1002\"}", Encoding.UTF8, "application/json");
            var resIdx2 = await http.PostAsync("/api/mst_contracttype/index", reqIdx2);
            var bodyIdx2 = await resIdx2.Content.ReadAsStringAsync();
            var docIdx2 = JsonDocument.Parse(bodyIdx2);
            Assert.Equal(System.Net.HttpStatusCode.OK, resIdx2.StatusCode);
            Assert.Equal(1, docIdx2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 2. Create: 1 ca guard trả lỗi và 1 ca round-trip ghi
            // Guard: body rỗng -> 400 Bad Request
            var reqCrErr = new StringContent("{}", Encoding.UTF8, "application/json");
            var resCrErr = await http.PostAsync("/api/mst_contracttype/create", reqCrErr);
            var bodyCrErr = await resCrErr.Content.ReadAsStringAsync();
            var docCrErr = JsonDocument.Parse(bodyCrErr);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resCrErr.StatusCode);
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Create", docCrErr.RootElement.GetProperty("ErrorCode").GetString());

            // Ghi: tạo mới thành công
            var reqCrOk = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"HD_LIVE_TEST\",\"ContractTypeName\":\"Hợp đồng kiểm thử live\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var resCrOk = await http.PostAsync("/api/mst_contracttype/create", reqCrOk);
            var bodyCrOk = await resCrOk.Content.ReadAsStringAsync();
            var docCrOk = JsonDocument.Parse(bodyCrOk);
            Assert.Equal(System.Net.HttpStatusCode.OK, resCrOk.StatusCode);
            Assert.True(docCrOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("HD_LIVE_TEST", docCrOk.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());

            // 3. Update: kiểm tra các ca đọc dữ liệu
            // Ca 3.1: Body rỗng -> trả về bản ghi đầu tiên ("HD_DV")
            var reqUpdEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
            var resUpdEmpty = await http.PostAsync("/api/mst_contracttype/update", reqUpdEmpty);
            var bodyUpdEmpty = await resUpdEmpty.Content.ReadAsStringAsync();
            var docUpdEmpty = JsonDocument.Parse(bodyUpdEmpty);
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpdEmpty.StatusCode);
            Assert.True(docUpdEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("HD_DV", docUpdEmpty.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());

            // Ca 3.2: Lấy theo ContractType = "HD_KT" -> trả về "HD_KT" kèm List_Mst_SubmissionForm
            var reqUpd1 = new StringContent("{\"ContractType\":\"HD_KT\"}", Encoding.UTF8, "application/json");
            var resUpd1 = await http.PostAsync("/api/mst_contracttype/update", reqUpd1);
            var bodyUpd1 = await resUpd1.Content.ReadAsStringAsync();
            var docUpd1 = JsonDocument.Parse(bodyUpd1);
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpd1.StatusCode);
            Assert.True(docUpd1.RootElement.GetProperty("Success").GetBoolean());
            var dataUpd1 = docUpd1.RootElement.GetProperty("Data");
            Assert.Equal("HD_KT", dataUpd1.GetProperty("ContractType").GetString());
            Assert.Equal("Hợp đồng kinh tế", dataUpd1.GetProperty("ContractTypeName").GetString());
            Assert.Equal(3, dataUpd1.GetProperty("List_Mst_SubmissionForm").GetArrayLength());

            // Ca 3.3: Lấy theo alias contracttype = "HD_LD" -> trả về "HD_LD"
            var reqUpd2 = new StringContent("{\"contracttype\":\"HD_LD\"}", Encoding.UTF8, "application/json");
            var resUpd2 = await http.PostAsync("/api/mst_contracttype/update", reqUpd2);
            var bodyUpd2 = await resUpd2.Content.ReadAsStringAsync();
            var docUpd2 = JsonDocument.Parse(bodyUpd2);
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpd2.StatusCode);
            Assert.True(docUpd2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("HD_LD", docUpd2.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());

            // Ca 3.4: Lấy theo mã không tồn tại -> Data null
            var reqUpdNone = new StringContent("{\"ContractType\":\"NOT_EXIST\"}", Encoding.UTF8, "application/json");
            var resUpdNone = await http.PostAsync("/api/mst_contracttype/update", reqUpdNone);
            var bodyUpdNone = await resUpdNone.Content.ReadAsStringAsync();
            var docUpdNone = JsonDocument.Parse(bodyUpdNone);
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpdNone.StatusCode);
            Assert.True(docUpdNone.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docUpdNone.RootElement.GetProperty("Data").ValueKind);

            // Ca 3.5: Lấy theo NetworkID = 1002 và HD_VAY -> tìm thấy
            var reqUpdNetOk = new StringContent("{\"NetworkID\":\"1002\",\"ContractType\":\"HD_VAY\"}", Encoding.UTF8, "application/json");
            var resUpdNetOk = await http.PostAsync("/api/mst_contracttype/update", reqUpdNetOk);
            var bodyUpdNetOk = await resUpdNetOk.Content.ReadAsStringAsync();
            var docUpdNetOk = JsonDocument.Parse(bodyUpdNetOk);
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpdNetOk.StatusCode);
            Assert.True(docUpdNetOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("HD_VAY", docUpdNetOk.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());

            // Ca 3.6: Lấy theo NetworkID = 1001 và HD_VAY -> không khớp -> Data null
            var reqUpdNetFail = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"HD_VAY\"}", Encoding.UTF8, "application/json");
            var resUpdNetFail = await http.PostAsync("/api/mst_contracttype/update", reqUpdNetFail);
            var bodyUpdNetFail = await resUpdNetFail.Content.ReadAsStringAsync();
            var docUpdNetFail = JsonDocument.Parse(bodyUpdNetFail);
            Assert.Equal(System.Net.HttpStatusCode.OK, resUpdNetFail.StatusCode);
            Assert.True(docUpdNetFail.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docUpdNetFail.RootElement.GetProperty("Data").ValueKind);
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
