using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class Mst_ColumnConfigGroupControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_ColumnConfigGroupControllerPort _controller;

    public Mst_ColumnConfigGroupControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_ColumnConfigGroup
        var seeder = new Mst_ColumnConfigGroupSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_ColumnConfigGroupControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_WithAllRecords_SuccessAndReturnsFullList()
    {
        var result = await _controller.Index(new Mst_ColumnConfigGroup_IndexReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(4, dataList.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByOrgID_ReturnsFilteredCount()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { OrgID = "7200000001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(3, dataList.GetArrayLength());
        foreach (var item in dataList.EnumerateArray())
        {
            Assert.Equal("7200000001", item.GetProperty("OrgID").GetString());
        }
    }

    [Fact]
    public async Task Index_FilterByOrgAndFlagActive_ReturnsActiveOnly()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { OrgID = "7200000001", FlagActive = "1" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(2, dataList.GetArrayLength());
        foreach (var item in dataList.EnumerateArray())
        {
            Assert.Equal("1", item.GetProperty("FlagActive").GetString());
        }
    }

    [Fact]
    public async Task Index_FilterByColumnGrpName_Contains_ReturnsMatched()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { ColumnGrpName = "tiền tệ" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(1, dataList.GetArrayLength());
        Assert.Equal("GRP01", dataList[0].GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Contains("tiền tệ", dataList[0].GetProperty("ColumnGrpName").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_FilterByColumnConfigGrpCode_ReturnsMatched()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { ColumnConfigGrpCode = "GRP02" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(1, dataList.GetArrayLength());
        Assert.Equal("GRP02", dataList[0].GetProperty("ColumnConfigGrpCode").GetString());
    }

    [Fact]
    public async Task Index_NoMatchingRecords_ReturnsEmptyList()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { OrgID = "NON_EXISTENT" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_Paging_Page0PageSize2_ReturnsFirstTwo()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { Page = 0, Recordcount = 2 };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data.GetProperty("PageSize").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(2, dataList.GetArrayLength());
    }

    [Fact]
    public async Task Index_Paging_Page1PageSize2_ReturnsNextTwo()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { Page = 1, Recordcount = 2 };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data.GetProperty("PageSize").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(2, dataList.GetArrayLength());
    }

    [Fact]
    public async Task Index_NegativePageIndex_ReturnsEmptyDataList()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { Page = -1, Recordcount = 10 };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(-1, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_ZeroPageSize_ReturnsEmptyDataListWithItemCount()
    {
        var req = new Mst_ColumnConfigGroup_IndexReq { Page = 0, Recordcount = 0 };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageSize").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_NullReq_SuccessWithDefaults()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data.GetProperty("PageSize").GetInt32());
        Assert.Equal(4, data.GetProperty("DataList").GetArrayLength());
    }
}
