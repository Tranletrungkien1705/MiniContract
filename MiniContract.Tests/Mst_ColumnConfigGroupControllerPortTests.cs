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
        psi.Environment["PORT"] = "8541";
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8541"), Timeout = TimeSpan.FromSeconds(5) };
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
}
