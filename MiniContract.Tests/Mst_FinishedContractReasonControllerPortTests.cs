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
    public async Task RealServer_Verification_Port8584()
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
        psi.Environment["PORT"] = "8584";
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8584"), Timeout = TimeSpan.FromSeconds(3) };
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
