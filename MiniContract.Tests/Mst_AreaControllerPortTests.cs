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

namespace MiniContract.Tests;

public class Mst_AreaControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly TestWebHostEnvironment _env;
    private readonly Mst_AreaControllerPort _controller;

    public Mst_AreaControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_Area
        var seeder = new Mst_AreaSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();

        _env = new TestWebHostEnvironment();
        _controller = new Mst_AreaControllerPort(_db, _env);
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
    public async Task Export_WithAllRecords_SuccessAndGeneratesExcel()
    {
        var result = await _controller.Export(new Mst_Area_ExportReq());
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
        string strUrl = root.GetProperty("strUrl").GetString()!;
        Assert.False(string.IsNullOrEmpty(strUrl));
        Assert.EndsWith(".xlsx", strUrl);

        // Kiểm tra file vật lý đã tạo
        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physicalPath));

        // Đọc lại file Excel để kiểm tra tiêu đề và dữ liệu
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Area");
        Assert.NotNull(ws);
        Assert.Equal("Mã khu vực", ws.Cell(1, 1).GetString());
        Assert.Equal("Tên khu vực", ws.Cell(1, 2).GetString());
        Assert.Equal("Mô tả", ws.Cell(1, 3).GetString());
        Assert.Equal("Mã khu vực cha", ws.Cell(1, 4).GetString());
        Assert.Equal("Trạng thái", ws.Cell(1, 5).GetString());

        // 4 dòng dữ liệu đã seed
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(5, lastRow); // Dòng 1 tiêu đề + 4 dòng dữ liệu
    }

    [Fact]
    public async Task Export_FilterByOrgID_ReturnsFilteredCount()
    {
        var req = new Mst_Area_ExportReq { OrgID = "7200000001" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        string strUrl = root.GetProperty("strUrl").GetString()!;

        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Area");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(4, lastRow); // Dòng 1 tiêu đề + 3 dòng của Org 7200000001
    }

    [Fact]
    public async Task Export_FilterByOrgAndFlagActive_ReturnsActiveOnly()
    {
        var req = new Mst_Area_ExportReq { OrgID = "7200000001", FlagActive = "1" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
        string strUrl = root.GetProperty("strUrl").GetString()!;

        var physicalPath = Path.Combine(_env.WebRootPath, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        using var wb = new XLWorkbook(physicalPath);
        var ws = wb.Worksheet("Mst_Area");
        var lastRow = ws.LastRowUsed()?.RowNumber();
        Assert.Equal(3, lastRow); // Dòng 1 tiêu đề + 2 dòng (MB, MN)
    }

    [Fact]
    public async Task Export_NoMatchingRecords_ReturnsNoData()
    {
        var req = new Mst_Area_ExportReq { OrgID = "NON_EXISTENT" };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Không có dữ liệu!", root.GetProperty("Title").GetString());
        Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
    }

    [Fact]
    public async Task Export_NegativePageIndexOrZeroCount_ReturnsNoData()
    {
        var req = new Mst_Area_ExportReq { Ft_RecordStart = -1 };
        var result = await _controller.Export(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Không có dữ liệu!", root.GetProperty("Title").GetString());
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
