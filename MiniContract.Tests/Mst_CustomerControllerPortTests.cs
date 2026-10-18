using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        new Mst_GovIDTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
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

    // ==========================================
    // CÁC TEST CHO IMPORT (Port #161)
    // Gate: Mst_CustomerController.cs:587-1318
    // ==========================================

    private static readonly string[] CustomerHeaders = new[]
    {
        "Mã khách hàng(*)", "Tên khách hàng(*)", "Tên loại khách hàng", "Tên nguồn khách hàng", "Tên nhóm khách hàng",
        "Điện thoại cố định", "Điện thoại di động", "Đường dẫn ảnh avatar", "Giới tính", "Email khách hàng",
        "Địa chỉ", "Tỉnh thành", "Quận huyện", "Phường xã", "Khu vực",
        "Là đại lý", "Là nhà cung cấp", "Là đơn vị vận chuyển", "Là khách hàng cá nhân", "Là ngân hàng",
        "Là công ty bảo hiểm", "Người đại diện", "Chức vụ", "Số CMND/Căn cước/Hộ chiếu", "Loại giấy tờ",
        "Số tài khoản ngân hàng", "Tên ngân hàng", "Tên người liên hệ", "Điện thoại người liên hệ", "Email người liên hệ",
        "Số Fax", "Ngày sinh", "Địa chỉ Facebook", "Tên người nhận hóa đơn", "Tên cơ quan",
        "Mã số thuế", "Địa chỉ nhận hóa đơn", "Email nhận hợp đồng", "Thuộc tính thêm"
    };

    private static IFormFile CreateExcelFormFile(string fileName, string[] headers, List<string?[]> dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mst_Customer");
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
        var ws = wb.Worksheets.Add("Mst_Customer");
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

    private static string?[] CreateRow(
        string code = "KH_NEW",
        string name = "Khách Hàng Mới",
        string type = "Cá nhân",
        string source = "Nguồn Mới",
        string group = "Nhóm Mới",
        string phone = "",
        string mobile = "0988000111",
        string gender = "Nam",
        string email = "newcus@domain.vn",
        string govType = "Căn cước công dân",
        string mst = "0309998881",
        string dynamic = "Sở thích:Bóng đá")
    {
        var row = new string?[39];
        row[0] = code;
        row[1] = name;
        row[2] = type;
        row[3] = source;
        row[4] = group;
        row[5] = phone;
        row[6] = mobile;
        row[7] = "";
        row[8] = gender;
        row[9] = email;
        row[10] = "123 Đường ABC";
        row[11] = "HN";
        row[12] = "CG";
        row[13] = "TH";
        row[14] = "KV01";
        row[15] = "0";
        row[16] = "0";
        row[17] = "0";
        row[18] = "1";
        row[19] = "0";
        row[20] = "0";
        row[21] = "Đại diện";
        row[22] = "Giám đốc";
        row[23] = "0012000001";
        row[24] = govType;
        row[25] = "123456789";
        row[26] = "Vietcombank";
        row[27] = "Người liên hệ";
        row[28] = "0988000111";
        row[29] = "contact@domain.vn";
        row[30] = "";
        row[31] = "1990-01-01";
        row[32] = "fb.com/khachhang";
        row[33] = "Tên hóa đơn";
        row[34] = "Cơ quan";
        row[35] = mst;
        row[36] = "Địa chỉ hóa đơn";
        row[37] = "invoice@domain.vn";
        row[38] = dynamic;
        return row;
    }

    [Fact]
    public async Task Import_NullFile_ReturnsBadRequestWithInvalidFileMessage()
    {
        var result = await _controller.Import(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidFileExtension_ReturnsBadRequestWithInvalidFileMessage()
    {
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(ms, 0, ms.Length, "file", "test.txt");
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_EmptyDataRows_ReturnsBadRequestWithEmptyDataMessage()
    {
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]>());
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không có dữ liệu!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidColumnCount_ReturnsBadRequestWithInvalidFileMessage()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã khách hàng(*)", "Tên khách hàng(*)" }, new List<string?[]>
        {
            new[] { "KH_BAD", "Khách hàng sai số cột" }
        });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File excel import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_RowWithEmptyCode_ReturnsBadRequestWithEmptyCodeMessage()
    {
        var row = CreateRow(code: "");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã khách hàng không được trống!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_RowWithEmptyName_ReturnsBadRequestWithEmptyNameMessage()
    {
        var row = CreateRow(name: "");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Tên khách hàng không được trống!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_DuplicateCodeWithinSameFile_ReturnsBadRequestWithDuplicateCodeMessage()
    {
        var row1 = CreateRow(code: "KH_DUP", mobile: "0988000111", email: "dup1@domain.vn", mst: "0301111111");
        var row2 = CreateRow(code: "KH_DUP", mobile: "0988000222", email: "dup2@domain.vn", mst: "0302222222");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row1, row2 });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã khách hàng 'KH_DUP' bị lặp trong file excel!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidCustomerType_ReturnsBadRequestWithCustomerTypeMessage()
    {
        var row = CreateRow(code: "KH_TEST", type: "SaiLoai");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Loại khách hàng của mã khách KH_TEST chỉ có thể nhập 'Cá nhân' hoặc 'Tổ chức' - Dòng 3!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidCustomerGender_ReturnsBadRequestWithGenderMessage()
    {
        var row = CreateRow(code: "KH_TEST", gender: "SaiGioiTinh");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Giới tính của mã khách KH_TEST chỉ có thể nhập 'Nữ', 'Nam' hoặc 'Khác' - Dòng 3!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidGovIDType_ReturnsBadRequestWithGovIDTypeMessage()
    {
        var row = CreateRow(code: "KH_TEST", govType: "Giấy tờ giả");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Loại giấy tờ của mã khách KH_TEST không có trong hệ thống  - Dòng 3!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidDynamicFieldFormat_ReturnsBadRequestWithDynamicFieldMessage()
    {
        var row = CreateRow(code: "KH_TEST", dynamic: "SaiDinhDangKhongCoDauHaiCham");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Trường động mã khách hàng'KH_TEST' không hợp lệ - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_DuplicatePhoneWithExistingDbRecord_ReturnsBadRequestWithPhoneMessage()
    {
        // 0901234567 đã có trong DB của CUS001
        var row = CreateRow(code: "KH_NEW_01", mobile: "0901234567");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Số điện thoại 0901234567 - Dòng 3 đã tồn tại trong hệ thống'", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_DuplicateEmailWithExistingDbRecord_ReturnsBadRequestWithEmailMessage()
    {
        // alpha@domain.vn đã có trong DB của CUS001
        var row = CreateRow(code: "KH_NEW_01", email: "alpha@domain.vn");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Email alpha@domain.vn - Dòng 3 đã tồn tại trong hệ thống'", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_DuplicateMSTWithExistingDbRecord_ReturnsBadRequestWithMSTMessage()
    {
        // 0101234567 đã có trong DB của CUS001
        var row = CreateRow(code: "KH_NEW_01", mst: "0101234567");
        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã số thuế 0101234567 - Dòng 3 đã tồn tại trong hệ thống'", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_ValidFile_ImportsAllRowsCreatesNewGroupAndSourceAndReturnsSuccess()
    {
        var row1 = CreateRow(
            code: "KH_IMP_01",
            name: "Khách Nhập 1",
            type: "Cá nhân",
            source: "Nguồn Tự Động Tạo",
            group: "Nhóm Tự Động Tạo",
            mobile: "0911222333",
            gender: "Nữ",
            email: "imp01@domain.vn",
            govType: "Căn cước công dân",
            mst: "0401112223",
            dynamic: "Sở thích:Bóng bàn|Nghề nghiệp:Kỹ sư"
        );
        var row2 = CreateRow(
            code: "KH_IMP_02",
            name: "Khách Nhập 2",
            type: "Tổ chức",
            source: "Trực tiếp", // Đã có trong DB
            group: "Nhóm Chiến Lược", // Đã có trong DB
            mobile: "0911222444",
            gender: "Nam",
            email: "imp02@domain.vn",
            govType: "Chứng minh nhân dân",
            mst: "0401112224",
            dynamic: "Phòng ban:CNTT"
        );

        var file = CreateExcelFormFile("test.xlsx", CustomerHeaders, new List<string?[]> { row1, row2 });
        var result = await _controller.Import(file);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã nhập dữ liệu excel thành công!", doc.RootElement.GetProperty("Title").GetString());
        Assert.Equal("/Mst_Customer/Index", doc.RootElement.GetProperty("RedirectUrl").GetString());

        // Kiểm tra lưu DB
        var saved1 = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCode == "KH_IMP_01");
        Assert.NotNull(saved1);
        Assert.Equal("Khách Nhập 1", saved1.CustomerName);
        Assert.Equal("CANHAN", saved1.CustomerType);
        Assert.Equal("FEMALE", saved1.CustomerGender);
        Assert.Equal("CCCD", saved1.GovIDType);
        Assert.Equal("1", saved1.FlagActive);
        Assert.Contains("Bóng bàn", saved1.ListOfCustDynamicFieldValue);

        var saved2 = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCode == "KH_IMP_02");
        Assert.NotNull(saved2);
        Assert.Equal("Khách Nhập 2", saved2.CustomerName);
        Assert.Equal("TOCHUC", saved2.CustomerType);
        Assert.Equal("MALE", saved2.CustomerGender);
        Assert.Equal("CMND", saved2.GovIDType);

        // Kiểm tra tự động tạo nhóm mới
        var newGrp = await _db.Set<Mst_CustomerGroup>().FirstOrDefaultAsync(x => x.CustomerGrpName == "Nhóm Tự Động Tạo");
        Assert.NotNull(newGrp);
        Assert.Equal("Create by excel", newGrp.CustomerGrpDesc);

        // Kiểm tra tự động tạo nguồn mới
        var newSrc = await _db.Set<Mst_CustomerSource>().FirstOrDefaultAsync(x => x.CustomerSourceName == "Nguồn Tự Động Tạo");
        Assert.NotNull(newSrc);
        Assert.Equal("Create by excel", newSrc.CustomerSourceDesc);

        // Kiểm tra lưu Mst_CustomerInCustomerGroup
        var custGroup1 = await _db.Set<Mst_CustomerInCustomerGroup>().FirstOrDefaultAsync(x => x.CustomerCodeSys == saved1.CustomerCodeSys);
        Assert.NotNull(custGroup1);
        Assert.Equal(newGrp.CustomerGrpCode, custGroup1.CustomerGrpCode);
    }

    [Fact]
    public async Task RealServer_Verification_Import()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_cust_imp_{Guid.NewGuid():N}.db");
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

            // 1. Guard check: File rỗng
            using (var emptyMultipart = new MultipartFormDataContent())
            {
                var emptyBytes = CreateExcelBytes(CustomerHeaders, new List<string?[]>());
                emptyMultipart.Add(new ByteArrayContent(emptyBytes), "file", "empty.xlsx");
                var resEmpty = await http.PostAsync("/api/mst_customer/import", emptyMultipart);
                var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
                _output?.WriteLine($"RealServer Import Empty: {bodyEmpty}");
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
                var docEmpty = JsonDocument.Parse(bodyEmpty);
                Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("File excel import không có dữ liệu!", docEmpty.RootElement.GetProperty("Params").GetProperty("Message").GetString());
            }

            // 2. Round-trip import hợp lệ
            using (var validMultipart = new MultipartFormDataContent())
            {
                var row = CreateRow(
                    code: "KH_SRV_01",
                    name: "Khách Server Thật",
                    type: "Cá nhân",
                    source: "Server Source",
                    group: "Server Group",
                    mobile: "0933999888",
                    email: "server_cust@domain.vn",
                    govType: "Căn cước công dân",
                    mst: "0509991112"
                );
                var validBytes = CreateExcelBytes(CustomerHeaders, new List<string?[]> { row });
                validMultipart.Add(new ByteArrayContent(validBytes), "file", "valid.xlsx");
                var resValid = await http.PostAsync("/api/mst_customer/import", validMultipart);
                var bodyValid = await resValid.Content.ReadAsStringAsync();
                _output?.WriteLine($"RealServer Import Valid: {bodyValid}");
                Assert.Equal(System.Net.HttpStatusCode.OK, resValid.StatusCode);
                var docValid = JsonDocument.Parse(bodyValid);
                Assert.True(docValid.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Đã nhập dữ liệu excel thành công!", docValid.RootElement.GetProperty("Title").GetString());
            }
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

    // ==========================================
    // CÁC TEST CHO DELETE (Port #162)
    // Gate: Mst_CustomerController.cs:1372-1461
    // API:  MstCustomerController.cs:19-89 WA_Mst_Customer_Save
    // BIZ:  Customer.cs:2968-3070 WAS_Mst_Customer_Save, 1619-1732 Mst_Customer_Save, 2359-2966 Mst_Customer_SaveX_New20231205
    // ==========================================

    [Fact]
    public async Task Delete_EmptyBody_ReturnsMst_Customer_Save_Input_Mst_CustomerTblNotFound()
    {
        var result = await _controller.Delete(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Save_Input_Mst_CustomerTblNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mst_Customer", root.GetProperty("Params").GetProperty("Check.TableName").GetString());
    }

    [Fact]
    public async Task Delete_EmptyListCustomersString_ReturnsMst_Customer_Save_Input_Mst_CustomerTblNotFound()
    {
        var req = new Mst_Customer_DeleteReq
        {
            ListCustomers = ""
        };
        var result = await _controller.Delete(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Save_Input_Mst_CustomerTblNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mst_Customer", root.GetProperty("Params").GetProperty("Check.TableName").GetString());
    }

    [Fact]
    public async Task Delete_EmptyListCustomersArray_ReturnsMst_Customer_Save_Input_Mst_CustomerTblNotFound()
    {
        var req = new Mst_Customer_DeleteReq
        {
            ListCustomers = "[]"
        };
        var result = await _controller.Delete(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Save_Input_Mst_CustomerTblNotFound", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mst_Customer", root.GetProperty("Params").GetProperty("Check.TableName").GetString());
    }

    [Fact]
    public async Task Delete_SingleCustomer_SuccessfullyDeletesFromCustomerAndCustomerGroup()
    {
        // Ban đầu CUS001 tồn tại trong Mst_Customer và có 2 nhóm trong Mst_CustomerInCustomerGroup
        var cusBefore = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCodeSys == "CUS001");
        Assert.NotNull(cusBefore);
        var groupsBefore = await _db.Set<Mst_CustomerInCustomerGroup>().Where(x => x.CustomerCodeSys == "CUS001").ToListAsync();
        Assert.Equal(2, groupsBefore.Count);

        var req = new Mst_Customer_DeleteReq
        {
            ListCustomers = "[{\"CustomerCodeSys\":\"CUS001\",\"OrgID\":\"7200000001\"}]"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa khách hàng thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("/Mst_Customer/Index", root.GetProperty("RedirectUrl").GetString());

        // Kiểm tra đã xóa khỏi Mst_Customer
        var cusAfter = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCodeSys == "CUS001");
        Assert.Null(cusAfter);

        // Kiểm tra đã xóa khỏi Mst_CustomerInCustomerGroup
        var groupsAfter = await _db.Set<Mst_CustomerInCustomerGroup>().Where(x => x.CustomerCodeSys == "CUS001").ToListAsync();
        Assert.Empty(groupsAfter);

        // Khách hàng CUS002 không bị ảnh hưởng
        var cus002 = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCodeSys == "CUS002");
        Assert.NotNull(cus002);
    }

    [Fact]
    public async Task Delete_MultipleCustomers_SuccessfullyDeletesAll()
    {
        var req = new Mst_Customer_DeleteReq
        {
            ListCustomers = "[{\"CustomerCodeSys\":\"CUS002\",\"OrgID\":\"7200000001\"},{\"CustomerCodeSys\":\"CUS003\",\"OrgID\":\"7200000001\"}]"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var cus002 = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCodeSys == "CUS002");
        Assert.Null(cus002);
        var cus003 = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCodeSys == "CUS003");
        Assert.Null(cus003);
    }

    [Fact]
    public async Task Delete_ByCustomerCode_SuccessfullyResolvesAndDeletes()
    {
        var cusBefore = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCode == "KH004");
        Assert.NotNull(cusBefore);

        var req = new Mst_Customer_DeleteReq
        {
            CustomerCode = "KH004",
            OrgID = "7200000001"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var cusAfter = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCode == "KH004");
        Assert.Null(cusAfter);
    }

    [Fact]
    public async Task Delete_NonExistentCustomer_ReturnsSuccessWithoutError()
    {
        var req = new Mst_Customer_DeleteReq
        {
            CustomerCodeSys = "NON_EXISTENT_CUS"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Xóa khách hàng thành công!", doc.RootElement.GetProperty("Title").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Delete()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_cust_del_{Guid.NewGuid():N}.db");
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

            // 1. Guard check: Body rỗng
            var reqEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
            var resEmpty = await http.PostAsync("/api/mst_customer/delete", reqEmpty);
            var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Delete Empty: {bodyEmpty}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(bodyEmpty);
            Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Save_Input_Mst_CustomerTblNotFound", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("Mst_Customer", docEmpty.RootElement.GetProperty("Params").GetProperty("Check.TableName").GetString());

            // 2. Kiểm tra trước khi xóa: CUS002 tồn tại
            var checkReq = new StringContent("{\"CustomerName\":\"Beta\"}", Encoding.UTF8, "application/json");
            var resCheck1 = await http.PostAsync("/api/mst_customer/dosearch", checkReq);
            var bodyCheck1 = await resCheck1.Content.ReadAsStringAsync();
            var docCheck1 = JsonDocument.Parse(bodyCheck1);
            Assert.Equal(1, docCheck1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // 3. Round-trip delete: Xóa CUS002
            var delReq = new StringContent("{\"listCustomers\":\"[{\\\"CustomerCodeSys\\\":\\\"CUS002\\\",\\\"OrgID\\\":\\\"7200000001\\\"}]\"}", Encoding.UTF8, "application/json");
            var resDel = await http.PostAsync("/api/mst_customer/delete", delReq);
            var bodyDel = await resDel.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Delete CUS002: {bodyDel}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resDel.StatusCode);
            var docDel = JsonDocument.Parse(bodyDel);
            Assert.True(docDel.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa khách hàng thành công!", docDel.RootElement.GetProperty("Title").GetString());

            // 4. Kiểm tra sau khi xóa: CUS002 không còn trong hệ thống
            var resCheck2 = await http.PostAsync("/api/mst_customer/dosearch", checkReq);
            var bodyCheck2 = await resCheck2.Content.ReadAsStringAsync();
            var docCheck2 = JsonDocument.Parse(bodyCheck2);
            Assert.Equal(0, docCheck2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
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

    // ==========================================
    // CÁC TEST CHO CREATE (Port #163)
    // Gate: Mst_CustomerController.cs:1466-1799 (Create GET), 1801-2038 (Create POST)
    // API:  MstCustomerController.cs:19-89 WA_Mst_Customer_Save
    // BIZ:  Customer.cs:2968-3070 WAS_Mst_Customer_Save, 1619-1732 Mst_Customer_Save, 2359-2966 Mst_Customer_SaveX_New20231205
    // ==========================================

    [Fact]
    public async Task Create_NullBody_ReturnsMst_Customer_Create_Check_strJson()
    {
        var result = await _controller.Create(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Create", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strJson", out _));
    }

    [Fact]
    public async Task Create_EmptyObject_ReturnsMst_Customer_Create_Check_strJson()
    {
        var req = new Mst_Customer_CreateReq();
        var result = await _controller.Create(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Create", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strJson", out _));
    }

    [Fact]
    public async Task Create_MissingCustomerCode_ReturnsBadRequest()
    {
        var req = new Mst_Customer_CreateReq
        {
            CustomerName = "Công ty TNHH Mới",
            OrgID = "7200000001"
        };
        var result = await _controller.Create(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Create", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã khách hàng không được trống!", root.GetProperty("Params").GetProperty("Check.CustomerCode").GetString());
    }

    [Fact]
    public async Task Create_MissingCustomerName_ReturnsBadRequest()
    {
        var req = new Mst_Customer_CreateReq
        {
            CustomerCode = "KH_NEW_001",
            OrgID = "7200000001"
        };
        var result = await _controller.Create(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Create", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Tên khách hàng không được trống!", root.GetProperty("Params").GetProperty("Check.CustomerName").GetString());
    }

    [Fact]
    public async Task Create_DuplicateCustomerCode_ReturnsBadRequest()
    {
        // KH001 đã có trong DB
        var req = new Mst_Customer_CreateReq
        {
            CustomerCode = "KH001",
            CustomerName = "Trùng mã khách hàng",
            OrgID = "7200000001"
        };
        var result = await _controller.Create(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Create", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã khách hàng đã tồn tại trong hệ thống!", root.GetProperty("Params").GetProperty("Check.CustomerCode").GetString());
    }

    [Fact]
    public async Task Create_ValidDirectFields_CreatesCustomerAndReturnsOk()
    {
        var req = new Mst_Customer_CreateReq
        {
            OrgID = "7200000001",
            CustomerCode = "KH_CREATE_01",
            CustomerName = "Công ty TNHH Sáng Tạo Mới",
            CustomerType = "ENTERPRISE",
            CustomerSourceCode = "SRC01",
            CustomerMobilePhone = "0988777666",
            CustomerEmail = "sangtao@domain.vn",
            MST = "0109998888",
            CustomerGrpCode = "GRP01,GRP02",
            FlagDealer = "1"
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Tạo mới khách hàng thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("/Mst_Customer/Index", root.GetProperty("RedirectUrl").GetString());

        // Kiểm tra round-trip trong DB
        var cusInDb = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCode == "KH_CREATE_01");
        Assert.NotNull(cusInDb);
        Assert.Equal("Công ty TNHH Sáng Tạo Mới", cusInDb.CustomerName);
        Assert.Equal("7200000001", cusInDb.OrgID);
        Assert.Equal("0988777666", cusInDb.CustomerMobilePhone);
        Assert.Equal("sangtao@domain.vn", cusInDb.CustomerEmail);
        Assert.Equal("0109998888", cusInDb.MST);
        Assert.Equal("1", cusInDb.FlagActive);

        // Kiểm tra nhóm khách hàng được lưu vào Mst_CustomerInCustomerGroup
        var grpsInDb = await _db.Set<Mst_CustomerInCustomerGroup>()
            .Where(x => x.CustomerCodeSys == cusInDb.CustomerCodeSys)
            .ToListAsync();
        Assert.Equal(2, grpsInDb.Count);
        Assert.Contains(grpsInDb, g => g.CustomerGrpCode == "GRP01");
        Assert.Contains(grpsInDb, g => g.CustomerGrpCode == "GRP02");
    }

    [Fact]
    public async Task Create_ViaModelJsonString_CreatesCustomerAndReturnsOk()
    {
        var modelJson = JsonSerializer.Serialize(new
        {
            OrgID = "7200000001",
            CustomerCode = "KH_CREATE_MODEL_01",
            CustomerName = "Khách Hàng Tạo Qua Chuỗi Model",
            CustomerMobilePhone = "0977111222",
            CustomerEmail = "model@domain.vn",
            FlagRedirect = "1"
        });

        var req = new Mst_Customer_CreateReq
        {
            Model = modelJson
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Tạo mới khách hàng thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("/Mst_Customer/Create", root.GetProperty("RedirectUrl").GetString());

        var cusInDb = await _db.Set<Mst_Customer>().FirstOrDefaultAsync(x => x.CustomerCode == "KH_CREATE_MODEL_01");
        Assert.NotNull(cusInDb);
        Assert.Equal("Khách Hàng Tạo Qua Chuỗi Model", cusInDb.CustomerName);
    }

    [Fact]
    public async Task RealServer_Verification_Create()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_cust_create_{Guid.NewGuid():N}.db");
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

            // 1. Guard check: Body rỗng {} -> 400 Bad Request
            var reqEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
            var resEmpty = await http.PostAsync("/api/mst_customer/create", reqEmpty);
            var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Create Empty: {bodyEmpty}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(bodyEmpty);
            Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Create", docEmpty.RootElement.GetProperty("ErrorCode").GetString());

            // 2. Guard check: Thiếu CustomerName -> 400 Bad Request
            var reqMissingName = new StringContent("{\"CustomerCode\":\"KH_TEST_01\"}", Encoding.UTF8, "application/json");
            var resMissingName = await http.PostAsync("/api/mst_customer/create", reqMissingName);
            var bodyMissingName = await resMissingName.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Create Missing Name: {bodyMissingName}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resMissingName.StatusCode);
            var docMissingName = JsonDocument.Parse(bodyMissingName);
            Assert.False(docMissingName.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Tên khách hàng không được trống!", docMissingName.RootElement.GetProperty("Params").GetProperty("Check.CustomerName").GetString());

            // 3. Guard check: Trùng CustomerCode đã có trong seed (KH001) -> 400 Bad Request
            var reqDup = new StringContent("{\"CustomerCode\":\"KH001\",\"CustomerName\":\"Trùng KH001\"}", Encoding.UTF8, "application/json");
            var resDup = await http.PostAsync("/api/mst_customer/create", reqDup);
            var bodyDup = await resDup.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Create Duplicate: {bodyDup}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resDup.StatusCode);
            var docDup = JsonDocument.Parse(bodyDup);
            Assert.False(docDup.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã khách hàng đã tồn tại trong hệ thống!", docDup.RootElement.GetProperty("Params").GetProperty("Check.CustomerCode").GetString());

            // 4. Round-trip Create: Tạo khách hàng mới thành công
            var newCustPayload = JsonSerializer.Serialize(new
            {
                CustomerCode = "KH_SRV_NEW_99",
                CustomerName = "Khách Hàng Máy Chủ Thật",
                CustomerMobilePhone = "0966888999",
                CustomerEmail = "real_server@domain.vn",
                OrgID = "7200000001",
                CustomerGrpCode = "GRP01",
                FlagDealer = "1"
            });
            var reqValid = new StringContent(newCustPayload, Encoding.UTF8, "application/json");
            var resValid = await http.PostAsync("/api/mst_customer/create", reqValid);
            var bodyValid = await resValid.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Create Valid: {bodyValid}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resValid.StatusCode);
            var docValid = JsonDocument.Parse(bodyValid);
            Assert.True(docValid.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Tạo mới khách hàng thành công!", docValid.RootElement.GetProperty("Title").GetString());

            // 5. Kiểm tra đọc lại qua DoSearch: thấy khách hàng vừa tạo
            var searchPayload = JsonSerializer.Serialize(new { CustomerName = "Máy Chủ Thật" });
            var searchReq = new StringContent(searchPayload, Encoding.UTF8, "application/json");
            var resSearch = await http.PostAsync("/api/mst_customer/dosearch", searchReq);
            var bodySearch = await resSearch.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer DoSearch verify: {bodySearch}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resSearch.StatusCode);
            var docSearch = JsonDocument.Parse(bodySearch);
            var data = docSearch.RootElement.GetProperty("Data");
            Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
            var foundCust = data.GetProperty("DataList")[0];
            Assert.Equal("KH_SRV_NEW_99", foundCust.GetProperty("CustomerCode").GetString());
            Assert.Equal("Khách Hàng Máy Chủ Thật", foundCust.GetProperty("CustomerName").GetString());
            Assert.Equal("0966888999", foundCust.GetProperty("CustomerMobilePhone").GetString());
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

    // ==========================================
    // CÁC TEST CHO CUSTOMER_CHECK (Port #164)
    // Gate: Mst_CustomerController.cs:2043-2166 Customer_Check(string model)
    // ==========================================

    [Fact]
    public async Task Customer_Check_NullReq_ReturnsBadRequest_CheckStrJson()
    {
        var result = await _controller.Customer_Check(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Customer_Check", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Customer_Check_EmptyReq_AllFieldsEmpty_ReturnsAction0()
    {
        var req = new Mst_Customer_Customer_CheckReq();
        var result = await _controller.Customer_Check(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("0", root.GetProperty("Action").GetString());
    }

    [Fact]
    public async Task Customer_Check_MatchingPhone_ReturnsAction1_AndWarningMessage()
    {
        // 0901234567 thuộc CUS001 (KH001)
        var req = new Mst_Customer_Customer_CheckReq
        {
            CustomerMobilePhone = "0901234567",
            OrgID = "7200000001"
        };
        var result = await _controller.Customer_Check(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("1", root.GetProperty("Action").GetString());
        Assert.Equal("KH001", root.GetProperty("CustomerCode").GetString());
        Assert.Equal("Khách hàng có số điện thoại '0901234567' đã tồn tại. Bạn có muốn lấy thông tin KH hiện tại không?", root.GetProperty("Messages").GetString());
    }

    [Fact]
    public async Task Customer_Check_MatchingEmail_ReturnsAction1_AndWarningMessage()
    {
        // beta@domain.vn thuộc CUS002 (KH002)
        var req = new Mst_Customer_Customer_CheckReq
        {
            ContactEmail = "beta@domain.vn",
            OrgID = "7200000001"
        };
        var result = await _controller.Customer_Check(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("1", root.GetProperty("Action").GetString());
        Assert.Equal("KH002", root.GetProperty("CustomerCode").GetString());
        Assert.Equal("Khách hàng có email 'beta@domain.vn' đã tồn tại. Bạn có muốn lấy thông tin KH hiện tại không?", root.GetProperty("Messages").GetString());
    }

    [Fact]
    public async Task Customer_Check_MatchingMST_ReturnsAction1_AndWarningMessage()
    {
        // 0101234567 thuộc CUS001 (KH001)
        var req = new Mst_Customer_Customer_CheckReq
        {
            MST = "0101234567",
            OrgID = "7200000001"
        };
        var result = await _controller.Customer_Check(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("1", root.GetProperty("Action").GetString());
        Assert.Equal("KH001", root.GetProperty("CustomerCode").GetString());
        Assert.Equal("Khách hàng có MST '0101234567' đã tồn tại. Bạn có muốn lấy thông tin KH hiện tại không?", root.GetProperty("Messages").GetString());
    }

    [Fact]
    public async Task Customer_Check_MatchingPhone_SameCustomerCodeSys_ReturnsAction0()
    {
        // Khi sửa chính CUS001 có SĐT 0901234567, không báo trùng chính nó
        var req = new Mst_Customer_Customer_CheckReq
        {
            CustomerCodeSys = "CUS001",
            CustomerMobilePhone = "0901234567",
            OrgID = "7200000001"
        };
        var result = await _controller.Customer_Check(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("0", root.GetProperty("Action").GetString());
        Assert.Equal("KH001", root.GetProperty("CustomerCode").GetString());
        Assert.Equal("", root.GetProperty("Messages").GetString());
    }

    [Fact]
    public async Task Customer_Check_NoMatchingRecord_ReturnsAction0()
    {
        var req = new Mst_Customer_Customer_CheckReq
        {
            CustomerMobilePhone = "0999888777",
            ContactEmail = "notfound@domain.vn",
            MST = "999888777",
            OrgID = "7200000001"
        };
        var result = await _controller.Customer_Check(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("0", root.GetProperty("Action").GetString());
    }

    [Fact]
    public async Task Customer_Check_ViaModelJsonString_ReturnsAction1()
    {
        var modelJson = JsonSerializer.Serialize(new
        {
            CustomerMobilePhone = "0912345678",
            OrgID = "7200000001"
        });
        var req = new Mst_Customer_Customer_CheckReq
        {
            Model = modelJson
        };
        var result = await _controller.Customer_Check(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("1", root.GetProperty("Action").GetString());
        Assert.Equal("KH002", root.GetProperty("CustomerCode").GetString());
        Assert.Equal("Khách hàng có số điện thoại '0912345678' đã tồn tại. Bạn có muốn lấy thông tin KH hiện tại không?", root.GetProperty("Messages").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Customer_Check()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_cust_chk_{Guid.NewGuid():N}.db");
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

            // 1. Guard check: Body rỗng (null/empty) -> 400 Bad Request
            var reqEmpty = new StringContent("", Encoding.UTF8, "application/json");
            var resEmpty = await http.PostAsync("/api/mst_customer/customer_check", reqEmpty);
            var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Customer_Check Empty Body: {bodyEmpty}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(bodyEmpty);
            Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Customer_Customer_Check", docEmpty.RootElement.GetProperty("ErrorCode").GetString());

            // 2. Ca body {} -> 200 OK, Action = "0"
            var reqObjEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
            var resObjEmpty = await http.PostAsync("/api/mst_customer/customer_check", reqObjEmpty);
            var bodyObjEmpty = await resObjEmpty.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Customer_Check Object Empty: {bodyObjEmpty}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resObjEmpty.StatusCode);
            var docObjEmpty = JsonDocument.Parse(bodyObjEmpty);
            Assert.True(docObjEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0", docObjEmpty.RootElement.GetProperty("Action").GetString());

            // 3. Ca trùng Phone -> Action = "1", cảnh báo
            var phonePayload = JsonSerializer.Serialize(new { CustomerMobilePhone = "0901234567" });
            var reqPhone = new StringContent(phonePayload, Encoding.UTF8, "application/json");
            var resPhone = await http.PostAsync("/api/mst_customer/customer_check", reqPhone);
            var bodyPhone = await resPhone.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Customer_Check Phone Match: {bodyPhone}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resPhone.StatusCode);
            var docPhone = JsonDocument.Parse(bodyPhone);
            Assert.True(docPhone.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("1", docPhone.RootElement.GetProperty("Action").GetString());
            Assert.Equal("KH001", docPhone.RootElement.GetProperty("CustomerCode").GetString());
            Assert.Contains("0901234567", docPhone.RootElement.GetProperty("Messages").GetString());

            // 4. Ca trùng Phone nhưng cùng CustomerCodeSys -> Action = "0"
            var sameSysPayload = JsonSerializer.Serialize(new { CustomerMobilePhone = "0901234567", CustomerCodeSys = "CUS001" });
            var reqSameSys = new StringContent(sameSysPayload, Encoding.UTF8, "application/json");
            var resSameSys = await http.PostAsync("/api/mst_customer/customer_check", reqSameSys);
            var bodySameSys = await resSameSys.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Customer_Check Same CodeSys: {bodySameSys}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resSameSys.StatusCode);
            var docSameSys = JsonDocument.Parse(bodySameSys);
            Assert.True(docSameSys.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0", docSameSys.RootElement.GetProperty("Action").GetString());

            // 5. Ca không trùng -> Action = "0"
            var nonMatchPayload = JsonSerializer.Serialize(new { CustomerMobilePhone = "0999000111", ContactEmail = "noone@domain.vn" });
            var reqNonMatch = new StringContent(nonMatchPayload, Encoding.UTF8, "application/json");
            var resNonMatch = await http.PostAsync("/api/mst_customer/customer_check", reqNonMatch);
            var bodyNonMatch = await resNonMatch.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Customer_Check Non Match: {bodyNonMatch}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resNonMatch.StatusCode);
            var docNonMatch = JsonDocument.Parse(bodyNonMatch);
            Assert.True(docNonMatch.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0", docNonMatch.RootElement.GetProperty("Action").GetString());
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

    // ==========================================
    // CÁC TEST CHO UPDATE (Port #165)
    // Gate: Mst_CustomerController.cs:2170-2606 Update(string customercode, string customercodesys, string flagcustomerexsist)
    // API:  MstCustomerController.cs:92-162 WA_Mst_Customer_Get
    // BIZ:  Customer.cs:1464-1612 WAS_Mst_Customer_Get, 996-1462 Mst_Customer_Get
    // ==========================================

    [Fact]
    public async Task Update_NullReq_ReturnsSuccessTrueWithNullData()
    {
        var result = await _controller.Update(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_EmptyCustomerCode_ReturnsSuccessTrueWithNullData()
    {
        var req = new Mst_Customer_UpdateReq
        {
            CustomerCode = "",
            CustomerCodeSys = ""
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_NotFoundCustomerCode_ReturnsSuccessTrueWithNullData()
    {
        var req = new Mst_Customer_UpdateReq
        {
            CustomerCode = "KH_NON_EXISTENT",
            OrgID = "7200000001"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ValidCustomerCode_ReturnsCustomerWithDropdowns()
    {
        // KH001 đã có trong DB
        var req = new Mst_Customer_UpdateReq
        {
            CustomerCode = "KH001",
            OrgID = "7200000001"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.NotEqual(JsonValueKind.Null, data.ValueKind);

        Assert.Equal("KH001", data.GetProperty("CustomerCode").GetString());
        Assert.Equal("CUS001", data.GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("Công ty Cổ phần Alpha", data.GetProperty("CustomerName").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());

        // Kiểm tra ghép nhóm khách hàng Mst_CustomerInCustomerGroup + Mst_CustomerGroup (Mst_CustomerController.cs:2491-2501, 287-301)
        Assert.Equal("GRP01,GRP02", data.GetProperty("CustomerGrpCode").GetString());
        Assert.Equal("Nhóm Chiến Lược,Nhóm Tiềm Năng", data.GetProperty("mcg_CustomerGrpName").GetString());

        // Kiểm tra các trường enriched: Mst_CustomerType, Mst_CustomerSource, Mst_Area, Mst_NNT
        Assert.Equal("Doanh nghiệp", data.GetProperty("mct_CustomerTypeName").GetString());
        Assert.Equal("Trực tiếp", data.GetProperty("mcs_CustomerSourceName").GetString());
        Assert.Equal("Miền Bắc", data.GetProperty("ma_AreaName").GetString());
        Assert.Equal("Công ty TNHH Giải Pháp Số Hà Nội", data.GetProperty("Org_NNTFullName").GetString());

        // Kiểm tra danh mục dropdown đi kèm
        var cusTypes = data.GetProperty("List_Mst_CustomerType");
        Assert.True(cusTypes.GetArrayLength() > 0);

        var cusGroups = data.GetProperty("ListMst_CustomerGroupExt");
        Assert.True(cusGroups.GetArrayLength() > 0);

        var cusSources = data.GetProperty("ListMst_CustomerSourceExt");
        Assert.True(cusSources.GetArrayLength() > 0);

        var areas = data.GetProperty("ListMst_AreaExt");
        Assert.True(areas.GetArrayLength() > 0);

        var govTypes = data.GetProperty("ListMst_GovIDType");
        Assert.True(govTypes.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Update_FilterByOrgID_DifferentFromCustomerOrg_ReturnsNullData()
    {
        // KH001 thuộc OrgID 7200000001, lọc theo OrgID 7200000002 -> không tìm thấy
        var req = new Mst_Customer_UpdateReq
        {
            CustomerCode = "KH001",
            OrgID = "7200000002"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ViaModelJsonString_ReturnsCustomerWithDropdowns()
    {
        var modelJson = JsonSerializer.Serialize(new
        {
            CustomerCode = "KH002",
            OrgID = "7200000001",
            FlagCustomerExsist = "1"
        });
        var req = new Mst_Customer_UpdateReq
        {
            Model = modelJson
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("KH002", data.GetProperty("CustomerCode").GetString());
        Assert.Equal("CUS002", data.GetProperty("CustomerCodeSys").GetString());
        Assert.Equal("Tập đoàn Beta", data.GetProperty("CustomerName").GetString());
        Assert.Equal("1", data.GetProperty("FlagCustomerExsist").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Update()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_cust_upd_{Guid.NewGuid():N}.db");
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

            // 1. Ca body null / rỗng -> 200 OK, Data = null
            var reqEmpty = new StringContent("", Encoding.UTF8, "application/json");
            var resEmpty = await http.PostAsync("/api/mst_customer/update", reqEmpty);
            var bodyEmpty = await resEmpty.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Update Empty Body: {bodyEmpty}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(bodyEmpty);
            Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Data").ValueKind);

            // 2. Ca CustomerCode không tồn tại -> 200 OK, Data = null
            var nonExistPayload = JsonSerializer.Serialize(new { CustomerCode = "KH_KHONG_TON_TAI_99" });
            var reqNonExist = new StringContent(nonExistPayload, Encoding.UTF8, "application/json");
            var resNonExist = await http.PostAsync("/api/mst_customer/update", reqNonExist);
            var bodyNonExist = await resNonExist.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Update Non-Existent: {bodyNonExist}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resNonExist.StatusCode);
            var docNonExist = JsonDocument.Parse(bodyNonExist);
            Assert.True(docNonExist.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docNonExist.RootElement.GetProperty("Data").ValueKind);

            // 3. Ca hợp lệ: CustomerCode = "KH001" -> 200 OK, trả về thông tin khách hàng và dropdown
            var validPayload = JsonSerializer.Serialize(new { CustomerCode = "KH001", OrgID = "7200000001", FlagCustomerExsist = "1" });
            var reqValid = new StringContent(validPayload, Encoding.UTF8, "application/json");
            var resValid = await http.PostAsync("/api/mst_customer/update", reqValid);
            var bodyValid = await resValid.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Update Valid: {bodyValid}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resValid.StatusCode);
            var docValid = JsonDocument.Parse(bodyValid);
            Assert.True(docValid.RootElement.GetProperty("Success").GetBoolean());
            var dataValid = docValid.RootElement.GetProperty("Data");
            Assert.Equal("KH001", dataValid.GetProperty("CustomerCode").GetString());
            Assert.Equal("CUS001", dataValid.GetProperty("CustomerCodeSys").GetString());
            Assert.Equal("Công ty Cổ phần Alpha", dataValid.GetProperty("CustomerName").GetString());
            Assert.Equal("GRP01,GRP02", dataValid.GetProperty("CustomerGrpCode").GetString());
            Assert.Equal("1", dataValid.GetProperty("FlagCustomerExsist").GetString());
            Assert.True(dataValid.GetProperty("List_Mst_CustomerType").GetArrayLength() > 0);
            Assert.True(dataValid.GetProperty("ListMst_CustomerGroupExt").GetArrayLength() > 0);

            // 4. Ca hợp lệ thứ 2: CustomerCode = "KH002" -> trả về Beta với nhóm GRP02
            var valid2Payload = JsonSerializer.Serialize(new { CustomerCode = "KH002", OrgID = "7200000001" });
            var reqValid2 = new StringContent(valid2Payload, Encoding.UTF8, "application/json");
            var resValid2 = await http.PostAsync("/api/mst_customer/update", reqValid2);
            var bodyValid2 = await resValid2.Content.ReadAsStringAsync();
            _output?.WriteLine($"RealServer Update Valid KH002: {bodyValid2}");
            Assert.Equal(System.Net.HttpStatusCode.OK, resValid2.StatusCode);
            var docValid2 = JsonDocument.Parse(bodyValid2);
            var dataValid2 = docValid2.RootElement.GetProperty("Data");
            Assert.Equal("KH002", dataValid2.GetProperty("CustomerCode").GetString());
            Assert.Equal("CUS002", dataValid2.GetProperty("CustomerCodeSys").GetString());
            Assert.Equal("Tập đoàn Beta", dataValid2.GetProperty("CustomerName").GetString());
            Assert.Equal("GRP02", dataValid2.GetProperty("CustomerGrpCode").GetString());
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

