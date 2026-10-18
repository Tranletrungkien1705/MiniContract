using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class Mst_ContractTypeControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_ContractTypeControllerPort _controller;

    public Mst_ContractTypeControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_ContractType
        var seeder = new Mst_ContractTypeSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

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
}
