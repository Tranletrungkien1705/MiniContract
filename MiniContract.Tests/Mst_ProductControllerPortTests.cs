using System.Diagnostics;
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

namespace MiniContract.Tests;

public class Mst_ProductControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_ProductControllerPort _controller;

    public Mst_ProductControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_Product và Mst_ProductGroup
        var seeder = new Mst_ProductSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();
        var groupSeeder = new Mst_ProductGroupSeeder();
        groupSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_ProductControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_WithDefaultOrEmptyBody_ReturnsAllRootProducts()
    {
        var result = await _controller.Index(new Mst_Product_IndexReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(4, list.GetArrayLength());

        // Kiểm tra CountPrdBase cho PRD_ROOT_01: có 2 BASEPRD -> CountPrdBase = 2 + 1 = 3
        var prd1 = list[0];
        Assert.Equal("PRD_ROOT_01", prd1.GetProperty("ProductCode").GetString());
        Assert.Equal("3", prd1.GetProperty("CountPrdBase").GetString());

        // Kiểm tra CountPrdBase cho PRD_ROOT_02: không có BASEPRD -> CountPrdBase = 0 + 1 = 1
        var prd2 = list[1];
        Assert.Equal("PRD_ROOT_02", prd2.GetProperty("ProductCode").GetString());
        Assert.Equal("1", prd2.GetProperty("CountPrdBase").GetString());
    }

    [Fact]
    public async Task Index_FilterByOrgId_ReturnsOrgProducts()
    {
        var req = new Mst_Product_IndexReq { OrgID = "1001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: OrgID = "1001" -> 3 sản phẩm gốc
        var reqOrg = new Mst_Product_IndexReq { OrgID = "1001" };
        var resOrg = await _controller.Index(reqOrg);
        var okOrg = Assert.IsType<OkObjectResult>(resOrg);
        var docOrg = JsonDocument.Parse(JsonSerializer.Serialize(okOrg.Value));
        var dataOrg = docOrg.RootElement.GetProperty("Data");
        Assert.Equal(3, dataOrg.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, dataOrg.GetProperty("DataList").GetArrayLength());

        // Bộ lọc 2: OrgID = "1001" và FlagActive = "1" -> 2 sản phẩm (PRD_ROOT_03 ngừng hoạt động)
        var reqActive = new Mst_Product_IndexReq { OrgID = "1001", FlagActive = "1" };
        var resActive = await _controller.Index(reqActive);
        var okActive = Assert.IsType<OkObjectResult>(resActive);
        var docActive = JsonDocument.Parse(JsonSerializer.Serialize(okActive.Value));
        var dataActive = docActive.RootElement.GetProperty("Data");
        Assert.Equal(2, dataActive.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataActive.GetProperty("DataList").GetArrayLength());

        // Hai bộ lọc cho kết quả số dòng khác nhau
        Assert.NotEqual(dataOrg.GetProperty("ItemCount").GetInt32(), dataActive.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Index_FilterByKeyWord_ReturnsMatchingProducts()
    {
        var req = new Mst_Product_IndexReq { KeyWord = "Dịch vụ" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("DataList").GetArrayLength());
        Assert.Equal("PRD_ROOT_02", data.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByProductGrpCode_ReturnsGroupProducts()
    {
        var req = new Mst_Product_IndexReq { OrgID = "1001", ProductGrpCode = "GRP01" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data.GetProperty("DataList").GetArrayLength());
        Assert.Equal("PRD_ROOT_01", data.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());
        Assert.Equal("PRD_ROOT_03", data.GetProperty("DataList")[1].GetProperty("ProductCode").GetString());
    }

    [Fact]
    public async Task Index_Pagination_ReturnsCorrectPageAndCounts()
    {
        // Trang 0, cỡ trang 2
        var reqPage0 = new Mst_Product_IndexReq { RecordCount = 2, Page = 0 };
        var resPage0 = await _controller.Index(reqPage0);
        var okPage0 = Assert.IsType<OkObjectResult>(resPage0);
        var docPage0 = JsonDocument.Parse(JsonSerializer.Serialize(okPage0.Value));
        var dataPage0 = docPage0.RootElement.GetProperty("Data");

        Assert.Equal(4, dataPage0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataPage0.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageSize").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("DataList").GetArrayLength());

        // Trang 1, cỡ trang 2
        var reqPage1 = new Mst_Product_IndexReq { RecordCount = 2, Page = 1 };
        var resPage1 = await _controller.Index(reqPage1);
        var okPage1 = Assert.IsType<OkObjectResult>(resPage1);
        var docPage1 = JsonDocument.Parse(JsonSerializer.Serialize(okPage1.Value));
        var dataPage1 = docPage1.RootElement.GetProperty("Data");

        Assert.Equal(4, dataPage1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataPage1.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, dataPage1.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, dataPage1.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_PageSizeZero_ReturnsEmptyDataListWithTotalCount()
    {
        var req = new Mst_Product_IndexReq { PageSize = 0 };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_NotFound_ReturnsEmptyDataList()
    {
        var req = new Mst_Product_IndexReq { KeyWord = "KHONG_TON_TAI_XYZ_123" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_WithDefaultOrEmptyBody_ReturnsAllRootProducts()
    {
        var result = await _controller.DoSearch(new Mst_Product_DoSearchReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var list = data.GetProperty("DataList");
        Assert.Equal(4, list.GetArrayLength());

        // Kiểm tra CountPrdBase cho PRD_ROOT_01: có 2 BASEPRD -> CountPrdBase = 2 + 1 = 3
        var prd1 = list[0];
        Assert.Equal("PRD_ROOT_01", prd1.GetProperty("ProductCode").GetString());
        Assert.Equal("3", prd1.GetProperty("CountPrdBase").GetString());

        // Kiểm tra CountPrdBase cho PRD_ROOT_02: không có BASEPRD -> CountPrdBase = 0 + 1 = 1
        var prd2 = list[1];
        Assert.Equal("PRD_ROOT_02", prd2.GetProperty("ProductCode").GetString());
        Assert.Equal("1", prd2.GetProperty("CountPrdBase").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByOrgId_ReturnsOrgProducts()
    {
        var req = new Mst_Product_DoSearchReq { OrgID = "1001" };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: OrgID = "1001" -> 3 sản phẩm gốc
        var reqOrg = new Mst_Product_DoSearchReq { OrgID = "1001" };
        var resOrg = await _controller.DoSearch(reqOrg);
        var okOrg = Assert.IsType<OkObjectResult>(resOrg);
        var docOrg = JsonDocument.Parse(JsonSerializer.Serialize(okOrg.Value));
        var dataOrg = docOrg.RootElement.GetProperty("Data");
        Assert.Equal(3, dataOrg.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, dataOrg.GetProperty("DataList").GetArrayLength());

        // Bộ lọc 2: OrgID = "1001" và FlagActive = "1" -> 2 sản phẩm (PRD_ROOT_03 ngừng hoạt động)
        var reqActive = new Mst_Product_DoSearchReq { OrgID = "1001", FlagActive = "1" };
        var resActive = await _controller.DoSearch(reqActive);
        var okActive = Assert.IsType<OkObjectResult>(resActive);
        var docActive = JsonDocument.Parse(JsonSerializer.Serialize(okActive.Value));
        var dataActive = docActive.RootElement.GetProperty("Data");
        Assert.Equal(2, dataActive.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataActive.GetProperty("DataList").GetArrayLength());

        // Hai bộ lọc cho kết quả số dòng khác nhau (3 != 2)
        Assert.NotEqual(dataOrg.GetProperty("ItemCount").GetInt32(), dataActive.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task DoSearch_FilterByCheckboxes_ReturnsMatchingProductTypes()
    {
        // Ca 1: ckbproduct = "1" -> Chỉ trả về PRODUCT (PRD_ROOT_01)
        var reqProduct = new Mst_Product_DoSearchReq { OrgID = "1001", CkbProduct = "1" };
        var resProduct = await _controller.DoSearch(reqProduct);
        var okProduct = Assert.IsType<OkObjectResult>(resProduct);
        var docProduct = JsonDocument.Parse(JsonSerializer.Serialize(okProduct.Value));
        var dataProduct = docProduct.RootElement.GetProperty("Data");
        Assert.Equal(1, dataProduct.GetProperty("ItemCount").GetInt32());
        Assert.Equal("PRD_ROOT_01", dataProduct.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());

        // Ca 2: ckbservices = "1" -> Chỉ trả về SERVICE (PRD_ROOT_02)
        var reqService = new Mst_Product_DoSearchReq { OrgID = "1001", CkbServices = "1" };
        var resService = await _controller.DoSearch(reqService);
        var okService = Assert.IsType<OkObjectResult>(resService);
        var docService = JsonDocument.Parse(JsonSerializer.Serialize(okService.Value));
        var dataService = docService.RootElement.GetProperty("Data");
        Assert.Equal(1, dataService.GetProperty("ItemCount").GetInt32());
        Assert.Equal("PRD_ROOT_02", dataService.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());

        // Ca 3: ckbcombo = "1" -> Chỉ trả về COMBO (PRD_ROOT_03)
        var reqCombo = new Mst_Product_DoSearchReq { OrgID = "1001", CkbCombo = "1" };
        var resCombo = await _controller.DoSearch(reqCombo);
        var okCombo = Assert.IsType<OkObjectResult>(resCombo);
        var docCombo = JsonDocument.Parse(JsonSerializer.Serialize(okCombo.Value));
        var dataCombo = docCombo.RootElement.GetProperty("Data");
        Assert.Equal(1, dataCombo.GetProperty("ItemCount").GetInt32());
        Assert.Equal("PRD_ROOT_03", dataCombo.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());

        // Ca 4: ckbproduct = "1" và ckbcombo = "1" -> Trả về PRODUCT và COMBO (2 sản phẩm)
        var reqProdCombo = new Mst_Product_DoSearchReq { OrgID = "1001", CkbProduct = "1", CkbCombo = "1" };
        var resProdCombo = await _controller.DoSearch(reqProdCombo);
        var okProdCombo = Assert.IsType<OkObjectResult>(resProdCombo);
        var docProdCombo = JsonDocument.Parse(JsonSerializer.Serialize(okProdCombo.Value));
        var dataProdCombo = docProdCombo.RootElement.GetProperty("Data");
        Assert.Equal(2, dataProdCombo.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task DoSearch_FilterByProductCodeUserName_MatchesCodeOrName()
    {
        // Khớp theo ProductCodeUser = SP001
        var reqCode = new Mst_Product_DoSearchReq { OrgID = "1001", ProductCodeUserName = "SP001" };
        var resCode = await _controller.DoSearch(reqCode);
        var okCode = Assert.IsType<OkObjectResult>(resCode);
        var docCode = JsonDocument.Parse(JsonSerializer.Serialize(okCode.Value));
        var dataCode = docCode.RootElement.GetProperty("Data");
        Assert.Equal(1, dataCode.GetProperty("ItemCount").GetInt32());
        Assert.Equal("PRD_ROOT_01", dataCode.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());

        // Khớp theo ProductName like %bảo trì%
        var reqName = new Mst_Product_DoSearchReq { OrgID = "1001", ProductCodeUserName = "bảo trì" };
        var resName = await _controller.DoSearch(reqName);
        var okName = Assert.IsType<OkObjectResult>(resName);
        var docName = JsonDocument.Parse(JsonSerializer.Serialize(okName.Value));
        var dataName = docName.RootElement.GetProperty("Data");
        Assert.Equal(1, dataName.GetProperty("ItemCount").GetInt32());
        Assert.Equal("PRD_ROOT_02", dataName.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());

        // Không khớp
        var reqNone = new Mst_Product_DoSearchReq { OrgID = "1001", ProductCodeUserName = "KHONG_TON_TAI_123" };
        var resNone = await _controller.DoSearch(reqNone);
        var okNone = Assert.IsType<OkObjectResult>(resNone);
        var docNone = JsonDocument.Parse(JsonSerializer.Serialize(okNone.Value));
        var dataNone = docNone.RootElement.GetProperty("Data");
        Assert.Equal(0, dataNone.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataNone.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task DoSearch_FilterByProductGrpCode_ReturnsGroupProducts()
    {
        var req = new Mst_Product_DoSearchReq { OrgID = "1001", ProductGrpCode = "GRP01" };
        var res = await _controller.DoSearch(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data.GetProperty("DataList").GetArrayLength());
        Assert.Equal("PRD_ROOT_01", data.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());
        Assert.Equal("PRD_ROOT_03", data.GetProperty("DataList")[1].GetProperty("ProductCode").GetString());
    }

    [Fact]
    public async Task DoSearch_Pagination_ReturnsCorrectPageAndCounts()
    {
        // Trang 0, cỡ trang 2
        var reqPage0 = new Mst_Product_DoSearchReq { RecordCount = 2, Page = 0 };
        var resPage0 = await _controller.DoSearch(reqPage0);
        var okPage0 = Assert.IsType<OkObjectResult>(resPage0);
        var docPage0 = JsonDocument.Parse(JsonSerializer.Serialize(okPage0.Value));
        var dataPage0 = docPage0.RootElement.GetProperty("Data");

        Assert.Equal(4, dataPage0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataPage0.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageSize").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("DataList").GetArrayLength());

        // Trang 1, cỡ trang 2
        var reqPage1 = new Mst_Product_DoSearchReq { RecordCount = 2, Page = 1 };
        var resPage1 = await _controller.DoSearch(reqPage1);
        var okPage1 = Assert.IsType<OkObjectResult>(resPage1);
        var docPage1 = JsonDocument.Parse(JsonSerializer.Serialize(okPage1.Value));
        var dataPage1 = docPage1.RootElement.GetProperty("Data");

        Assert.Equal(4, dataPage1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataPage1.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, dataPage1.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, dataPage1.GetProperty("PageSize").GetInt32());
        Assert.Equal(2, dataPage1.GetProperty("DataList").GetArrayLength());

        // Cỡ trang 0
        var reqPage0Size = new Mst_Product_DoSearchReq { PageSize = 0 };
        var resPage0Size = await _controller.DoSearch(reqPage0Size);
        var okPage0Size = Assert.IsType<OkObjectResult>(resPage0Size);
        var docPage0Size = JsonDocument.Parse(JsonSerializer.Serialize(okPage0Size.Value));
        var dataPage0Size = docPage0Size.RootElement.GetProperty("Data");

        Assert.Equal(4, dataPage0Size.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataPage0Size.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataPage0Size.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task LoadData_WithEmptyReq_ReturnsBadRequestFail()
    {
        // Body rỗng -> guard Check.prcode
        var res = await _controller.LoadData(new Mst_Product_LoadDataReq());
        var badRes = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badRes.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_Product_Get", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task LoadData_WithEmptyPrCode_ReturnsBadRequestFail()
    {
        // prcode rỗng -> guard Check.prcode
        var res = await _controller.LoadData(new Mst_Product_LoadDataReq { prcode = "" });
        var badRes = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badRes.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_Product_Get", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task LoadData_ValidProductWithChildren_ReturnsBaseAndLevel2Products()
    {
        // PRD_ROOT_01 có 2 BASEPRD (PRD_BASE_01_1, PRD_BASE_01_2) và 1 L2PRD (PRD_L2_01_1)
        var req = new Mst_Product_LoadDataReq { prcode = "PRD_ROOT_01", OrgID = "1001" };
        var res = await _controller.LoadData(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var dataArray = doc.RootElement.GetProperty("Data");
        Assert.Equal(1, dataArray.GetArrayLength());

        var productBase = dataArray[0];
        var lstBase = productBase.GetProperty("LstPrdBase");
        Assert.Equal(2, lstBase.GetArrayLength());
        Assert.Equal("PRD_BASE_01_1", lstBase[0].GetProperty("ProductCode").GetString());
        Assert.Equal("PRD_BASE_01_2", lstBase[1].GetProperty("ProductCode").GetString());

        var lstL2 = productBase.GetProperty("LstPrdLeve2");
        Assert.Equal(1, lstL2.GetArrayLength());
        Assert.Equal("PRD_L2_01_1", lstL2[0].GetProperty("ProductCode").GetString());
    }

    [Fact]
    public async Task LoadData_ProductWithoutChildren_ReturnsEmptyBaseAndLevel2Lists()
    {
        // PRD_ROOT_02 không có sản phẩm con nào
        var req = new Mst_Product_LoadDataReq { prcode = "PRD_ROOT_02", OrgID = "1001" };
        var res = await _controller.LoadData(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var dataArray = doc.RootElement.GetProperty("Data");
        Assert.Equal(1, dataArray.GetArrayLength());

        var productBase = dataArray[0];
        var lstBase = productBase.GetProperty("LstPrdBase");
        Assert.Equal(0, lstBase.GetArrayLength());

        var lstL2 = productBase.GetProperty("LstPrdLeve2");
        Assert.Equal(0, lstL2.GetArrayLength());
    }

    [Fact]
    public async Task LoadData_TwoDifferentProducts_ReturnDifferentCounts()
    {
        // PRD_ROOT_01 trả về 2 base products
        var req1 = new Mst_Product_LoadDataReq { prcode = "PRD_ROOT_01", OrgID = "1001" };
        var res1 = await _controller.LoadData(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data")[0].GetProperty("LstPrdBase").GetArrayLength();
        Assert.Equal(2, count1);

        // PRD_ROOT_02 trả về 0 base products
        var req2 = new Mst_Product_LoadDataReq { prcode = "PRD_ROOT_02", OrgID = "1001" };
        var res2 = await _controller.LoadData(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data")[0].GetProperty("LstPrdBase").GetArrayLength();
        Assert.Equal(0, count2);

        // 2 bộ lọc cho số dòng khác nhau (2 != 0)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task Search_Product_Auto_WithEmptyBody_ReturnsEmptyListAndSuccessTrue()
    {
        // Body rỗng -> Success = true, ListProduct = []
        var res = await _controller.Search_Product_Auto(new Mst_Product_Search_Product_AutoReq());
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var listPrd = doc.RootElement.GetProperty("ListProduct");
        Assert.Equal(0, listPrd.GetArrayLength());
    }

    [Fact]
    public async Task Search_Product_Auto_SearchByProductCodeUser_ReturnsMatchingActiveProducts()
    {
        // SP001 khớp PRD_ROOT_01, PRD_BASE_01_1, PRD_BASE_01_2, PRD_L2_01_1 (4 sản phẩm)
        var req = new Mst_Product_Search_Product_AutoReq { productcodename = "SP001", OrgID = "1001" };
        var res = await _controller.Search_Product_Auto(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var listPrd = doc.RootElement.GetProperty("ListProduct");
        Assert.Equal(4, listPrd.GetArrayLength());
        Assert.Equal("PRD_BASE_01_1", listPrd[0].GetProperty("ProductCode").GetString());
        Assert.Contains(listPrd.EnumerateArray(), p => p.GetProperty("ProductCode").GetString() == "PRD_ROOT_01");
    }

    [Fact]
    public async Task Search_Product_Auto_SearchByProductName_ReturnsMatchingActiveProducts()
    {
        // "bảo trì" khớp PRD_ROOT_02 (1 sản phẩm)
        var req = new Mst_Product_Search_Product_AutoReq { productcodename = "bảo trì", OrgID = "1001" };
        var res = await _controller.Search_Product_Auto(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var listPrd = doc.RootElement.GetProperty("ListProduct");
        Assert.Equal(1, listPrd.GetArrayLength());
        Assert.Equal("PRD_ROOT_02", listPrd[0].GetProperty("ProductCode").GetString());
    }

    [Fact]
    public async Task Search_Product_Auto_InactiveProduct_ReturnsZeroRecords()
    {
        // SP003 là PRD_ROOT_03 có FlagActive = "0" -> Gate lọc FlagActive = "1" nên trả về 0 dòng
        var req = new Mst_Product_Search_Product_AutoReq { productcodename = "SP003", OrgID = "1001" };
        var res = await _controller.Search_Product_Auto(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var listPrd = doc.RootElement.GetProperty("ListProduct");
        Assert.Equal(0, listPrd.GetArrayLength());
    }

    [Fact]
    public async Task Search_Product_Auto_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: productcodename = "SP001" -> 4 sản phẩm
        var req1 = new Mst_Product_Search_Product_AutoReq { productcodename = "SP001", OrgID = "1001" };
        var res1 = await _controller.Search_Product_Auto(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("ListProduct").GetArrayLength();
        Assert.Equal(4, count1);

        // Bộ lọc 2: productcodename = "SP002" -> 1 sản phẩm
        var req2 = new Mst_Product_Search_Product_AutoReq { productcodename = "SP002", OrgID = "1001" };
        var res2 = await _controller.Search_Product_Auto(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("ListProduct").GetArrayLength();
        Assert.Equal(1, count2);

        // 2 bộ lọc cho số dòng khác nhau (4 != 1)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task Create_WithNullReq_ReturnsFailCheckStrJson()
    {
        var res = await _controller.Create(null);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_Create", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.True(doc.RootElement.GetProperty("Params").TryGetProperty("Check.strJson", out _));
    }

    [Fact]
    public async Task Create_WithEmptyReq_ReturnsFailMstProductTblNotFound()
    {
        var res = await _controller.Create(new Mst_Product_CreateReq());
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_Create_Input_Mst_ProductTblNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_WithMissingOrgId_ReturnsFailInvalidOrgID()
    {
        var req = new Mst_Product_CreateReq
        {
            OrgID = "",
            Lst_Mst_ProductUI_Create = new List<Mst_Product_CreateItem>
            {
                new() { OrgID = "", ProductName = "Sản phẩm A" }
            }
        };
        var res = await _controller.Create(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_Create_InvalidOrgID", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_WithEmptyProductName_ReturnsFailInvalidProductName()
    {
        var req = new Mst_Product_CreateReq
        {
            OrgID = "1001",
            Lst_Mst_ProductUI_Create = new List<Mst_Product_CreateItem>
            {
                new() { OrgID = "1001", ProductName = "" }
            }
        };
        var res = await _controller.Create(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_Create_InvalidProductName", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_WithIsInitTrue_ReturnsScreenInitMasterData()
    {
        var req = new Mst_Product_CreateReq { IsInit = true, OrgID = "1001" };
        var res = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("OrgID").GetString());
        Assert.True(data.TryGetProperty("List_Mst_ProductType", out _));
        Assert.True(data.TryGetProperty("List_Mst_ProductGroup", out _));
        Assert.True(data.TryGetProperty("List_Mst_Brand", out _));
    }

    [Fact]
    public async Task Create_SingleProduct_SuccessRoundTrip()
    {
        var prdCode = "PRD_CREATE_TEST_01";
        var req = new Mst_Product_CreateReq
        {
            OrgID = "1001",
            ProductCode = prdCode,
            ProductName = "Sản phẩm kiểm tra Create 1",
            ProductCodeUser = "SP_TEST_01",
            ProductLevelSys = "ROOTPRD",
            BrandCode = "BRAND_01",
            ProductType = "PRODUCT",
            UnitCode = "CAI",
            UPBuy = 100000,
            UPSell = 150000,
            FlagActive = "1"
        };

        var res = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        // Đọc lại từ database kiểm tra round-trip
        var saved = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == prdCode);
        Assert.NotNull(saved);
        Assert.Equal("Sản phẩm kiểm tra Create 1", saved.ProductName);
        Assert.Equal("SP_TEST_01", saved.ProductCodeUser);
        Assert.Equal("ROOTPRD", saved.ProductLevelSys);
        Assert.Equal(prdCode, saved.ProductCodeRoot);
        Assert.Equal(prdCode, saved.ProductCodeBase);
        Assert.Equal(100000, saved.UPBuy);
        Assert.Equal(150000, saved.UPSell);
        Assert.Equal("1", saved.FlagActive);
    }

    [Fact]
    public async Task Create_MultiProducts_ViaModelJson_SuccessRoundTrip()
    {
        var jsonModel = JsonSerializer.Serialize(new
        {
            OrgID = "1001",
            Lst_Mst_ProductUI_Create = new[]
            {
                new
                {
                    OrgID = "1001",
                    ProductCode = "PRD_ROOT_MULTI_01",
                    ProductCodeUser = "SP_MULTI_01",
                    ProductName = "Sản phẩm gốc Multi",
                    ProductLevelSys = "ROOTPRD"
                },
                new
                {
                    OrgID = "1001",
                    ProductCode = "PRD_BASE_MULTI_01_1",
                    ProductCodeUser = "SP_MULTI_01_1",
                    ProductName = "Sản phẩm quy cách 1",
                    ProductLevelSys = "BASEPRD"
                }
            }
        });

        var req = new Mst_Product_CreateReq { Model = jsonModel };
        var res = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var rootPrd = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == "PRD_ROOT_MULTI_01");
        var basePrd = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == "PRD_BASE_MULTI_01_1");

        Assert.NotNull(rootPrd);
        Assert.NotNull(basePrd);
        Assert.Equal("PRD_ROOT_MULTI_01", rootPrd.ProductCodeRoot);
        Assert.Equal("PRD_ROOT_MULTI_01", basePrd.ProductCodeRoot);
        Assert.Equal("PRD_BASE_MULTI_01_1", basePrd.ProductCodeBase);
    }

    [Fact]
    public async Task GetBOM_WithNullOrEmptyReq_ReturnsAllProducts()
    {
        // Body rỗng -> Success = true, Data trả về toàn bộ sản phẩm đã seed
        var resNull = await _controller.GetBOM(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(7, docNull.RootElement.GetProperty("Data").GetArrayLength());

        var resEmpty = await _controller.GetBOM(new Mst_Product_GetBOMReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(7, docEmpty.RootElement.GetProperty("Data").GetArrayLength());
    }

    [Fact]
    public async Task GetBOM_FilterByOrgId_ReturnsOrgProducts()
    {
        var req = new Mst_Product_GetBOMReq { OrgID = "1001" };
        var res = await _controller.GetBOM(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(6, data.GetArrayLength());
    }

    [Fact]
    public async Task GetBOM_FilterByPrdId_MatchesProductCodeUser()
    {
        // Khớp theo ProductCodeUser = SP001
        var req = new Mst_Product_GetBOMReq { prdid = "SP001", OrgID = "1001" };
        var res = await _controller.GetBOM(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("PRD_ROOT_01", data[0].GetProperty("ProductCode").GetString());
        Assert.Equal("SP001", data[0].GetProperty("ProductCodeUser").GetString());
    }

    [Fact]
    public async Task GetBOM_FilterByPrdId_MatchesProductName()
    {
        // Khớp theo ProductName = "Sản phẩm mẫu 01"
        var req = new Mst_Product_GetBOMReq { prdid = "Sản phẩm mẫu 01", OrgID = "1001" };
        var res = await _controller.GetBOM(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("PRD_ROOT_01", data[0].GetProperty("ProductCode").GetString());
        Assert.Equal("Sản phẩm mẫu 01", data[0].GetProperty("ProductName").GetString());
    }

    [Fact]
    public async Task GetBOM_FilterByPrdId_NotFound_ReturnsEmptyList()
    {
        var req = new Mst_Product_GetBOMReq { prdid = "KHONG_TON_TAI_XYZ_999", OrgID = "1001" };
        var res = await _controller.GetBOM(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task GetBOM_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: OrgID = "1001" -> 6 sản phẩm
        var req1 = new Mst_Product_GetBOMReq { OrgID = "1001" };
        var res1 = await _controller.GetBOM(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(6, count1);

        // Bộ lọc 2: prdid = "SP001", OrgID = "1001" -> 1 sản phẩm
        var req2 = new Mst_Product_GetBOMReq { prdid = "SP001", OrgID = "1001" };
        var res2 = await _controller.GetBOM(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(1, count2);

        // 2 bộ lọc cho số dòng khác nhau (6 != 1)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task GetBOM_Pagination_ReturnsCorrectSubset()
    {
        // Lấy 2 sản phẩm đầu tiên của OrgID 1001
        var req = new Mst_Product_GetBOMReq { OrgID = "1001", Ft_RecordStart = 0, Ft_RecordCount = 2 };
        var res = await _controller.GetBOM(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(2, data.GetArrayLength());
    }

    [Fact]
    public async Task SearchProduct_WithNullOrEmptyReq_ReturnsAllProducts()
    {
        // Body rỗng -> Success = true, Data trả về toàn bộ sản phẩm đã seed (7 sản phẩm)
        var resNull = await _controller.SearchProduct(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(7, docNull.RootElement.GetProperty("Data").GetArrayLength());

        var resEmpty = await _controller.SearchProduct(new Mst_Product_SearchProductReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(7, docEmpty.RootElement.GetProperty("Data").GetArrayLength());
    }

    [Fact]
    public async Task SearchProduct_FilterByOrgId_ReturnsOrgProducts()
    {
        var req = new Mst_Product_SearchProductReq { OrgID = "1001" };
        var res = await _controller.SearchProduct(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(6, data.GetArrayLength());
    }

    [Fact]
    public async Task SearchProduct_FilterByPrdId_MatchesProductCodeUser()
    {
        // Khớp theo ProductCodeUser chứa SP001 (like %SP001%):
        // PRD_ROOT_01 (SP001), PRD_BASE_01_1 (SP001_HOP), PRD_BASE_01_2 (SP001_THUNG), PRD_L2_01_1 (SP001_L2) -> 4 sản phẩm
        var req = new Mst_Product_SearchProductReq { prdid = "SP001", OrgID = "1001" };
        var res = await _controller.SearchProduct(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(4, data.GetArrayLength());
        Assert.Contains(data.EnumerateArray(), p => p.GetProperty("ProductCode").GetString() == "PRD_ROOT_01");
        Assert.Contains(data.EnumerateArray(), p => p.GetProperty("ProductCode").GetString() == "PRD_BASE_01_1");
        Assert.Contains(data.EnumerateArray(), p => p.GetProperty("ProductCode").GetString() == "PRD_BASE_01_2");
        Assert.Contains(data.EnumerateArray(), p => p.GetProperty("ProductCode").GetString() == "PRD_L2_01_1");
    }

    [Fact]
    public async Task SearchProduct_FilterByPrdId_MatchesProductName()
    {
        // Khớp theo ProductName like %mẫu 01%:
        // 4 sản phẩm cùng gốc PRD_ROOT_01 đều có tên chứa "mẫu 01"
        var req = new Mst_Product_SearchProductReq { prdid = "mẫu 01", OrgID = "1001" };
        var res = await _controller.SearchProduct(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(4, data.GetArrayLength());
    }

    [Fact]
    public async Task SearchProduct_FilterByPrdId_SingleMatch_ReturnsOneProduct()
    {
        // Khớp theo ProductName like %bảo trì%: PRD_ROOT_02 -> 1 sản phẩm
        var req = new Mst_Product_SearchProductReq { prdid = "bảo trì", OrgID = "1001" };
        var res = await _controller.SearchProduct(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("PRD_ROOT_02", data[0].GetProperty("ProductCode").GetString());
    }

    [Fact]
    public async Task SearchProduct_FilterByPrdId_NotFound_ReturnsEmptyList()
    {
        var req = new Mst_Product_SearchProductReq { prdid = "KHONG_TON_TAI_XYZ_999", OrgID = "1001" };
        var res = await _controller.SearchProduct(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task SearchProduct_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: prdid = "SP001", OrgID = "1001" -> 4 sản phẩm
        var req1 = new Mst_Product_SearchProductReq { prdid = "SP001", OrgID = "1001" };
        var res1 = await _controller.SearchProduct(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(4, count1);

        // Bộ lọc 2: prdid = "bảo trì", OrgID = "1001" -> 1 sản phẩm
        var req2 = new Mst_Product_SearchProductReq { prdid = "bảo trì", OrgID = "1001" };
        var res2 = await _controller.SearchProduct(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(1, count2);

        // 2 bộ lọc cho số dòng khác nhau (4 != 1)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task SearchProduct_Pagination_ReturnsCorrectSubset()
    {
        // Lấy 2 sản phẩm đầu tiên của OrgID 1001
        var req = new Mst_Product_SearchProductReq { OrgID = "1001", Ft_RecordStart = 0, Ft_RecordCount = 2 };
        var res = await _controller.SearchProduct(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(2, data.GetArrayLength());
    }

    [Fact]
    public async Task GetSearch_WithNullOrEmptyReq_ReturnsAllActiveProducts()
    {
        // Body rỗng -> Success = true, FlagActive = "1", Data trả về RT_Mst_Product chứa danh sách sản phẩm hoạt động
        var resNull = await _controller.GetSearch(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        var dataNull = docNull.RootElement.GetProperty("Data");
        var listNull = dataNull.GetProperty("Lst_Mst_Product");
        // 6 sản phẩm active trên toàn hệ thống (PRD_ROOT_01, PRD_BASE_01_1, PRD_BASE_01_2, PRD_L2_01_1, PRD_ROOT_02, PRD_ROOT_99)
        Assert.Equal(6, listNull.GetArrayLength());
        Assert.Equal(6, docNull.RootElement.GetProperty("ItemCount").GetInt32());

        var resEmpty = await _controller.GetSearch(new Mst_Product_GetSearchReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(6, docEmpty.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product").GetArrayLength());
    }

    [Fact]
    public async Task GetSearch_FilterByOrgId_ReturnsOrgActiveProducts()
    {
        // OrgID = "1001" -> 5 sản phẩm active thuộc Org 1001 (PRD_ROOT_03 FlagActive=0, PRD_ROOT_99 Org=9999)
        var req = new Mst_Product_GetSearchReq { OrgID = "1001" };
        var res = await _controller.GetSearch(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var list = doc.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(5, list.GetArrayLength());
        Assert.Equal(5, doc.RootElement.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task GetSearch_FilterByProductCodeUserName_MatchesCodeOrName()
    {
        // Khớp theo ProductCodeUser = SP001
        var reqCode = new Mst_Product_GetSearchReq { OrgID = "1001", ProductCodeUserName = "SP001" };
        var resCode = await _controller.GetSearch(reqCode);
        var okCode = Assert.IsType<OkObjectResult>(resCode);
        var docCode = JsonDocument.Parse(JsonSerializer.Serialize(okCode.Value));
        var listCode = docCode.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(1, listCode.GetArrayLength());
        Assert.Equal("PRD_ROOT_01", listCode[0].GetProperty("ProductCode").GetString());

        // Khớp theo ProductName like %bảo trì%
        var reqName = new Mst_Product_GetSearchReq { OrgID = "1001", ProductCodeUserName = "bảo trì" };
        var resName = await _controller.GetSearch(reqName);
        var okName = Assert.IsType<OkObjectResult>(resName);
        var docName = JsonDocument.Parse(JsonSerializer.Serialize(okName.Value));
        var listName = docName.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(1, listName.GetArrayLength());
        Assert.Equal("PRD_ROOT_02", listName[0].GetProperty("ProductCode").GetString());

        // Không khớp
        var reqNone = new Mst_Product_GetSearchReq { OrgID = "1001", ProductCodeUserName = "KHONG_TON_TAI_123" };
        var resNone = await _controller.GetSearch(reqNone);
        var okNone = Assert.IsType<OkObjectResult>(resNone);
        var docNone = JsonDocument.Parse(JsonSerializer.Serialize(okNone.Value));
        Assert.Equal(0, docNone.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product").GetArrayLength());
        Assert.Equal(0, docNone.RootElement.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task GetSearch_FilterByProductGrpCode_ReturnsGroupProducts()
    {
        // GRP01 thuộc Org 1001 có 4 sản phẩm active (PRD_ROOT_01, PRD_BASE_01_1, PRD_BASE_01_2, PRD_L2_01_1)
        var req = new Mst_Product_GetSearchReq { OrgID = "1001", ProductGrpCode = "GRP01" };
        var res = await _controller.GetSearch(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var list = doc.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(4, list.GetArrayLength());
        Assert.Equal(4, doc.RootElement.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task GetSearch_FilterByCheckboxes_ReturnsMatchingProductTypes()
    {
        // Ca 1: ckbproduct = "1" -> Chỉ trả về PRODUCT thuộc Org 1001 (4 sản phẩm)
        var reqProduct = new Mst_Product_GetSearchReq { OrgID = "1001", CkbProduct = "1" };
        var resProduct = await _controller.GetSearch(reqProduct);
        var okProduct = Assert.IsType<OkObjectResult>(resProduct);
        var docProduct = JsonDocument.Parse(JsonSerializer.Serialize(okProduct.Value));
        var listProduct = docProduct.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(4, listProduct.GetArrayLength());

        // Ca 2: ckbservices = "1" -> Chỉ trả về SERVICE thuộc Org 1001 (PRD_ROOT_02 - 1 sản phẩm)
        var reqService = new Mst_Product_GetSearchReq { OrgID = "1001", CkbServices = "1" };
        var resService = await _controller.GetSearch(reqService);
        var okService = Assert.IsType<OkObjectResult>(resService);
        var docService = JsonDocument.Parse(JsonSerializer.Serialize(okService.Value));
        var listService = docService.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(1, listService.GetArrayLength());
        Assert.Equal("PRD_ROOT_02", listService[0].GetProperty("ProductCode").GetString());

        // Ca 3: ckbcombo = "1" -> Trả về COMBO thuộc Org 1001 (PRD_ROOT_03 - 1 sản phẩm; khi chọn checkbox không ép FlagActive=1)
        var reqCombo = new Mst_Product_GetSearchReq { OrgID = "1001", CkbCombo = "1" };
        var resCombo = await _controller.GetSearch(reqCombo);
        var okCombo = Assert.IsType<OkObjectResult>(resCombo);
        var docCombo = JsonDocument.Parse(JsonSerializer.Serialize(okCombo.Value));
        var listCombo = docCombo.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(1, listCombo.GetArrayLength());
        Assert.Equal("PRD_ROOT_03", listCombo[0].GetProperty("ProductCode").GetString());

        // Ca 4: ckbproduct = "1" và ckbservices = "1" -> Trả về PRODUCT và SERVICE (5 sản phẩm)
        var reqProdServ = new Mst_Product_GetSearchReq { OrgID = "1001", CkbProduct = "1", CkbServices = "1" };
        var resProdServ = await _controller.GetSearch(reqProdServ);
        var okProdServ = Assert.IsType<OkObjectResult>(resProdServ);
        var docProdServ = JsonDocument.Parse(JsonSerializer.Serialize(okProdServ.Value));
        var listProdServ = docProdServ.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
        Assert.Equal(5, listProdServ.GetArrayLength());
    }

    [Fact]
    public async Task GetSearch_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: ckbproduct = "1", OrgID = "1001" -> 4 sản phẩm
        var req1 = new Mst_Product_GetSearchReq { OrgID = "1001", CkbProduct = "1" };
        var res1 = await _controller.GetSearch(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product").GetArrayLength();
        Assert.Equal(4, count1);

        // Bộ lọc 2: ckbservices = "1", OrgID = "1001" -> 1 sản phẩm
        var req2 = new Mst_Product_GetSearchReq { OrgID = "1001", CkbServices = "1" };
        var res2 = await _controller.GetSearch(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product").GetArrayLength();
        Assert.Equal(1, count2);

        // 2 bộ lọc cho số dòng khác nhau (4 != 1)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task GetSearch_Pagination_ReturnsCorrectPageAndCounts()
    {
        // Trang 0, cỡ trang 2 cho OrgID 1001 (tổng 5 bản ghi active)
        var reqPage0 = new Mst_Product_GetSearchReq { OrgID = "1001", RecordCount = 2, Page = 0 };
        var resPage0 = await _controller.GetSearch(reqPage0);
        var okPage0 = Assert.IsType<OkObjectResult>(resPage0);
        var docPage0 = JsonDocument.Parse(JsonSerializer.Serialize(okPage0.Value));

        Assert.Equal(5, docPage0.RootElement.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, docPage0.RootElement.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, docPage0.RootElement.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, docPage0.RootElement.GetProperty("PageSize").GetInt32());
        Assert.Equal(2, docPage0.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product").GetArrayLength());

        // Trang 2, cỡ trang 2 cho OrgID 1001 (1 bản ghi cuối)
        var reqPage2 = new Mst_Product_GetSearchReq { OrgID = "1001", RecordCount = 2, Page = 2 };
        var resPage2 = await _controller.GetSearch(reqPage2);
        var okPage2 = Assert.IsType<OkObjectResult>(resPage2);
        var docPage2 = JsonDocument.Parse(JsonSerializer.Serialize(okPage2.Value));

        Assert.Equal(5, docPage2.RootElement.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, docPage2.RootElement.GetProperty("PageCount").GetInt32());
        Assert.Equal(2, docPage2.RootElement.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, docPage2.RootElement.GetProperty("PageSize").GetInt32());
        Assert.Equal(1, docPage2.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product").GetArrayLength());

        // Cỡ trang 0
        var reqPage0Size = new Mst_Product_GetSearchReq { OrgID = "1001", PageSize = 0 };
        var resPage0Size = await _controller.GetSearch(reqPage0Size);
        var okPage0Size = Assert.IsType<OkObjectResult>(resPage0Size);
        var docPage0Size = JsonDocument.Parse(JsonSerializer.Serialize(okPage0Size.Value));

        Assert.Equal(5, docPage0Size.RootElement.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, docPage0Size.RootElement.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, docPage0Size.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product").GetArrayLength());
    }

    [Fact]
    public async Task ProductIngredient_WithNullOrEmptyReq_ReturnsAllRootProductsAndProductGroups()
    {
        // Body rỗng -> Success = true, Data trả về toàn bộ sản phẩm ROOTPRD, ListMstProductGroup trả về các nhóm
        var resNull = await _controller.ProductIngredient(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        var dataNull = docNull.RootElement.GetProperty("Data");
        Assert.Equal(4, dataNull.GetArrayLength());
        Assert.Equal(4, docNull.RootElement.GetProperty("ItemCount").GetInt32());
        var groupsNull = docNull.RootElement.GetProperty("ListMstProductGroup");
        Assert.Equal(3, groupsNull.GetArrayLength());

        var resEmpty = await _controller.ProductIngredient(new Mst_Product_ProductIngredientReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(4, docEmpty.RootElement.GetProperty("Data").GetArrayLength());
    }

    [Fact]
    public async Task ProductIngredient_FilterByOrgId_ReturnsOrgRootProductsAndGroups()
    {
        // OrgID = "1001" -> 3 sản phẩm ROOTPRD thuộc Org 1001, 2 nhóm thuộc Org 1001
        var req = new Mst_Product_ProductIngredientReq { OrgID = "1001" };
        var res = await _controller.ProductIngredient(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var list = doc.RootElement.GetProperty("Data");
        Assert.Equal(3, list.GetArrayLength());
        Assert.Equal(3, doc.RootElement.GetProperty("ItemCount").GetInt32());

        var groups = doc.RootElement.GetProperty("ListMstProductGroup");
        Assert.Equal(2, groups.GetArrayLength());
        Assert.Equal("GRP01", groups[0].GetProperty("ProductGrpCode").GetString());
        Assert.Equal("GRP02", groups[1].GetProperty("ProductGrpCode").GetString());
    }

    [Fact]
    public async Task ProductIngredient_FilterByProductCodeUserName_MatchesProductCodeUser()
    {
        // Khớp theo ProductCodeUser = SP001
        var req = new Mst_Product_ProductIngredientReq { OrgID = "1001", ProductCodeUserName = "SP001" };
        var res = await _controller.ProductIngredient(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var list = doc.RootElement.GetProperty("Data");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("PRD_ROOT_01", list[0].GetProperty("ProductCode").GetString());
        Assert.Equal("SP001", list[0].GetProperty("ProductCodeUser").GetString());
    }

    [Fact]
    public async Task ProductIngredient_FilterByProductCodeUserName_NotFound_ReturnsZeroRecords()
    {
        var req = new Mst_Product_ProductIngredientReq { OrgID = "1001", ProductCodeUserName = "KHONG_TON_TAI_XYZ" };
        var res = await _controller.ProductIngredient(req);
        var ok = Assert.IsType<OkObjectResult>(res);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var list = doc.RootElement.GetProperty("Data");
        Assert.Equal(0, list.GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task ProductIngredient_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: OrgID = "1001" -> 3 sản phẩm ROOTPRD
        var req1 = new Mst_Product_ProductIngredientReq { OrgID = "1001" };
        var res1 = await _controller.ProductIngredient(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(3, count1);

        // Bộ lọc 2: ProductCodeUserName = "SP001", OrgID = "1001" -> 1 sản phẩm ROOTPRD
        var req2 = new Mst_Product_ProductIngredientReq { OrgID = "1001", ProductCodeUserName = "SP001" };
        var res2 = await _controller.ProductIngredient(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data").GetArrayLength();
        Assert.Equal(1, count2);

        // 2 bộ lọc cho số dòng khác nhau (3 != 1)
        Assert.NotEqual(count1, count2);
    }

    [Fact]
    public async Task ProductIngredient_Pagination_ReturnsCorrectPageAndCounts()
    {
        // Trang 0, cỡ trang 2 cho OrgID 1001 (tổng 3 bản ghi ROOTPRD)
        var reqPage0 = new Mst_Product_ProductIngredientReq { OrgID = "1001", Page = 0, PageCount = 2 };
        var resPage0 = await _controller.ProductIngredient(reqPage0);
        var okPage0 = Assert.IsType<OkObjectResult>(resPage0);
        var docPage0 = JsonDocument.Parse(JsonSerializer.Serialize(okPage0.Value));

        Assert.Equal(3, docPage0.RootElement.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, docPage0.RootElement.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, docPage0.RootElement.GetProperty("PageCur").GetInt32());
        Assert.Equal(2, docPage0.RootElement.GetProperty("PageSize").GetInt32());
        Assert.Equal(2, docPage0.RootElement.GetProperty("Data").GetArrayLength());

        // Trang 1, cỡ trang 2 cho OrgID 1001 (1 bản ghi cuối)
        var reqPage1 = new Mst_Product_ProductIngredientReq { OrgID = "1001", Page = 1, PageCount = 2 };
        var resPage1 = await _controller.ProductIngredient(reqPage1);
        var okPage1 = Assert.IsType<OkObjectResult>(resPage1);
        var docPage1 = JsonDocument.Parse(JsonSerializer.Serialize(okPage1.Value));

        Assert.Equal(3, docPage1.RootElement.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, docPage1.RootElement.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, docPage1.RootElement.GetProperty("PageCur").GetInt32());
        Assert.Equal(2, docPage1.RootElement.GetProperty("PageSize").GetInt32());
        Assert.Equal(1, docPage1.RootElement.GetProperty("Data").GetArrayLength());

        // Cỡ trang 0
        var reqPage0Size = new Mst_Product_ProductIngredientReq { OrgID = "1001", PageSize = 0 };
        var resPage0Size = await _controller.ProductIngredient(reqPage0Size);
        var okPage0Size = Assert.IsType<OkObjectResult>(resPage0Size);
        var docPage0Size = JsonDocument.Parse(JsonSerializer.Serialize(okPage0Size.Value));

        Assert.Equal(3, docPage0Size.RootElement.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, docPage0Size.RootElement.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, docPage0Size.RootElement.GetProperty("Data").GetArrayLength());
    }

    [Fact]
    public async Task Update_WithNullReq_ReturnsFailCheckStrJson()
    {
        var res = await _controller.Update(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_Product_Get", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.True(doc.RootElement.GetProperty("Params").TryGetProperty("Check.strJson", out _));
    }

    [Fact]
    public async Task Update_WithMissingPrlv_ReturnsFailCheckPrlv()
    {
        var req = new Mst_Product_UpdateReq { prroot = "PRD_ROOT_01" };
        var res = await _controller.Update(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_Product_Get", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.True(doc.RootElement.GetProperty("Params").TryGetProperty("Check.prlv", out _));
    }

    [Fact]
    public async Task Update_RootProduct_ReturnsRootAndBaseProductsAndUnitCodeBaseEmpty()
    {
        var req = new Mst_Product_UpdateReq
        {
            OrgID = "1001",
            prroot = "PRD_ROOT_01",
            prlv = "ROOTPRD"
        };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        var prd = data.GetProperty("Product");
        Assert.Equal("PRD_BASE_01_1", prd.GetProperty("ProductCode").GetString());
        Assert.Equal("BASEPRD", prd.GetProperty("ProductLevelSys").GetString());
        Assert.Equal("", data.GetProperty("UnitCodeBase").GetString());

        var listUnitCode = data.GetProperty("ListUnitCode");
        Assert.Equal(1, listUnitCode.GetArrayLength());
        Assert.Equal("PRD_L2_01_1", listUnitCode[0].GetProperty("ProductCode").GetString());
        Assert.Equal("L2PRD", listUnitCode[0].GetProperty("ProductLevelSys").GetString());

        var groups = data.GetProperty("List_Mst_ProductGroup");
        Assert.Equal(2, groups.GetArrayLength());
        var groupsUI = data.GetProperty("List_Mst_ProductGroupUI");
        Assert.Equal(2, groupsUI.GetArrayLength());
    }

    [Fact]
    public async Task Update_Level2Product_ReturnsUnitCodeBaseFromRootAndListUnitCode()
    {
        var req = new Mst_Product_UpdateReq
        {
            OrgID = "1001",
            prcode = "PRD_L2_01_1",
            prroot = "PRD_ROOT_01",
            prlv = "L2PRD"
        };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("CAI", data.GetProperty("UnitCodeBase").GetString());

        var listUnitCode = data.GetProperty("ListUnitCode");
        Assert.Equal(1, listUnitCode.GetArrayLength());
        Assert.Equal("PRD_L2_01_1", listUnitCode[0].GetProperty("ProductCode").GetString());
    }

    [Fact]
    public async Task Update_NonExistentProduct_ReturnsEmptyProductAndEmptyListUnitCode()
    {
        var req = new Mst_Product_UpdateReq
        {
            OrgID = "1001",
            prcode = "KHONG_TON_TAI",
            prroot = "KHONG_TON_TAI",
            prlv = "ROOTPRD"
        };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(JsonValueKind.Null, data.GetProperty("Product").ValueKind);
        Assert.Equal(0, data.GetProperty("ListUnitCode").GetArrayLength());
        Assert.Equal(2, data.GetProperty("List_Mst_ProductGroup").GetArrayLength());
    }

    [Fact]
    public async Task Update_TwoFilters_ReturnDifferentCounts()
    {
        // Bộ lọc 1: prroot = "PRD_ROOT_01", prlv = "ROOTPRD", OrgID = "1001" -> ListUnitCode có 1 phần tử (PRD_L2_01_1)
        var req1 = new Mst_Product_UpdateReq
        {
            OrgID = "1001",
            prroot = "PRD_ROOT_01",
            prlv = "ROOTPRD"
        };
        var res1 = await _controller.Update(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var count1 = doc1.RootElement.GetProperty("Data").GetProperty("ListUnitCode").GetArrayLength();
        Assert.Equal(1, count1);

        // Bộ lọc 2: prroot = "PRD_ROOT_02", prlv = "ROOTPRD", OrgID = "1001" -> ListUnitCode có 0 phần tử
        var req2 = new Mst_Product_UpdateReq
        {
            OrgID = "1001",
            prroot = "PRD_ROOT_02",
            prlv = "ROOTPRD"
        };
        var res2 = await _controller.Update(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        var count2 = doc2.RootElement.GetProperty("Data").GetProperty("ListUnitCode").GetArrayLength();
        Assert.Equal(0, count2);

        // 2 bộ lọc cho số dòng khác nhau (1 != 0)
        Assert.NotEqual(count1, count2);
    }

    // =========================================================================
    // Các ca kiểm thử cho action Edit (/api/mst_product/edit)
    // =========================================================================

    [Fact]
    public async Task Edit_WithNullReq_ReturnsFailMstProductSave()
    {
        // Guard: Body null hoàn toàn -> báo lỗi Mst_Product_Save
        var res = await _controller.Edit(null);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_Product_Save", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_WithMissingProductLevelSys_ReturnsFailMstProductSave()
    {
        // Guard: Body thiếu ProductLevelSys -> báo lỗi Mst_Product_Save
        var req = new Mst_Product_EditReq
        {
            Mst_Product = new Mst_Product_EditItem
            {
                ProductCode = "PRD_ROOT_01",
                ProductLevelSys = null
            }
        };
        var res = await _controller.Edit(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_Product_Save", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_NonExistentProduct_ReturnsFailMstProductSave()
    {
        // Guard: Không tìm thấy sản phẩm cần sửa -> báo lỗi Mst_Product_Save
        var req = new Mst_Product_EditReq
        {
            Mst_Product = new Mst_Product_EditItem
            {
                OrgID = "1001",
                ProductCode = "PRD_KHONG_TON_TAI_XYZ",
                ProductCodeRoot = "PRD_KHONG_TON_TAI_XYZ",
                ProductLevelSys = "ROOTPRD"
            }
        };
        var res = await _controller.Edit(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_Product_Save", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_DuplicateUnitCode_ReturnsSuccessTrueWithDuplicateWarning()
    {
        // Nghiệp vụ bẫy nguồn (Gate Mst_ProductController.cs:2398-2400):
        // Khi thêm đơn vị mới bị trùng mã đơn vị đã tồn tại (CAI), gate trả về Success = true kèm thông báo cảnh báo
        var req = new Mst_Product_EditReq
        {
            Mst_Product = new Mst_Product_EditItem
            {
                OrgID = "1001",
                ProductCode = "PRD_ROOT_01",
                ProductCodeRoot = "PRD_ROOT_01",
                ProductLevelSys = "ROOTPRD"
            },
            Lst_Unit = new List<Mst_Unit_EditItem>
            {
                new() { UnitCode = "CAI", ProductCode = "PRD_ROOT_01" }
            },
            Lst_Mst_ProductUI_Create = new List<Mst_Product_EditItem>
            {
                new() { UnitCode = "CAI", ProductName = "Sản phẩm trùng đơn vị" }
            }
        };
        var res = await _controller.Edit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var msg = doc.RootElement.GetProperty("Messages")[0].GetString();
        Assert.Contains("Mã đơn vị 'CAI' không được trùng!", msg);
    }

    [Fact]
    public async Task Edit_RootProduct_SuccessRoundTrip()
    {
        // Sửa sản phẩm gốc PRD_ROOT_01 và thêm thuộc tính mới nối vào tên
        var req = new Mst_Product_EditReq
        {
            Mst_Product = new Mst_Product_EditItem
            {
                OrgID = "1001",
                ProductCode = "PRD_ROOT_01",
                ProductCodeRoot = "PRD_ROOT_01",
                ProductLevelSys = "ROOTPRD",
                ProductCodeUser = "SP001_UPD",
                ProductName = "Sản phẩm mẫu 01 - Đã sửa",
                ProductNameEN = "Sample Product 01 - Updated",
                UPBuy = 12000.0,
                UPSell = 18000.0,
                Remark = "Ghi chú đã cập nhật",
                FlagActive = "1"
            },
            Lst_Prd_Attribute_New = new List<Prd_Attribute>
            {
                new() { AttributeCode = "COLOR", AttributeValue = "DO" }
            },
            FlagRedirect = "1"
        };

        var res = await _controller.Edit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("/api/mst_product/create", doc.RootElement.GetProperty("RedirectUrl").GetString());

        // Round-trip: Đọc lại từ database kiểm tra giá trị đã lưu
        var updated = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == "PRD_ROOT_01");
        Assert.NotNull(updated);
        Assert.Equal("SP001_UPD", updated.ProductCodeUser);
        Assert.Equal("Sản phẩm mẫu 01 - Đã sửa-DO", updated.ProductName);
        Assert.Equal(12000.0, updated.UPBuy);
        Assert.Equal(18000.0, updated.UPSell);
        Assert.Equal("Ghi chú đã cập nhật", updated.Remark);
    }

    [Fact]
    public async Task Edit_Level2Product_SuccessRoundTrip()
    {
        // Sửa sản phẩm cấp 2 PRD_L2_01_1 (L2PRD)
        var req = new Mst_Product_EditReq
        {
            Mst_Product = new Mst_Product_EditItem
            {
                OrgID = "1001",
                ProductCode = "PRD_L2_01_1",
                ProductLevelSys = "L2PRD",
                ProductCodeUser = "SP001_L2_UPD",
                ProductName = "Sản phẩm cấp 2 - Đã sửa",
                ProductBarCode = "BARCODE_L2_999",
                UPSell = 75000.0,
                Remark = "Ghi chú cấp 2"
            },
            FlagRedirect = "0"
        };

        var res = await _controller.Edit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("/api/mst_product/index", doc.RootElement.GetProperty("RedirectUrl").GetString());

        // Round-trip: Đọc lại từ database kiểm tra giá trị đã lưu
        var updated = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == "PRD_L2_01_1");
        Assert.NotNull(updated);
        Assert.Equal("SP001_L2_UPD", updated.ProductCodeUser);
        Assert.Equal("Sản phẩm cấp 2 - Đã sửa", updated.ProductName);
        Assert.Equal("BARCODE_L2_999", updated.ProductBarCode);
        Assert.Equal(75000.0, updated.UPSell);
    }

    [Fact]
    public async Task Edit_RootProduct_AddNewUnitProduct_SuccessRoundTrip()
    {
        // Thêm sản phẩm đơn vị mới qua Lst_Mst_ProductUI_Create
        var newUnitPrdCode = "PRD_BASE_01_NEW_UNIT";
        var req = new Mst_Product_EditReq
        {
            Mst_Product = new Mst_Product_EditItem
            {
                OrgID = "1001",
                ProductCode = "PRD_ROOT_01",
                ProductCodeRoot = "PRD_ROOT_01",
                ProductLevelSys = "ROOTPRD"
            },
            Lst_Unit = new List<Mst_Unit_EditItem>
            {
                new() { UnitCode = "GOI", ProductCode = newUnitPrdCode }
            },
            Lst_Mst_ProductUI_Create = new List<Mst_Product_EditItem>
            {
                new()
                {
                    OrgID = "1001",
                    ProductCode = newUnitPrdCode,
                    ProductLevelSys = "BASEPRD",
                    ProductCodeUser = "SP001_GOI",
                    ProductName = "Sản phẩm mẫu 01 - Gói",
                    UnitCode = "GOI",
                    ValConvert = 5.0,
                    UPBuy = 45000.0,
                    UPSell = 65000.0
                }
            }
        };

        var res = await _controller.Edit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        // Round-trip: Kiểm tra sản phẩm mới đã được lưu vào database
        var created = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == newUnitPrdCode);
        Assert.NotNull(created);
        Assert.Equal("GOI", created.UnitCode);
        Assert.Equal(5.0, created.ValConvert);
        Assert.Equal(45000.0, created.UPBuy);
        Assert.Equal(65000.0, created.UPSell);
        Assert.Equal("PRD_ROOT_01", created.ProductCodeRoot);
    }

    [Fact]
    public async Task DeleteUnit_WithEmptyBody_ReturnsEmptyCodeMessage()
    {
        var res = await _controller.DeleteUnit(null);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var msgs = doc.RootElement.GetProperty("Messages");
        Assert.Equal("Mã sản phẩm trống!", msgs[0].GetString());
    }

    [Fact]
    public async Task DeleteUnit_WithEmptyCodeString_ReturnsEmptyCodeMessage()
    {
        var req = new Mst_Product_DeleteUnitReq { prdCode = "   " };
        var res = await _controller.DeleteUnit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var msgs = doc.RootElement.GetProperty("Messages");
        Assert.Equal("Mã sản phẩm trống!", msgs[0].GetString());
    }

    [Fact]
    public async Task DeleteUnit_WithNonExistentProduct_ReturnsNotFoundMessage()
    {
        var req = new Mst_Product_DeleteUnitReq { prdCode = "PRD_NON_EXISTENT_999", OrgID = "1001" };
        var res = await _controller.DeleteUnit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var msgs = doc.RootElement.GetProperty("Messages");
        Assert.Equal("Mã sản phẩm 'PRD_NON_EXISTENT_999' không có trong hệ thống!", msgs[0].GetString());
    }

    [Fact]
    public async Task DeleteUnit_WithExistingProduct_DeletesProductAndReturnsSuccess_RoundTrip()
    {
        var unitCode = "PRD_UNIT_TEMP_DEL_01";
        var newUnit = new Mst_Product
        {
            OrgID = "1001",
            ProductCode = unitCode,
            NetworkID = "1001",
            ProductLevelSys = "L2PRD",
            ProductCodeUser = "SP001_DEL_TEMP",
            ProductName = "Sản phẩm tạm để xóa",
            ProductCodeRoot = "PRD_ROOT_01",
            ProductCodeBase = "PRD_ROOT_01",
            UnitCode = "THUNG",
            FlagActive = "1"
        };
        _db.Set<Mst_Product>().Add(newUnit);
        await _db.SaveChangesAsync();

        // Kiểm tra sản phẩm đã tồn tại trong DB trước khi xóa
        var before = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == unitCode);
        Assert.NotNull(before);

        // Gọi action DeleteUnit
        var req = new Mst_Product_DeleteUnitReq { prdCode = unitCode, OrgID = "1001" };
        var res = await _controller.DeleteUnit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var msgs = doc.RootElement.GetProperty("Messages");
        Assert.Equal("Xóa đơn vị thành công!", msgs[0].GetString());
        Assert.Equal("/api/mst_product/index", doc.RootElement.GetProperty("RedirectUrl").GetString());

        // Round-trip: Kiểm tra sản phẩm đã bị xóa khỏi DB
        var after = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == unitCode);
        Assert.Null(after);

        // Gọi lại DeleteUnit với cùng mã sản phẩm -> thông báo không có trong hệ thống
        var resAgain = await _controller.DeleteUnit(req);
        var okAgain = Assert.IsType<OkObjectResult>(resAgain);
        var docAgain = JsonDocument.Parse(JsonSerializer.Serialize(okAgain.Value));
        Assert.True(docAgain.RootElement.GetProperty("Success").GetBoolean());
        var msgsAgain = docAgain.RootElement.GetProperty("Messages");
        Assert.Equal($"Mã sản phẩm '{unitCode}' không có trong hệ thống!", msgsAgain[0].GetString());
    }

    [Fact]
    public async Task DeleteUnit_WithProductCodeAlias_DeletesProduct()
    {
        var unitCode = "PRD_UNIT_TEMP_DEL_02";
        var newUnit = new Mst_Product
        {
            OrgID = "1001",
            ProductCode = unitCode,
            NetworkID = "1001",
            ProductLevelSys = "L2PRD",
            ProductCodeUser = "SP001_DEL_ALIAS",
            ProductName = "Sản phẩm xóa bằng alias ProductCode",
            ProductCodeRoot = "PRD_ROOT_01",
            ProductCodeBase = "PRD_ROOT_01",
            UnitCode = "HOP",
            FlagActive = "1"
        };
        _db.Set<Mst_Product>().Add(newUnit);
        await _db.SaveChangesAsync();

        // Dùng ProductCode thay vì prdCode
        var req = new Mst_Product_DeleteUnitReq { ProductCode = unitCode, OrgID = "1001" };
        var res = await _controller.DeleteUnit(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var msgs = doc.RootElement.GetProperty("Messages");
        Assert.Equal("Xóa đơn vị thành công!", msgs[0].GetString());

        var after = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.OrgID == "1001" && x.ProductCode == unitCode);
        Assert.Null(after);
    }

    [Fact]
    public void ExportTemplate_WithNullReq_ReturnsExcelSuccessWith33Columns()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        var result = controller.ExportTemplate(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);
        Assert.EndsWith(".xlsx", strUrl);

        var physicalPath = Path.Combine(testEnv.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Product");
        Assert.NotNull(ws);

        // Tiêu đề: đúng 33 cột từ GetImportDicColumsTemplate() (Mst_ProductController.cs:4818-4859)
        Assert.Equal(33, ws.Row(1).LastCellUsed()?.Address.ColumnNumber);
        Assert.Equal("Mã hàng(*)", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên hàng(*)", ws.Cell(1, 2).GetString());
        Assert.Equal("Mã vạch", ws.Cell(1, 3).GetString());
        Assert.Equal("Mã hàng liên quan", ws.Cell(1, 4).GetString());
        Assert.Equal("Mã hàng gốc", ws.Cell(1, 5).GetString());
        Assert.Equal("Mã nhãn hiệu", ws.Cell(1, 6).GetString());
        Assert.Equal("Nhóm hàng (*)", ws.Cell(1, 7).GetString());
        Assert.Equal("Loại hàng", ws.Cell(1, 8).GetString());
        Assert.Equal("Ảnh sản phẩm", ws.Cell(1, 9).GetString());
        Assert.Equal("File đính kèm", ws.Cell(1, 10).GetString());
        Assert.Equal("QL Serial", ws.Cell(1, 11).GetString());
        Assert.Equal("QL LOT", ws.Cell(1, 12).GetString());
        Assert.Equal("Đơn vị", ws.Cell(1, 13).GetString());
        Assert.Equal("Quy đổi", ws.Cell(1, 14).GetString());
        Assert.Equal("Được bán", ws.Cell(1, 15).GetString());
        Assert.Equal("Được mua", ws.Cell(1, 16).GetString());
        Assert.Equal("Giá mua", ws.Cell(1, 17).GetString());
        Assert.Equal("Giá bán", ws.Cell(1, 18).GetString());
        Assert.Equal("Tồn nhỏ nhất", ws.Cell(1, 19).GetString());
        Assert.Equal("Tồn tối ưu", ws.Cell(1, 20).GetString());
        Assert.Equal("Tồn lớn nhất", ws.Cell(1, 21).GetString());
        Assert.Equal("Thuộc tính", ws.Cell(1, 22).GetString());
        Assert.Equal("Hàng thành phần", ws.Cell(1, 23).GetString());
        Assert.Equal("List các trường động", ws.Cell(1, 24).GetString());
        Assert.Equal("Tiêu chuẩn", ws.Cell(1, 25).GetString());
        Assert.Equal("Hạn sử dụng", ws.Cell(1, 26).GetString());
        Assert.Equal("Quy cách", ws.Cell(1, 27).GetString());
        Assert.Equal("Link quy trình sx", ws.Cell(1, 28).GetString());
        Assert.Equal("Chi tiết sản phẩm", ws.Cell(1, 29).GetString());
        Assert.Equal("Hướng dẫn sử dụng", ws.Cell(1, 30).GetString());
        Assert.Equal("Bản vẽ", ws.Cell(1, 31).GetString());
        Assert.Equal("Xuất xứ", ws.Cell(1, 32).GetString());
        Assert.Equal("Ghi chú", ws.Cell(1, 33).GetString());

        // File mẫu rỗng: không có dòng dữ liệu (dòng cuối cùng sử dụng là dòng 1 tiêu đề)
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(1, lastRow);

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public void ExportTemplate_WithEmptyBody_AlsoSucceeds()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        var result = controller.ExportTemplate(new Mst_Product_ExportTemplateReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public void ExportTemplate_WhenExceptionOccurs_ReturnsFailResult()
    {
        var brokenEnv = new TestWebHostEnvironment { WebRootPath = "Z:\\non_existent_drive_for_test\\" };
        var brokenController = new Mst_ProductControllerPort(_db, null, brokenEnv);

        var result = brokenController.ExportTemplate(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_ExportTemplate", root.GetProperty("ErrorCode").GetString());
    }

    #region Import Unit Tests

    private static readonly string[] ProductImportHeaders = new[]
    {
        "Mã hàng(*)", "Tên hàng(*)", "Mã vạch", "Mã hàng liên quan", "Mã hàng gốc",
        "Mã nhãn hiệu", "Nhóm hàng (*)", "Loại hàng", "Ảnh sản phẩm", "File đính kèm",
        "QL Serial", "QL LOT", "Đơn vị", "Quy đổi", "Được bán", "Được mua",
        "Giá mua", "Giá bán", "Tồn nhỏ nhất", "Tồn tối ưu", "Tồn lớn nhất",
        "Thuộc tính", "Hàng thành phần", "List các trường động", "Tiêu chuẩn",
        "Hạn sử dụng", "Quy cách", "Link quy trình sx", "Chi tiết sản phẩm",
        "Hướng dẫn sử dụng", "Bản vẽ", "Xuất xứ", "Ghi chú"
    };

    private static string?[] CreateProductRow(
        string code = "PRD_IMP_01",
        string name = "Hàng nhập khẩu 01",
        string? barCode = null,
        string? codeBase = null,
        string? codeRoot = null,
        string? brandCode = "BRAND01",
        string grpCode = "GRP01",
        string type = "PRODUCT",
        string? imagePath = null,
        string? filePath = null,
        string flagSerial = "0",
        string flagLot = "0",
        string? unitCode = "CAI",
        string? valConvert = "1",
        string flagSell = "1",
        string flagBuy = "1",
        string? upBuy = "10000",
        string? upSell = "15000",
        string? qtyMin = "5",
        string? qtyEff = "10",
        string? qtyMax = "50",
        string? attribute = null,
        string? bom = null,
        string? dynamicField = null,
        string? std = null,
        string? expiry = null,
        string? quyCach = null,
        string? mnfUrl = null,
        string? intro = null,
        string? userGuide = null,
        string? drawing = null,
        string? origin = "VN",
        string? remark = "Ghi chú test"
    )
    {
        return new string?[]
        {
            code, name, barCode, codeBase, codeRoot, brandCode, grpCode, type,
            imagePath, filePath, flagSerial, flagLot, unitCode, valConvert,
            flagSell, flagBuy, upBuy, upSell, qtyMin, qtyEff, qtyMax,
            attribute, bom, dynamicField, std, expiry, quyCach, mnfUrl,
            intro, userGuide, drawing, origin, remark
        };
    }

    private static byte[] CreateProductExcelBytes(string[] headers, List<string?[]> dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mst_Product");
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

    private static IFormFile CreateProductFormFile(byte[] bytes, string fileName = "test.xlsx")
    {
        var ms = new MemoryStream(bytes);
        return new FormFile(ms, 0, ms.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private static Mst_ProductControllerPort CreateControllerWithContext(AppDbContext db, string? flagimport = null, string? orgId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(flagimport))
        {
            httpContext.Request.QueryString = new QueryString($"?flagimport={flagimport}");
        }
        if (!string.IsNullOrEmpty(orgId))
        {
            httpContext.Request.Headers["OrgID"] = orgId;
        }
        return new Mst_ProductControllerPort(db)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task Import_WhenFileIsNull_ReturnsInvalidFileFail()
    {
        var result = await _controller.Import(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("File excel import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenFileLengthZero_ReturnsInvalidFileFail()
    {
        var file = CreateProductFormFile(Array.Empty<byte>(), "empty.xlsx");
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("File excel import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenFileExtensionNotExcel_ReturnsInvalidFileFail()
    {
        var bytes = Encoding.UTF8.GetBytes("not an excel file");
        var file = CreateProductFormFile(bytes, "data.txt");
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("File excel import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenColumnCountMismatch_ReturnsInvalidFileFail()
    {
        var bytes = CreateProductExcelBytes(new[] { "Col1", "Col2" }, new List<string?[]>());
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("File excel import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenNoDataRows_ReturnsNoDataFail()
    {
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]>());
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("File excel import không có dữ liệu!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenProductCodeUserEmpty_ReturnsMessageFail()
    {
        var row = CreateProductRow(code: "");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã hàng không được trống! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenProductNameEmpty_ReturnsMessageFail()
    {
        var row = CreateProductRow(name: "");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Tên hàng không được trống! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenProductGrpCodeEmpty_ReturnsMessageFail()
    {
        var row = CreateProductRow(grpCode: "");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Nhóm hàng không được trống! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenProductGrpCodeNotInDb_ReturnsMessageFail()
    {
        var row = CreateProductRow(grpCode: "GRP_UNKNOWN");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Nhóm hàng GRP_UNKNOWN không có trong hệ thống! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenProductTypeInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(type: "INVALID_TYPE");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Loại hàng INVALID_TYPE không có trong hệ thống! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenValConvertInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(code: "SP_L2_VAL", codeBase: "SP_BASE", codeRoot: "SP_ROOT", valConvert: "not_a_number");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Giá trị quy đổi không đúng định dạng! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenUPBuyInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(upBuy: "not_a_price");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Giá mua không đúng định dạng! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenUPSellInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(upSell: "not_a_price");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Giá bán không đúng định dạng! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenQtyMaxStInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(qtyMax: "not_a_qty");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Tồn lớn nhất không đúng định dạng! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenQtyEffStInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(qtyEff: "not_a_qty");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Tồn tối ưu không đúng định dạng! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenQtyMinStInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(qtyMin: "not_a_qty");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Tồn nhỏ nhất không đúng định dạng! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenDynamicFieldInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(code: "SP_DYN", dynamicField: "INVALID_FIELD_WITHOUT_COLON");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Trường động mã hàng'SP_DYN' không hợp lệ - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenAttributeInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(code: "SP_ATTR", attribute: "INVALID_ATTR_NO_VAL");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Thuộc tính của mã hàng 'SP_ATTR' không hợp lệ - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenBOMFormatInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(code: "SP_BOM_FMT", bom: "INVALID_BOM_FORMAT");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Hàng thành phần của mã hàng 'SP_BOM_FMT' không hợp lệ - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenFlagSerialInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(flagSerial: "9");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("QL Serial nhập '0' hoặc '1'! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenFlagLotInvalid_ReturnsMessageFail()
    {
        var row = CreateProductRow(flagLot: "9");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("QL LOT nhập '0' hoặc '1'! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenBothFlagSerialAndLotAre1_ReturnsMessageFail()
    {
        var row = CreateProductRow(flagSerial: "1", flagLot: "1");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("QL LOT và QL Serial chỉ chọn 1 trong 2! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenDuplicateRootInFile_ReturnsMessageFail()
    {
        var row1 = CreateProductRow(code: "SP_ROOT_DUP");
        var row2 = CreateProductRow(code: "SP_ROOT_DUP");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row1, row2 });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã hàng gốc 'SP_ROOT_DUP' bị lặp trong file import! - Dòng 4", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenDuplicateBaseInFile_ReturnsMessageFail()
    {
        var row1 = CreateProductRow(code: "SP_BASE_DUP", codeRoot: "SP_ROOT_01", codeBase: "SP_BASE_DUP");
        var row2 = CreateProductRow(code: "SP_BASE_DUP", codeRoot: "SP_ROOT_01", codeBase: "SP_BASE_DUP");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row1, row2 });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã hàng gốc 'SP_ROOT_01' - mã hàng liên quan 'SP_BASE_DUP' bị lặp trong file import! - Dòng 4", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenDuplicateL2InFile_ReturnsMessageFail()
    {
        var row1 = CreateProductRow(code: "SP_L2_DUP", codeRoot: "SP_ROOT_01", codeBase: "SP_BASE_01");
        var row2 = CreateProductRow(code: "SP_L2_DUP", codeRoot: "SP_ROOT_01", codeBase: "SP_BASE_01");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row1, row2 });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã hàng gốc 'SP_ROOT_01' - mã hàng liên quan 'SP_BASE_01' - mã hàng 'SP_L2_DUP' bị lặp trong file import! - Dòng 4", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_WhenMissingBOMComponent_ReturnsMessageFail()
    {
        var row = CreateProductRow(code: "SP_BOM_TEST", bom: "NON_EXISTENT_BOM:2");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Mã hàng thành phần 'NON_EXISTENT_BOM' không có trên hệ thống! - Dòng 3", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_RoundTrip_CreateSuccess()
    {
        var controller = CreateControllerWithContext(_db, flagimport: "1");
        var row1 = CreateProductRow(code: "SP_NEW_ROOT_01", name: "Sản phẩm mới nhập 01", upBuy: "12000", upSell: "18000");
        var row2 = CreateProductRow(code: "SP_NEW_L2_01", name: "Sản phẩm cấp 2 nhập 01", codeRoot: "SP_NEW_ROOT_01", codeBase: "SP_NEW_ROOT_01", valConvert: "2");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row1, row2 });
        var file = CreateProductFormFile(bytes);

        var result = await controller.Import(file);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã nhập dữ liệu excel thành công!", doc.RootElement.GetProperty("Title").GetString());
        Assert.Equal("/Mst_Product", doc.RootElement.GetProperty("RedirectUrl").GetString());

        var dbRoot = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.ProductCodeUser == "SP_NEW_ROOT_01");
        Assert.NotNull(dbRoot);
        Assert.Equal("Sản phẩm mới nhập 01", dbRoot.ProductName);
        Assert.Equal(12000.0, dbRoot.UPBuy);
        Assert.Equal(18000.0, dbRoot.UPSell);

        var dbL2 = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.ProductCodeUser == "SP_NEW_L2_01");
        Assert.NotNull(dbL2);
        Assert.Equal("Sản phẩm cấp 2 nhập 01", dbL2.ProductName);
        Assert.Equal(2.0, dbL2.ValConvert);
    }

    [Fact]
    public async Task Import_RoundTrip_UpdateSuccess()
    {
        var controller = CreateControllerWithContext(_db, flagimport: "0");
        var row = CreateProductRow(code: "SP001", name: "Sản phẩm mẫu 01 đã cập nhật", upBuy: "22000", upSell: "33000");
        var bytes = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { row });
        var file = CreateProductFormFile(bytes);

        var result = await controller.Import(file);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã nhập dữ liệu excel thành công!", doc.RootElement.GetProperty("Title").GetString());

        var updated = await _db.Set<Mst_Product>().FirstOrDefaultAsync(x => x.ProductCodeUser == "SP001");
        Assert.NotNull(updated);
        Assert.Equal("Sản phẩm mẫu 01 đã cập nhật", updated.ProductName);
        Assert.Equal(22000.0, updated.UPBuy);
        Assert.Equal(33000.0, updated.UPSell);
    }

    #endregion

    #region Export Tests

    [Fact]
    public async Task Export_WithNullReq_ReturnsExcelSuccessWith6ActiveProducts()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        var result = await controller.Export(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);
        Assert.EndsWith(".xlsx", strUrl);

        var physicalPath = Path.Combine(testEnv.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Product");
        Assert.NotNull(ws);

        // 33 cột: Mã hàng liên quan bị xóa, Trạng thái được thêm vào
        Assert.Equal(33, ws.Row(1).LastCellUsed()?.Address.ColumnNumber);
        Assert.Equal("Trạng thái", ws.Cell(1, 33).GetString());

        // Mặc định không chọn checkbox: chỉ xuất FlagActive = "1" -> 6 sản phẩm active (tổng dòng = 1 header + 6 data = 7)
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(7, lastRow);

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public async Task Export_WithEmptyReq_AlsoSucceeds()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        var result = await controller.Export(new Mst_Product_ExportReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public async Task Export_WithFilterOrgId1001_Returns5ActiveProducts()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        // OrgID = 1001, mặc định FlagActive = 1 -> PRD_ROOT_01, PRD_BASE_01_1, PRD_BASE_01_2, PRD_L2_01_1, PRD_ROOT_02 (5 dòng)
        var result = await controller.Export(new Mst_Product_ExportReq { OrgID = "1001" });
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(testEnv.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Product");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(6, lastRow); // 1 header + 5 data rows

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public async Task Export_WithFilterCkbCombo_ReturnsComboProducts()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        // CkbCombo = "1" -> ProductType = "COMBO", defaultActiveOnly = false. PRD_ROOT_03 có FlagActive = "0" nhưng khớp COMBO -> 1 dòng
        var result = await controller.Export(new Mst_Product_ExportReq { OrgID = "1001", CkbCombo = "1" });
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(testEnv.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Product");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(2, lastRow); // 1 header + 1 combo product
        Assert.Equal("Combo", ws.Cell(2, 7).GetString()); // Cột Loại hàng (ProductType) được map thành "Combo"

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public async Task Export_WithFilterCkbServices_ReturnsServiceProducts()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        // CkbServices = "1" -> ProductType in ("SERVICE", "services") -> PRD_ROOT_02 (1 dòng)
        var result = await controller.Export(new Mst_Product_ExportReq { OrgID = "1001", CkbServices = "1" });
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Success").GetBoolean());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        var physicalPath = Path.Combine(testEnv.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Product");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(2, lastRow); // 1 header + 1 service product
        Assert.Equal("Dịch vụ", ws.Cell(2, 7).GetString()); // Cột Loại hàng (ProductType) được map thành "Dịch vụ"

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public async Task Export_TwoDifferentFilters_ReturnDifferentRowCounts()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        // Bộ lọc 1: OrgID = 1001 -> 5 dòng
        var res1 = await controller.Export(new Mst_Product_ExportReq { OrgID = "1001" });
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(res1).Value));
        var path1 = Path.Combine(testEnv.WebRootPath, doc1.RootElement.GetProperty("strUrl").GetString()!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb1 = new XLWorkbook(path1);
        var count1 = wb1.Worksheet("Mst_Product").LastRowUsed()?.RowNumber();

        // Bộ lọc 2: OrgID = 1001 + CkbProduct = "1" -> 4 dòng (PRD_ROOT_01, PRD_BASE_01_1, PRD_BASE_01_2, PRD_L2_01_1)
        var res2 = await controller.Export(new Mst_Product_ExportReq { OrgID = "1001", CkbProduct = "1" });
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(res2).Value));
        var path2 = Path.Combine(testEnv.WebRootPath, doc2.RootElement.GetProperty("strUrl").GetString()!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb2 = new XLWorkbook(path2);
        var count2 = wb2.Worksheet("Mst_Product").LastRowUsed()?.RowNumber();

        Assert.Equal(6, count1); // 1 header + 5 data
        Assert.Equal(5, count2); // 1 header + 4 data
        Assert.NotEqual(count1, count2);

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public async Task Export_WithInvalidLstAttribute_ReturnsFailResult()
    {
        var testEnv = new TestWebHostEnvironment();
        Directory.CreateDirectory(testEnv.WebRootPath);
        var controller = new Mst_ProductControllerPort(_db, null, testEnv);

        var result = await controller.Export(new Mst_Product_ExportReq { LstAttribute = "INVALID_JSON_{" });
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_Export", root.GetProperty("ErrorCode").GetString());
        Assert.Equal("Tham số lstattribute không hợp lệ!", root.GetProperty("Params").GetProperty("Exception").GetString());

        try { Directory.Delete(testEnv.WebRootPath, true); } catch { }
    }

    [Fact]
    public async Task Export_WhenExceptionOccurs_ReturnsFailResult()
    {
        var brokenEnv = new TestWebHostEnvironment { WebRootPath = "Z:\\non_existent_drive_for_test\\" };
        var brokenController = new Mst_ProductControllerPort(_db, null, brokenEnv);

        var result = await brokenController.Export(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_Export", root.GetProperty("ErrorCode").GetString());
    }

    #endregion

    #region ShowPopupImportExcel Tests

    [Fact]
    public void ShowPopupImportExcel_WithNullReq_ReturnsSuccessTrueAndHtml()
    {
        // Khi req null (body rỗng): trả về Success = true, Html chứa modal nhập Excel (Gate Mst_ProductController.cs:4801-4806)
        var result = _controller.ShowPopupImportExcel(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var html = root.GetProperty("Html").GetString();
        Assert.NotNull(html);
        Assert.Contains("Nhập hàng hóa từ file Excel", html);
        Assert.Contains("manageFormImportExcel", html);
    }

    [Fact]
    public void ShowPopupImportExcel_WithEmptyReq_ReturnsSuccessTrueAndHtml()
    {
        // Khi req rỗng (new Mst_Product_ShowPopupImportExcelReq()): trả về Success = true, Html hợp lệ
        var req = new Mst_Product_ShowPopupImportExcelReq();
        var result = _controller.ShowPopupImportExcel(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var html = root.GetProperty("Html").GetString();
        Assert.NotNull(html);
        Assert.Contains("Nhập hàng hóa từ file Excel", html);
    }

    [Fact]
    public void ShowPopupImportExcel_HtmlContainsExpectedModalElements()
    {
        // Kiểm tra chi tiết các thành phần giao diện của modal nhập Excel theo Views/Mst_Product/_ImportExcel.cshtml
        var result = _controller.ShowPopupImportExcel(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var html = doc.RootElement.GetProperty("Html").GetString()!;

        // 1. Tiêu đề modal
        Assert.Contains("Nhập hàng hóa từ file Excel", html);
        // 2. ID Form quản lý import
        Assert.Contains("id=\"manageFormImportExcel\"", html);
        // 3. Radio xử lý trùng mã hàng: 1 = Báo lỗi và dừng import
        Assert.Contains("id=\"importErrorAndStop\"", html);
        Assert.Contains("value=\"1\"", html);
        Assert.Contains("Báo lỗi và dừng import", html);
        // 4. Radio xử lý trùng mã hàng: 0 = Cập nhật thông tin mới
        Assert.Contains("id=\"importReplaceProduct\"", html);
        Assert.Contains("value=\"0\"", html);
        Assert.Contains("Cập nhật thông tin mới cho hàng hóa", html);
        // 5. Thẻ input file
        Assert.Contains("type=\"file\"", html);
        Assert.Contains("name=\"file\"", html);
        Assert.Contains("id=\"file\"", html);
        // 6. Nút bấm submit Import
        Assert.Contains("id=\"btnSaveImport\"", html);
        Assert.Contains("Nhập</a>", html);
    }

    #endregion

    #region StopProducts Tests

    [Fact]
    public async Task StopProducts_WithNullReq_ReturnsSuccessTrue()
    {
        // Body rỗng hoàn toàn (null): trả về Success = true, Messages = ["Ngừng kinh doanh hàng hóa thành công!"] (Gate dòng 5578-5623)
        var result = await _controller.StopProducts(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Ngừng kinh doanh hàng hóa thành công!", root.GetProperty("Messages")[0].GetString());
        Assert.Equal("/Mst_Product/Index", root.GetProperty("RedirectUrl").GetString());
    }

    [Fact]
    public async Task StopProducts_WithEmptyReq_ReturnsSuccessTrue()
    {
        var req = new Mst_Product_StopProductsReq();
        var result = await _controller.StopProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Ngừng kinh doanh hàng hóa thành công!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task StopProducts_WithEmptyList_ReturnsSuccessTrue()
    {
        var req = new Mst_Product_StopProductsReq { listproduct = "[]", OrgID = "1001" };
        var result = await _controller.StopProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Ngừng kinh doanh hàng hóa thành công!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task StopProducts_WithInvalidJson_ReturnsFailResult()
    {
        // Khi listproduct là JSON lỗi -> ném JsonException -> catch (gate dòng 5625-5628) -> PortResult.Fail
        var req = new Mst_Product_StopProductsReq { listproduct = "{invalid_json_format" };
        var result = await _controller.StopProducts(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_StopProducts", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task StopProducts_SingleProduct_UpdatesFlagActiveToZero()
    {
        // Round-trip check: PRD_BASE_01_1 ban đầu có FlagActive = "1"
        var before = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_BASE_01_1");
        Assert.NotNull(before);
        Assert.Equal("1", before.FlagActive);

        // Gọi StopProducts cho PRD_BASE_01_1
        var req = new Mst_Product_StopProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_BASE_01_1\"}]",
            OrgID = "1001"
        };
        var result = await _controller.StopProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Ngừng kinh doanh hàng hóa thành công!", doc.RootElement.GetProperty("Messages")[0].GetString());

        // Kiểm tra sau khi cập nhật: FlagActive = "0"
        var after = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_BASE_01_1");
        Assert.NotNull(after);
        Assert.Equal("0", after.FlagActive);
    }

    [Fact]
    public async Task StopProducts_MultipleProducts_UpdatesAllFlagActiveToZero()
    {
        // Cập nhật nhiều sản phẩm cùng lúc
        var req = new Mst_Product_StopProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_BASE_01_2\"},{\"ProductCode\":\"PRD_L2_01_1\"}]",
            OrgID = "1001"
        };
        var result = await _controller.StopProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var p1 = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_BASE_01_2");
        var p2 = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_L2_01_1");
        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.Equal("0", p1.FlagActive);
        Assert.Equal("0", p2.FlagActive);
    }

    [Fact]
    public async Task StopProducts_NonExistentProduct_ReturnsSuccessTrue()
    {
        // Khi mã hàng không tồn tại: gate vẫn trả về Success = true, 0 dòng bị cập nhật (dòng 5578-5623)
        var req = new Mst_Product_StopProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_KHONG_TON_TAI_999\"}]",
            OrgID = "1001"
        };
        var result = await _controller.StopProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Ngừng kinh doanh hàng hóa thành công!", doc.RootElement.GetProperty("Messages")[0].GetString());
    }

    #endregion

    #region ActiveProducts Tests

    [Fact]
    public async Task ActiveProducts_WithNullReq_ReturnsSuccessTrue()
    {
        // Body rỗng hoàn toàn (null): trả về Success = true, Messages = ["Kinh doanh hàng hóa thành công!"] (Gate dòng 5677-5722)
        var result = await _controller.ActiveProducts(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Kinh doanh hàng hóa thành công!", root.GetProperty("Messages")[0].GetString());
        Assert.Equal("/Mst_Product/Index", root.GetProperty("RedirectUrl").GetString());
    }

    [Fact]
    public async Task ActiveProducts_WithEmptyReq_ReturnsSuccessTrue()
    {
        var req = new Mst_Product_ActiveProductsReq();
        var result = await _controller.ActiveProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Kinh doanh hàng hóa thành công!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task ActiveProducts_WithEmptyList_ReturnsSuccessTrue()
    {
        var req = new Mst_Product_ActiveProductsReq { listproduct = "[]", OrgID = "1001" };
        var result = await _controller.ActiveProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Kinh doanh hàng hóa thành công!", root.GetProperty("Messages")[0].GetString());
    }

    [Fact]
    public async Task ActiveProducts_WithInvalidJson_ReturnsFailResult()
    {
        // Khi listproduct là JSON lỗi -> ném JsonException -> catch (gate dòng 5724-5727) -> PortResult.Fail
        var req = new Mst_Product_ActiveProductsReq { listproduct = "{invalid_json_format" };
        var result = await _controller.ActiveProducts(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Product_ActiveProducts", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task ActiveProducts_SingleProduct_UpdatesFlagActiveToOne()
    {
        // Đặt trước FlagActive = "0" cho sản phẩm PRD_BASE_01_1 qua StopProducts
        await _controller.StopProducts(new Mst_Product_StopProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_BASE_01_1\"}]",
            OrgID = "1001"
        });

        var before = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_BASE_01_1");
        Assert.NotNull(before);
        Assert.Equal("0", before.FlagActive);

        // Gọi ActiveProducts cho PRD_BASE_01_1
        var req = new Mst_Product_ActiveProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_BASE_01_1\"}]",
            OrgID = "1001"
        };
        var result = await _controller.ActiveProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Kinh doanh hàng hóa thành công!", doc.RootElement.GetProperty("Messages")[0].GetString());

        // Kiểm tra sau khi cập nhật: FlagActive = "1"
        var after = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_BASE_01_1");
        Assert.NotNull(after);
        Assert.Equal("1", after.FlagActive);
    }

    [Fact]
    public async Task ActiveProducts_MultipleProducts_UpdatesAllFlagActiveToOne()
    {
        // Đặt trước FlagActive = "0" cho 2 sản phẩm
        await _controller.StopProducts(new Mst_Product_StopProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_BASE_01_2\"},{\"ProductCode\":\"PRD_L2_01_1\"}]",
            OrgID = "1001"
        });

        var b1 = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_BASE_01_2");
        var b2 = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_L2_01_1");
        Assert.NotNull(b1);
        Assert.NotNull(b2);
        Assert.Equal("0", b1.FlagActive);
        Assert.Equal("0", b2.FlagActive);

        // Kích hoạt kinh doanh lại cả 2 sản phẩm
        var req = new Mst_Product_ActiveProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_BASE_01_2\"},{\"ProductCode\":\"PRD_L2_01_1\"}]",
            OrgID = "1001"
        };
        var result = await _controller.ActiveProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var p1 = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_BASE_01_2");
        var p2 = await _db.Set<Mst_Product>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == "PRD_L2_01_1");
        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.Equal("1", p1.FlagActive);
        Assert.Equal("1", p2.FlagActive);
    }

    [Fact]
    public async Task ActiveProducts_NonExistentProduct_ReturnsSuccessTrue()
    {
        // Khi mã hàng không tồn tại: gate vẫn trả về Success = true, 0 dòng bị cập nhật (dòng 5677-5722)
        var req = new Mst_Product_ActiveProductsReq
        {
            listproduct = "[{\"ProductCode\":\"PRD_KHONG_TON_TAI_999\"}]",
            OrgID = "1001"
        };
        var result = await _controller.ActiveProducts(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Kinh doanh hàng hóa thành công!", doc.RootElement.GetProperty("Messages")[0].GetString());
    }

    #endregion

    [Fact]
    public async Task Index_ServerIntegrationTest_FreePort()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"minicontract_product_test_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bin", "Release", "net8.0", "MiniContract.dll"));
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

            // Ca 1: POST empty body -> 200 OK, Success = true, ItemCount = 4
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_product/index", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            Assert.True(res1.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res1.StatusCode}, body: {body1}");
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(4, data1.GetProperty("ItemCount").GetInt32());

            // Ca 2: POST bộ lọc OrgID = 1001 và FlagActive = 1 -> 200 OK, Success = true, ItemCount = 2
            var req2 = new StringContent("{\"OrgID\":\"1001\",\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_product/index", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(2, data2.GetProperty("ItemCount").GetInt32());

            // Ca 3: POST /api/mst_product/dosearch với body rỗng -> 200 OK, ItemCount = 4
            var req3 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_product/dosearch", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(4, doc3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // Ca 4: POST /api/mst_product/dosearch với CkbProduct = 1, OrgID = 1001 -> 200 OK, ItemCount = 1
            var req4 = new StringContent("{\"OrgID\":\"1001\",\"CkbProduct\":\"1\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_product/dosearch", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(1, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal("PRD_ROOT_01", data4.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());

            // Ca 5: POST /api/mst_product/dosearch với CkbServices = 1, OrgID = 1001 -> 200 OK, ItemCount = 1
            var req5 = new StringContent("{\"OrgID\":\"1001\",\"CkbServices\":\"1\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_product/dosearch", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal(1, data5.GetProperty("ItemCount").GetInt32());
            Assert.Equal("PRD_ROOT_02", data5.GetProperty("DataList")[0].GetProperty("ProductCode").GetString());

            // Ca 6: POST /api/mst_product/dosearch với 2 bộ lọc khác nhau cho ra số dòng khác nhau (OrgID 1001 vs OrgID 1001 + FlagActive 1)
            var req6 = new StringContent("{\"OrgID\":\"1001\",\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_product/dosearch", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(2, data6.GetProperty("ItemCount").GetInt32());

            // Ca 7: POST /api/mst_product/loaddata với body rỗng -> 400 Bad Request, Success = false
            var req7 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_product/loaddata", req7);
            var body7 = await res7.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res7.StatusCode);
            var doc7 = JsonDocument.Parse(body7);
            Assert.False(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_Product_Get", doc7.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 8: POST /api/mst_product/loaddata với prcode = PRD_ROOT_01, OrgID = 1001 -> 200 OK, LstPrdBase = 2, LstPrdLeve2 = 1
            var req8 = new StringContent("{\"prcode\":\"PRD_ROOT_01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_product/loaddata", req8);
            var body8 = await res8.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var doc8 = JsonDocument.Parse(body8);
            Assert.True(doc8.RootElement.GetProperty("Success").GetBoolean());
            var data8 = doc8.RootElement.GetProperty("Data")[0];
            Assert.Equal(2, data8.GetProperty("LstPrdBase").GetArrayLength());
            Assert.Equal(1, data8.GetProperty("LstPrdLeve2").GetArrayLength());

            // Ca 9: POST /api/mst_product/loaddata với prcode = PRD_ROOT_02, OrgID = 1001 -> 200 OK, LstPrdBase = 0, LstPrdLeve2 = 0
            var req9 = new StringContent("{\"prcode\":\"PRD_ROOT_02\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/mst_product/loaddata", req9);
            var body9 = await res9.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res9.StatusCode);
            var doc9 = JsonDocument.Parse(body9);
            Assert.True(doc9.RootElement.GetProperty("Success").GetBoolean());
            var data9 = doc9.RootElement.GetProperty("Data")[0];
            Assert.Equal(0, data9.GetProperty("LstPrdBase").GetArrayLength());
            Assert.Equal(0, data9.GetProperty("LstPrdLeve2").GetArrayLength());

            // Ca 10: POST /api/mst_product/search_product_auto với body rỗng -> 200 OK, ListProduct = []
            var req10 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res10 = await http.PostAsync("/api/mst_product/search_product_auto", req10);
            var body10 = await res10.Content.ReadAsStringAsync();
            Assert.True(res10.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res10.StatusCode}, body: {body10}");
            var doc10 = JsonDocument.Parse(body10);
            Assert.True(doc10.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, doc10.RootElement.GetProperty("ListProduct").GetArrayLength());

            // Ca 11: POST /api/mst_product/search_product_auto với productcodename = SP001, OrgID = 1001 -> 200 OK, ListProduct = 4
            var req11 = new StringContent("{\"productcodename\":\"SP001\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res11 = await http.PostAsync("/api/mst_product/search_product_auto", req11);
            var body11 = await res11.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res11.StatusCode);
            var doc11 = JsonDocument.Parse(body11);
            Assert.True(doc11.RootElement.GetProperty("Success").GetBoolean());
            var count11 = doc11.RootElement.GetProperty("ListProduct").GetArrayLength();
            Assert.Equal(4, count11);

            // Ca 12: POST /api/mst_product/search_product_auto với productcodename = SP002, OrgID = 1001 -> 200 OK, ListProduct = 1
            var req12 = new StringContent("{\"productcodename\":\"SP002\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res12 = await http.PostAsync("/api/mst_product/search_product_auto", req12);
            var body12 = await res12.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res12.StatusCode);
            var doc12 = JsonDocument.Parse(body12);
            Assert.True(doc12.RootElement.GetProperty("Success").GetBoolean());
            var count12 = doc12.RootElement.GetProperty("ListProduct").GetArrayLength();
            Assert.Equal(1, count12);

            // 2 bộ lọc cho số dòng khác nhau (4 != 1)
            Assert.NotEqual(count11, count12);

            // Ca 13: POST /api/mst_product/create với body rỗng -> 400 Bad Request, Success = false
            var req13 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res13 = await http.PostAsync("/api/mst_product/create", req13);
            var body13 = await res13.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res13.StatusCode);
            var doc13 = JsonDocument.Parse(body13);
            Assert.False(doc13.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_Product_Create_Input_Mst_ProductTblNotFound", doc13.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 14: POST /api/mst_product/create với IsInit = true -> 200 OK, Success = true
            var req14 = new StringContent("{\"IsInit\":true,\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res14 = await http.PostAsync("/api/mst_product/create", req14);
            var body14 = await res14.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res14.StatusCode);
            var doc14 = JsonDocument.Parse(body14);
            Assert.True(doc14.RootElement.GetProperty("Success").GetBoolean());
            Assert.True(doc14.RootElement.GetProperty("Data").TryGetProperty("List_Mst_ProductType", out _));

            // Ca 15: POST /api/mst_product/create tạo sản phẩm mới PRD_LIVE_NEW_01 -> 200 OK, Success = true
            var createJson = "{\"OrgID\":\"1001\",\"ProductCode\":\"PRD_LIVE_NEW_01\",\"ProductName\":\"Hang Hoa Live Test 01\",\"ProductCodeUser\":\"HH_LIVE_01\",\"ProductLevelSys\":\"ROOTPRD\",\"FlagActive\":\"1\"}";
            var req15 = new StringContent(createJson, Encoding.UTF8, "application/json");
            var res15 = await http.PostAsync("/api/mst_product/create", req15);
            var body15 = await res15.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res15.StatusCode);
            var doc15 = JsonDocument.Parse(body15);
            Assert.True(doc15.RootElement.GetProperty("Success").GetBoolean());

            // Ca 16: Round-trip đọc lại qua /api/mst_product/search_product_auto với "HH_LIVE_01" -> 200 OK, ListProduct có 1 phần tử
            var req16 = new StringContent("{\"productcodename\":\"HH_LIVE_01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res16 = await http.PostAsync("/api/mst_product/search_product_auto", req16);
            var body16 = await res16.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res16.StatusCode);
            var doc16 = JsonDocument.Parse(body16);
            Assert.True(doc16.RootElement.GetProperty("Success").GetBoolean());
            var listPrdLive = doc16.RootElement.GetProperty("ListProduct");
            Assert.Equal(1, listPrdLive.GetArrayLength());
            Assert.Equal("PRD_LIVE_NEW_01", listPrdLive[0].GetProperty("ProductCode").GetString());
            Assert.Equal("Hang Hoa Live Test 01", listPrdLive[0].GetProperty("ProductName").GetString());

            // Ca 17: POST /api/mst_product/getbom với body rỗng -> 200 OK, Success = true, Data = 8 sản phẩm
            var req17 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res17 = await http.PostAsync("/api/mst_product/getbom", req17);
            var body17 = await res17.Content.ReadAsStringAsync();
            Assert.True(res17.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res17.StatusCode}, body: {body17}");
            var doc17 = JsonDocument.Parse(body17);
            Assert.True(doc17.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(8, doc17.RootElement.GetProperty("Data").GetArrayLength());

            // Ca 18: POST /api/mst_product/getbom với OrgID = 1001 -> 200 OK, Data = 7 sản phẩm
            var req18 = new StringContent("{\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res18 = await http.PostAsync("/api/mst_product/getbom", req18);
            var body18 = await res18.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res18.StatusCode);
            var doc18 = JsonDocument.Parse(body18);
            Assert.True(doc18.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(7, doc18.RootElement.GetProperty("Data").GetArrayLength());

            // Ca 19: POST /api/mst_product/getbom với prdid = SP001, OrgID = 1001 -> 200 OK, Data = 1 (PRD_ROOT_01)
            var req19 = new StringContent("{\"prdid\":\"SP001\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res19 = await http.PostAsync("/api/mst_product/getbom", req19);
            var body19 = await res19.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res19.StatusCode);
            var doc19 = JsonDocument.Parse(body19);
            Assert.True(doc19.RootElement.GetProperty("Success").GetBoolean());
            var data19 = doc19.RootElement.GetProperty("Data");
            Assert.Equal(1, data19.GetArrayLength());
            Assert.Equal("PRD_ROOT_01", data19[0].GetProperty("ProductCode").GetString());

            // Ca 20: POST /api/mst_product/getbom với prdid = "Sản phẩm mẫu 01", OrgID = 1001 -> 200 OK, Data = 1 (PRD_ROOT_01)
            var req20 = new StringContent("{\"prdid\":\"Sản phẩm mẫu 01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res20 = await http.PostAsync("/api/mst_product/getbom", req20);
            var body20 = await res20.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res20.StatusCode);
            var doc20 = JsonDocument.Parse(body20);
            Assert.True(doc20.RootElement.GetProperty("Success").GetBoolean());
            var data20 = doc20.RootElement.GetProperty("Data");
            Assert.Equal(1, data20.GetArrayLength());
            Assert.Equal("PRD_ROOT_01", data20[0].GetProperty("ProductCode").GetString());

            // Ca 21: POST /api/mst_product/getbom với prdid không tồn tại -> 200 OK, Data = 0 phần tử
            var req21 = new StringContent("{\"prdid\":\"KHONG_TON_TAI_XYZ\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res21 = await http.PostAsync("/api/mst_product/getbom", req21);
            var body21 = await res21.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res21.StatusCode);
            var doc21 = JsonDocument.Parse(body21);
            Assert.True(doc21.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, doc21.RootElement.GetProperty("Data").GetArrayLength());

            // Ca 22: 2 bộ lọc cho ra số dòng khác nhau (OrgID 1001 có 7 dòng != prdid SP001 có 1 dòng)
            Assert.NotEqual(doc18.RootElement.GetProperty("Data").GetArrayLength(), data19.GetArrayLength());

            // Ca 23: POST /api/mst_product/searchproduct với body rỗng -> 200 OK, Success = true, Data = 8 sản phẩm
            var req23 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res23 = await http.PostAsync("/api/mst_product/searchproduct", req23);
            var body23 = await res23.Content.ReadAsStringAsync();
            Assert.True(res23.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res23.StatusCode}, body: {body23}");
            var doc23 = JsonDocument.Parse(body23);
            Assert.True(doc23.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(8, doc23.RootElement.GetProperty("Data").GetArrayLength());

            // Ca 24: POST /api/mst_product/searchproduct với OrgID = 1001 -> 200 OK, Data = 7 sản phẩm
            var req24 = new StringContent("{\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res24 = await http.PostAsync("/api/mst_product/searchproduct", req24);
            var body24 = await res24.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res24.StatusCode);
            var doc24 = JsonDocument.Parse(body24);
            Assert.True(doc24.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(7, doc24.RootElement.GetProperty("Data").GetArrayLength());

            // Ca 25: POST /api/mst_product/searchproduct với prdid = SP001, OrgID = 1001 -> 200 OK, Data = 4 sản phẩm (like %SP001%)
            var req25 = new StringContent("{\"prdid\":\"SP001\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res25 = await http.PostAsync("/api/mst_product/searchproduct", req25);
            var body25 = await res25.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res25.StatusCode);
            var doc25 = JsonDocument.Parse(body25);
            Assert.True(doc25.RootElement.GetProperty("Success").GetBoolean());
            var data25 = doc25.RootElement.GetProperty("Data");
            Assert.Equal(4, data25.GetArrayLength());

            // Ca 26: POST /api/mst_product/searchproduct với prdid = "bảo trì", OrgID = 1001 -> 200 OK, Data = 1 (PRD_ROOT_02)
            var req26 = new StringContent("{\"prdid\":\"bảo trì\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res26 = await http.PostAsync("/api/mst_product/searchproduct", req26);
            var body26 = await res26.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res26.StatusCode);
            var doc26 = JsonDocument.Parse(body26);
            Assert.True(doc26.RootElement.GetProperty("Success").GetBoolean());
            var data26 = doc26.RootElement.GetProperty("Data");
            Assert.Equal(1, data26.GetArrayLength());
            Assert.Equal("PRD_ROOT_02", data26[0].GetProperty("ProductCode").GetString());

            // Ca 27: POST /api/mst_product/searchproduct với prdid không tồn tại -> 200 OK, Data = 0 phần tử
            var req27 = new StringContent("{\"prdid\":\"KHONG_TON_TAI_XYZ\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res27 = await http.PostAsync("/api/mst_product/searchproduct", req27);
            var body27 = await res27.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res27.StatusCode);
            var doc27 = JsonDocument.Parse(body27);
            Assert.True(doc27.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, doc27.RootElement.GetProperty("Data").GetArrayLength());

            // Ca 28: 2 bộ lọc cho ra số dòng khác nhau (SP001 có 4 dòng != bảo trì có 1 dòng)
            Assert.NotEqual(data25.GetArrayLength(), data26.GetArrayLength());

            // Ca 29: POST /api/mst_product/getsearch với body rỗng -> 200 OK, Success = true, Lst_Mst_Product = 7 sản phẩm active
            var req29 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res29 = await http.PostAsync("/api/mst_product/getsearch", req29);
            var body29 = await res29.Content.ReadAsStringAsync();
            Assert.True(res29.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res29.StatusCode}, body: {body29}");
            var doc29 = JsonDocument.Parse(body29);
            Assert.True(doc29.RootElement.GetProperty("Success").GetBoolean());
            var list29 = doc29.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
            Assert.Equal(7, list29.GetArrayLength());

            // Ca 30: POST /api/mst_product/getsearch với OrgID = 1001 -> 200 OK, Lst_Mst_Product = 6 sản phẩm active thuộc Org 1001
            var req30 = new StringContent("{\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res30 = await http.PostAsync("/api/mst_product/getsearch", req30);
            var body30 = await res30.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res30.StatusCode);
            var doc30 = JsonDocument.Parse(body30);
            Assert.True(doc30.RootElement.GetProperty("Success").GetBoolean());
            var list30 = doc30.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
            Assert.Equal(6, list30.GetArrayLength());

            // Ca 31: POST /api/mst_product/getsearch với ckbproduct = 1, OrgID = 1001 -> 200 OK, Lst_Mst_Product = 5 sản phẩm PRODUCT
            var req31 = new StringContent("{\"OrgID\":\"1001\",\"ckbproduct\":\"1\"}", Encoding.UTF8, "application/json");
            var res31 = await http.PostAsync("/api/mst_product/getsearch", req31);
            var body31 = await res31.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res31.StatusCode);
            var doc31 = JsonDocument.Parse(body31);
            Assert.True(doc31.RootElement.GetProperty("Success").GetBoolean());
            var list31 = doc31.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
            Assert.Equal(5, list31.GetArrayLength());

            // Ca 32: POST /api/mst_product/getsearch với ckbservices = 1, OrgID = 1001 -> 200 OK, Lst_Mst_Product = 1 sản phẩm SERVICE (PRD_ROOT_02)
            var req32 = new StringContent("{\"OrgID\":\"1001\",\"ckbservices\":\"1\"}", Encoding.UTF8, "application/json");
            var res32 = await http.PostAsync("/api/mst_product/getsearch", req32);
            var body32 = await res32.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res32.StatusCode);
            var doc32 = JsonDocument.Parse(body32);
            Assert.True(doc32.RootElement.GetProperty("Success").GetBoolean());
            var list32 = doc32.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
            Assert.Equal(1, list32.GetArrayLength());
            Assert.Equal("PRD_ROOT_02", list32[0].GetProperty("ProductCode").GetString());

            // Ca 33: POST /api/mst_product/getsearch với ckbcombo = 1, OrgID = 1001 -> 200 OK, Lst_Mst_Product = 1 sản phẩm COMBO (PRD_ROOT_03)
            var req33 = new StringContent("{\"OrgID\":\"1001\",\"ckbcombo\":\"1\"}", Encoding.UTF8, "application/json");
            var res33 = await http.PostAsync("/api/mst_product/getsearch", req33);
            var body33 = await res33.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res33.StatusCode);
            var doc33 = JsonDocument.Parse(body33);
            Assert.True(doc33.RootElement.GetProperty("Success").GetBoolean());
            var list33 = doc33.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
            Assert.Equal(1, list33.GetArrayLength());
            Assert.Equal("PRD_ROOT_03", list33[0].GetProperty("ProductCode").GetString());

            // Ca 34: 2 bộ lọc cho ra số dòng khác nhau (ckbproduct có 5 dòng != ckbservices có 1 dòng)
            Assert.NotEqual(list31.GetArrayLength(), list32.GetArrayLength());

            // Ca 35: POST /api/mst_product/productingredient với body rỗng -> 200 OK, Data = 5 items ROOTPRD (4 ban đầu + 1 tạo mới ở Ca 15), ListMstProductGroup = 3 items
            var req35 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res35 = await http.PostAsync("/api/mst_product/productingredient", req35);
            var body35 = await res35.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res35.StatusCode);
            var doc35 = JsonDocument.Parse(body35);
            Assert.True(doc35.RootElement.GetProperty("Success").GetBoolean());
            var data35 = doc35.RootElement.GetProperty("Data");
            Assert.Equal(5, data35.GetArrayLength());
            Assert.Equal(3, doc35.RootElement.GetProperty("ListMstProductGroup").GetArrayLength());

            // Ca 36: POST /api/mst_product/productingredient với OrgID = 1001, ProductCodeUserName = SP001 -> 200 OK, Data = 1 item, ListMstProductGroup = 2 items
            var req36 = new StringContent("{\"OrgID\":\"1001\",\"ProductCodeUserName\":\"SP001\"}", Encoding.UTF8, "application/json");
            var res36 = await http.PostAsync("/api/mst_product/productingredient", req36);
            var body36 = await res36.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res36.StatusCode);
            var doc36 = JsonDocument.Parse(body36);
            Assert.True(doc36.RootElement.GetProperty("Success").GetBoolean());
            var data36 = doc36.RootElement.GetProperty("Data");
            Assert.Equal(1, data36.GetArrayLength());
            Assert.Equal("PRD_ROOT_01", data36[0].GetProperty("ProductCode").GetString());
            Assert.Equal(2, doc36.RootElement.GetProperty("ListMstProductGroup").GetArrayLength());

            // Ca 37: 2 bộ lọc ProductIngredient cho ra số dòng khác nhau (OrgID 1001 có 4 ROOTPRD != OrgID 9999 có 1 ROOTPRD)
            var req37a = new StringContent("{\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res37a = await http.PostAsync("/api/mst_product/productingredient", req37a);
            var doc37a = JsonDocument.Parse(await res37a.Content.ReadAsStringAsync());
            var len37a = doc37a.RootElement.GetProperty("Data").GetArrayLength();

            var req37b = new StringContent("{\"OrgID\":\"9999\"}", Encoding.UTF8, "application/json");
            var res37b = await http.PostAsync("/api/mst_product/productingredient", req37b);
            var doc37b = JsonDocument.Parse(await res37b.Content.ReadAsStringAsync());
            var len37b = doc37b.RootElement.GetProperty("Data").GetArrayLength();

            Assert.Equal(4, len37a);
            Assert.Equal(1, len37b);
            Assert.NotEqual(len37a, len37b);

            // Ca 38: POST /api/mst_product/update với body rỗng hoàn toàn -> 400 Bad Request, Check.strJson
            var req38 = new StringContent("", Encoding.UTF8, "application/json");
            var res38 = await http.PostAsync("/api/mst_product/update", req38);
            var body38 = await res38.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res38.StatusCode);
            var doc38 = JsonDocument.Parse(body38);
            Assert.False(doc38.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_Product_Get", doc38.RootElement.GetProperty("ErrorCode").GetString());
            Assert.True(doc38.RootElement.GetProperty("Params").TryGetProperty("Check.strJson", out _));

            // Ca 39: POST /api/mst_product/update với body JSON {} (thiếu prlv) -> 400 Bad Request, Check.prlv
            var req39 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res39 = await http.PostAsync("/api/mst_product/update", req39);
            var body39 = await res39.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res39.StatusCode);
            var doc39 = JsonDocument.Parse(body39);
            Assert.False(doc39.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_Product_Get", doc39.RootElement.GetProperty("ErrorCode").GetString());
            Assert.True(doc39.RootElement.GetProperty("Params").TryGetProperty("Check.prlv", out _));

            // Ca 40: POST /api/mst_product/update với prroot = PRD_ROOT_01, prlv = ROOTPRD, OrgID = 1001 -> 200 OK
            var req40 = new StringContent("{\"OrgID\":\"1001\",\"prroot\":\"PRD_ROOT_01\",\"prlv\":\"ROOTPRD\"}", Encoding.UTF8, "application/json");
            var res40 = await http.PostAsync("/api/mst_product/update", req40);
            var body40 = await res40.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res40.StatusCode);
            var doc40 = JsonDocument.Parse(body40);
            Assert.True(doc40.RootElement.GetProperty("Success").GetBoolean());
            var data40 = doc40.RootElement.GetProperty("Data");
            Assert.Equal("PRD_BASE_01_1", data40.GetProperty("Product").GetProperty("ProductCode").GetString());
            Assert.Equal("", data40.GetProperty("UnitCodeBase").GetString());
            Assert.Equal(1, data40.GetProperty("ListUnitCode").GetArrayLength());
            Assert.Equal("PRD_L2_01_1", data40.GetProperty("ListUnitCode")[0].GetProperty("ProductCode").GetString());
            Assert.Equal(2, data40.GetProperty("List_Mst_ProductGroup").GetArrayLength());
            Assert.Equal(2, data40.GetProperty("List_Mst_ProductGroupUI").GetArrayLength());

            // Ca 41: POST /api/mst_product/update với prcode = PRD_L2_01_1, prroot = PRD_ROOT_01, prlv = L2PRD, OrgID = 1001 -> 200 OK, UnitCodeBase = CAI
            var req41 = new StringContent("{\"OrgID\":\"1001\",\"prcode\":\"PRD_L2_01_1\",\"prroot\":\"PRD_ROOT_01\",\"prlv\":\"L2PRD\"}", Encoding.UTF8, "application/json");
            var res41 = await http.PostAsync("/api/mst_product/update", req41);
            var body41 = await res41.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res41.StatusCode);
            var doc41 = JsonDocument.Parse(body41);
            Assert.True(doc41.RootElement.GetProperty("Success").GetBoolean());
            var data41 = doc41.RootElement.GetProperty("Data");
            Assert.Equal("CAI", data41.GetProperty("UnitCodeBase").GetString());
            Assert.Equal(1, data41.GetProperty("ListUnitCode").GetArrayLength());
            Assert.Equal("PRD_L2_01_1", data41.GetProperty("ListUnitCode")[0].GetProperty("ProductCode").GetString());

            // Ca 42: 2 bộ lọc Update cho ra số dòng khác nhau (PRD_ROOT_01 có ListUnitCode 1 dòng != PRD_ROOT_02 có ListUnitCode 0 dòng)
            var req42 = new StringContent("{\"OrgID\":\"1001\",\"prroot\":\"PRD_ROOT_02\",\"prlv\":\"ROOTPRD\"}", Encoding.UTF8, "application/json");
            var res42 = await http.PostAsync("/api/mst_product/update", req42);
            var doc42 = JsonDocument.Parse(await res42.Content.ReadAsStringAsync());
            var count42 = doc42.RootElement.GetProperty("Data").GetProperty("ListUnitCode").GetArrayLength();
            Assert.Equal(0, count42);
            Assert.NotEqual(data40.GetProperty("ListUnitCode").GetArrayLength(), count42);

            // Ca 43: POST /api/mst_product/edit với body rỗng -> Fail Mst_Product_Save
            var req43 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res43 = await http.PostAsync("/api/mst_product/edit", req43);
            var body43 = await res43.Content.ReadAsStringAsync();
            var doc43 = JsonDocument.Parse(body43);
            Assert.False(doc43.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_Product_Save", doc43.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 44: POST /api/mst_product/edit với đơn vị trùng (CAI) -> 200 OK, Success = true, cảnh báo trùng
            var req44Json = "{\"Mst_Product\":{\"OrgID\":\"1001\",\"ProductCode\":\"PRD_ROOT_01\",\"ProductCodeRoot\":\"PRD_ROOT_01\",\"ProductLevelSys\":\"ROOTPRD\"},\"Lst_Unit\":[{\"UnitCode\":\"CAI\",\"ProductCode\":\"PRD_ROOT_01\"}],\"Lst_Mst_ProductUI_Create\":[{\"UnitCode\":\"CAI\",\"ProductName\":\"Trùng\"}]}";
            var req44 = new StringContent(req44Json, Encoding.UTF8, "application/json");
            var res44 = await http.PostAsync("/api/mst_product/edit", req44);
            var body44 = await res44.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res44.StatusCode);
            var doc44 = JsonDocument.Parse(body44);
            Assert.True(doc44.RootElement.GetProperty("Success").GetBoolean());
            var msg44 = doc44.RootElement.GetProperty("Messages")[0].GetString();
            Assert.Contains("Mã đơn vị 'CAI' không được trùng!", msg44);

            // Ca 45: POST /api/mst_product/edit cho ROOTPRD (PRD_ROOT_01) -> 200 OK, Success = true, sửa thành công
            var req45Json = "{\"Mst_Product\":{\"OrgID\":\"1001\",\"ProductCode\":\"PRD_ROOT_01\",\"ProductCodeRoot\":\"PRD_ROOT_01\",\"ProductLevelSys\":\"ROOTPRD\",\"ProductCodeUser\":\"SP001_LIVE_UPD\",\"ProductName\":\"Sản phẩm Live đã sửa\",\"UPSell\":25000.0},\"FlagRedirect\":\"1\"}";
            var req45 = new StringContent(req45Json, Encoding.UTF8, "application/json");
            var res45 = await http.PostAsync("/api/mst_product/edit", req45);
            var body45 = await res45.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res45.StatusCode);
            var doc45 = JsonDocument.Parse(body45);
            Assert.True(doc45.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/api/mst_product/create", doc45.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 46: Round-trip đọc lại qua /api/mst_product/getsearch thấy PRD_ROOT_01 đã đổi tên thành "Sản phẩm Live đã sửa"
            var req46 = new StringContent("{\"OrgID\":\"1001\",\"ProductCodeUserName\":\"SP001_LIVE_UPD\"}", Encoding.UTF8, "application/json");
            var res46 = await http.PostAsync("/api/mst_product/getsearch", req46);
            var body46 = await res46.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res46.StatusCode);
            var doc46 = JsonDocument.Parse(body46);
            Assert.True(doc46.RootElement.GetProperty("Success").GetBoolean());
            var list46 = doc46.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Product");
            Assert.True(list46.GetArrayLength() >= 1);
            Assert.Equal("Sản phẩm Live đã sửa", list46[0].GetProperty("ProductName").GetString());

            // Ca 47: POST /api/mst_product/edit cho L2PRD (PRD_L2_01_1) -> 200 OK, Success = true
            var req47Json = "{\"Mst_Product\":{\"OrgID\":\"1001\",\"ProductCode\":\"PRD_L2_01_1\",\"ProductLevelSys\":\"L2PRD\",\"ProductName\":\"Sản phẩm Cấp 2 Live đã sửa\",\"ProductBarCode\":\"BARCODE_LIVE_L2\"},\"FlagRedirect\":\"0\"}";
            var req47 = new StringContent(req47Json, Encoding.UTF8, "application/json");
            var res47 = await http.PostAsync("/api/mst_product/edit", req47);
            var body47 = await res47.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res47.StatusCode);
            var doc47 = JsonDocument.Parse(body47);
            Assert.True(doc47.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/api/mst_product/index", doc47.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 48: POST /api/mst_product/deleteunit với body rỗng hoàn toàn -> 200 OK, Messages = ["Mã sản phẩm trống!"]
            var req48 = new StringContent("", Encoding.UTF8, "application/json");
            var res48 = await http.PostAsync("/api/mst_product/deleteunit", req48);
            var body48 = await res48.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res48.StatusCode);
            var doc48 = JsonDocument.Parse(body48);
            Assert.True(doc48.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã sản phẩm trống!", doc48.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 49: POST /api/mst_product/deleteunit với body JSON {} -> 200 OK, Messages = ["Mã sản phẩm trống!"]
            var req49 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res49 = await http.PostAsync("/api/mst_product/deleteunit", req49);
            var body49 = await res49.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res49.StatusCode);
            var doc49 = JsonDocument.Parse(body49);
            Assert.True(doc49.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã sản phẩm trống!", doc49.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 50: POST /api/mst_product/deleteunit với prdCode không tồn tại -> 200 OK, Messages = ["Mã sản phẩm 'PRD_UNKNOWN_XYZ' không có trong hệ thống!"]
            var req50 = new StringContent("{\"OrgID\":\"1001\",\"prdCode\":\"PRD_UNKNOWN_XYZ\"}", Encoding.UTF8, "application/json");
            var res50 = await http.PostAsync("/api/mst_product/deleteunit", req50);
            var body50 = await res50.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res50.StatusCode);
            var doc50 = JsonDocument.Parse(body50);
            Assert.True(doc50.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã sản phẩm 'PRD_UNKNOWN_XYZ' không có trong hệ thống!", doc50.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 51: POST /api/mst_product/create tạo sản phẩm mới PRD_UNIT_DELETE_LIVE -> 200 OK
            var req51Json = "{\"Lst_Mst_ProductUI_Create\":[{\"OrgID\":\"1001\",\"ProductCode\":\"PRD_UNIT_DELETE_LIVE\",\"ProductLevelSys\":\"L2PRD\",\"ProductCodeUser\":\"SP_UNIT_DEL_LIVE\",\"ProductName\":\"Sản phẩm Live để xóa\",\"BrandCode\":\"BRAND01\",\"ProductType\":\"PRODUCT\",\"ProductGrpCode\":\"GRP01\",\"UnitCode\":\"CAI\",\"FlagActive\":\"1\",\"FlagBuy\":\"1\",\"FlagSell\":\"1\"}]}";
            var req51 = new StringContent(req51Json, Encoding.UTF8, "application/json");
            var res51 = await http.PostAsync("/api/mst_product/create", req51);
            Assert.Equal(System.Net.HttpStatusCode.OK, res51.StatusCode);

            // Ca 52: POST /api/mst_product/deleteunit xoá PRD_UNIT_DELETE_LIVE -> 200 OK, Xóa đơn vị thành công!
            var req52 = new StringContent("{\"OrgID\":\"1001\",\"prdCode\":\"PRD_UNIT_DELETE_LIVE\"}", Encoding.UTF8, "application/json");
            var res52 = await http.PostAsync("/api/mst_product/deleteunit", req52);
            var body52 = await res52.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res52.StatusCode);
            var doc52 = JsonDocument.Parse(body52);
            Assert.True(doc52.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa đơn vị thành công!", doc52.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("/api/mst_product/index", doc52.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 53: POST /api/mst_product/deleteunit xoá lại PRD_UNIT_DELETE_LIVE (round-trip check) -> 200 OK, không có trong hệ thống
            var req53 = new StringContent("{\"OrgID\":\"1001\",\"prdCode\":\"PRD_UNIT_DELETE_LIVE\"}", Encoding.UTF8, "application/json");
            var res53 = await http.PostAsync("/api/mst_product/deleteunit", req53);
            var body53 = await res53.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res53.StatusCode);
            var doc53 = JsonDocument.Parse(body53);
            Assert.True(doc53.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã sản phẩm 'PRD_UNIT_DELETE_LIVE' không có trong hệ thống!", doc53.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 54: POST /api/mst_product/exporttemplate với body rỗng {} -> 200 OK, Success = true, CheckSuccess = "1", strUrl trỏ tới file .xlsx
            var req54 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res54 = await http.PostAsync("/api/mst_product/exporttemplate", req54);
            var body54 = await res54.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res54.StatusCode);
            var doc54 = JsonDocument.Parse(body54);
            Assert.True(doc54.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công!", doc54.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", doc54.RootElement.GetProperty("CheckSuccess").GetString());
            var url54 = doc54.RootElement.GetProperty("strUrl").GetString();
            Assert.NotNull(url54);
            Assert.StartsWith("/TempFiles/", url54);
            Assert.EndsWith(".xlsx", url54);

            // Ca 55: POST /api/mst_product/exporttemplate với body rỗng hoàn toàn "" -> 200 OK
            var req55 = new StringContent("", Encoding.UTF8, "application/json");
            var res55 = await http.PostAsync("/api/mst_product/exporttemplate", req55);
            var body55 = await res55.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res55.StatusCode);
            var doc55 = JsonDocument.Parse(body55);
            Assert.True(doc55.RootElement.GetProperty("Success").GetBoolean());

            // Ca 56: POST /api/mst_product/export-template (kebab-case route alias) -> 200 OK
            var req56 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res56 = await http.PostAsync("/api/mst_product/export-template", req56);
            var body56 = await res56.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res56.StatusCode);
            var doc56 = JsonDocument.Parse(body56);
            Assert.True(doc56.RootElement.GetProperty("Success").GetBoolean());

            // Ca 57: POST /api/mst_product/import với body rỗng "" -> 400 Bad Request, "File excel import không hợp lệ!"
            var req57 = new StringContent("", Encoding.UTF8, "application/json");
            var res57 = await http.PostAsync("/api/mst_product/import", req57);
            var body57 = await res57.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res57.StatusCode);
            var doc57 = JsonDocument.Parse(body57);
            Assert.False(doc57.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("File excel import không hợp lệ!", doc57.RootElement.GetProperty("Params").GetProperty("Message").GetString());

            // Ca 58: POST /api/mst_product/import với multipart rỗng (0 file) -> 400 Bad Request, "File excel import không hợp lệ!"
            using (var emptyMultipart = new MultipartFormDataContent())
            {
                var res58 = await http.PostAsync("/api/mst_product/import", emptyMultipart);
                var body58 = await res58.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, res58.StatusCode);
                var doc58 = JsonDocument.Parse(body58);
                Assert.False(doc58.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("File excel import không hợp lệ!", doc58.RootElement.GetProperty("Params").GetProperty("Message").GetString());
            }

            // Ca 59: POST /api/mst_product/import với file .txt -> 400 Bad Request, "File excel import không hợp lệ!"
            using (var textMultipart = new MultipartFormDataContent())
            {
                textMultipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("not excel")), "file", "test.txt");
                var res59 = await http.PostAsync("/api/mst_product/import", textMultipart);
                var body59 = await res59.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, res59.StatusCode);
                var doc59 = JsonDocument.Parse(body59);
                Assert.False(doc59.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("File excel import không hợp lệ!", doc59.RootElement.GetProperty("Params").GetProperty("Message").GetString());
            }

            // Ca 60: POST /api/mst_product/import với Excel không có dòng dữ liệu (0 row) -> 400 Bad Request, "File excel import không có dữ liệu!"
            using (var zeroRowMultipart = new MultipartFormDataContent())
            {
                var emptyExcel = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]>());
                zeroRowMultipart.Add(new ByteArrayContent(emptyExcel), "file", "zero_rows.xlsx");
                var res60 = await http.PostAsync("/api/mst_product/import", zeroRowMultipart);
                var body60 = await res60.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, res60.StatusCode);
                var doc60 = JsonDocument.Parse(body60);
                Assert.False(doc60.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("File excel import không có dữ liệu!", doc60.RootElement.GetProperty("Params").GetProperty("Message").GetString());
            }

            // Ca 61: POST /api/mst_product/import với nhóm hàng không tồn tại -> 400 Bad Request, "Nhóm hàng GRP_UNKNOWN không có trong hệ thống! - Dòng 3"
            using (var invalidGrpMultipart = new MultipartFormDataContent())
            {
                var invalidGrpRow = CreateProductRow(code: "SP_LIVE_BAD_GRP", grpCode: "GRP_UNKNOWN");
                var invalidGrpExcel = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { invalidGrpRow });
                invalidGrpMultipart.Add(new ByteArrayContent(invalidGrpExcel), "file", "bad_grp.xlsx");
                var res61 = await http.PostAsync("/api/mst_product/import", invalidGrpMultipart);
                var body61 = await res61.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, res61.StatusCode);
                var doc61 = JsonDocument.Parse(body61);
                Assert.False(doc61.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Nhóm hàng GRP_UNKNOWN không có trong hệ thống! - Dòng 3", doc61.RootElement.GetProperty("Params").GetProperty("Message").GetString());
            }

            // Ca 62: POST /api/mst_product/import tạo mới sản phẩm SP_LIVE_IMP_01 -> 200 OK, "Đã nhập dữ liệu excel thành công!"
            using (var validCreateMultipart = new MultipartFormDataContent())
            {
                var createRow = CreateProductRow(code: "SP_LIVE_IMP_01", name: "Sản phẩm Live Import 01", upBuy: "55000", upSell: "75000");
                var createExcel = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { createRow });
                validCreateMultipart.Add(new ByteArrayContent(createExcel), "file", "valid_create.xlsx");
                validCreateMultipart.Add(new StringContent("1"), "flagimport");
                var res62 = await http.PostAsync("/api/mst_product/import", validCreateMultipart);
                var body62 = await res62.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.OK, res62.StatusCode);
                var doc62 = JsonDocument.Parse(body62);
                Assert.True(doc62.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Đã nhập dữ liệu excel thành công!", doc62.RootElement.GetProperty("Title").GetString());
            }

            // Ca 63: Round-trip kiểm tra qua /api/mst_product/search_product_auto với "SP_LIVE_IMP_01" -> 200 OK, ListProduct có 1 phần tử
            var req63 = new StringContent("{\"productcodename\":\"SP_LIVE_IMP_01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res63 = await http.PostAsync("/api/mst_product/search_product_auto", req63);
            var body63 = await res63.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res63.StatusCode);
            var doc63 = JsonDocument.Parse(body63);
            Assert.True(doc63.RootElement.GetProperty("Success").GetBoolean());
            var list63 = doc63.RootElement.GetProperty("ListProduct");
            Assert.Equal(1, list63.GetArrayLength());
            Assert.Equal("SP_LIVE_IMP_01", list63[0].GetProperty("ProductCodeUser").GetString());
            Assert.Equal("Sản phẩm Live Import 01", list63[0].GetProperty("ProductName").GetString());

            // Ca 64: POST /api/mst_product/import cập nhật SP_LIVE_IMP_01 với flagimport = 0 -> 200 OK, "Đã nhập dữ liệu excel thành công!"
            using (var validUpdateMultipart = new MultipartFormDataContent())
            {
                var updateRow = CreateProductRow(code: "SP_LIVE_IMP_01", name: "Sản phẩm Live Import 01 Đã Sửa", upBuy: "60000", upSell: "90000");
                var updateExcel = CreateProductExcelBytes(ProductImportHeaders, new List<string?[]> { updateRow });
                validUpdateMultipart.Add(new ByteArrayContent(updateExcel), "file", "valid_update.xlsx");
                validUpdateMultipart.Add(new StringContent("0"), "flagimport");
                var res64 = await http.PostAsync("/api/mst_product/import", validUpdateMultipart);
                var body64 = await res64.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.OK, res64.StatusCode);
                var doc64 = JsonDocument.Parse(body64);
                Assert.True(doc64.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Đã nhập dữ liệu excel thành công!", doc64.RootElement.GetProperty("Title").GetString());
            }

            // Ca 65: Round-trip kiểm tra sau update qua /api/mst_product/search_product_auto -> 200 OK, ProductName mới = "Sản phẩm Live Import 01 Đã Sửa"
            var req65 = new StringContent("{\"productcodename\":\"SP_LIVE_IMP_01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res65 = await http.PostAsync("/api/mst_product/search_product_auto", req65);
            var body65 = await res65.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res65.StatusCode);
            var doc65 = JsonDocument.Parse(body65);
            Assert.True(doc65.RootElement.GetProperty("Success").GetBoolean());
            var list65 = doc65.RootElement.GetProperty("ListProduct");
            Assert.Equal(1, list65.GetArrayLength());
            Assert.Equal("Sản phẩm Live Import 01 Đã Sửa", list65[0].GetProperty("ProductName").GetString());

            // Ca 66: POST /api/mst_product/export với body rỗng -> 200 OK, "Đã xuất excel thành công!", CheckSuccess = "1", strUrl có .xlsx
            var req66 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res66 = await http.PostAsync("/api/mst_product/export", req66);
            var body66 = await res66.Content.ReadAsStringAsync();
            Assert.True(res66.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res66.StatusCode}, body: {body66}");
            var doc66 = JsonDocument.Parse(body66);
            Assert.True(doc66.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("1", doc66.RootElement.GetProperty("CheckSuccess").GetString());
            Assert.Equal("Đã xuất excel thành công!", doc66.RootElement.GetProperty("Title").GetString());
            Assert.EndsWith(".xlsx", doc66.RootElement.GetProperty("strUrl").GetString()!);

            // Ca 67: POST /api/mst_product/export với OrgID = 1001, ckbcombo = 1 -> 200 OK, CheckSuccess = 1
            var req67 = new StringContent("{\"OrgID\":\"1001\",\"ckbcombo\":\"1\"}", Encoding.UTF8, "application/json");
            var res67 = await http.PostAsync("/api/mst_product/export", req67);
            var body67 = await res67.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res67.StatusCode);
            var doc67 = JsonDocument.Parse(body67);
            Assert.True(doc67.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("1", doc67.RootElement.GetProperty("CheckSuccess").GetString());

            // Ca 68: POST /api/mst_product/export với lstattribute không hợp lệ -> 400 Bad Request, Erridn.Skycic.QContract.Mst_Product_Export
            var req68 = new StringContent("{\"lstattribute\":\"{bad_json\"}", Encoding.UTF8, "application/json");
            var res68 = await http.PostAsync("/api/mst_product/export", req68);
            var body68 = await res68.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res68.StatusCode);
            var doc68 = JsonDocument.Parse(body68);
            Assert.False(doc68.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Product_Export", doc68.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("Tham số lstattribute không hợp lệ!", doc68.RootElement.GetProperty("Params").GetProperty("Exception").GetString());

            // Ca 69: POST /api/mst_product/showpopupimportexcel với body rỗng -> 200 OK, Success = true, Html có "Nhập hàng hóa từ file Excel"
            var req69 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res69 = await http.PostAsync("/api/mst_product/showpopupimportexcel", req69);
            var body69 = await res69.Content.ReadAsStringAsync();
            Assert.True(res69.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res69.StatusCode}, body: {body69}");
            var doc69 = JsonDocument.Parse(body69);
            Assert.True(doc69.RootElement.GetProperty("Success").GetBoolean());
            var html69 = doc69.RootElement.GetProperty("Html").GetString();
            Assert.NotNull(html69);
            Assert.Contains("Nhập hàng hóa từ file Excel", html69);
            Assert.Contains("manageFormImportExcel", html69);

            // Ca 70: GET /api/mst_product/showpopupimportexcel -> 200 OK, Success = true, Html có "Nhập hàng hóa từ file Excel"
            var res70 = await http.GetAsync("/api/mst_product/showpopupimportexcel");
            var body70 = await res70.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res70.StatusCode);
            var doc70 = JsonDocument.Parse(body70);
            Assert.True(doc70.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Nhập hàng hóa từ file Excel", doc70.RootElement.GetProperty("Html").GetString()!);

            // Ca 71: POST /api/mst_product/show-popup-import-excel (kebab-case route alias) -> 200 OK, Success = true
            var req71 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res71 = await http.PostAsync("/api/mst_product/show-popup-import-excel", req71);
            var body71 = await res71.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res71.StatusCode);
            var doc71 = JsonDocument.Parse(body71);
            Assert.True(doc71.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("manageFormImportExcel", doc71.RootElement.GetProperty("Html").GetString()!);

            // Ca 72: So sánh kết quả cấu trúc modal ShowPopupImportExcel: Html không rỗng và dài hơn 100 ký tự
            Assert.True(html69.Length > 100);

            // Ca 73: POST /api/mst_product/stopproducts với body rỗng -> 200 OK, Success = true, Messages[0] = "Ngừng kinh doanh hàng hóa thành công!"
            var req73 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res73 = await http.PostAsync("/api/mst_product/stopproducts", req73);
            var body73 = await res73.Content.ReadAsStringAsync();
            Assert.True(res73.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res73.StatusCode}, body: {body73}");
            var doc73 = JsonDocument.Parse(body73);
            Assert.True(doc73.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Ngừng kinh doanh hàng hóa thành công!", doc73.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("/Mst_Product/Index", doc73.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 74: POST /api/mst_product/stopproducts với JSON lỗi trong listproduct -> 400 Bad Request, Erridn.Skycic.QContract.Mst_Product_StopProducts
            var req74 = new StringContent("{\"listproduct\":\"{bad_json\"}", Encoding.UTF8, "application/json");
            var res74 = await http.PostAsync("/api/mst_product/stopproducts", req74);
            var body74 = await res74.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res74.StatusCode);
            var doc74 = JsonDocument.Parse(body74);
            Assert.False(doc74.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Product_StopProducts", doc74.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 75: POST /api/mst_product/create tạo sản phẩm PRD_STOP_LIVE_01 (FlagActive = "1") -> 200 OK, Success = true
            var createStopJson = JsonSerializer.Serialize(new
            {
                OrgID = "1001",
                NetworkID = "NET1",
                ProductCode = "PRD_STOP_LIVE_01",
                ProductCodeUser = "PRD_STOP_LIVE_01",
                ProductName = "Sản phẩm Live Stop 01",
                ProductLevelSys = "ROOTPRD",
                ProductType = "PRODUCT",
                ProductGrpCode = "GRP_CON_01",
                UnitCode = "CAI",
                FlagActive = "1"
            });
            var res75 = await http.PostAsync("/api/mst_product/create", new StringContent(createStopJson, Encoding.UTF8, "application/json"));
            var body75 = await res75.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res75.StatusCode);

            // Xác nhận trước khi stop: search_product_auto tìm thấy PRD_STOP_LIVE_01 (vì FlagActive mặc định là 1)
            var checkBeforeStop = await http.PostAsync("/api/mst_product/search_product_auto",
                new StringContent("{\"productcodename\":\"PRD_STOP_LIVE_01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json"));
            var bodyBefore = await checkBeforeStop.Content.ReadAsStringAsync();
            var docBefore = JsonDocument.Parse(bodyBefore);
            Assert.Equal(1, docBefore.RootElement.GetProperty("ListProduct").GetArrayLength());

            // Ca 76: POST /api/mst_product/stopproducts ngừng kinh doanh PRD_STOP_LIVE_01 -> 200 OK, Success = true
            var stopReqJson = JsonSerializer.Serialize(new
            {
                listproduct = "[{\"ProductCode\":\"PRD_STOP_LIVE_01\"}]",
                OrgID = "1001"
            });
            var res76 = await http.PostAsync("/api/mst_product/stopproducts", new StringContent(stopReqJson, Encoding.UTF8, "application/json"));
            var body76 = await res76.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res76.StatusCode);
            var doc76 = JsonDocument.Parse(body76);
            Assert.True(doc76.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Ngừng kinh doanh hàng hóa thành công!", doc76.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 77: Round-trip kiểm tra qua search_product_auto:
            // - Với FlagActive = 1 (mặc định): không còn tìm thấy PRD_STOP_LIVE_01 (0 phần tử)
            // - Với FlagActive = 0: tìm thấy đúng PRD_STOP_LIVE_01 (1 phần tử)
            var checkAfterStopActive1 = await http.PostAsync("/api/mst_product/search_product_auto",
                new StringContent("{\"productcodename\":\"PRD_STOP_LIVE_01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json"));
            var docAfter1 = JsonDocument.Parse(await checkAfterStopActive1.Content.ReadAsStringAsync());
            Assert.Equal(0, docAfter1.RootElement.GetProperty("ListProduct").GetArrayLength());

            var checkAfterStopActive0 = await http.PostAsync("/api/mst_product/search_product_auto",
                new StringContent("{\"productcodename\":\"PRD_STOP_LIVE_01\",\"OrgID\":\"1001\",\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json"));
            var docAfter0 = JsonDocument.Parse(await checkAfterStopActive0.Content.ReadAsStringAsync());
            Assert.Equal(1, docAfter0.RootElement.GetProperty("ListProduct").GetArrayLength());
            Assert.Equal("PRD_STOP_LIVE_01", docAfter0.RootElement.GetProperty("ListProduct")[0].GetProperty("ProductCodeUser").GetString());

            // Ca 78: POST /api/mst_product/stop-products (kebab-case route alias) với body rỗng -> 200 OK, Success = true
            var req78 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res78 = await http.PostAsync("/api/mst_product/stop-products", req78);
            var body78 = await res78.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res78.StatusCode);
            var doc78 = JsonDocument.Parse(body78);
            Assert.True(doc78.RootElement.GetProperty("Success").GetBoolean());

            // Ca 79: POST /api/mst_product/activeproducts với body rỗng -> 200 OK, Success = true, Messages[0] = "Kinh doanh hàng hóa thành công!"
            var req79 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res79 = await http.PostAsync("/api/mst_product/activeproducts", req79);
            var body79 = await res79.Content.ReadAsStringAsync();
            Assert.True(res79.StatusCode == System.Net.HttpStatusCode.OK, $"Status {res79.StatusCode}, body: {body79}");
            var doc79 = JsonDocument.Parse(body79);
            Assert.True(doc79.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Kinh doanh hàng hóa thành công!", doc79.RootElement.GetProperty("Messages")[0].GetString());
            Assert.Equal("/Mst_Product/Index", doc79.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 80: POST /api/mst_product/activeproducts với JSON lỗi trong listproduct -> 400 Bad Request, Erridn.Skycic.QContract.Mst_Product_ActiveProducts
            var req80 = new StringContent("{\"listproduct\":\"{bad_json\"}", Encoding.UTF8, "application/json");
            var res80 = await http.PostAsync("/api/mst_product/activeproducts", req80);
            var body80 = await res80.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res80.StatusCode);
            var doc80 = JsonDocument.Parse(body80);
            Assert.False(doc80.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Product_ActiveProducts", doc80.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 81: POST /api/mst_product/activeproducts kích hoạt kinh doanh lại PRD_STOP_LIVE_01 (sau Ca 76 có FlagActive = "0") -> 200 OK, Success = true
            var activeReqJson = JsonSerializer.Serialize(new
            {
                listproduct = "[{\"ProductCode\":\"PRD_STOP_LIVE_01\"}]",
                OrgID = "1001"
            });
            var res81 = await http.PostAsync("/api/mst_product/activeproducts", new StringContent(activeReqJson, Encoding.UTF8, "application/json"));
            var body81 = await res81.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res81.StatusCode);
            var doc81 = JsonDocument.Parse(body81);
            Assert.True(doc81.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Kinh doanh hàng hóa thành công!", doc81.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 82: Round-trip kiểm tra qua search_product_auto sau khi active:
            // - Với FlagActive = 1 (mặc định): tìm thấy đúng PRD_STOP_LIVE_01 (1 phần tử)
            // - Với FlagActive = 0: không còn tìm thấy PRD_STOP_LIVE_01 (0 phần tử)
            var checkAfterActive1 = await http.PostAsync("/api/mst_product/search_product_auto",
                new StringContent("{\"productcodename\":\"PRD_STOP_LIVE_01\",\"OrgID\":\"1001\"}", Encoding.UTF8, "application/json"));
            var docActive1 = JsonDocument.Parse(await checkAfterActive1.Content.ReadAsStringAsync());
            Assert.Equal(1, docActive1.RootElement.GetProperty("ListProduct").GetArrayLength());
            Assert.Equal("PRD_STOP_LIVE_01", docActive1.RootElement.GetProperty("ListProduct")[0].GetProperty("ProductCodeUser").GetString());

            var checkAfterActive0 = await http.PostAsync("/api/mst_product/search_product_auto",
                new StringContent("{\"productcodename\":\"PRD_STOP_LIVE_01\",\"OrgID\":\"1001\",\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json"));
            var docActive0 = JsonDocument.Parse(await checkAfterActive0.Content.ReadAsStringAsync());
            Assert.Equal(0, docActive0.RootElement.GetProperty("ListProduct").GetArrayLength());

            // Ca 83: POST /api/mst_product/active-products (kebab-case route alias) với body rỗng -> 200 OK, Success = true
            var req83 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res83 = await http.PostAsync("/api/mst_product/active-products", req83);
            var body83 = await res83.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res83.StatusCode);
            var doc83 = JsonDocument.Parse(body83);
            Assert.True(doc83.RootElement.GetProperty("Success").GetBoolean());
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

            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "test_wwwroot_prd_" + Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MiniContract.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
    }
}
