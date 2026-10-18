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
    public async Task DoSearch_WithAllRecords_SuccessAndReturnsFullList()
    {
        var result = await _controller.DoSearch(new Mst_ColumnConfigGroup_DoSearchReq());
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
    public async Task DoSearch_FilterByOrgID_ReturnsFilteredCount()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { OrgID = "7200000001" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_FilterByOrgID_Org2_ReturnsDifferentCount()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { OrgID = "7200000002" };
        var result = await _controller.DoSearch(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data.GetProperty("PageCount").GetInt32());

        var dataList = data.GetProperty("DataList");
        Assert.Equal(1, dataList.GetArrayLength());
        Assert.Equal("7200000002", dataList[0].GetProperty("OrgID").GetString());
    }

    [Fact]
    public async Task DoSearch_FilterByOrgAndFlagActive_ReturnsActiveOnly()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { OrgID = "7200000001", FlagActive = "1" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_FilterByColumnGrpName_Contains_ReturnsMatched()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { ColumnGrpName = "tiền tệ" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_FilterByColumnConfigGrpCode_ReturnsMatched()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { ColumnConfigGrpCode = "GRP02" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_NoMatchingRecords_ReturnsEmptyList()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { OrgID = "NON_EXISTENT" };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_Paging_Page0PageSize2_ReturnsFirstTwo()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { Page = 0, Recordcount = 2 };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_Paging_Page1PageSize2_ReturnsNextTwo()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { Page = 1, Recordcount = 2 };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_NegativePageIndex_ReturnsEmptyDataList()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { Page = -1, Recordcount = 10 };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_ZeroPageSize_ReturnsEmptyDataListWithItemCount()
    {
        var req = new Mst_ColumnConfigGroup_DoSearchReq { Page = 0, Recordcount = 0 };
        var result = await _controller.DoSearch(req);
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
    public async Task DoSearch_NullReq_SuccessWithDefaults()
    {
        var result = await _controller.DoSearch(null);
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

    [Fact]
    public async Task RealServer_Verification_Port8541()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_dosearch_{Guid.NewGuid():N}.db");
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
        var freePort8541 = FreePort.Get(); psi.Environment["PORT"] = freePort8541.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort8541}"), Timeout = TimeSpan.FromSeconds(5) };
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

            // 1. Ca body rỗng: POST {} -> 200 OK, ItemCount=4, PageCount=1, DataList=4
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_columnconfiggroup/dosearch", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(4, data1.GetProperty("ItemCount").GetInt32());
            Assert.Equal(4, data1.GetProperty("DataList").GetArrayLength());

            // 2. Ca lọc OrgID="7200000001" -> 200 OK, ItemCount=3, DataList=3
            var req2 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_columnconfiggroup/dosearch", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(3, data2.GetProperty("ItemCount").GetInt32());
            Assert.Equal(3, data2.GetProperty("DataList").GetArrayLength());

            // 3. Ca lọc OrgID="7200000002" -> 200 OK, ItemCount=1, DataList=1 (khác số dòng với ca 2)
            var req3 = new StringContent("{\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_columnconfiggroup/dosearch", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(1, data3.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data3.GetProperty("DataList").GetArrayLength());

            // 4. Ca lọc ColumnGrpName="tiền tệ" -> 200 OK, ItemCount=1, GRP01
            var req4 = new StringContent("{\"ColumnGrpName\":\"tiền tệ\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_columnconfiggroup/dosearch", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(1, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal("GRP01", data4.GetProperty("DataList")[0].GetProperty("ColumnConfigGrpCode").GetString());

            // 5. Ca phân trang Page=0, Recordcount=2 -> 200 OK, ItemCount=4, PageCount=2, DataList=2
            var req5 = new StringContent("{\"Page\":0,\"Recordcount\":2}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_columnconfiggroup/dosearch", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal(4, data5.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data5.GetProperty("PageCount").GetInt32());
            Assert.Equal(2, data5.GetProperty("DataList").GetArrayLength());

            // 6. Ca không khớp: OrgID="NON_EXISTENT" -> 200 OK, ItemCount=0, PageCount=0, DataList=[]
            var req6 = new StringContent("{\"OrgID\":\"NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_columnconfiggroup/dosearch", req6);
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(0, data6.GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, data6.GetProperty("PageCount").GetInt32());
            Assert.Equal(0, data6.GetProperty("DataList").GetArrayLength());
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
                var shm = tempDb + "-shm";
                var wal = tempDb + "-wal";
                if (File.Exists(shm)) File.Delete(shm);
                if (File.Exists(wal)) File.Delete(wal);
            }
            catch { }
        }
    }

    [Fact]
    public async Task Create_NullRequest_ReturnsFailCheckStrJson()
    {
        var result = await _controller.Create(null);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_Create", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Create_EmptyFields_ReturnsFailCheckStrJson()
    {
        var result = await _controller.Create(new Mst_ColumnConfigGroup_CreateReq());
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_Create", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Create_MissingCode_ReturnsInvalidColumnConfigGrpCode()
    {
        var req = new Mst_ColumnConfigGroup_CreateReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = ""
        };
        var result = await _controller.Create(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_Create_InvalidColumnConfigGrpCode", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsColumnConfigGrpCodeExist()
    {
        var req = new Mst_ColumnConfigGroup_CreateReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP01",
            ColumnGrpName = "Trùng lặp GRP01"
        };
        var result = await _controller.Create(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_CheckDB_ColumnConfigGrpCodeExist", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Create_MissingOrgID_ReturnsInvalid_OrgID_NotNull()
    {
        var req = new Mst_ColumnConfigGroup_CreateReq
        {
            OrgID = "",
            ColumnConfigGrpCode = "GRP99",
            ColumnGrpName = "Thiếu OrgID"
        };
        var result = await _controller.Create(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_Create_Invalid_OrgID_NotNull", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Create_Success_RoundTrip()
    {
        var req = new Mst_ColumnConfigGroup_CreateReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP04",
            ColumnGrpName = "Nhóm cấu hình thuế VAT",
            ColumnGrpFormat = "0.0%",
            ColumnGrpDesc = "Tỷ lệ VAT"
        };
        var result = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP04", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("Nhóm cấu hình thuế VAT", data.GetProperty("ColumnGrpName").GetString());
        Assert.Equal("0.0%", data.GetProperty("ColumnGrpFormat").GetString());
        Assert.Equal("1", data.GetProperty("FlagActive").GetString());

        // Đọc lại để xác nhận round-trip
        var searchRes = await _controller.DoSearch(new Mst_ColumnConfigGroup_DoSearchReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP04"
        });
        var searchOk = Assert.IsType<OkObjectResult>(searchRes);
        var searchDoc = JsonDocument.Parse(JsonSerializer.Serialize(searchOk.Value));
        Assert.Equal(1, searchDoc.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Create_ViaModelJsonString_Success()
    {
        var req = new Mst_ColumnConfigGroup_CreateReq
        {
            Model = "{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP05\",\"ColumnGrpName\":\"Nhóm chiết khấu\"}"
        };
        var result = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP05", data.GetProperty("ColumnConfigGrpCode").GetString());
    }

    [Fact]
    public async Task RealServer_Create_Verification_Port8542()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_create_{Guid.NewGuid():N}.db");
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
        var freePort8542 = FreePort.Get(); psi.Environment["PORT"] = freePort8542.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort8542}"), Timeout = TimeSpan.FromSeconds(5) };
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

            // 1. Ca body rỗng: POST {} -> 400 Bad Request, Erridn.Skycic.QContract.Mst_ColumnConfigGroup_Create
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_columnconfiggroup/create", req1);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.False(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_ColumnConfigGroup_Create", doc1.RootElement.GetProperty("ErrorCode").GetString()!);

            // 2. Ca guard mã trống: POST {"OrgID":"7200000001","ColumnConfigGrpCode":""} -> 400
            var req2 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_columnconfiggroup/create", req2);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.False(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_ColumnConfigGroup_Create_InvalidColumnConfigGrpCode", doc2.RootElement.GetProperty("ErrorCode").GetString()!);

            // 3. Ca guard trùng mã: POST {"OrgID":"7200000001","ColumnConfigGrpCode":"GRP01"} -> 400
            var req3 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP01\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_columnconfiggroup/create", req3);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.False(doc3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_ColumnConfigGroup_CheckDB_ColumnConfigGrpCodeExist", doc3.RootElement.GetProperty("ErrorCode").GetString()!);

            // 4. Ca guard thiếu OrgID: POST {"OrgID":"","ColumnConfigGrpCode":"GRP99"} -> 400
            var req4 = new StringContent("{\"OrgID\":\"\",\"ColumnConfigGrpCode\":\"GRP99\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_columnconfiggroup/create", req4);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.False(doc4.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_ColumnConfigGroup_Create_Invalid_OrgID_NotNull", doc4.RootElement.GetProperty("ErrorCode").GetString()!);

            // 5. Ca thành công round-trip: POST {"OrgID":"7200000001","ColumnConfigGrpCode":"GRP04","ColumnGrpName":"Nhóm cấu hình thuế VAT","ColumnGrpFormat":"0.0%","ColumnGrpDesc":"Tỷ lệ VAT"} -> 200 OK
            var req5 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP04\",\"ColumnGrpName\":\"Nhóm cấu hình thuế VAT\",\"ColumnGrpFormat\":\"0.0%\",\"ColumnGrpDesc\":\"Tỷ lệ VAT\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_columnconfiggroup/create", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal("GRP04", data5.GetProperty("ColumnConfigGrpCode").GetString());

            // 6. Đọc lại qua dosearch xác nhận đã lưu vào DB thật
            var req6 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP04\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_columnconfiggroup/dosearch", req6);
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(1, data6.GetProperty("ItemCount").GetInt32());
            Assert.Equal("GRP04", data6.GetProperty("DataList")[0].GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("Nhóm cấu hình thuế VAT", data6.GetProperty("DataList")[0].GetProperty("ColumnGrpName").GetString());
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
                var shm = tempDb + "-shm";
                var wal = tempDb + "-wal";
                if (File.Exists(shm)) File.Delete(shm);
                if (File.Exists(wal)) File.Delete(wal);
            }
            catch { }
        }
    }

    [Fact]
    public async Task Update_NullBody_ReturnsSuccessWithNullData()
    {
        var result = await _controller.Update(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_EmptyCode_ReturnsSuccessWithNullData()
    {
        var req = new Mst_ColumnConfigGroup_UpdateReq();
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_NonExistentCode_ReturnsSuccessWithNullData()
    {
        var req = new Mst_ColumnConfigGroup_UpdateReq { ColumnConfigGrpCode = "NON_EXIST" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ExistingCode_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_UpdateReq { ColumnConfigGrpCode = "GRP01" };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP01", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("Nhóm cấu hình tiền tệ", data.GetProperty("ColumnGrpName").GetString());
    }

    [Fact]
    public async Task Update_ExistingCodeAndOrgID_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_UpdateReq
        {
            ColumnConfigGrpCode = "GRP01",
            OrgID = "7200000002"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP01", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("7200000002", data.GetProperty("OrgID").GetString());
        Assert.Equal("Nhóm cấu hình Org 2", data.GetProperty("ColumnGrpName").GetString());
    }

    [Fact]
    public async Task Update_ExistingCodeDifferentOrgID_ReturnsNullWhenMismatch()
    {
        var req = new Mst_ColumnConfigGroup_UpdateReq
        {
            ColumnConfigGrpCode = "GRP02",
            OrgID = "7200000002"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_ViaModelJsonString_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_UpdateReq
        {
            Model = "{\"ColumnConfigGrpCode\":\"GRP03\",\"OrgID\":\"7200000001\"}"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP03", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("Nhóm cấu hình tỷ lệ phần trăm", data.GetProperty("ColumnGrpName").GetString());
    }

    [Fact]
    public async Task Update_ViaRawModelString_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_UpdateReq
        {
            Model = "GRP02"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP02", data.GetProperty("ColumnConfigGrpCode").GetString());
    }

    [Fact]
    public async Task RealServer_Update_Verification_Port8543()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_update_{Guid.NewGuid():N}.db");
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
        var freePort8543 = FreePort.Get(); psi.Environment["PORT"] = freePort8543.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort8543}"), Timeout = TimeSpan.FromSeconds(5) };
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

            // 1. Ca body rỗng: POST {} -> 200 OK, Success=true, Data=null
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_columnconfiggroup/update", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc1.RootElement.GetProperty("Data").ValueKind);

            // 2. Ca mã không tồn tại: POST {"ColumnConfigGrpCode":"NON_EXIST"} -> 200 OK, Success=true, Data=null
            var req2 = new StringContent("{\"ColumnConfigGrpCode\":\"NON_EXIST\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_columnconfiggroup/update", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").ValueKind);

            // 3. Ca lọc bộ A (GRP01, OrgID=7200000001) -> 200 OK, Success=true, trả về bản ghi Org 1
            var req3 = new StringContent("{\"ColumnConfigGrpCode\":\"GRP01\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_columnconfiggroup/update", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data3.ValueKind);
            Assert.Equal("GRP01", data3.GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("7200000001", data3.GetProperty("OrgID").GetString());
            Assert.Equal("Nhóm cấu hình tiền tệ", data3.GetProperty("ColumnGrpName").GetString());

            // 4. Ca lọc bộ B (GRP01, OrgID=7200000002) -> 200 OK, Success=true, trả về bản ghi Org 2 (khác với ca 3)
            var req4 = new StringContent("{\"ColumnConfigGrpCode\":\"GRP01\",\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_columnconfiggroup/update", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data4.ValueKind);
            Assert.Equal("GRP01", data4.GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("7200000002", data4.GetProperty("OrgID").GetString());
            Assert.Equal("Nhóm cấu hình Org 2", data4.GetProperty("ColumnGrpName").GetString());

            // 5. Ca lọc bộ C (GRP02, OrgID=7200000002) -> 200 OK, Success=true, Data=null (GRP02 không thuộc Org 2)
            var req5 = new StringContent("{\"ColumnConfigGrpCode\":\"GRP02\",\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_columnconfiggroup/update", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc5.RootElement.GetProperty("Data").ValueKind);

            // 6. Ca gửi qua Model JSON string -> 200 OK, Success=true, GRP03
            var req6 = new StringContent("{\"Model\":\"{\\\"ColumnConfigGrpCode\\\":\\\"GRP03\\\"}\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_columnconfiggroup/update", req6);
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data6.ValueKind);
            Assert.Equal("GRP03", data6.GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("##0.00%", data6.GetProperty("ColumnGrpFormat").GetString());
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
                var shm = tempDb + "-shm";
                var wal = tempDb + "-wal";
                if (File.Exists(shm)) File.Delete(shm);
                if (File.Exists(wal)) File.Delete(wal);
            }
            catch { }
        }
    }

    [Fact]
    public async Task Edit_NullRequest_ReturnsFailCheckStrJson()
    {
        var result = await _controller.Edit(null);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_Update", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Edit_EmptyFields_ReturnsFailCheckStrJson()
    {
        var result = await _controller.Edit(new Mst_ColumnConfigGroup_EditReq());
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_Update", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Edit_MissingCode_ReturnsCheckDBColumnConfigGrpCodeNotFound()
    {
        var req = new Mst_ColumnConfigGroup_EditReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "",
            ColumnGrpName = "Cập nhật không mã"
        };
        var result = await _controller.Edit(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_CheckDB_ColumnConfigGrpCodeNotFound", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Edit_NonExistentCode_ReturnsCheckDBColumnConfigGrpCodeNotFound()
    {
        var req = new Mst_ColumnConfigGroup_EditReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "NON_EXIST",
            ColumnGrpName = "Nhóm không tồn tại"
        };
        var result = await _controller.Edit(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Contains("Mst_ColumnConfigGroup_CheckDB_ColumnConfigGrpCodeNotFound", root.GetProperty("ErrorCode").GetString()!);
    }

    [Fact]
    public async Task Edit_Success_RoundTrip()
    {
        var req = new Mst_ColumnConfigGroup_EditReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP01",
            ColumnGrpName = "Nhóm cấu hình tiền tệ cập nhật",
            ColumnGrpFormat = "###,###,###.00",
            ColumnGrpDesc = "Mô tả tiền tệ mới",
            FlagActive = "1"
        };
        var result = await _controller.Edit(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP01", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("Nhóm cấu hình tiền tệ cập nhật", data.GetProperty("ColumnGrpName").GetString());
        Assert.Equal("###,###,###.00", data.GetProperty("ColumnGrpFormat").GetString());
        Assert.Equal("Mô tả tiền tệ mới", data.GetProperty("ColumnGrpDesc").GetString());

        // Đọc lại để xác nhận round-trip lưu vào DB
        var updateCheck = await _controller.Update(new Mst_ColumnConfigGroup_UpdateReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP01"
        });
        var checkOk = Assert.IsType<OkObjectResult>(updateCheck);
        var checkDoc = JsonDocument.Parse(JsonSerializer.Serialize(checkOk.Value));
        var checkData = checkDoc.RootElement.GetProperty("Data");
        Assert.Equal("Nhóm cấu hình tiền tệ cập nhật", checkData.GetProperty("ColumnGrpName").GetString());
        Assert.Equal("###,###,###.00", checkData.GetProperty("ColumnGrpFormat").GetString());
        Assert.Equal("Mô tả tiền tệ mới", checkData.GetProperty("ColumnGrpDesc").GetString());
    }

    [Fact]
    public async Task Edit_ViaModelJsonString_Success()
    {
        var req = new Mst_ColumnConfigGroup_EditReq
        {
            Model = "{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP02\",\"ColumnGrpName\":\"Nhóm số lượng sửa qua model\",\"ColumnGrpFormat\":\"###,###\"}"
        };
        var result = await _controller.Edit(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("GRP02", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("Nhóm số lượng sửa qua model", data.GetProperty("ColumnGrpName").GetString());
        Assert.Equal("###,###", data.GetProperty("ColumnGrpFormat").GetString());
    }

    [Fact]
    public async Task RealServer_Edit_Verification_Port8544()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_edit_{Guid.NewGuid():N}.db");
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
        var freePort8544 = FreePort.Get(); psi.Environment["PORT"] = freePort8544.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort8544}"), Timeout = TimeSpan.FromSeconds(5) };
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

            // 1. Ca body rỗng: POST {} -> 400 Bad Request, Erridn.Skycic.QContract.Mst_ColumnConfigGroup_Update
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_columnconfiggroup/edit", req1);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.False(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_ColumnConfigGroup_Update", doc1.RootElement.GetProperty("ErrorCode").GetString()!);

            // 2. Ca guard mã trống: POST {"OrgID":"7200000001","ColumnConfigGrpCode":""} -> 400
            var req2 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"\",\"ColumnGrpName\":\"Test\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_columnconfiggroup/edit", req2);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.False(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_ColumnConfigGroup_CheckDB_ColumnConfigGrpCodeNotFound", doc2.RootElement.GetProperty("ErrorCode").GetString()!);

            // 3. Ca guard mã không tồn tại: POST {"OrgID":"7200000001","ColumnConfigGrpCode":"NON_EXIST","ColumnGrpName":"Test"} -> 400
            var req3 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"NON_EXIST\",\"ColumnGrpName\":\"Test\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_columnconfiggroup/edit", req3);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.False(doc3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mst_ColumnConfigGroup_CheckDB_ColumnConfigGrpCodeNotFound", doc3.RootElement.GetProperty("ErrorCode").GetString()!);

            // 4. Ca thành công round-trip: POST sửa GRP01 -> 200 OK
            var req4 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP01\",\"ColumnGrpName\":\"Tiền tệ VNĐ cập nhật\",\"ColumnGrpFormat\":\"###,###,### VNĐ\",\"ColumnGrpDesc\":\"Định dạng VNĐ\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_columnconfiggroup/edit", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal("GRP01", data4.GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("Tiền tệ VNĐ cập nhật", data4.GetProperty("ColumnGrpName").GetString());

            // 5. Đọc lại qua update endpoint xác nhận đã lưu vào DB thật
            var req5 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP01\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_columnconfiggroup/update", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            var data5 = doc5.RootElement.GetProperty("Data");
            Assert.Equal("Tiền tệ VNĐ cập nhật", data5.GetProperty("ColumnGrpName").GetString());
            Assert.Equal("###,###,### VNĐ", data5.GetProperty("ColumnGrpFormat").GetString());
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
                var shm = tempDb + "-shm";
                var wal = tempDb + "-wal";
                if (File.Exists(shm)) File.Delete(shm);
                if (File.Exists(wal)) File.Delete(wal);
            }
            catch { }
        }
    }

    [Fact]
    public async Task Detail_NullBody_ReturnsSuccessWithNullData()
    {
        var result = await _controller.Detail(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_EmptyCode_ReturnsSuccessWithNullData()
    {
        var req = new Mst_ColumnConfigGroup_DetailReq();
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_NonExistentCode_ReturnsSuccessWithNullData()
    {
        var req = new Mst_ColumnConfigGroup_DetailReq { ColumnConfigGrpCode = "NON_EXIST" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ExistingCode_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_DetailReq { ColumnConfigGrpCode = "GRP01" };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP01", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("7200000001", data.GetProperty("OrgID").GetString());
        Assert.Equal("Nhóm cấu hình tiền tệ", data.GetProperty("ColumnGrpName").GetString());
    }

    [Fact]
    public async Task Detail_ExistingCodeAndOrgID_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_DetailReq
        {
            ColumnConfigGrpCode = "GRP01",
            OrgID = "7200000002"
        };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP01", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("7200000002", data.GetProperty("OrgID").GetString());
        Assert.Equal("Nhóm cấu hình Org 2", data.GetProperty("ColumnGrpName").GetString());
    }

    [Fact]
    public async Task Detail_ExistingCodeDifferentOrgID_ReturnsNullWhenMismatch()
    {
        var req = new Mst_ColumnConfigGroup_DetailReq
        {
            ColumnConfigGrpCode = "GRP02",
            OrgID = "7200000002"
        };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Detail_ViaModelJsonString_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_DetailReq
        {
            Model = "{\"ColumnConfigGrpCode\":\"GRP03\",\"OrgID\":\"7200000001\"}"
        };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP03", data.GetProperty("ColumnConfigGrpCode").GetString());
        Assert.Equal("Nhóm cấu hình tỷ lệ phần trăm", data.GetProperty("ColumnGrpName").GetString());
    }

    [Fact]
    public async Task Detail_ViaRawModelString_ReturnsMatchedRecord()
    {
        var req = new Mst_ColumnConfigGroup_DetailReq
        {
            Model = "GRP02"
        };
        var result = await _controller.Detail(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal("GRP02", data.GetProperty("ColumnConfigGrpCode").GetString());
    }

    [Fact]
    public async Task RealServer_Detail_Verification_Port8545()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_detail_{Guid.NewGuid():N}.db");
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
        var freePort8545 = FreePort.Get(); psi.Environment["PORT"] = freePort8545.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort8545}"), Timeout = TimeSpan.FromSeconds(5) };
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

            // 1. Ca body rỗng: POST {} -> 200 OK, Success=true, Data=null
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_columnconfiggroup/detail", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc1.RootElement.GetProperty("Data").ValueKind);

            // 2. Ca mã không tồn tại: POST {"ColumnConfigGrpCode":"NON_EXIST"} -> 200 OK, Success=true, Data=null
            var req2 = new StringContent("{\"ColumnConfigGrpCode\":\"NON_EXIST\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_columnconfiggroup/detail", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").ValueKind);

            // 3. Ca lọc bộ A (GRP01, OrgID=7200000001) -> 200 OK, Success=true, trả về bản ghi Org 1
            var req3 = new StringContent("{\"ColumnConfigGrpCode\":\"GRP01\",\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_columnconfiggroup/detail", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data3.ValueKind);
            Assert.Equal("GRP01", data3.GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("7200000001", data3.GetProperty("OrgID").GetString());
            Assert.Equal("Nhóm cấu hình tiền tệ", data3.GetProperty("ColumnGrpName").GetString());

            // 4. Ca lọc bộ B (GRP01, OrgID=7200000002) -> 200 OK, Success=true, trả về bản ghi Org 2 (khác với ca 3)
            var req4 = new StringContent("{\"ColumnConfigGrpCode\":\"GRP01\",\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_columnconfiggroup/detail", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data4.ValueKind);
            Assert.Equal("GRP01", data4.GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("7200000002", data4.GetProperty("OrgID").GetString());
            Assert.Equal("Nhóm cấu hình Org 2", data4.GetProperty("ColumnGrpName").GetString());

            // 5. Ca lọc bộ C (GRP02, OrgID=7200000002) -> 200 OK, Success=true, Data=null (GRP02 không thuộc Org 2)
            var req5 = new StringContent("{\"ColumnConfigGrpCode\":\"GRP02\",\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_columnconfiggroup/detail", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc5.RootElement.GetProperty("Data").ValueKind);

            // 6. Ca gửi qua Model JSON string -> 200 OK, Success=true, GRP03
            var req6 = new StringContent("{\"Model\":\"{\\\"ColumnConfigGrpCode\\\":\\\"GRP03\\\"}\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_columnconfiggroup/detail", req6);
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.Equal(JsonValueKind.Object, data6.ValueKind);
            Assert.Equal("GRP03", data6.GetProperty("ColumnConfigGrpCode").GetString());
            Assert.Equal("##0.00%", data6.GetProperty("ColumnGrpFormat").GetString());
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
                var shm = tempDb + "-shm";
                var wal = tempDb + "-wal";
                if (File.Exists(shm)) File.Delete(shm);
                if (File.Exists(wal)) File.Delete(wal);
            }
            catch { }
        }
    }

    [Fact]
    public async Task Delete_NullBody_ReturnsSuccessWithEmptyGroupMessage()
    {
        var result = await _controller.Delete(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Contains("Nhóm cấu hình trống!", msgs[0].GetString()!);
    }

    [Fact]
    public async Task Delete_EmptyCode_ReturnsSuccessWithEmptyGroupMessage()
    {
        var req = new Mst_ColumnConfigGroup_DeleteReq { ColumnConfigGrpCode = "" };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Contains("Nhóm cấu hình trống!", msgs[0].GetString()!);
    }

    [Fact]
    public async Task Delete_NonExistentCode_ReturnsSuccessWithNotFoundMessage()
    {
        var req = new Mst_ColumnConfigGroup_DeleteReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "NON_EXIST"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Contains("Mã nhóm cấu hình'NON_EXIST' không có trong hệ thống!", msgs[0].GetString()!);
    }

    [Fact]
    public async Task Delete_CodeInDifferentOrg_ReturnsSuccessWithNotFoundMessage()
    {
        var req = new Mst_ColumnConfigGroup_DeleteReq
        {
            OrgID = "7200000002",
            ColumnConfigGrpCode = "GRP02"
        };
        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Contains("Mã nhóm cấu hình'GRP02' không có trong hệ thống!", msgs[0].GetString()!);
    }

    [Fact]
    public async Task Delete_Success_RoundTrip()
    {
        // Kiểm tra trước khi xóa: GRP03 tồn tại ở Org 7200000001
        var detailBefore = await _controller.Detail(new Mst_ColumnConfigGroup_DetailReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP03"
        });
        var okBefore = Assert.IsType<OkObjectResult>(detailBefore);
        var docBefore = JsonDocument.Parse(JsonSerializer.Serialize(okBefore.Value));
        Assert.Equal("GRP03", docBefore.RootElement.GetProperty("Data").GetProperty("ColumnConfigGrpCode").GetString());

        // Thực hiện xóa
        var req = new Mst_ColumnConfigGroup_DeleteReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP03"
        };
        var result = await _controller.Delete(req);
        var ok = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Contains("Xóa nhóm cấu hình thành công!", msgs[0].GetString()!);
        Assert.Equal("GRP03", root.GetProperty("Data").GetProperty("ColumnConfigGrpCode").GetString());

        // Đọc lại để xác nhận round-trip: bản ghi không còn trong DB
        var detailAfter = await _controller.Detail(new Mst_ColumnConfigGroup_DetailReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP03"
        });
        var okAfter = Assert.IsType<OkObjectResult>(detailAfter);
        var docAfter = JsonDocument.Parse(JsonSerializer.Serialize(okAfter.Value));
        Assert.Equal(JsonValueKind.Null, docAfter.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Delete_ViaModelJsonString_Success()
    {
        var req = new Mst_ColumnConfigGroup_DeleteReq
        {
            Model = "{\"OrgID\":\"7200000002\",\"ColumnConfigGrpCode\":\"GRP01\"}"
        };
        var result = await _controller.Delete(req);
        var ok = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var msgs = root.GetProperty("Messages");
        Assert.Contains("Xóa nhóm cấu hình thành công!", msgs[0].GetString()!);

        // Kiểm tra Org 2 GRP01 đã bị xóa
        var detailOrg2 = await _controller.Detail(new Mst_ColumnConfigGroup_DetailReq
        {
            OrgID = "7200000002",
            ColumnConfigGrpCode = "GRP01"
        });
        var okOrg2 = Assert.IsType<OkObjectResult>(detailOrg2);
        var docOrg2 = JsonDocument.Parse(JsonSerializer.Serialize(okOrg2.Value));
        Assert.Equal(JsonValueKind.Null, docOrg2.RootElement.GetProperty("Data").ValueKind);

        // Org 1 GRP01 vẫn còn tồn tại
        var detailOrg1 = await _controller.Detail(new Mst_ColumnConfigGroup_DetailReq
        {
            OrgID = "7200000001",
            ColumnConfigGrpCode = "GRP01"
        });
        var okOrg1 = Assert.IsType<OkObjectResult>(detailOrg1);
        var docOrg1 = JsonDocument.Parse(JsonSerializer.Serialize(okOrg1.Value));
        Assert.Equal(JsonValueKind.Object, docOrg1.RootElement.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task RealServer_Delete_Verification_Port8546()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_delete_{Guid.NewGuid():N}.db");
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
        var freePort8546 = FreePort.Get(); psi.Environment["PORT"] = freePort8546.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort8546}"), Timeout = TimeSpan.FromSeconds(5) };
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

            // 1. Ca body rỗng: POST {} -> 200 OK, Success=true, Messages=["Nhóm cấu hình trống!"]
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_columnconfiggroup/delete", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Nhóm cấu hình trống!", doc1.RootElement.GetProperty("Messages")[0].GetString()!);

            // 2. Ca guard mã trống: POST {"OrgID":"7200000001","ColumnConfigGrpCode":""} -> 200 OK, Messages=["Nhóm cấu hình trống!"]
            var req2 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_columnconfiggroup/delete", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Nhóm cấu hình trống!", doc2.RootElement.GetProperty("Messages")[0].GetString()!);

            // 3. Ca guard mã không tồn tại: POST {"OrgID":"7200000001","ColumnConfigGrpCode":"NON_EXIST"} -> 200 OK, Messages=["Mã nhóm cấu hình'NON_EXIST' không có trong hệ thống!"]
            var req3 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"NON_EXIST\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_columnconfiggroup/delete", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mã nhóm cấu hình'NON_EXIST' không có trong hệ thống!", doc3.RootElement.GetProperty("Messages")[0].GetString()!);

            // 4. Ca guard mã tồn tại ở Org khác: POST {"OrgID":"7200000002","ColumnConfigGrpCode":"GRP02"} -> 200 OK
            var req4 = new StringContent("{\"OrgID\":\"7200000002\",\"ColumnConfigGrpCode\":\"GRP02\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_columnconfiggroup/delete", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Mã nhóm cấu hình'GRP02' không có trong hệ thống!", doc4.RootElement.GetProperty("Messages")[0].GetString()!);

            // 5. Ca thành công round-trip: POST xóa GRP03 -> 200 OK
            var req5 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP03\"}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_columnconfiggroup/delete", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Xóa nhóm cấu hình thành công!", doc5.RootElement.GetProperty("Messages")[0].GetString()!);
            Assert.Equal("GRP03", doc5.RootElement.GetProperty("Data").GetProperty("ColumnConfigGrpCode").GetString());

            // 6. Đọc lại qua detail endpoint xác nhận đã xóa thật khỏi DB
            var req6 = new StringContent("{\"OrgID\":\"7200000001\",\"ColumnConfigGrpCode\":\"GRP03\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_columnconfiggroup/detail", req6);
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc6.RootElement.GetProperty("Data").ValueKind);

            // 7. Ca xóa qua Model JSON string: xóa Org 2 GRP01 -> 200 OK
            var req7 = new StringContent("{\"Model\":\"{\\\"OrgID\\\":\\\"7200000002\\\",\\\"ColumnConfigGrpCode\\\":\\\"GRP01\\\"}\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_columnconfiggroup/delete", req7);
            Assert.Equal(System.Net.HttpStatusCode.OK, res7.StatusCode);
            var body7 = await res7.Content.ReadAsStringAsync();
            var doc7 = JsonDocument.Parse(body7);
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Contains("Xóa nhóm cấu hình thành công!", doc7.RootElement.GetProperty("Messages")[0].GetString()!);
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
                var shm = tempDb + "-shm";
                var wal = tempDb + "-wal";
                if (File.Exists(shm)) File.Delete(shm);
                if (File.Exists(wal)) File.Delete(wal);
            }
            catch { }
        }
    }
}
