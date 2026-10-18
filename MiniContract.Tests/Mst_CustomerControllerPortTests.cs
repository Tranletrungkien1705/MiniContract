using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MiniContract.Tests;

public class Mst_CustomerControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly TestWebHostEnvironment _env;
    private readonly Mst_CustomerControllerPort _controller;
    private readonly ITestOutputHelper? _output;

    public Mst_CustomerControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
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

        _env = new TestWebHostEnvironment();
        _controller = new Mst_CustomerControllerPort(_db, _env);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
        try
        {
            if (Directory.Exists(_env.WebRootPath))
            {
                Directory.Delete(_env.WebRootPath, true);
            }
        }
        catch { }
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

    // ==========================================
    // CÁC TEST CHO EXPORT (Port #159)
    // Gate: Mst_CustomerController.cs:435-561
    // ==========================================

    [Fact]
    public async Task Export_EmptyBody_ReturnsAllSeededItems()
    {
        var result = await _controller.Export(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);

        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        Assert.Equal(39, ws.Row(1).LastCellUsed()?.Address.ColumnNumber);
        Assert.Equal("Mã khách hàng(*)", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên khách hàng(*)", ws.Cell(1, 2).GetString());
        Assert.Equal("Thuộc tính thêm", ws.Cell(1, 39).GetString());

        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(6, lastRow); // Dòng 1 tiêu đề + 5 dòng dữ liệu

        // Kiểm tra thứ tự sắp xếp theo CreateDTimeUTC desc (Customer.cs:1136, 1250)
        Assert.Equal("KH005", ws.Cell(2, 1).GetString());
        Assert.Equal("KH001", ws.Cell(6, 1).GetString());
    }

    [Fact]
    public async Task Export_FilterByFlagDealer_1_Returns2Items()
    {
        var req = new Mst_Customer_ExportReq
        {
            FlagDealer = "1"
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(3, lastRow); // Dòng 1 tiêu đề + 2 dòng (CUS005, CUS001)
    }

    [Fact]
    public async Task Export_FilterByFlagSupplier_1_Returns1Item()
    {
        var req = new Mst_Customer_ExportReq
        {
            FlagSupplier = "1"
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(2, lastRow); // Dòng 1 tiêu đề + 1 dòng (CUS002)
        Assert.Equal("KH002", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task Export_FilterByCustomerName_Returns1Item()
    {
        var req = new Mst_Customer_ExportReq
        {
            CustomerName = "Alpha"
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(2, lastRow); // Dòng 1 tiêu đề + 1 dòng (CUS001)
        Assert.Equal("KH001", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task Export_FilterByOrgID_Returns4Items()
    {
        var req = new Mst_Customer_ExportReq
        {
            OrgID = "7200000001"
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(5, lastRow); // Dòng 1 tiêu đề + 4 dòng
    }

    [Fact]
    public async Task Export_NoMatchingRecords_ReturnsHeaderOnly()
    {
        var req = new Mst_Customer_ExportReq
        {
            CustomerName = "NON_EXISTENT_CUSTOMER_XYZ"
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(1, lastRow); // Chỉ có dòng 1 tiêu đề
    }

    [Fact]
    public async Task Export_NegativePageIndexOrZeroCount_ReturnsHeaderOnly()
    {
        var req = new Mst_Customer_ExportReq
        {
            Ft_RecordStart = -1
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(1, lastRow);
    }

    // ==========================================
    // CÁC TEST CHO EXPORTTEMPLATE (Port #160)
    // Gate: Mst_CustomerController.cs:567-578
    // ==========================================

    [Fact]
    public async Task ExportTemplate_NullBody_GeneratesExcelWith39ColumnsAndOnlyHeaderRow()
    {
        var result = await _controller.ExportTemplate(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);

        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        Assert.Equal(39, ws.Row(1).LastCellUsed()?.Address.ColumnNumber);
        Assert.Equal("Mã khách hàng(*)", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên khách hàng(*)", ws.Cell(1, 2).GetString());
        Assert.Equal("Thuộc tính thêm", ws.Cell(1, 39).GetString());

        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(1, lastRow); // Chỉ có dòng 1 tiêu đề, 0 dòng dữ liệu
    }

    [Fact]
    public async Task ExportTemplate_WithEmptyReq_GeneratesValidExcelTemplate()
    {
        var req = new Mst_Customer_ExportTemplateReq();
        var result = await _controller.ExportTemplate(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);

        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Customer");
        Assert.Equal(39, ws.Row(1).LastCellUsed()?.Address.ColumnNumber);
        Assert.Equal(1, ws.LastRowUsed()?.RowNumber());
    }

    [Fact]
    public async Task ExportTemplate_CallTwice_GeneratesDistinctFilePaths()
    {
        var res1 = await _controller.ExportTemplate(null);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        string url1 = doc1.RootElement.GetProperty("strUrl").GetString()!;

        var res2 = await _controller.ExportTemplate(null);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        string url2 = doc2.RootElement.GetProperty("strUrl").GetString()!;

        Assert.NotEqual(url1, url2);

        var p1 = Path.Combine(_env.WebRootPath, url1.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var p2 = Path.Combine(_env.WebRootPath, url2.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(p1));
        Assert.True(File.Exists(p2));
    }

    [Fact]
    public async Task RealServer_Verification_ExportTemplate()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_cust_tpl_{Guid.NewGuid():N}.db");
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

            // Request 1: POST ExportTemplate với body rỗng {}
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_customer/exporttemplate", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ1 ExportTemplate (body {{}}): {body1}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc1.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc1.RootElement.GetProperty("CheckSuccess").GetString());
            var url1 = doc1.RootElement.GetProperty("strUrl").GetString();
            Assert.NotNull(url1);
            Assert.StartsWith("/TempFiles/", url1);

            // Request 2: POST ExportTemplate không body
            var req2 = new StringContent("", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_customer/exporttemplate", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ2 ExportTemplate (empty body): {body2}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc2.RootElement.GetProperty("Title").GetString());
            var url2 = doc2.RootElement.GetProperty("strUrl").GetString();
            Assert.NotNull(url2);
            Assert.StartsWith("/TempFiles/", url2);

            // Kiểm tra hai tệp xuất ra là riêng biệt
            Assert.NotEqual(url1, url2);
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
                if (File.Exists(tempDb)) File.Delete(tempDb);
            }
            catch { }
        }
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "test_wwwroot_" + Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MiniContract.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
    }
}

