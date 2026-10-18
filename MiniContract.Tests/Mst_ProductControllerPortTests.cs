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
