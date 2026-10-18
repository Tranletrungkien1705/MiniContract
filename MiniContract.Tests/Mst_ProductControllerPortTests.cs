using System.Diagnostics;
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

        // Chạy seeder Mst_Product
        var seeder = new Mst_ProductSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

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
}
