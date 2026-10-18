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
