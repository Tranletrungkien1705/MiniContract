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

public class Mst_FinishedContractReasonControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_FinishedContractReasonControllerPort _controller;

    public Mst_FinishedContractReasonControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_FinishedContractReason
        var seeder = new Mst_FinishedContractReasonSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_FinishedContractReasonControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_WithDefaultOrEmptyBody_ReturnsAllSeededItems()
    {
        var result = await _controller.Index(new Mst_FinishedContractReason_IndexReq());
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
    public async Task Index_FilterByNetworkID_ReturnsFilteredCount()
    {
        // Bộ lọc 1: NetworkID = "1001" -> 4 bản ghi
        var req1001 = new Mst_FinishedContractReason_IndexReq { NetworkID = "1001" };
        var res1001 = await _controller.Index(req1001);
        var ok1001 = Assert.IsType<OkObjectResult>(res1001);
        var doc1001 = JsonDocument.Parse(JsonSerializer.Serialize(ok1001.Value));
        var data1001 = doc1001.RootElement.GetProperty("Data");
        Assert.Equal(4, data1001.GetProperty("ItemCount").GetInt32());
        Assert.Equal(4, data1001.GetProperty("DataList").GetArrayLength());

        // Bộ lọc 2: NetworkID = "1002" -> 1 bản ghi
        var req1002 = new Mst_FinishedContractReason_IndexReq { NetworkID = "1002" };
        var res1002 = await _controller.Index(req1002);
        var ok1002 = Assert.IsType<OkObjectResult>(res1002);
        var doc1002 = JsonDocument.Parse(JsonSerializer.Serialize(ok1002.Value));
        var data1002 = doc1002.RootElement.GetProperty("Data");
        Assert.Equal(1, data1002.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data1002.GetProperty("DataList").GetArrayLength());
        Assert.Equal("REASON05", data1002.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());

        // Đối chiếu 2 bộ lọc ra số dòng khác nhau (4 != 1)
        Assert.NotEqual(data1001.GetProperty("ItemCount").GetInt32(), data1002.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Index_FilterByFlagActive_ReturnsActiveOnly()
    {
        // FlagActive = "1" -> 4 bản ghi hoạt động
        var reqActive = new Mst_FinishedContractReason_IndexReq { FlagActive = "1" };
        var resActive = await _controller.Index(reqActive);
        var okActive = Assert.IsType<OkObjectResult>(resActive);
        var docActive = JsonDocument.Parse(JsonSerializer.Serialize(okActive.Value));
        var dataActive = docActive.RootElement.GetProperty("Data");
        Assert.Equal(4, dataActive.GetProperty("ItemCount").GetInt32());
        Assert.Equal(4, dataActive.GetProperty("DataList").GetArrayLength());

        // FlagActive = "0" -> 1 bản ghi ngưng hoạt động
        var reqInactive = new Mst_FinishedContractReason_IndexReq { FlagActive = "0" };
        var resInactive = await _controller.Index(reqInactive);
        var okInactive = Assert.IsType<OkObjectResult>(resInactive);
        var docInactive = JsonDocument.Parse(JsonSerializer.Serialize(okInactive.Value));
        var dataInactive = docInactive.RootElement.GetProperty("Data");
        Assert.Equal(1, dataInactive.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, dataInactive.GetProperty("DataList").GetArrayLength());
        Assert.Equal("REASON04", dataInactive.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByKeyWord_ReturnsMatchingItems()
    {
        var req = new Mst_FinishedContractReason_IndexReq { KeyWord = "thỏa thuận" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("DataList").GetArrayLength());
        Assert.Equal("REASON02", data.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Hai bên thỏa thuận chấm dứt", data.GetProperty("DataList")[0].GetProperty("ContractFinishReasonName").GetString());
    }

    [Fact]
    public async Task Index_Pagination_PaginatesCorrectly()
    {
        // Trang 0, kích thước 2 -> bản ghi 0, 1 (REASON01, REASON02)
        var reqPage0 = new Mst_FinishedContractReason_IndexReq { Page = 0, RecordCount = 2 };
        var resPage0 = await _controller.Index(reqPage0);
        var okPage0 = Assert.IsType<OkObjectResult>(resPage0);
        var docPage0 = JsonDocument.Parse(JsonSerializer.Serialize(okPage0.Value));
        var dataPage0 = docPage0.RootElement.GetProperty("Data");

        Assert.Equal(5, dataPage0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, dataPage0.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataPage0.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageSize").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("DataList").GetArrayLength());
        Assert.Equal("REASON01", dataPage0.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("REASON02", dataPage0.GetProperty("DataList")[1].GetProperty("ContractFinishReasonCode").GetString());

        // Trang 1, kích thước 2 -> bản ghi 2, 3 (REASON03, REASON04)
        var reqPage1 = new Mst_FinishedContractReason_IndexReq { Page = 1, RecordCount = 2 };
        var resPage1 = await _controller.Index(reqPage1);
        var okPage1 = Assert.IsType<OkObjectResult>(resPage1);
        var docPage1 = JsonDocument.Parse(JsonSerializer.Serialize(okPage1.Value));
        var dataPage1 = docPage1.RootElement.GetProperty("Data");

        Assert.Equal(5, dataPage1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, dataPage1.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, dataPage1.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, dataPage1.GetProperty("DataList").GetArrayLength());
        Assert.Equal("REASON03", dataPage1.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("REASON04", dataPage1.GetProperty("DataList")[1].GetProperty("ContractFinishReasonCode").GetString());

        // Trang 2, kích thước 2 -> bản ghi 4 (REASON05)
        var reqPage2 = new Mst_FinishedContractReason_IndexReq { Page = 2, RecordCount = 2 };
        var resPage2 = await _controller.Index(reqPage2);
        var okPage2 = Assert.IsType<OkObjectResult>(resPage2);
        var docPage2 = JsonDocument.Parse(JsonSerializer.Serialize(okPage2.Value));
        var dataPage2 = docPage2.RootElement.GetProperty("Data");

        Assert.Equal(5, dataPage2.GetProperty("ItemCount").GetInt32());
        Assert.Equal(3, dataPage2.GetProperty("PageCount").GetInt32());
        Assert.Equal(2, dataPage2.GetProperty("PageIndex").GetInt32());
        Assert.Equal(1, dataPage2.GetProperty("DataList").GetArrayLength());
        Assert.Equal("REASON05", dataPage2.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Index_NegativePageIndexOrZeroPageSize_ReturnsEmptyDataList()
    {
        // Ft_RecordStart < 0
        var reqNegative = new Mst_FinishedContractReason_IndexReq { Ft_RecordStart = -1 };
        var resNegative = await _controller.Index(reqNegative);
        var okNegative = Assert.IsType<OkObjectResult>(resNegative);
        var docNegative = JsonDocument.Parse(JsonSerializer.Serialize(okNegative.Value));
        var dataNegative = docNegative.RootElement.GetProperty("Data");

        Assert.Equal(5, dataNegative.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataNegative.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataNegative.GetProperty("DataList").GetArrayLength());

        // RecordCount = 0
        var reqZero = new Mst_FinishedContractReason_IndexReq { RecordCount = 0 };
        var resZero = await _controller.Index(reqZero);
        var okZero = Assert.IsType<OkObjectResult>(resZero);
        var docZero = JsonDocument.Parse(JsonSerializer.Serialize(okZero.Value));
        var dataZero = docZero.RootElement.GetProperty("Data");

        Assert.Equal(5, dataZero.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_NonExistentFilter_ReturnsZeroItemCountAndEmptyList()
    {
        var req = new Mst_FinishedContractReason_IndexReq { NetworkID = "NON_EXISTENT" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Create_EmptyOrNullBody_ReturnsBadRequestWithStrJsonError()
    {
        var result = await _controller.Create(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Create", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.True(doc.RootElement.GetProperty("Params").TryGetProperty("Check.strJson", out _));

        var resultEmpty = await _controller.Create(new Mst_FinishedContractReason_CreateReq());
        var badEmpty = Assert.IsType<BadRequestObjectResult>(resultEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(badEmpty.Value));
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Create", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_InvalidNetworkID_ReturnsInvalidNetworkIDError()
    {
        var req = new Mst_FinishedContractReason_CreateReq
        {
            NetworkID = "",
            ContractFinishReasonCode = "TEST01",
            ContractFinishReasonName = "Test Reason"
        };
        var result = await _controller.Create(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Create_InvalidNetworkID", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_ExistingCode_ReturnsOrganExistError()
    {
        var req = new Mst_FinishedContractReason_CreateReq
        {
            NetworkID = "1001",
            ContractFinishReasonCode = "REASON01",
            ContractFinishReasonName = "Hết hạn trùng lặp"
        };
        var result = await _controller.Create(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganExist", doc.RootElement.GetProperty("ErrorCode").GetString());
        var p = doc.RootElement.GetProperty("Params");
        Assert.Equal("1001", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("REASON01", p.GetProperty("Check.ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Create_ValidItem_CreatesAndReturnsSuccess()
    {
        var req = new Mst_FinishedContractReason_CreateReq
        {
            NetworkID = "1001",
            ContractFinishReasonCode = "REASON_NEW",
            ContractFinishReasonName = "Thanh lý trước hạn",
            FinishDescription = "Thỏa thuận thanh lý sớm"
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("REASON_NEW", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Thanh lý trước hạn", data.GetProperty("ContractFinishReasonName").GetString());
        Assert.Equal("FINISHED", data.GetProperty("FinishType").GetString());
        Assert.Equal("Thỏa thuận thanh lý sớm", data.GetProperty("FinishDescription").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());

        // Kiểm tra round-trip: đọc lại từ DB
        var saved = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON_NEW");
        Assert.NotNull(saved);
        Assert.Equal("Thanh lý trước hạn", saved.ContractFinishReasonName);
        Assert.Equal("FINISHED", saved.FinishType);
        Assert.Equal("1", saved.FlagActive);
    }

    [Fact]
    public async Task Create_ViaModelProperty_CreatesSuccessfully()
    {
        var req = new Mst_FinishedContractReason_CreateReq
        {
            Model = JsonSerializer.Serialize(new Mst_FinishedContractReason_CreateReq
            {
                NetworkID = "1001",
                ContractFinishReasonCode = "REASON_JSON",
                ContractFinishReasonName = "Tạo qua JSON Model",
                FinishDescription = "Mô tả JSON"
            })
        };
        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var saved = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON_JSON");
        Assert.NotNull(saved);
        Assert.Equal("REASON_JSON", saved.ContractFinishReasonCode);
        Assert.Equal("Tạo qua JSON Model", saved.ContractFinishReasonName);
    }

    [Fact]
    public async Task Update_WithValidContractFinishReasonCode_ReturnsItem()
    {
        var req = new Mst_FinishedContractReason_UpdateReq { ContractFinishReasonCode = "REASON01" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("REASON01", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Hết hạn hợp đồng", data.GetProperty("ContractFinishReasonName").GetString());
    }

    [Fact]
    public async Task Update_WithReasonCodeAlias_ReturnsItem()
    {
        var req = new Mst_FinishedContractReason_UpdateReq { ReasonCode = "REASON02" };
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("REASON02", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Hai bên thỏa thuận chấm dứt", data.GetProperty("ContractFinishReasonName").GetString());
    }

    [Fact]
    public async Task Update_WithReasoncodeLowerAlias_ReturnsItem()
    {
        var json = "{\"reasoncode\":\"REASON03\"}";
        var req = JsonSerializer.Deserialize<Mst_FinishedContractReason_UpdateReq>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var res = await _controller.Update(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("REASON03", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Một bên đơn phương chấm dứt", data.GetProperty("ContractFinishReasonName").GetString());
    }

    [Fact]
    public async Task Update_WithNonExistentCode_ReturnsNullData()
    {
        var req = new Mst_FinishedContractReason_UpdateReq { ContractFinishReasonCode = "NON_EXISTENT" };
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
        Assert.Equal("REASON01", docNull.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

        var resEmpty = await _controller.Update(new Mst_FinishedContractReason_UpdateReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("REASON01", docEmpty.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Update_WithNetworkIDFilter_FiltersCorrectly()
    {
        // NetworkID = 1002 và REASON05 -> tìm thấy
        var req1 = new Mst_FinishedContractReason_UpdateReq { NetworkID = "1002", ContractFinishReasonCode = "REASON05" };
        var res1 = await _controller.Update(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("REASON05", doc1.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

        // NetworkID = 1001 và REASON05 -> không khớp NetworkID -> Data null
        var req2 = new Mst_FinishedContractReason_UpdateReq { NetworkID = "1001", ContractFinishReasonCode = "REASON05" };
        var res2 = await _controller.Update(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_WithValidContractFinishReasonCode_ReturnsItem()
    {
        var req = new Mst_FinishedContractReason_DetailReq { ContractFinishReasonCode = "REASON01" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("REASON01", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Hết hạn hợp đồng", data.GetProperty("ContractFinishReasonName").GetString());
    }

    [Fact]
    public async Task Detail_WithReasonCodeAlias_ReturnsItem()
    {
        var req = new Mst_FinishedContractReason_DetailReq { ReasonCode = "REASON02" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("REASON02", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Hai bên thỏa thuận chấm dứt", data.GetProperty("ContractFinishReasonName").GetString());
    }

    [Fact]
    public async Task Detail_WithReasoncodeLowerAlias_ReturnsItem()
    {
        var json = "{\"reasoncode\":\"REASON03\"}";
        var req = JsonSerializer.Deserialize<Mst_FinishedContractReason_DetailReq>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("REASON03", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Một bên đơn phương chấm dứt", data.GetProperty("ContractFinishReasonName").GetString());
    }

    [Fact]
    public async Task Detail_WithNonExistentCode_ReturnsNullData()
    {
        var req = new Mst_FinishedContractReason_DetailReq { ContractFinishReasonCode = "NON_EXISTENT" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_WithEmptyBodyOrNullReq_ReturnsNullData()
    {
        var resNull = await _controller.Detail(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docNull.RootElement.GetProperty("Data").ValueKind);

        var resEmpty = await _controller.Detail(new Mst_FinishedContractReason_DetailReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_WithNetworkIDFilter_FiltersCorrectly()
    {
        // Bộ lọc 1: NetworkID = 1002 và REASON05 -> tìm thấy
        var req1 = new Mst_FinishedContractReason_DetailReq { NetworkID = "1002", ContractFinishReasonCode = "REASON05" };
        var res1 = await _controller.Detail(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("REASON05", doc1.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

        // Bộ lọc 2: NetworkID = 1001 và REASON05 -> không khớp NetworkID -> Data null
        var req2 = new Mst_FinishedContractReason_DetailReq { NetworkID = "1001", ContractFinishReasonCode = "REASON05" };
        var res2 = await _controller.Detail(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Edit_EmptyOrNullBody_ReturnsBadRequestWithStrJsonError()
    {
        var resultNull = await _controller.Edit(null);
        var badNull = Assert.IsType<BadRequestObjectResult>(resultNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(badNull.Value));
        Assert.False(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Update", docNull.RootElement.GetProperty("ErrorCode").GetString());
        Assert.True(docNull.RootElement.GetProperty("Params").TryGetProperty("Check.strJson", out _));

        var resultEmpty = await _controller.Edit(new Mst_FinishedContractReason_EditReq());
        var badEmpty = Assert.IsType<BadRequestObjectResult>(resultEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(badEmpty.Value));
        Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Update", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_NonExistentReasonCode_ReturnsOrganNotFoundError()
    {
        var req = new Mst_FinishedContractReason_EditReq
        {
            ContractFinishReasonCode = "NON_EXISTENT",
            ContractFinishReasonName = "Tên không tồn tại"
        };
        var result = await _controller.Edit(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
        var p = doc.RootElement.GetProperty("Params");
        Assert.Equal("1001", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("NON_EXISTENT", p.GetProperty("Check.ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Edit_InvalidFinishType_ReturnsInvalidFinishTypeError()
    {
        var req = new Mst_FinishedContractReason_EditReq
        {
            ContractFinishReasonCode = "REASON01",
            ContractFinishReasonName = "Hết hạn",
            FinishType = "INVALID_TYPE"
        };
        var result = await _controller.Edit(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Invalid_FinishType", doc.RootElement.GetProperty("ErrorCode").GetString());
        var p = doc.RootElement.GetProperty("Params");
        Assert.Equal("1001", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("REASON01", p.GetProperty("Check.ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Edit_ValidItem_UpdatesAndReturnsSuccess()
    {
        var req = new Mst_FinishedContractReason_EditReq
        {
            ContractFinishReasonCode = "REASON01",
            ContractFinishReasonName = "Hết hạn hợp đồng (Đã cập nhật)",
            FinishType = "STOPPED",
            FinishDescription = "Mô tả cập nhật mới",
            FlagActive = "1"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("REASON01", data.GetProperty("ContractFinishReasonCode").GetString());
        Assert.Equal("Hết hạn hợp đồng (Đã cập nhật)", data.GetProperty("ContractFinishReasonName").GetString());
        Assert.Equal("STOPPED", data.GetProperty("FinishType").GetString());
        Assert.Equal("Mô tả cập nhật mới", data.GetProperty("FinishDescription").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());

        // Kiểm tra round-trip đọc trực tiếp từ DB
        var saved = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON01");
        Assert.NotNull(saved);
        Assert.Equal("Hết hạn hợp đồng (Đã cập nhật)", saved.ContractFinishReasonName);
        Assert.Equal("STOPPED", saved.FinishType);
        Assert.Equal("Mô tả cập nhật mới", saved.FinishDescription);
        Assert.Equal("1", saved.FlagActive);
    }

    [Fact]
    public async Task Edit_ViaModelProperty_UpdatesSuccessfully()
    {
        var req = new Mst_FinishedContractReason_EditReq
        {
            Model = JsonSerializer.Serialize(new Mst_FinishedContractReason_EditReq
            {
                ContractFinishReasonCode = "REASON02",
                ContractFinishReasonName = "Hai bên thỏa thuận (Cập nhật qua JSON Model)",
                FinishType = "FINISHED",
                FinishDescription = "Mô tả qua model",
                FlagActive = "0"
            })
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var saved = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON02");
        Assert.NotNull(saved);
        Assert.Equal("Hai bên thỏa thuận (Cập nhật qua JSON Model)", saved.ContractFinishReasonName);
        Assert.Equal("0", saved.FlagActive);
    }

    [Fact]
    public async Task Edit_WithFtColsUpd_UpdatesOnlySpecifiedColumns()
    {
        var req = new Mst_FinishedContractReason_EditReq
        {
            ContractFinishReasonCode = "REASON03",
            ContractFinishReasonName = "Tên mới chỉ định",
            FinishDescription = "Mô tả không được cập nhật",
            Ft_Cols_Upd = "Mst_FinishedContractReason.ContractFinishReasonName,"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var saved = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON03");
        Assert.NotNull(saved);
        Assert.Equal("Tên mới chỉ định", saved.ContractFinishReasonName);
        // FinishDescription giữ nguyên giá trị cũ từ Seeder
        Assert.Equal("Đơn phương chấm dứt hợp đồng", saved.FinishDescription);
    }

    [Fact]
    public async Task Delete_EmptyOrNullBody_ReturnsBadRequestWithStrJsonError()
    {
        var resultNull = await _controller.Delete(null);
        var badNull = Assert.IsType<BadRequestObjectResult>(resultNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(badNull.Value));
        Assert.False(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Delete", docNull.RootElement.GetProperty("ErrorCode").GetString());
        Assert.True(docNull.RootElement.GetProperty("Params").TryGetProperty("Check.strJson", out _));

        var resultEmpty = await _controller.Delete(new Mst_FinishedContractReason_DeleteReq());
        var badEmpty = Assert.IsType<BadRequestObjectResult>(resultEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(badEmpty.Value));
        Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Delete", docEmpty.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_NonExistentReasonCode_ReturnsOrganNotFoundError()
    {
        var req = new Mst_FinishedContractReason_DeleteReq
        {
            ContractFinishReasonCode = "NON_EXISTENT"
        };
        var result = await _controller.Delete(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
        var p = doc.RootElement.GetProperty("Params");
        Assert.Equal("1001", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("NON_EXISTENT", p.GetProperty("Check.ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Delete_WithNetworkIDFilter_NonMatchingNetworkID_ReturnsOrganNotFoundError()
    {
        var req = new Mst_FinishedContractReason_DeleteReq
        {
            NetworkID = "1002",
            ContractFinishReasonCode = "REASON01" // REASON01 thuộc NetworkID 1001
        };
        var result = await _controller.Delete(req);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
        var p = doc.RootElement.GetProperty("Params");
        Assert.Equal("1002", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("REASON01", p.GetProperty("Check.ContractFinishReasonCode").GetString());
    }

    [Fact]
    public async Task Delete_ValidItem_DeletesAndReturnsSuccess()
    {
        // Kiểm tra tồn tại trước khi xóa
        var existing = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON04");
        Assert.NotNull(existing);

        var req = new Mst_FinishedContractReason_DeleteReq
        {
            ContractFinishReasonCode = "REASON04"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Xoá nguyên nhân chấm dứt thành công!", doc.RootElement.GetProperty("Title").GetString());

        // Round-trip: Kiểm tra đã xóa khỏi DB
        var afterDelete = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON04");
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task Delete_WithReasonCodeAlias_DeletesSuccessfully()
    {
        var req = new Mst_FinishedContractReason_DeleteReq
        {
            ReasonCode = "REASON03"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var afterDelete = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractFinishReasonCode == "REASON03");
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task Delete_ViaModelProperty_DeletesSuccessfully()
    {
        var req = new Mst_FinishedContractReason_DeleteReq
        {
            Model = JsonSerializer.Serialize(new Mst_FinishedContractReason_DeleteReq
            {
                NetworkID = "1002",
                ContractFinishReasonCode = "REASON05"
            })
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var afterDelete = await _db.Set<Mst_FinishedContractReason>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1002" && x.ContractFinishReasonCode == "REASON05");
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task RealServer_Verification_Port8585()
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
        var freePort = FreePort.Get(); psi.Environment["PORT"] = freePort.ToString();
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

            // Request 1: POST empty body -> 200 OK, returns all 5 seeded records
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_finishedcontractreason/index", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ1: {body1}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(5, data1.GetProperty("ItemCount").GetInt32());
            Assert.Equal(5, data1.GetProperty("DataList").GetArrayLength());

            // Request 2: POST Filter NetworkID = 1001 -> 200 OK, returns 4 records
            var req2 = new StringContent("{\"NetworkID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_finishedcontractreason/index", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ2: {body2}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(4, data2.GetProperty("ItemCount").GetInt32());
            Assert.Equal(4, data2.GetProperty("DataList").GetArrayLength());

            // Request 3: POST Filter NetworkID = 1002 -> 200 OK, returns 1 record ("REASON05")
            var req3 = new StringContent("{\"NetworkID\":\"1002\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_finishedcontractreason/index", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ3: {body3}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(1, data3.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data3.GetProperty("DataList").GetArrayLength());
            Assert.Equal("REASON05", data3.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());

            // Request 4: POST Filter FlagActive = 0 -> 200 OK, returns 1 record ("REASON04")
            var req4 = new StringContent("{\"FlagActive\":\"0\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_finishedcontractreason/index", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ4: {body4}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(1, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal("REASON04", data4.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());

            // Request 5: POST Pagination Page = 0, RecordCount = 2 -> 200 OK, returns 2 records, PageCount = 3
            var req5 = new StringContent("{\"Page\":0,\"RecordCount\":2}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_finishedcontractreason/index", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ5: {body5}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var doc5 = JsonDocument.Parse(body5);
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal(5, data5.GetProperty("ItemCount").GetInt32());
            Assert.Equal(3, data5.GetProperty("PageCount").GetInt32());
            Assert.Equal(0, data5.GetProperty("PageIndex").GetInt32());
            Assert.Equal(2, data5.GetProperty("PageSize").GetInt32());
            Assert.Equal(2, data5.GetProperty("DataList").GetArrayLength());

            // ================== Kiểm thử endpoint Create ==================
            // Request 6: POST Create empty body -> 400 Bad Request, Mst_FinishedContractReason_Create
            var req6 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_finishedcontractreason/create", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ6: {body6}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            Assert.False(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Create", doc6.RootElement.GetProperty("ErrorCode").GetString());

            // Request 7: POST Create duplicate key -> 400 Bad Request, Mst_FinishedContractReason_CheckDB_OrganExist
            var req7 = new StringContent("{\"NetworkID\":\"1001\",\"ContractFinishReasonCode\":\"REASON01\",\"ContractFinishReasonName\":\"Trùng lặp\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_finishedcontractreason/create", req7);
            var body7 = await res7.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ7: {body7}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res7.StatusCode);
            var doc7 = JsonDocument.Parse(body7);
            Assert.False(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganExist", doc7.RootElement.GetProperty("ErrorCode").GetString());

            // Request 8: POST Create invalid NetworkID -> 400 Bad Request, Mst_FinishedContractReason_Create_InvalidNetworkID
            var req8 = new StringContent("{\"NetworkID\":\"\",\"ContractFinishReasonCode\":\"REASON_INV\",\"ContractFinishReasonName\":\"Lỗi Network\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_finishedcontractreason/create", req8);
            var body8 = await res8.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ8: {body8}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res8.StatusCode);
            var doc8 = JsonDocument.Parse(body8);
            Assert.False(doc8.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Create_InvalidNetworkID", doc8.RootElement.GetProperty("ErrorCode").GetString());

            // Request 9: POST Create valid item -> 200 OK round-trip
            var req9 = new StringContent("{\"NetworkID\":\"1001\",\"ContractFinishReasonCode\":\"REASON_LIVE\",\"ContractFinishReasonName\":\"Lý do trực tiếp live\",\"FinishDescription\":\"Mô tả chạy live\"}", Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/mst_finishedcontractreason/create", req9);
            var body9 = await res9.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ9: {body9}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res9.StatusCode);
            var doc9 = JsonDocument.Parse(body9);
            Assert.True(doc9.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON_LIVE", doc9.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

            // Request 10: Query lại bản ghi vừa tạo qua /api/mst_finishedcontractreason/index
            var req10 = new StringContent("{\"ContractFinishReasonCode\":\"REASON_LIVE\"}", Encoding.UTF8, "application/json");
            var res10 = await http.PostAsync("/api/mst_finishedcontractreason/index", req10);
            var body10 = await res10.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ10: {body10}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res10.StatusCode);
            var doc10 = JsonDocument.Parse(body10);
            var data10 = doc10.RootElement.GetProperty("Data");
            Assert.Equal(1, data10.GetProperty("ItemCount").GetInt32());
            Assert.Equal("REASON_LIVE", data10.GetProperty("DataList")[0].GetProperty("ContractFinishReasonCode").GetString());
            Assert.Equal("Lý do trực tiếp live", data10.GetProperty("DataList")[0].GetProperty("ContractFinishReasonName").GetString());
            Assert.Equal("FINISHED", data10.GetProperty("DataList")[0].GetProperty("FinishType").GetString());
            Assert.Equal("1", data10.GetProperty("DataList")[0].GetProperty("FlagActive").GetString());

            // ================== Kiểm thử endpoint Update ==================
            // Request 11: POST Update empty body -> 200 OK, trả về bản ghi đầu tiên ("REASON01")
            var req11 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res11 = await http.PostAsync("/api/mst_finishedcontractreason/update", req11);
            var body11 = await res11.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ11: {body11}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res11.StatusCode);
            var doc11 = JsonDocument.Parse(body11);
            Assert.True(doc11.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON01", doc11.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

            // Request 12: POST Update với ContractFinishReasonCode = REASON02 -> 200 OK, trả về REASON02
            var req12 = new StringContent("{\"ContractFinishReasonCode\":\"REASON02\"}", Encoding.UTF8, "application/json");
            var res12 = await http.PostAsync("/api/mst_finishedcontractreason/update", req12);
            var body12 = await res12.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ12: {body12}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res12.StatusCode);
            var doc12 = JsonDocument.Parse(body12);
            Assert.True(doc12.RootElement.GetProperty("Success").GetBoolean());
            var data12 = doc12.RootElement.GetProperty("Data");
            Assert.Equal("REASON02", data12.GetProperty("ContractFinishReasonCode").GetString());
            Assert.Equal("Hai bên thỏa thuận chấm dứt", data12.GetProperty("ContractFinishReasonName").GetString());

            // Request 13: POST Update với alias ReasonCode = REASON03 -> 200 OK, trả về REASON03
            var req13 = new StringContent("{\"ReasonCode\":\"REASON03\"}", Encoding.UTF8, "application/json");
            var res13 = await http.PostAsync("/api/mst_finishedcontractreason/update", req13);
            var body13 = await res13.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ13: {body13}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res13.StatusCode);
            var doc13 = JsonDocument.Parse(body13);
            Assert.True(doc13.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON03", doc13.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

            // Request 14: POST Update với mã không tồn tại -> 200 OK, trả về Data null
            var req14 = new StringContent("{\"ContractFinishReasonCode\":\"NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res14 = await http.PostAsync("/api/mst_finishedcontractreason/update", req14);
            var body14 = await res14.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ14: {body14}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res14.StatusCode);
            var doc14 = JsonDocument.Parse(body14);
            Assert.True(doc14.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc14.RootElement.GetProperty("Data").ValueKind);

            // Request 15: POST Update với NetworkID = 1002 và REASON05 -> 200 OK, tìm thấy
            var req15 = new StringContent("{\"NetworkID\":\"1002\",\"ContractFinishReasonCode\":\"REASON05\"}", Encoding.UTF8, "application/json");
            var res15 = await http.PostAsync("/api/mst_finishedcontractreason/update", req15);
            var body15 = await res15.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ15: {body15}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res15.StatusCode);
            var doc15 = JsonDocument.Parse(body15);
            Assert.True(doc15.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON05", doc15.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

            // Request 16: POST Update với NetworkID = 1001 và REASON05 -> 200 OK, không khớp NetworkID -> Data null
            var req16 = new StringContent("{\"NetworkID\":\"1001\",\"ContractFinishReasonCode\":\"REASON05\"}", Encoding.UTF8, "application/json");
            var res16 = await http.PostAsync("/api/mst_finishedcontractreason/update", req16);
            var body16 = await res16.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ16: {body16}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res16.StatusCode);
            var doc16 = JsonDocument.Parse(body16);
            Assert.True(doc16.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc16.RootElement.GetProperty("Data").ValueKind);

            // ================== Kiểm thử endpoint Edit ==================
            // Request 17: POST Edit empty body -> 400 Bad Request, Mst_FinishedContractReason_Update
            var req17 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res17 = await http.PostAsync("/api/mst_finishedcontractreason/edit", req17);
            var body17 = await res17.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ17: {body17}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res17.StatusCode);
            var doc17 = JsonDocument.Parse(body17);
            Assert.False(doc17.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Update", doc17.RootElement.GetProperty("ErrorCode").GetString());

            // Request 18: POST Edit non-existent code -> 400 Bad Request, Mst_FinishedContractReason_CheckDB_OrganNotFound
            var req18 = new StringContent("{\"ContractFinishReasonCode\":\"NON_EXISTENT\",\"ContractFinishReasonName\":\"Tên không có\"}", Encoding.UTF8, "application/json");
            var res18 = await http.PostAsync("/api/mst_finishedcontractreason/edit", req18);
            var body18 = await res18.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ18: {body18}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res18.StatusCode);
            var doc18 = JsonDocument.Parse(body18);
            Assert.False(doc18.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganNotFound", doc18.RootElement.GetProperty("ErrorCode").GetString());

            // Request 19: POST Edit invalid FinishType -> 400 Bad Request, Mst_FinishedContractReason_Invalid_FinishType
            var req19 = new StringContent("{\"ContractFinishReasonCode\":\"REASON01\",\"ContractFinishReasonName\":\"Hết hạn\",\"FinishType\":\"INVALID_TYPE\"}", Encoding.UTF8, "application/json");
            var res19 = await http.PostAsync("/api/mst_finishedcontractreason/edit", req19);
            var body19 = await res19.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ19: {body19}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res19.StatusCode);
            var doc19 = JsonDocument.Parse(body19);
            Assert.False(doc19.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Invalid_FinishType", doc19.RootElement.GetProperty("ErrorCode").GetString());

            // Request 20: POST Edit valid item -> 200 OK
            var req20 = new StringContent("{\"ContractFinishReasonCode\":\"REASON01\",\"ContractFinishReasonName\":\"Hết hạn cập nhật live\",\"FinishType\":\"STOPPED\",\"FinishDescription\":\"Mô tả live\",\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var res20 = await http.PostAsync("/api/mst_finishedcontractreason/edit", req20);
            var body20 = await res20.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ20: {body20}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res20.StatusCode);
            var doc20 = JsonDocument.Parse(body20);
            Assert.True(doc20.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON01", doc20.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());
            Assert.Equal("Hết hạn cập nhật live", doc20.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonName").GetString());
            Assert.Equal("STOPPED", doc20.RootElement.GetProperty("Data").GetProperty("FinishType").GetString());

            // Request 21: Query lại qua /api/mst_finishedcontractreason/update để kiểm tra round-trip dữ liệu đã lưu
            var req21 = new StringContent("{\"ContractFinishReasonCode\":\"REASON01\"}", Encoding.UTF8, "application/json");
            var res21 = await http.PostAsync("/api/mst_finishedcontractreason/update", req21);
            var body21 = await res21.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ21: {body21}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res21.StatusCode);
            var doc21 = JsonDocument.Parse(body21);
            Assert.True(doc21.RootElement.GetProperty("Success").GetBoolean());
            var data21 = doc21.RootElement.GetProperty("Data");
            Assert.Equal("REASON01", data21.GetProperty("ContractFinishReasonCode").GetString());
            Assert.Equal("Hết hạn cập nhật live", data21.GetProperty("ContractFinishReasonName").GetString());
            Assert.Equal("STOPPED", data21.GetProperty("FinishType").GetString());
            Assert.Equal("Mô tả live", data21.GetProperty("FinishDescription").GetString());
            Assert.Equal("1", data21.GetProperty("FlagActive").GetString());

            // ================== Kiểm thử endpoint Detail ==================
            // Request 22: POST Detail empty body -> 200 OK, trả về Data null (gate trả View(new Mst_FinishedContractReason()))
            var req22 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res22 = await http.PostAsync("/api/mst_finishedcontractreason/detail", req22);
            var body22 = await res22.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ22: {body22}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res22.StatusCode);
            var doc22 = JsonDocument.Parse(body22);
            Assert.True(doc22.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc22.RootElement.GetProperty("Data").ValueKind);

            // Request 23: POST Detail với ContractFinishReasonCode = REASON01 -> 200 OK, trả về REASON01
            var req23 = new StringContent("{\"ContractFinishReasonCode\":\"REASON01\"}", Encoding.UTF8, "application/json");
            var res23 = await http.PostAsync("/api/mst_finishedcontractreason/detail", req23);
            var body23 = await res23.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ23: {body23}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res23.StatusCode);
            var doc23 = JsonDocument.Parse(body23);
            Assert.True(doc23.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON01", doc23.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());
            Assert.Equal("Hết hạn cập nhật live", doc23.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonName").GetString());
            Assert.Equal("STOPPED", doc23.RootElement.GetProperty("Data").GetProperty("FinishType").GetString());

            // Request 24: POST Detail với alias ReasonCode = REASON02 -> 200 OK, trả về REASON02
            var req24 = new StringContent("{\"ReasonCode\":\"REASON02\"}", Encoding.UTF8, "application/json");
            var res24 = await http.PostAsync("/api/mst_finishedcontractreason/detail", req24);
            var body24 = await res24.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ24: {body24}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res24.StatusCode);
            var doc24 = JsonDocument.Parse(body24);
            Assert.True(doc24.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON02", doc24.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

            // Request 25: POST Detail với mã không tồn tại -> 200 OK, trả về Data null
            var req25 = new StringContent("{\"ContractFinishReasonCode\":\"NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res25 = await http.PostAsync("/api/mst_finishedcontractreason/detail", req25);
            var body25 = await res25.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ25: {body25}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res25.StatusCode);
            var doc25 = JsonDocument.Parse(body25);
            Assert.True(doc25.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc25.RootElement.GetProperty("Data").ValueKind);

            // Request 26: POST Detail với NetworkID = 1002 và REASON05 -> 200 OK, tìm thấy
            var req26 = new StringContent("{\"NetworkID\":\"1002\",\"ContractFinishReasonCode\":\"REASON05\"}", Encoding.UTF8, "application/json");
            var res26 = await http.PostAsync("/api/mst_finishedcontractreason/detail", req26);
            var body26 = await res26.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ26: {body26}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res26.StatusCode);
            var doc26 = JsonDocument.Parse(body26);
            Assert.True(doc26.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("REASON05", doc26.RootElement.GetProperty("Data").GetProperty("ContractFinishReasonCode").GetString());

            // Request 27: POST Detail với NetworkID = 1001 và REASON05 -> 200 OK, không khớp NetworkID -> Data null
            var req27 = new StringContent("{\"NetworkID\":\"1001\",\"ContractFinishReasonCode\":\"REASON05\"}", Encoding.UTF8, "application/json");
            var res27 = await http.PostAsync("/api/mst_finishedcontractreason/detail", req27);
            var body27 = await res27.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ27: {body27}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res27.StatusCode);
            var doc27 = JsonDocument.Parse(body27);
            Assert.True(doc27.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc27.RootElement.GetProperty("Data").ValueKind);

            // ================== Kiểm thử endpoint Delete ==================
            // Request 28: POST Delete empty body -> 400 Bad Request, Mst_FinishedContractReason_Delete
            var req28 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res28 = await http.PostAsync("/api/mst_finishedcontractreason/delete", req28);
            var body28 = await res28.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ28: {body28}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res28.StatusCode);
            var doc28 = JsonDocument.Parse(body28);
            Assert.False(doc28.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_Delete", doc28.RootElement.GetProperty("ErrorCode").GetString());

            // Request 29: POST Delete non-existent code -> 400 Bad Request, Mst_FinishedContractReason_CheckDB_OrganNotFound
            var req29 = new StringContent("{\"ContractFinishReasonCode\":\"NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res29 = await http.PostAsync("/api/mst_finishedcontractreason/delete", req29);
            var body29 = await res29.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ29: {body29}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res29.StatusCode);
            var doc29 = JsonDocument.Parse(body29);
            Assert.False(doc29.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganNotFound", doc29.RootElement.GetProperty("ErrorCode").GetString());

            // Request 30: POST Delete valid item (REASON_LIVE vừa tạo ở REQ9) -> 200 OK
            var req30 = new StringContent("{\"ContractFinishReasonCode\":\"REASON_LIVE\"}", Encoding.UTF8, "application/json");
            var res30 = await http.PostAsync("/api/mst_finishedcontractreason/delete", req30);
            var body30 = await res30.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ30: {body30}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res30.StatusCode);
            var doc30 = JsonDocument.Parse(body30);
            Assert.True(doc30.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xoá nguyên nhân chấm dứt thành công!", doc30.RootElement.GetProperty("Title").GetString());

            // Request 31: Query lại REASON_LIVE qua index -> 0 bản ghi (round-trip xóa thành công)
            var req31 = new StringContent("{\"ContractFinishReasonCode\":\"REASON_LIVE\"}", Encoding.UTF8, "application/json");
            var res31 = await http.PostAsync("/api/mst_finishedcontractreason/index", req31);
            var body31 = await res31.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ31: {body31}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res31.StatusCode);
            var doc31 = JsonDocument.Parse(body31);
            Assert.Equal(0, doc31.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // Request 32: POST Delete lại REASON_LIVE -> 400 Bad Request, OrganNotFound
            var req32 = new StringContent("{\"ContractFinishReasonCode\":\"REASON_LIVE\"}", Encoding.UTF8, "application/json");
            var res32 = await http.PostAsync("/api/mst_finishedcontractreason/delete", req32);
            var body32 = await res32.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ32: {body32}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res32.StatusCode);
            var doc32 = JsonDocument.Parse(body32);
            Assert.False(doc32.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_FinishedContractReason_CheckDB_OrganNotFound", doc32.RootElement.GetProperty("ErrorCode").GetString());
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
