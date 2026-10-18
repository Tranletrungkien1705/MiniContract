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
    public async Task Edit_NullBody_ReturnsCheckStrJson()
    {
        var result = await _controller.Edit(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Update", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strJson", out _));
    }

    [Fact]
    public async Task Edit_EmptyBody_ReturnsCheckStrJson()
    {
        var result = await _controller.Edit(new Mst_ContractType_EditReq());
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Update", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_InvalidNetworkID_ReturnsCheckStrNetworkID()
    {
        var req = new Mst_ContractType_EditReq
        {
            NetworkID = "",
            ContractType = "HD_KT",
            ContractTypeName = "Hợp đồng kinh tế sửa"
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Update", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strNetworkID", out _));
    }

    [Fact]
    public async Task Edit_EmptyContractType_ReturnsCheckContractType()
    {
        var req = new Mst_ContractType_EditReq
        {
            NetworkID = "1001",
            ContractType = "",
            ContractTypeName = "Hợp đồng kinh tế sửa"
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Update", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.ContractType", out _));
    }

    [Fact]
    public async Task Edit_NonExistentContractType_ReturnsOrganNotFound()
    {
        var req = new Mst_ContractType_EditReq
        {
            NetworkID = "1001",
            ContractType = "NOT_EXIST",
            ContractTypeName = "Không tồn tại"
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("1001", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("NOT_EXIST", p.GetProperty("Check.ContractType").GetString());
    }

    [Fact]
    public async Task Edit_Success_RoundTrip()
    {
        var req = new Mst_ContractType_EditReq
        {
            NetworkID = "1001",
            ContractType = "HD_KT",
            ContractTypeName = "Hợp đồng kinh tế - Cập nhật mới",
            Description = "Mô tả mới đã cập nhật",
            FlagActive = "0"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/Mst_ContractType", root.GetProperty("RedirectUrl").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("HD_KT", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng kinh tế - Cập nhật mới", data.GetProperty("ContractTypeName").GetString());
        Assert.Equal("Mô tả mới đã cập nhật", data.GetProperty("Description").GetString());
        Assert.Equal("0", data.GetProperty("FlagActive").GetString());
        Assert.Equal("port", data.GetProperty("LogLUBy").GetString());

        // Kiểm tra đọc lại trực tiếp từ DB
        var updated = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_KT");
        Assert.NotNull(updated);
        Assert.Equal("Hợp đồng kinh tế - Cập nhật mới", updated.ContractTypeName);
        Assert.Equal("Mô tả mới đã cập nhật", updated.Description);
        Assert.Equal("0", updated.FlagActive);

        // Kiểm tra đọc lại qua Update
        var updRes = await _controller.Update(new Mst_ContractType_UpdateReq { ContractType = "HD_KT" });
        var okUpd = Assert.IsType<OkObjectResult>(updRes);
        var docUpd = JsonDocument.Parse(JsonSerializer.Serialize(okUpd.Value));
        Assert.Equal("Hợp đồng kinh tế - Cập nhật mới", docUpd.RootElement.GetProperty("Data").GetProperty("ContractTypeName").GetString());
    }

    [Fact]
    public async Task Edit_WithModelString_Success()
    {
        var req = new Mst_ContractType_EditReq
        {
            Model = "{\"NetworkID\":\"1001\",\"ContractType\":\"HD_LD\",\"ContractTypeName\":\"Hợp đồng lao động - Qua Model JSON\",\"Description\":\"Cập nhật qua chuỗi Model\"}"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("HD_LD", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng lao động - Qua Model JSON", data.GetProperty("ContractTypeName").GetString());

        var updated = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_LD");
        Assert.NotNull(updated);
        Assert.Equal("Hợp đồng lao động - Qua Model JSON", updated.ContractTypeName);
    }

    [Fact]
    public async Task Edit_PartialUpdate_WithFtColsUpd()
    {
        var req = new Mst_ContractType_EditReq
        {
            NetworkID = "1001",
            ContractType = "HD_DV",
            ContractTypeName = "Hợp đồng dịch vụ - Chỉ sửa tên",
            Description = "Mô tả này không được lưu",
            Ft_Cols_Upd = "Mst_ContractType.ContractTypeName"
        };
        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_DV");
        Assert.NotNull(updated);
        Assert.Equal("Hợp đồng dịch vụ - Chỉ sửa tên", updated.ContractTypeName);
        // Mô tả vẫn giữ nguyên từ seed
        Assert.Equal("Hợp đồng cung cấp dịch vụ", updated.Description);
    }

    [Fact]
    public async Task Detail_EmptyBodyOrNull_ReturnsNullData()
    {
        var resNull = await _controller.Detail(null);
        var okNull = Assert.IsType<OkObjectResult>(resNull);
        var docNull = JsonDocument.Parse(JsonSerializer.Serialize(okNull.Value));
        Assert.True(docNull.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docNull.RootElement.GetProperty("Data").ValueKind);

        var resEmpty = await _controller.Detail(new Mst_ContractType_DetailReq());
        var okEmpty = Assert.IsType<OkObjectResult>(resEmpty);
        var docEmpty = JsonDocument.Parse(JsonSerializer.Serialize(okEmpty.Value));
        Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, docEmpty.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ValidContractType_ReturnsMatchingItem()
    {
        var req = new Mst_ContractType_DetailReq { ContractType = "HD_KT" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
        Assert.Equal("HD_KT", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng kinh tế", data.GetProperty("ContractTypeName").GetString());
        Assert.Equal("Hợp đồng kinh tế thương mại", data.GetProperty("Description").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());
    }

    [Fact]
    public async Task Detail_ValidContractType_NetworkID1002_ReturnsItem()
    {
        // 2 bộ lọc ra 2 kết quả khác nhau: HD_VAY thuộc NetworkID 1002
        var req = new Mst_ContractType_DetailReq { ContractType = "HD_VAY" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("1002", data.GetProperty("NetworkID").GetString());
        Assert.Equal("HD_VAY", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng vay vốn", data.GetProperty("ContractTypeName").GetString());
    }

    [Fact]
    public async Task Detail_WithLowercaseAlias_ReturnsItem()
    {
        var json = "{\"contracttype\":\"HD_LD\"}";
        var req = JsonSerializer.Deserialize<Mst_ContractType_DetailReq>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("HD_LD", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng lao động", data.GetProperty("ContractTypeName").GetString());
    }

    [Fact]
    public async Task Detail_WithModelString_ReturnsItem()
    {
        var req = new Mst_ContractType_DetailReq
        {
            Model = JsonSerializer.Serialize(new Mst_ContractType_DetailReq { ContractType = "HD_DV" })
        };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal("HD_DV", data.GetProperty("ContractType").GetString());
        Assert.Equal("Hợp đồng dịch vụ", data.GetProperty("ContractTypeName").GetString());
    }

    [Fact]
    public async Task Detail_NonExistentContractType_ReturnsNullData()
    {
        var req = new Mst_ContractType_DetailReq { ContractType = "NOT_EXIST" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_NetworkIDFilterMismatch_ReturnsNullData()
    {
        // NetworkID = 1002 và HD_VAY -> tìm thấy
        var req1 = new Mst_ContractType_DetailReq { NetworkID = "1002", ContractType = "HD_VAY" };
        var res1 = await _controller.Detail(req1);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("HD_VAY", doc1.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());

        // NetworkID = 1001 và HD_VAY -> không khớp NetworkID -> Data null
        var req2 = new Mst_ContractType_DetailReq { NetworkID = "1001", ContractType = "HD_VAY" };
        var res2 = await _controller.Detail(req2);
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Delete_NullBody_ReturnsCheckStrJson()
    {
        var result = await _controller.Delete(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Delete", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Check.strJson", out _));
    }

    [Fact]
    public async Task Delete_EmptyBody_ReturnsCheckStrJson()
    {
        var result = await _controller.Delete(new Mst_ContractType_DeleteReq());
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Delete", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_NonExistentContractType_ReturnsOrganNotFound()
    {
        var req = new Mst_ContractType_DeleteReq
        {
            NetworkID = "1001",
            ContractType = "NOT_EXIST"
        };
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("1001", p.GetProperty("Check.NetworkID").GetString());
        Assert.Equal("NOT_EXIST", p.GetProperty("Check.ContractType").GetString());
    }

    [Fact]
    public async Task Delete_UsedInTempContract_ReturnsContractTypeUsed()
    {
        // Tạo bảng Contract_TempContract tạm trên connection test và chèn dòng mẫu
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Contract_TempContract (
                    TContractCode TEXT PRIMARY KEY,
                    ContractType TEXT
                );
                INSERT INTO Contract_TempContract (TContractCode, ContractType) VALUES ('T001', 'HD_KT');
            ";
            cmd.ExecuteNonQuery();
        }

        try
        {
            var req = new Mst_ContractType_DeleteReq
            {
                NetworkID = "1001",
                ContractType = "HD_KT"
            };
            var result = await _controller.Delete(req);
            var badResult = Assert.IsType<BadRequestObjectResult>(result);

            var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
            var root = doc.RootElement;

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Delete_ContractTypeUsed", root.GetProperty("ErrorCode").GetString());
            var p = root.GetProperty("Params");
            Assert.Equal("T001", p.GetProperty("Check.TContractCode").GetString());
            Assert.Equal("HD_KT", p.GetProperty("Check.ContractType").GetString());
            Assert.Equal(1, p.GetProperty("Check.TContractCount").GetInt32());
        }
        finally
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "DROP TABLE IF EXISTS Contract_TempContract;";
                cmd.ExecuteNonQuery();
            }
        }
    }

    [Fact]
    public async Task Delete_UsedInContractParty_ReturnsContractTypeUsed()
    {
        // Tạo bảng Contract_ContractParty tạm trên connection test và chèn dòng mẫu
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Contract_ContractParty (
                    ContractCode TEXT PRIMARY KEY,
                    ContractType TEXT
                );
                INSERT INTO Contract_ContractParty (ContractCode, ContractType) VALUES ('C001', 'HD_LD');
            ";
            cmd.ExecuteNonQuery();
        }

        try
        {
            var req = new Mst_ContractType_DeleteReq
            {
                NetworkID = "1001",
                ContractType = "HD_LD"
            };
            var result = await _controller.Delete(req);
            var badResult = Assert.IsType<BadRequestObjectResult>(result);

            var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
            var root = doc.RootElement;

            Assert.False(root.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Delete_ContractTypeUsed", root.GetProperty("ErrorCode").GetString());
            var p = root.GetProperty("Params");
            Assert.Equal("C001", p.GetProperty("Check.TContractCode").GetString());
            Assert.Equal("HD_LD", p.GetProperty("Check.ContractType").GetString());
            Assert.Equal(1, p.GetProperty("Check.TContractCount").GetInt32());
        }
        finally
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "DROP TABLE IF EXISTS Contract_ContractParty;";
                cmd.ExecuteNonQuery();
            }
        }
    }

    [Fact]
    public async Task Delete_Success_RoundTrip()
    {
        // 1. Tạo mới bản ghi
        var createReq = new Mst_ContractType_CreateReq
        {
            NetworkID = "1001",
            ContractType = "HD_TO_DEL",
            ContractTypeName = "Hợp đồng sẽ xóa",
            Description = "Chuẩn bị xóa"
        };
        var crRes = await _controller.Create(createReq);
        Assert.IsType<OkObjectResult>(crRes);

        var saved = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_TO_DEL");
        Assert.NotNull(saved);

        // 2. Gọi Delete
        var delReq = new Mst_ContractType_DeleteReq
        {
            NetworkID = "1001",
            ContractType = "HD_TO_DEL"
        };
        var delRes = await _controller.Delete(delReq);
        var okResult = Assert.IsType<OkObjectResult>(delRes);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("/Mst_ContractType", root.GetProperty("RedirectUrl").GetString());
        Assert.Equal("Xoá loại hợp đồng thành công!", root.GetProperty("Messages")[0].GetString());

        // 3. Kiểm tra đọc lại trực tiếp từ DB
        var afterDel = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_TO_DEL");
        Assert.Null(afterDel);

        // 4. Kiểm tra đọc lại qua Index
        var indexRes = await _controller.Index(new Mst_ContractType_IndexReq { ContractType = "HD_TO_DEL" });
        var okIndex = Assert.IsType<OkObjectResult>(indexRes);
        var docIndex = JsonDocument.Parse(JsonSerializer.Serialize(okIndex.Value));
        Assert.Equal(0, docIndex.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Delete_WithModelString_Success()
    {
        // Tạo mới bản ghi
        await _controller.Create(new Mst_ContractType_CreateReq
        {
            NetworkID = "1001",
            ContractType = "HD_DEL_JSON",
            ContractTypeName = "Xóa qua JSON"
        });

        // Xóa qua chuỗi Model
        var delReq = new Mst_ContractType_DeleteReq
        {
            Model = "{\"NetworkID\":\"1001\",\"ContractType\":\"HD_DEL_JSON\"}"
        };
        var delRes = await _controller.Delete(delReq);
        var okResult = Assert.IsType<OkObjectResult>(delRes);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var afterDel = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_DEL_JSON");
        Assert.Null(afterDel);
    }

    [Fact]
    public async Task Delete_WithLowercaseAlias_Success()
    {
        // Tạo mới bản ghi
        await _controller.Create(new Mst_ContractType_CreateReq
        {
            NetworkID = "1001",
            ContractType = "HD_DEL_ALIAS",
            ContractTypeName = "Xóa qua Alias"
        });

        // Xóa qua contracttype viết thường
        var json = "{\"contracttype\":\"HD_DEL_ALIAS\"}";
        var delReq = JsonSerializer.Deserialize<Mst_ContractType_DeleteReq>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var delRes = await _controller.Delete(delReq);
        var okResult = Assert.IsType<OkObjectResult>(delRes);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var afterDel = await _db.Set<Mst_ContractType>().FindAsync("1001", "HD_DEL_ALIAS");
        Assert.Null(afterDel);
    }

    private static IFormFile CreateExcelFormFile(string fileName, string[] headers, List<string?[]> dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
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
        var ws = wb.Worksheets.Add("Sheet1");
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

    [Fact]
    public async Task Import_NullFile_ReturnsBadRequestWithInvalidFileMessage()
    {
        var result = await _controller.Import(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File dữ liệu import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_EmptyFile_ReturnsBadRequestWithInvalidFileMessage()
    {
        var emptyStream = new MemoryStream();
        var file = new FormFile(emptyStream, 0, 0, "file", "test.xlsx");
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File dữ liệu import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
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
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File dữ liệu import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_EmptyDataRows_ReturnsBadRequestWithEmptyDataMessage()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>());
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File Excel nhập không có dữ liệu!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_InvalidColumnCount_ReturnsBadRequestWithInvalidFileMessage()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)" }, new List<string?[]>
        {
            new[] { "HD_BAD_COL", "Tên" }
        });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("File dữ liệu import không hợp lệ!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_RowWithEmptyContractType_ReturnsBadRequestWithEmptyCodeMessage()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>
        {
            new[] { "", "Tên hợp lệ", "Mô tả" }
        });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã loại hợp đồng không được trống!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_RowWithEmptyContractTypeName_ReturnsBadRequestWithEmptyNameMessage()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>
        {
            new[] { "HD_NO_NAME", "  ", "Mô tả" }
        });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Tên loại hợp đồng không được trống!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_DuplicateCodeWithExistingDbRecord_ReturnsBadRequestWithAlreadyExistsMessage()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>
        {
            new[] { "HD_KT", "Trùng mã HD_KT", "Mô tả" }
        });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã loại hợp đồng: 'HD_KT' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Import_DuplicateCodeWithinSameFile_CommitsPreviousRowAndReturnsAlreadyExistsForSecondRow()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>
        {
            new[] { "HD_IMP_DUP", "Tên 1", "Mô tả 1" },
            new[] { "HD_IMP_DUP", "Tên 2 trùng", "Mô tả 2" }
        });
        var result = await _controller.Import(file);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", doc.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("Mã loại hợp đồng: 'HD_IMP_DUP' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!", doc.RootElement.GetProperty("Params").GetProperty("Message").GetString());

        // Xác nhận partial import: dòng 1 đã được lưu vào DB
        var saved = await _db.Set<Mst_ContractType>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractType == "HD_IMP_DUP");
        Assert.NotNull(saved);
        Assert.Equal("Tên 1", saved.ContractTypeName);
    }

    [Fact]
    public async Task Import_ValidFile_ImportsAllRowsAndReturnsSuccess()
    {
        var file = CreateExcelFormFile("test.xlsx", new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>
        {
            new[] { "HD_IMP_1", "Hợp đồng nhập khẩu 1", "Mô tả nhập 1" },
            new[] { "HD_IMP_2", "Hợp đồng nhập khẩu 2", "Mô tả nhập 2" }
        });
        var result = await _controller.Import(file);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã nhập dữ liệu excel thành công", doc.RootElement.GetProperty("Title").GetString());

        var row1 = await _db.Set<Mst_ContractType>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractType == "HD_IMP_1");
        Assert.NotNull(row1);
        Assert.Equal("Hợp đồng nhập khẩu 1", row1.ContractTypeName);
        Assert.Equal("Mô tả nhập 1", row1.Description);
        Assert.Equal("1", row1.FlagActive);

        var row2 = await _db.Set<Mst_ContractType>()
            .FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractType == "HD_IMP_2");
        Assert.NotNull(row2);
        Assert.Equal("Hợp đồng nhập khẩu 2", row2.ContractTypeName);
    }

    [Fact]
    public async Task Export_EmptyBody_ReturnsExcelSuccessWith5Rows()
    {
        var result = await _controller.Export(null);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        var strUrl = root.GetProperty("strUrl").GetString();
        Assert.False(string.IsNullOrWhiteSpace(strUrl));

        // Kiểm tra file vật lý đã được tạo và đọc nội dung
        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath), $"Exported file does not exist at {physicalPath}");

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal("Mst_ContractType", ws.Name);
        Assert.Equal("Mã loại hợp đồng(*)", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên loại hợp đồng(*)", ws.Cell(1, 2).GetString());
        Assert.Equal("Mô tả", ws.Cell(1, 3).GetString());
        Assert.Equal("Trạng thái", ws.Cell(1, 4).GetString());

        // 5 dòng dữ liệu từ seed
        Assert.Equal(6, ws.LastRowUsed()?.RowNumber());
    }

    [Fact]
    public async Task Export_FilterByNetworkID1001_Returns4Rows()
    {
        var req = new Mst_ContractType_ExportReq { NetworkID = "1001" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công", root.GetProperty("Title").GetString());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal(5, ws.LastRowUsed()?.RowNumber()); // 1 header + 4 rows
    }

    [Fact]
    public async Task Export_FilterByNetworkID1002_Returns1Row()
    {
        var req = new Mst_ContractType_ExportReq { NetworkID = "1002" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công", root.GetProperty("Title").GetString());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal(2, ws.LastRowUsed()?.RowNumber()); // 1 header + 1 row
        Assert.Equal("HD_VAY", ws.Cell(2, 1).GetString());
        Assert.Equal("Hợp đồng vay vốn", ws.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task Export_FilterNonExistent_ReturnsCheckFileNullMessage()
    {
        var req = new Mst_ContractType_ExportReq { ContractType = "NOT_EXIST" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("File Excel nhập không có dữ liệu!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        Assert.False(root.TryGetProperty("strUrl", out _));
    }

    [Fact]
    public async Task Export_Pagination_Start0_Count2_Returns2Rows()
    {
        var req = new Mst_ContractType_ExportReq
        {
            Ft_RecordStart = 0,
            Ft_RecordCount = 2
        };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var strUrl = root.GetProperty("strUrl").GetString();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet(1);
        Assert.Equal(3, ws.LastRowUsed()?.RowNumber()); // 1 header + 2 rows
        Assert.Equal("HD_DV", ws.Cell(2, 1).GetString());
        Assert.Equal("HD_KT", ws.Cell(3, 1).GetString());
    }

    [Fact]
    public void ExportTemplate_EmptyBody_ReturnsExcelSuccessWithThreeHeaderColumnsOnly()
    {
        var result = _controller.ExportTemplate(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);
        Assert.EndsWith(".xlsx", strUrl);

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRoot, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_ContractType");
        Assert.NotNull(ws);

        // Tiêu đề: đúng 3 cột từ GetImportDicColums() (Gate:837-845)
        Assert.Equal(3, ws.Row(1).LastCellUsed()?.Address.ColumnNumber);
        Assert.Equal("Mã loại hợp đồng(*)", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên loại hợp đồng(*)", ws.Cell(1, 2).GetString());
        Assert.Equal("Mô tả", ws.Cell(1, 3).GetString());

        // File mẫu rỗng: không có dòng dữ liệu (dòng cuối cùng sử dụng là dòng 1 tiêu đề)
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(1, lastRow);
    }

    [Fact]
    public void ExportTemplate_WithRequestBody_AlsoSucceeds()
    {
        var result = _controller.ExportTemplate(new { arbitrary = "test_value" });
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());

        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.StartsWith("/TempFiles/", strUrl);
    }

    [Fact]
    public void ExportTemplate_WhenExceptionOccurs_ReturnsFailResult()
    {
        // Môi trường lỗi với đường dẫn không hợp lệ khiến việc tạo thư mục/file ném ngoại lệ
        var brokenEnv = new TestWebHostEnvironment { WebRootPath = "Z:\\non_existent_drive_for_test\\" };
        var brokenController = new Mst_ContractTypeControllerPort(_db, null, brokenEnv);

        var result = brokenController.ExportTemplate(null);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badReq.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_ExportTemplate", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("Exception", out _));
    }

    [Fact]
    public async Task ExportTemplate_FillDataAndImport_RoundTripSucceeds()
    {
        // 1. Xuất template
        var exportRes = _controller.ExportTemplate(null);
        var okExport = Assert.IsType<OkObjectResult>(exportRes);
        var docExport = JsonDocument.Parse(JsonSerializer.Serialize(okExport.Value));
        var strUrl = docExport.RootElement.GetProperty("strUrl").GetString()!;
        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var templatePhysical = Path.Combine(webRoot, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        // 2. Mở file template mẫu và điền 1 dòng dữ liệu hợp lệ
        using (var wb = new XLWorkbook(templatePhysical))
        {
            var ws = wb.Worksheet("Mst_ContractType");
            ws.Cell(2, 1).Value = "HD_FROM_TPL";
            ws.Cell(2, 2).Value = "Hợp đồng từ template";
            ws.Cell(2, 3).Value = "Mô tả điền từ template mẫu";
            wb.Save();
        }

        // 3. Nhập file template đã điền dữ liệu qua endpoint Import
        var fileBytes = await File.ReadAllBytesAsync(templatePhysical);
        using var ms = new MemoryStream(fileBytes);
        var formFile = new FormFile(ms, 0, ms.Length, "file", "template_filled.xlsx");

        var importRes = await _controller.Import(formFile);
        var okImport = Assert.IsType<OkObjectResult>(importRes);
        var docImport = JsonDocument.Parse(JsonSerializer.Serialize(okImport.Value));
        Assert.True(docImport.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã nhập dữ liệu excel thành công", docImport.RootElement.GetProperty("Title").GetString());

        // 4. Kiểm tra round-trip: dữ liệu từ file template đã được ghi nhận vào DB
        var saved = await _db.Set<Mst_ContractType>().FirstOrDefaultAsync(x => x.NetworkID == "1001" && x.ContractType == "HD_FROM_TPL");
        Assert.NotNull(saved);
        Assert.Equal("Hợp đồng từ template", saved.ContractTypeName);
        Assert.Equal("Mô tả điền từ template mẫu", saved.Description);
        Assert.Equal("1", saved.FlagActive);
    }

    [Fact]
    public async Task RealServer_Verification_Port8589()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_ct_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        }
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
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort}"), Timeout = TimeSpan.FromSeconds(3) };
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
            _output?.WriteLine($"[Index Filter1 NetworkID=1001] StatusCode={(int)resIdx1.StatusCode} Body={bodyIdx1}");

            // Bộ lọc 2: NetworkID = "1002" -> 1 bản ghi
            var reqIdx2 = new StringContent("{\"NetworkID\":\"1002\"}", Encoding.UTF8, "application/json");
            var resIdx2 = await http.PostAsync("/api/mst_contracttype/index", reqIdx2);
            var bodyIdx2 = await resIdx2.Content.ReadAsStringAsync();
            var docIdx2 = JsonDocument.Parse(bodyIdx2);
            Assert.Equal(System.Net.HttpStatusCode.OK, resIdx2.StatusCode);
            Assert.Equal(1, docIdx2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            _output?.WriteLine($"[Index Filter2 NetworkID=1002] StatusCode={(int)resIdx2.StatusCode} Body={bodyIdx2}");

            // 2. Create: 1 ca guard trả lỗi và 1 ca round-trip ghi
            // Guard: body rỗng -> 400 Bad Request
            var reqCrErr = new StringContent("{}", Encoding.UTF8, "application/json");
            var resCrErr = await http.PostAsync("/api/mst_contracttype/create", reqCrErr);
            var bodyCrErr = await resCrErr.Content.ReadAsStringAsync();
            var docCrErr = JsonDocument.Parse(bodyCrErr);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resCrErr.StatusCode);
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Create", docCrErr.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[Create Guard EmptyBody] StatusCode={(int)resCrErr.StatusCode} Body={bodyCrErr}");

            // Ghi: tạo mới thành công
            var reqCrOk = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"HD_LIVE_TEST\",\"ContractTypeName\":\"Hợp đồng kiểm thử live\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var resCrOk = await http.PostAsync("/api/mst_contracttype/create", reqCrOk);
            var bodyCrOk = await resCrOk.Content.ReadAsStringAsync();
            var docCrOk = JsonDocument.Parse(bodyCrOk);
            Assert.Equal(System.Net.HttpStatusCode.OK, resCrOk.StatusCode);
            Assert.True(docCrOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("HD_LIVE_TEST", docCrOk.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());
            _output?.WriteLine($"[Create Success] StatusCode={(int)resCrOk.StatusCode} Body={bodyCrOk}");

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

            // 4. Edit: 1 ca guard trả lỗi và 1 ca round-trip ghi thành công
            // Ca 4.1: Guard body rỗng -> 400 Bad Request
            var reqEditEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
            var resEditEmpty = await http.PostAsync("/api/mst_contracttype/edit", reqEditEmpty);
            var bodyEditEmpty = await resEditEmpty.Content.ReadAsStringAsync();
            var docEditEmpty = JsonDocument.Parse(bodyEditEmpty);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEditEmpty.StatusCode);
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Update", docEditEmpty.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[Edit Guard EmptyBody] StatusCode={(int)resEditEmpty.StatusCode} Body={bodyEditEmpty}");

            // Ca 4.2: Guard không tồn tại -> 400 Bad Request (Mst_ContractType_CheckDB_OrganNotFound)
            var reqEditNone = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"NOT_EXIST\",\"ContractTypeName\":\"Không tồn tại\"}", Encoding.UTF8, "application/json");
            var resEditNone = await http.PostAsync("/api/mst_contracttype/edit", reqEditNone);
            var bodyEditNone = await resEditNone.Content.ReadAsStringAsync();
            var docEditNone = JsonDocument.Parse(bodyEditNone);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resEditNone.StatusCode);
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_CheckDB_OrganNotFound", docEditNone.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[Edit Guard NonExistent] StatusCode={(int)resEditNone.StatusCode} Body={bodyEditNone}");

            // Ca 4.3: Round-trip cập nhật thành công và đọc lại
            var reqEditOk = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"HD_LIVE_TEST\",\"ContractTypeName\":\"Hợp đồng kiểm thử live - Đã sửa\",\"Description\":\"Mô tả đã sửa live\",\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var resEditOk = await http.PostAsync("/api/mst_contracttype/edit", reqEditOk);
            var bodyEditOk = await resEditOk.Content.ReadAsStringAsync();
            var docEditOk = JsonDocument.Parse(bodyEditOk);
            Assert.Equal(System.Net.HttpStatusCode.OK, resEditOk.StatusCode);
            Assert.True(docEditOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Hợp đồng kiểm thử live - Đã sửa", docEditOk.RootElement.GetProperty("Data").GetProperty("ContractTypeName").GetString());
            _output?.WriteLine($"[Edit Success RoundTrip] StatusCode={(int)resEditOk.StatusCode} Body={bodyEditOk}");

            // Đọc lại qua Update để xác minh dữ liệu đã được cập nhật thật
            var reqVerify = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"HD_LIVE_TEST\"}", Encoding.UTF8, "application/json");
            var resVerify = await http.PostAsync("/api/mst_contracttype/update", reqVerify);
            var bodyVerify = await resVerify.Content.ReadAsStringAsync();
            var docVerify = JsonDocument.Parse(bodyVerify);
            Assert.Equal(System.Net.HttpStatusCode.OK, resVerify.StatusCode);
            Assert.Equal("Hợp đồng kiểm thử live - Đã sửa", docVerify.RootElement.GetProperty("Data").GetProperty("ContractTypeName").GetString());
            Assert.Equal("Mô tả đã sửa live", docVerify.RootElement.GetProperty("Data").GetProperty("Description").GetString());
            _output?.WriteLine($"[Verify ReadBack Update] StatusCode={(int)resVerify.StatusCode} Body={bodyVerify}");

            // 5. Detail: kiểm tra các ca đọc chi tiết
            // Ca 5.1: Body rỗng -> trả về Data = null (Gate: dòng 407 View(new Mst_ContractType()))
            var reqDetEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
            var resDetEmpty = await http.PostAsync("/api/mst_contracttype/detail", reqDetEmpty);
            var bodyDetEmpty = await resDetEmpty.Content.ReadAsStringAsync();
            var docDetEmpty = JsonDocument.Parse(bodyDetEmpty);
            Assert.Equal(System.Net.HttpStatusCode.OK, resDetEmpty.StatusCode);
            Assert.True(docDetEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docDetEmpty.RootElement.GetProperty("Data").ValueKind);
            _output?.WriteLine($"[Detail EmptyBody] StatusCode={(int)resDetEmpty.StatusCode} Body={bodyDetEmpty}");

            // Ca 5.2: Lấy theo ContractType = "HD_KT" -> trả về "HD_KT"
            var reqDet1 = new StringContent("{\"ContractType\":\"HD_KT\"}", Encoding.UTF8, "application/json");
            var resDet1 = await http.PostAsync("/api/mst_contracttype/detail", reqDet1);
            var bodyDet1 = await resDet1.Content.ReadAsStringAsync();
            var docDet1 = JsonDocument.Parse(bodyDet1);
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet1.StatusCode);
            Assert.True(docDet1.RootElement.GetProperty("Success").GetBoolean());
            var dataDet1 = docDet1.RootElement.GetProperty("Data");
            Assert.Equal("HD_KT", dataDet1.GetProperty("ContractType").GetString());
            Assert.Equal("Hợp đồng kinh tế", dataDet1.GetProperty("ContractTypeName").GetString());
            _output?.WriteLine($"[Detail Filter1 HD_KT] StatusCode={(int)resDet1.StatusCode} Body={bodyDet1}");

            // Ca 5.3: Lấy theo ContractType = "HD_VAY" (NetworkID 1002) -> trả về "HD_VAY" (2 bộ lọc ra 2 bản ghi khác nhau)
            var reqDet2 = new StringContent("{\"ContractType\":\"HD_VAY\"}", Encoding.UTF8, "application/json");
            var resDet2 = await http.PostAsync("/api/mst_contracttype/detail", reqDet2);
            var bodyDet2 = await resDet2.Content.ReadAsStringAsync();
            var docDet2 = JsonDocument.Parse(bodyDet2);
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet2.StatusCode);
            Assert.True(docDet2.RootElement.GetProperty("Success").GetBoolean());
            var dataDet2 = docDet2.RootElement.GetProperty("Data");
            Assert.Equal("1002", dataDet2.GetProperty("NetworkID").GetString());
            Assert.Equal("HD_VAY", dataDet2.GetProperty("ContractType").GetString());
            Assert.Equal("Hợp đồng vay vốn", dataDet2.GetProperty("ContractTypeName").GetString());
            _output?.WriteLine($"[Detail Filter2 HD_VAY] StatusCode={(int)resDet2.StatusCode} Body={bodyDet2}");

            // Ca 5.4: Lấy theo alias contracttype = "HD_LD" -> trả về "HD_LD"
            var reqDet3 = new StringContent("{\"contracttype\":\"HD_LD\"}", Encoding.UTF8, "application/json");
            var resDet3 = await http.PostAsync("/api/mst_contracttype/detail", reqDet3);
            var bodyDet3 = await resDet3.Content.ReadAsStringAsync();
            var docDet3 = JsonDocument.Parse(bodyDet3);
            Assert.Equal(System.Net.HttpStatusCode.OK, resDet3.StatusCode);
            Assert.True(docDet3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("HD_LD", docDet3.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());
            _output?.WriteLine($"[Detail Alias contracttype] StatusCode={(int)resDet3.StatusCode} Body={bodyDet3}");

            // Ca 5.5: Lấy theo mã không tồn tại -> Data null
            var reqDetNone = new StringContent("{\"ContractType\":\"NOT_EXIST\"}", Encoding.UTF8, "application/json");
            var resDetNone = await http.PostAsync("/api/mst_contracttype/detail", reqDetNone);
            var bodyDetNone = await resDetNone.Content.ReadAsStringAsync();
            var docDetNone = JsonDocument.Parse(bodyDetNone);
            Assert.Equal(System.Net.HttpStatusCode.OK, resDetNone.StatusCode);
            Assert.True(docDetNone.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docDetNone.RootElement.GetProperty("Data").ValueKind);
            _output?.WriteLine($"[Detail NonExistent] StatusCode={(int)resDetNone.StatusCode} Body={bodyDetNone}");

            // Ca 5.6: Lấy theo NetworkID = 1001 và HD_VAY -> không khớp -> Data null
            var reqDetNetFail = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"HD_VAY\"}", Encoding.UTF8, "application/json");
            var resDetNetFail = await http.PostAsync("/api/mst_contracttype/detail", reqDetNetFail);
            var bodyDetNetFail = await resDetNetFail.Content.ReadAsStringAsync();
            var docDetNetFail = JsonDocument.Parse(bodyDetNetFail);
            Assert.Equal(System.Net.HttpStatusCode.OK, resDetNetFail.StatusCode);
            Assert.True(docDetNetFail.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, docDetNetFail.RootElement.GetProperty("Data").ValueKind);
            _output?.WriteLine($"[Detail NetMismatch] StatusCode={(int)resDetNetFail.StatusCode} Body={bodyDetNetFail}");

            // 6. Delete: 1 ca guard rỗng, 1 ca guard không tồn tại, 1 ca xóa round-trip
            // Ca 6.1: Guard body rỗng -> 400 Bad Request
            var reqDelEmpty = new StringContent("{}", Encoding.UTF8, "application/json");
            var resDelEmpty = await http.PostAsync("/api/mst_contracttype/delete", reqDelEmpty);
            var bodyDelEmpty = await resDelEmpty.Content.ReadAsStringAsync();
            var docDelEmpty = JsonDocument.Parse(bodyDelEmpty);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resDelEmpty.StatusCode);
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Delete", docDelEmpty.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[Delete Guard EmptyBody] StatusCode={(int)resDelEmpty.StatusCode} Body={bodyDelEmpty}");

            // Ca 6.2: Guard không tồn tại -> 400 Bad Request
            var reqDelNone = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"NOT_EXIST\"}", Encoding.UTF8, "application/json");
            var resDelNone = await http.PostAsync("/api/mst_contracttype/delete", reqDelNone);
            var bodyDelNone = await resDelNone.Content.ReadAsStringAsync();
            var docDelNone = JsonDocument.Parse(bodyDelNone);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resDelNone.StatusCode);
            Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_CheckDB_OrganNotFound", docDelNone.RootElement.GetProperty("ErrorCode").GetString());
            _output?.WriteLine($"[Delete Guard NonExistent] StatusCode={(int)resDelNone.StatusCode} Body={bodyDelNone}");

            // Ca 6.3: Xóa thành công bản ghi HD_LIVE_TEST đã tạo và sửa ở trên
            var reqDelOk = new StringContent("{\"NetworkID\":\"1001\",\"ContractType\":\"HD_LIVE_TEST\"}", Encoding.UTF8, "application/json");
            var resDelOk = await http.PostAsync("/api/mst_contracttype/delete", reqDelOk);
            var bodyDelOk = await resDelOk.Content.ReadAsStringAsync();
            var docDelOk = JsonDocument.Parse(bodyDelOk);
            Assert.Equal(System.Net.HttpStatusCode.OK, resDelOk.StatusCode);
            Assert.True(docDelOk.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/Mst_ContractType", docDelOk.RootElement.GetProperty("RedirectUrl").GetString());
            _output?.WriteLine($"[Delete Success RoundTrip] StatusCode={(int)resDelOk.StatusCode} Body={bodyDelOk}");

            // Đọc lại qua Detail để xác nhận bản ghi đã bị xóa thật sự
            var reqVerifyDel = new StringContent("{\"ContractType\":\"HD_LIVE_TEST\"}", Encoding.UTF8, "application/json");
            var resVerifyDel = await http.PostAsync("/api/mst_contracttype/detail", reqVerifyDel);
            var bodyVerifyDel = await resVerifyDel.Content.ReadAsStringAsync();
            var docVerifyDel = JsonDocument.Parse(bodyVerifyDel);
            Assert.Equal(System.Net.HttpStatusCode.OK, resVerifyDel.StatusCode);
            Assert.Equal(JsonValueKind.Null, docVerifyDel.RootElement.GetProperty("Data").ValueKind);
            _output?.WriteLine($"[Verify After Delete] StatusCode={(int)resVerifyDel.StatusCode} Body={bodyVerifyDel}");

            // 7. Import: các ca kiểm thử file Excel
            // Ca 7.1: Không có file part -> 400 Bad Request
            using (var emptyMultipart = new MultipartFormDataContent())
            {
                emptyMultipart.Add(new StringContent("no-file"), "dummyField");
                var resImpEmpty = await http.PostAsync("/api/mst_contracttype/import", emptyMultipart);
                var bodyImpEmpty = await resImpEmpty.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resImpEmpty.StatusCode);
                var docImpEmpty = JsonDocument.Parse(bodyImpEmpty);
                Assert.False(docImpEmpty.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", docImpEmpty.RootElement.GetProperty("ErrorCode").GetString());
                Assert.Equal("File dữ liệu import không hợp lệ!", docImpEmpty.RootElement.GetProperty("Params").GetProperty("Message").GetString());
                _output?.WriteLine($"[Import EmptyMultipart] StatusCode={(int)resImpEmpty.StatusCode} Body={bodyImpEmpty}");
            }

            // Ca 7.2: File không phải .xlsx (.txt) -> 400 Bad Request
            using (var txtMultipart = new MultipartFormDataContent())
            {
                var txtBytes = Encoding.UTF8.GetBytes("Nội dung text không phải xlsx");
                txtMultipart.Add(new ByteArrayContent(txtBytes), "file", "test.txt");
                var resImpTxt = await http.PostAsync("/api/mst_contracttype/import", txtMultipart);
                var bodyImpTxt = await resImpTxt.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resImpTxt.StatusCode);
                var docImpTxt = JsonDocument.Parse(bodyImpTxt);
                Assert.False(docImpTxt.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", docImpTxt.RootElement.GetProperty("ErrorCode").GetString());
                Assert.Equal("File dữ liệu import không hợp lệ!", docImpTxt.RootElement.GetProperty("Params").GetProperty("Message").GetString());
                _output?.WriteLine($"[Import TxtExtension] StatusCode={(int)resImpTxt.StatusCode} Body={bodyImpTxt}");
            }

            // Ca 7.3: File rỗng không có dòng dữ liệu -> 400 Bad Request
            using (var emptyDataMultipart = new MultipartFormDataContent())
            {
                var emptyDataBytes = CreateExcelBytes(new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>());
                emptyDataMultipart.Add(new ByteArrayContent(emptyDataBytes), "file", "test_empty.xlsx");
                var resImpEmptyData = await http.PostAsync("/api/mst_contracttype/import", emptyDataMultipart);
                var bodyImpEmptyData = await resImpEmptyData.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resImpEmptyData.StatusCode);
                var docImpEmptyData = JsonDocument.Parse(bodyImpEmptyData);
                Assert.False(docImpEmptyData.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", docImpEmptyData.RootElement.GetProperty("ErrorCode").GetString());
                Assert.Equal("File Excel nhập không có dữ liệu!", docImpEmptyData.RootElement.GetProperty("Params").GetProperty("Message").GetString());
                _output?.WriteLine($"[Import EmptyDataRows] StatusCode={(int)resImpEmptyData.StatusCode} Body={bodyImpEmptyData}");
            }

            // Ca 7.4: File sai số cột (2 cột thay vì 3) -> 400 Bad Request
            using (var badColMultipart = new MultipartFormDataContent())
            {
                var badColBytes = CreateExcelBytes(new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)" }, new List<string?[]>
                {
                    new[] { "HD_BAD_COL", "Tên" }
                });
                badColMultipart.Add(new ByteArrayContent(badColBytes), "file", "test_badcols.xlsx");
                var resImpBadCol = await http.PostAsync("/api/mst_contracttype/import", badColMultipart);
                var bodyImpBadCol = await resImpBadCol.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resImpBadCol.StatusCode);
                var docImpBadCol = JsonDocument.Parse(bodyImpBadCol);
                Assert.False(docImpBadCol.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", docImpBadCol.RootElement.GetProperty("ErrorCode").GetString());
                Assert.Equal("File dữ liệu import không hợp lệ!", docImpBadCol.RootElement.GetProperty("Params").GetProperty("Message").GetString());
                _output?.WriteLine($"[Import BadColumnCount] StatusCode={(int)resImpBadCol.StatusCode} Body={bodyImpBadCol}");
            }

            // Ca 7.5: Trùng mã đã có trong DB (HD_KT) -> 400 Bad Request
            using (var dupMultipart = new MultipartFormDataContent())
            {
                var dupBytes = CreateExcelBytes(new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>
                {
                    new[] { "HD_KT", "Trùng mã KT", "Mô tả" }
                });
                dupMultipart.Add(new ByteArrayContent(dupBytes), "file", "test_dup.xlsx");
                var resImpDup = await http.PostAsync("/api/mst_contracttype/import", dupMultipart);
                var bodyImpDup = await resImpDup.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, resImpDup.StatusCode);
                var docImpDup = JsonDocument.Parse(bodyImpDup);
                Assert.False(docImpDup.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.QContract.Mst_ContractType_Import", docImpDup.RootElement.GetProperty("ErrorCode").GetString());
                Assert.Equal("Mã loại hợp đồng: 'HD_KT' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!", docImpDup.RootElement.GetProperty("Params").GetProperty("Message").GetString());
                _output?.WriteLine($"[Import DuplicateDbCode] StatusCode={(int)resImpDup.StatusCode} Body={bodyImpDup}");
            }

            // Ca 7.6: Import file hợp lệ (2 dòng mới) -> 200 OK
            using (var validMultipart = new MultipartFormDataContent())
            {
                var validBytes = CreateExcelBytes(new[] { "Mã loại hợp đồng(*)", "Tên loại hợp đồng(*)", "Mô tả" }, new List<string?[]>
                {
                    new[] { "HD_IMP_LIVE1", "Hợp đồng nhập live 1", "Mô tả live 1" },
                    new[] { "HD_IMP_LIVE2", "Hợp đồng nhập live 2", "Mô tả live 2" }
                });
                validMultipart.Add(new ByteArrayContent(validBytes), "file", "test_valid.xlsx");
                var resImpValid = await http.PostAsync("/api/mst_contracttype/import", validMultipart);
                var bodyImpValid = await resImpValid.Content.ReadAsStringAsync();
                Assert.Equal(System.Net.HttpStatusCode.OK, resImpValid.StatusCode);
                var docImpValid = JsonDocument.Parse(bodyImpValid);
                Assert.True(docImpValid.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Đã nhập dữ liệu excel thành công", docImpValid.RootElement.GetProperty("Title").GetString());
                _output?.WriteLine($"[Import Success RoundTrip] StatusCode={(int)resImpValid.StatusCode} Body={bodyImpValid}");
            }

            // Đọc lại qua Detail để xác minh bản ghi nhập khẩu thực sự đã được ghi vào DB
            var reqVerifyImp = new StringContent("{\"ContractType\":\"HD_IMP_LIVE1\"}", Encoding.UTF8, "application/json");
            var resVerifyImp = await http.PostAsync("/api/mst_contracttype/detail", reqVerifyImp);
            var bodyVerifyImp = await resVerifyImp.Content.ReadAsStringAsync();
            var docVerifyImp = JsonDocument.Parse(bodyVerifyImp);
            Assert.Equal(System.Net.HttpStatusCode.OK, resVerifyImp.StatusCode);
            Assert.Equal("HD_IMP_LIVE1", docVerifyImp.RootElement.GetProperty("Data").GetProperty("ContractType").GetString());
            Assert.Equal("Hợp đồng nhập live 1", docVerifyImp.RootElement.GetProperty("Data").GetProperty("ContractTypeName").GetString());
            _output?.WriteLine($"[Verify After Import] StatusCode={(int)resVerifyImp.StatusCode} Body={bodyVerifyImp}");

            // 8. Export: kiểm tra các ca xuất file Excel
            // Ca 8.1: Lọc mã không tồn tại -> trả về File Excel nhập không có dữ liệu!
            var reqExpEmpty = new StringContent("{\"ContractType\":\"NOT_EXIST\"}", Encoding.UTF8, "application/json");
            var resExpEmpty = await http.PostAsync("/api/mst_contracttype/export", reqExpEmpty);
            var bodyExpEmpty = await resExpEmpty.Content.ReadAsStringAsync();
            var docExpEmpty = JsonDocument.Parse(bodyExpEmpty);
            Assert.Equal(System.Net.HttpStatusCode.OK, resExpEmpty.StatusCode);
            Assert.True(docExpEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("File Excel nhập không có dữ liệu!", docExpEmpty.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", docExpEmpty.RootElement.GetProperty("CheckSuccess").GetString());
            _output?.WriteLine($"[Export EmptyFilter] StatusCode={(int)resExpEmpty.StatusCode} Body={bodyExpEmpty}");

            // Ca 8.2: Xuất với bộ lọc 1: NetworkID = "1001" -> thành công trả về strUrl
            var reqExp1 = new StringContent("{\"NetworkID\":\"1001\"}", Encoding.UTF8, "application/json");
            var resExp1 = await http.PostAsync("/api/mst_contracttype/export", reqExp1);
            var bodyExp1 = await resExp1.Content.ReadAsStringAsync();
            var docExp1 = JsonDocument.Parse(bodyExp1);
            Assert.Equal(System.Net.HttpStatusCode.OK, resExp1.StatusCode);
            Assert.True(docExp1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công", docExp1.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", docExp1.RootElement.GetProperty("CheckSuccess").GetString());
            var url1 = docExp1.RootElement.GetProperty("strUrl").GetString();
            Assert.False(string.IsNullOrWhiteSpace(url1));
            _output?.WriteLine($"[Export Filter1 NetworkID=1001] StatusCode={(int)resExp1.StatusCode} Body={bodyExp1}");

            // Ca 8.3: Xuất với bộ lọc 2: NetworkID = "1002" -> thành công trả về strUrl (2 bộ lọc ra 2 tập dữ liệu khác nhau)
            var reqExp2 = new StringContent("{\"NetworkID\":\"1002\"}", Encoding.UTF8, "application/json");
            var resExp2 = await http.PostAsync("/api/mst_contracttype/export", reqExp2);
            var bodyExp2 = await resExp2.Content.ReadAsStringAsync();
            var docExp2 = JsonDocument.Parse(bodyExp2);
            Assert.Equal(System.Net.HttpStatusCode.OK, resExp2.StatusCode);
            Assert.True(docExp2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công", docExp2.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", docExp2.RootElement.GetProperty("CheckSuccess").GetString());
            var url2 = docExp2.RootElement.GetProperty("strUrl").GetString();
            Assert.False(string.IsNullOrWhiteSpace(url2));
            _output?.WriteLine($"[Export Filter2 NetworkID=1002] StatusCode={(int)resExp2.StatusCode} Body={bodyExp2}");

            // 9. ExportTemplate: kiểm tra các ca xuất file mẫu Excel
            // Ca 9.1: Xuất template với body rỗng JSON {}
            var reqExpTpl1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resExpTpl1 = await http.PostAsync("/api/mst_contracttype/exporttemplate", reqExpTpl1);
            var bodyExpTpl1 = await resExpTpl1.Content.ReadAsStringAsync();
            var docExpTpl1 = JsonDocument.Parse(bodyExpTpl1);
            Assert.Equal(System.Net.HttpStatusCode.OK, resExpTpl1.StatusCode);
            Assert.True(docExpTpl1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công", docExpTpl1.RootElement.GetProperty("Title").GetString());
            Assert.Equal("1", docExpTpl1.RootElement.GetProperty("CheckSuccess").GetString());
            var urlTpl1 = docExpTpl1.RootElement.GetProperty("strUrl").GetString();
            Assert.False(string.IsNullOrWhiteSpace(urlTpl1));
            _output?.WriteLine($"[ExportTemplate Success EmptyJSON] StatusCode={(int)resExpTpl1.StatusCode} Body={bodyExpTpl1}");

            // Kiểm tra file vật lý template được tạo trong wwwroot
            var tplPhysicalPath = Path.Combine(Path.GetDirectoryName(dllPath)!, "wwwroot", urlTpl1!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(tplPhysicalPath), $"Template file does not exist at {tplPhysicalPath}");
            using (var wb = new XLWorkbook(tplPhysicalPath))
            {
                var ws = wb.Worksheet("Mst_ContractType");
                Assert.NotNull(ws);
                Assert.Equal(3, ws.Row(1).LastCellUsed()?.Address.ColumnNumber);
                Assert.Equal("Mã loại hợp đồng(*)", ws.Cell(1, 1).GetString());
                Assert.Equal("Tên loại hợp đồng(*)", ws.Cell(1, 2).GetString());
                Assert.Equal("Mô tả", ws.Cell(1, 3).GetString());
                Assert.Equal(1, ws.LastRowUsed()?.RowNumber());
            }

            // Ca 9.2: Xuất template với request không có content/body null
            var resExpTpl2 = await http.PostAsync("/api/mst_contracttype/exporttemplate", null);
            var bodyExpTpl2 = await resExpTpl2.Content.ReadAsStringAsync();
            var docExpTpl2 = JsonDocument.Parse(bodyExpTpl2);
            Assert.Equal(System.Net.HttpStatusCode.OK, resExpTpl2.StatusCode);
            Assert.True(docExpTpl2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Đã xuất excel thành công", docExpTpl2.RootElement.GetProperty("Title").GetString());
            _output?.WriteLine($"[ExportTemplate Success NullBody] StatusCode={(int)resExpTpl2.StatusCode} Body={bodyExpTpl2}");
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

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "test_wwwroot_ct_" + Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MiniContract.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
    }
}
