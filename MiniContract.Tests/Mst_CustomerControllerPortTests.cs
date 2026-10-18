using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MiniContract.Tests;

public class Mst_CustomerControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_CustomerControllerPort _controller;

    public Mst_CustomerControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy các seeder phụ thuộc và seeder Mst_Customer
        new Mst_AreaSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_NNTSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_CustomerTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_CustomerSourceSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_CustomerGroupSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_CustomerInCustomerGroupSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_CustomerSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_CustomerControllerPort(_db);
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

        // Kiểm tra thứ tự sắp xếp theo CreateDTimeUTC desc (Customer.cs:1136, 1250)
        Assert.Equal("CUS005", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS004", list[1].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS003", list[2].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS002", list[3].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS001", list[4].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_FilterByOrgID_Returns4Items()
    {
        var req = new Mst_Customer_IndexReq
        {
            OrgID = "7200000001"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(4, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByFlagActive_0_Returns1Item()
    {
        var req = new Mst_Customer_IndexReq
        {
            FlagActive = "0"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS004", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_FilterByCustomerName_ReturnsMatchingItem()
    {
        var req = new Mst_Customer_IndexReq
        {
            CustomerName = "Alpha"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS001", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_FilterByCustomerType_Array_ReturnsMatchingItems()
    {
        var req = new Mst_Customer_IndexReq
        {
            CustomerType = new[] { "ENTERPRISE" }
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByCustomerGrpCode_Array_ReturnsMatchingItems()
    {
        var req = new Mst_Customer_IndexReq
        {
            CustomerGrpCode = new[] { "GRP01" }
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByCustomerSourceCode_Array_ReturnsMatchingItem()
    {
        var req = new Mst_Customer_IndexReq
        {
            CustomerSourceCode = new[] { "SRC02" }
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS002", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_FilterByCustomerMobilePhone_Array_ReturnsMatchingItem()
    {
        var req = new Mst_Customer_IndexReq
        {
            CustomerMobilePhone = new[] { "0901234567" }
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS001", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_FilterByContactEmail_Array_ReturnsMatchingItem()
    {
        var req = new Mst_Customer_IndexReq
        {
            ContactEmail = new[] { "alpha@domain.vn" }
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS001", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_FilterByKeyWord_ReturnsMatchingItem()
    {
        var req = new Mst_Customer_IndexReq
        {
            KeyWord = "Beta"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS002", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_Pagination_Page0_Size2()
    {
        var req = new Mst_Customer_IndexReq
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
        Assert.Equal("CUS005", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS004", list[1].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_Pagination_Page1_Size2()
    {
        var req = new Mst_Customer_IndexReq
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
        Assert.Equal("CUS003", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS002", list[1].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_Pagination_Page2_Size2()
    {
        var req = new Mst_Customer_IndexReq
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
        Assert.Equal("CUS001", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task Index_Pagination_PageSizeZero_ReturnsEmptyListAndPageCount0()
    {
        var req = new Mst_Customer_IndexReq
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
        var req = new Mst_Customer_IndexReq
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
    public async Task Index_JoinedFields_PopulatedCorrectly()
    {
        var req = new Mst_Customer_IndexReq
        {
            CustomerName = "Alpha"
        };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var list = doc.RootElement.GetProperty("Data").GetProperty("DataList");

        Assert.Equal(1, list.GetArrayLength());
        var cus = list[0];

        // Mst_CustomerInCustomerGroup + Mst_CustomerGroup ghép thành chuỗi phân tách bởi dấu phẩy (Mst_CustomerController.cs:196-197)
        Assert.Equal("GRP01,GRP02", cus.GetProperty("CustomerGrpCode").GetString());
        Assert.Equal("Nhóm Chiến Lược,Nhóm Tiềm Năng", cus.GetProperty("mcg_CustomerGrpName").GetString());

        // Mst_CustomerType (Customer.cs:1187)
        Assert.Equal("Doanh nghiệp", cus.GetProperty("mct_CustomerTypeName").GetString());

        // Mst_CustomerSource (Customer.cs:1220)
        Assert.Equal("Trực tiếp", cus.GetProperty("mcs_CustomerSourceName").GetString());

        // Mst_Area (Customer.cs:1224)
        Assert.Equal("Miền Bắc", cus.GetProperty("ma_AreaName").GetString());

        // Mst_NNT (Customer.cs:1184-1185)
        Assert.Equal("Công ty TNHH Giải Pháp Số Hà Nội", cus.GetProperty("Org_NNTFullName").GetString());
    }

    // ==========================================
    // CÁC TEST CHO DOSEARCH (Port #158)
    // Gate: Mst_CustomerController.cs:211-323
    // ==========================================

    [Fact]
    public async Task DoSearch_EmptyBody_ReturnsAllSeededItems()
    {
        var result = await _controller.DoSearch(null);
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

        // Kiểm tra thứ tự sắp xếp theo CreateDTimeUTC desc (Customer.cs:1136, 1250)
        Assert.Equal("CUS005", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS004", list[1].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS003", list[2].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS002", list[3].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS001", list[4].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByFlagDealer_1_Returns2Items()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            FlagDealer = "1"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("CUS005", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS001", list[1].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByFlagSupplier_1_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            FlagSupplier = "1"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS002", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByFlagShipper_1_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            FlagShipper = "1"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS002", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByFlagBank_1_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            FlagBank = "1"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS003", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByFlagInsurrance_1_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            FlagInsurrance = "1"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS004", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByFlagEndUser_1_Returns2Items()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            FlagEndUser = "1"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("CUS005", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS001", list[1].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByCustomerMobilePhone_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            CustomerMobilePhone = "0901234567"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS001", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByContactEmail_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            ContactEmail = "vanc@domain.vn"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS003", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByCustomerName_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            CustomerName = "Gamma"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS004", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByCustomerType_Array_Returns3Items()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            CustomerType = new[] { "ENTERPRISE" }
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(3, list.GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_FilterByCustomerGrpCode_Array_Returns2Items()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            CustomerGrpCode = new[] { "GRP01" }
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_FilterByCustomerSourceCode_Array_Returns1Item()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            CustomerSourceCode = new[] { "SRC02" }
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS002", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByOrgID_Returns4Items()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            OrgID = "7200000001"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList");
        Assert.Equal(4, list.GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_Pagination_Page0_Size2()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            Page = 0,
            RecordCount = 2
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("CUS005", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS004", list[1].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_Pagination_Page1_Size2()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            Page = 1,
            RecordCount = 2
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageIndex").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("CUS003", list[0].GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("CUS002", list[1].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_Pagination_Page2_Size2()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            Page = 2,
            RecordCount = 2
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(2, data.GetProperty("PageIndex").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("CUS001", list[0].GetProperty("CustomerCodeSys").GetString());
    }

    [Fact]
    public async Task DoSearch_Pagination_PageSizeZero_ReturnsEmptyListAndPageCount0()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            PageSize = 0
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_Pagination_NegativeRecordStart_ReturnsEmptyList()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            Ft_RecordStart = -1,
            Ft_RecordCount = 10
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(5, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_FilterNotFound_ReturnsEmptyList()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            CustomerName = "NON_EXISTENT_CUSTOMER_XYZ"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_JoinedFields_PopulatedCorrectly()
    {
        var req = new Mst_Customer_DoSearchReq
        {
            CustomerName = "Alpha"
        };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var list = doc.RootElement.GetProperty("Data").GetProperty("DataList");

        Assert.Equal(1, list.GetArrayLength());
        var cus = list[0];

        // Mst_CustomerInCustomerGroup + Mst_CustomerGroup ghép thành chuỗi phân tách bởi dấu phẩy (Mst_CustomerController.cs:287-301)
        Assert.Equal("GRP01,GRP02", cus.GetProperty("CustomerGrpCode").GetString());
        Assert.Equal("Nhóm Chiến Lược,Nhóm Tiềm Năng", cus.GetProperty("mcg_CustomerGrpName").GetString());

        // Mst_CustomerType (Customer.cs:1187)
        Assert.Equal("Doanh nghiệp", cus.GetProperty("mct_CustomerTypeName").GetString());

        // Mst_CustomerSource (Customer.cs:1220)
        Assert.Equal("Trực tiếp", cus.GetProperty("mcs_CustomerSourceName").GetString());

        // Mst_Area (Customer.cs:1224)
        Assert.Equal("Miền Bắc", cus.GetProperty("ma_AreaName").GetString());

        // Mst_NNT (Customer.cs:1184-1185)
        Assert.Equal("Công ty TNHH Giải Pháp Số Hà Nội", cus.GetProperty("Org_NNTFullName").GetString());
    }
}

