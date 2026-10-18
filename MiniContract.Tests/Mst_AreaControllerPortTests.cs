using System.Diagnostics;
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

namespace MiniContract.Tests;

public class Mst_AreaControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly TestWebHostEnvironment _env;
    private readonly Mst_AreaControllerPort _controller;
    private readonly ITestOutputHelper? _output;

    public Mst_AreaControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
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

    [Fact]
    public async Task Index_NullOrEmptyBody_ReturnsAllSeededAreasWithDefaultPaging()
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

        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(4, list.Count);
        // Thứ tự sắp xếp OrgID asc, AreaCode asc (Customer.cs:380, 420)
        Assert.Equal("7200000001", list[0].GetProperty("OrgID").GetString());
        Assert.Equal("MB", list[0].GetProperty("AreaCode").GetString());
        Assert.Equal("7200000001", list[1].GetProperty("OrgID").GetString());
        Assert.Equal("MN", list[1].GetProperty("AreaCode").GetString());
        Assert.Equal("7200000001", list[2].GetProperty("OrgID").GetString());
        Assert.Equal("MT", list[2].GetProperty("AreaCode").GetString());
        Assert.Equal("7200000002", list[3].GetProperty("OrgID").GetString());
        Assert.Equal("MB", list[3].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByOrgID_ReturnsFilteredItems()
    {
        var req = new Mst_Area_IndexReq { OrgID = "7200000001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(3, list.Count);
        Assert.All(list, item => Assert.Equal("7200000001", item.GetProperty("OrgID").GetString()));
    }

    [Fact]
    public async Task Index_FilterByOrgAndFlagActive_ReturnsActiveOnly()
    {
        var req = new Mst_Area_IndexReq { OrgID = "7200000001", FlagActive = "1" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("MB", list[0].GetProperty("AreaCode").GetString());
        Assert.Equal("MN", list[1].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByAreaCode_ReturnsMatchingCodeAcrossOrgs()
    {
        var req = new Mst_Area_IndexReq { AreaCode = "MB" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("7200000001", list[0].GetProperty("OrgID").GetString());
        Assert.Equal("7200000002", list[1].GetProperty("OrgID").GetString());
    }

    [Fact]
    public async Task Index_FilterByNetworkID_ReturnsMatchingNetwork()
    {
        var req = new Mst_Area_IndexReq { NetworkID = "1002" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Single(list);
        Assert.Equal("7200000002", list[0].GetProperty("OrgID").GetString());
        Assert.Equal("MB", list[0].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_NoMatchingRecords_ReturnsEmptyListAndZeroCounts()
    {
        var req = new Mst_Area_IndexReq { OrgID = "NON_EXISTENT" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task Index_Paging_FirstPageAndSecondPage_CalculatesCorrectOffsets()
    {
        // Trang 0, kích thước 2
        var req0 = new Mst_Area_IndexReq { Page = 0, RecordCount = 2 };
        var result0 = await _controller.Index(req0);
        var doc0 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result0).Value));
        var data0 = doc0.RootElement.GetProperty("Data");

        Assert.Equal(4, data0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data0.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data0.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data0.GetProperty("PageSize").GetInt32());
        var list0 = data0.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list0.Count);
        Assert.Equal("MB", list0[0].GetProperty("AreaCode").GetString());
        Assert.Equal("MN", list0[1].GetProperty("AreaCode").GetString());

        // Trang 1, kích thước 2
        var req1 = new Mst_Area_IndexReq { Page = 1, RecordCount = 2 };
        var result1 = await _controller.Index(req1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result1).Value));
        var data1 = doc1.RootElement.GetProperty("Data");

        Assert.Equal(4, data1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, data1.GetProperty("PageCount").GetInt32());
        Assert.Equal(1, data1.GetProperty("PageIndex").GetInt32());
        Assert.Equal(2, data1.GetProperty("PageSize").GetInt32());
        var list1 = data1.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Equal(2, list1.Count);
        Assert.Equal("MT", list1[0].GetProperty("AreaCode").GetString());
        Assert.Equal("MB", list1[1].GetProperty("AreaCode").GetString());
    }

    [Fact]
    public async Task Index_Paging_PageBeyondTotal_ReturnsEmptyList()
    {
        var req = new Mst_Area_IndexReq { Page = 10, RecordCount = 2 };
        var result = await _controller.Index(req);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32()); // Empty list -> PageCount = 0 (gate Mst_AreaController.cs:48, 92)
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task Index_NegativePageIndexOrZeroCount_ReturnsEmptyDataList()
    {
        var req = new Mst_Area_IndexReq { Ft_RecordStart = -1 };
        var result = await _controller.Index(req);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal(4, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        var list = data.GetProperty("DataList").EnumerateArray().ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task RealServer_Verification_Port8525()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_area_{Guid.NewGuid():N}.db");
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
        psi.Environment["PORT"] = "8525";
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8525"), Timeout = TimeSpan.FromSeconds(5) };
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
            Assert.True(started, "MiniContract server failed to start within 15 seconds on port 8525.");

            // Request 1: POST empty body -> 200 OK, returns all 4 seeded records
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_area/index", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ1 (Empty body): {body1}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(4, data1.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data1.GetProperty("PageCount").GetInt32());
            Assert.Equal(4, data1.GetProperty("DataList").GetArrayLength());

            // Request 2: POST Filter OrgID = 7200000001 -> 200 OK, returns 3 records
            var req2 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_area/index", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ2 (OrgID 7200000001): {body2}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(3, data2.GetProperty("ItemCount").GetInt32());
            Assert.Equal(3, data2.GetProperty("DataList").GetArrayLength());

            // Request 3: POST Filter OrgID = 7200000001 AND FlagActive = 1 -> 200 OK, returns 2 records (MB, MN)
            var req3 = new StringContent("{\"OrgID\":\"7200000001\",\"FlagActive\":\"1\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_area/index", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ3 (OrgID + FlagActive): {body3}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(2, data3.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data3.GetProperty("DataList").GetArrayLength());

            // Request 4: POST Filter AreaCode = MB -> 200 OK, returns 2 records across both orgs
            var req4 = new StringContent("{\"AreaCode\":\"MB\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_area/index", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ4 (AreaCode MB): {body4}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(2, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data4.GetProperty("DataList").GetArrayLength());

            // Request 5: POST Filter NetworkID = 1002 -> 200 OK, returns 1 record
            var req5 = new StringContent("{\"NetworkID\":\"1002\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_area/index", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ5 (NetworkID 1002): {body5}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var doc5 = JsonDocument.Parse(body5);
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal(1, data5.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data5.GetProperty("DataList").GetArrayLength());

            // Request 6: POST Pagination Page = 0, Recordcount = 2 -> 200 OK, returns 2 records, PageCount = 2
            var req6 = new StringContent("{\"Page\":0,\"Recordcount\":2}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_area/index", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ6 (Pagination): {body6}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(4, data6.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data6.GetProperty("PageCount").GetInt32());
            Assert.Equal(2, data6.GetProperty("DataList").GetArrayLength());

            // Request 7: POST Non-existent Org -> 200 OK, returns empty DataList
            var req7 = new StringContent("{\"OrgID\":\"NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_area/index", req7);
            var body7 = await res7.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ7 (Non-existent): {body7}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res7.StatusCode);
            var doc7 = JsonDocument.Parse(body7);
            var data7 = doc7.RootElement.GetProperty("Data");
            Assert.Equal(0, data7.GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, data7.GetProperty("PageCount").GetInt32());
            Assert.Equal(0, data7.GetProperty("DataList").GetArrayLength());
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
