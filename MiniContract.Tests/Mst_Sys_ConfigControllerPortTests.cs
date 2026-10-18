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

public class Mst_Sys_ConfigControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_Sys_ConfigControllerPort _controller;

    public Mst_Sys_ConfigControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_Sys_Config
        var sysConfigSeeder = new Mst_Sys_ConfigSeeder();
        sysConfigSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        // Chạy seeder Mst_Attribute_Contract
        var attrSeeder = new Mst_Attribute_ContractSeeder();
        attrSeeder.SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_Sys_ConfigControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_NullReq_SuccessWithAllRecords()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(3, listConfig.GetArrayLength());

        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
    }

    [Fact]
    public async Task Index_EmptyReq_SuccessWithAllRecords()
    {
        var req = new Mst_Sys_Config_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(3, listConfig.GetArrayLength());

        var firstConfig = data.GetProperty("Mst_Sys_Config");
        Assert.Equal(JsonValueKind.Object, firstConfig.ValueKind);
        Assert.False(string.IsNullOrEmpty(firstConfig.GetProperty("OrgID").GetString()));
    }

    [Fact]
    public async Task Index_FilterByOrgID_ReturnsSingleMatchedConfig()
    {
        var req = new Mst_Sys_Config_IndexReq { OrgID = "7200000001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(1, listConfig.GetArrayLength());
        Assert.Equal("7200000001", listConfig[0].GetProperty("OrgID").GetString());
        Assert.Equal("1001", listConfig[0].GetProperty("NetworkID").GetString());
        Assert.Equal("1", listConfig[0].GetProperty("FlagOTP").GetString());

        var configObj = data.GetProperty("Mst_Sys_Config");
        Assert.Equal("7200000001", configObj.GetProperty("OrgID").GetString());

        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByNonExistentOrgID_ReturnsZeroConfigs()
    {
        var req = new Mst_Sys_Config_IndexReq { OrgID = "9999999999" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(0, listConfig.GetArrayLength());

        var configObj = data.GetProperty("Mst_Sys_Config");
        Assert.Equal(JsonValueKind.Null, configObj.ValueKind);

        // Thuộc tính hợp đồng vẫn được trả về đầy đủ
        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByNetworkID_ReturnsFilteredConfigsAndAttributes()
    {
        var req = new Mst_Sys_Config_IndexReq { NetworkID = "1001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());

        var listConfig = data.GetProperty("Lst_Mst_Sys_Config");
        Assert.Equal(2, listConfig.GetArrayLength());
        foreach (var c in listConfig.EnumerateArray())
        {
            Assert.Equal("1001", c.GetProperty("NetworkID").GetString());
        }

        var listAttr = data.GetProperty("ListMst_Attribute_Contract");
        Assert.Equal(3, listAttr.GetArrayLength());
        foreach (var a in listAttr.EnumerateArray())
        {
            Assert.Equal("1001", a.GetProperty("NetworkID").GetString());
        }
    }

    [Fact]
    public async Task Index_Attributes_OrderedByCodeAscending()
    {
        var req = new Mst_Sys_Config_IndexReq();
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var data = doc.RootElement.GetProperty("Data");
        var listAttr = data.GetProperty("ListMst_Attribute_Contract");

        Assert.Equal(3, listAttr.GetArrayLength());
        Assert.Equal("ATTR01", listAttr[0].GetProperty("AttributeContractCode").GetString());
        Assert.Equal("ATTR02", listAttr[1].GetProperty("AttributeContractCode").GetString());
        Assert.Equal("ATTR03", listAttr[2].GetProperty("AttributeContractCode").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Port8544()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_sys_config_{Guid.NewGuid():N}.db");
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
        var freePort = FreePort.Get();
        psi.Environment["PORT"] = freePort.ToString();
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

            // 1. Ca body rỗng: POST {} -> 200 OK, ItemCount=3, Lst_Mst_Sys_Config=3, ListMst_Attribute_Contract=3
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_sys_config/index", req1);
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var body1 = await res1.Content.ReadAsStringAsync();
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            var data1 = doc1.RootElement.GetProperty("Data");
            Assert.Equal(3, data1.GetProperty("ItemCount").GetInt32());
            Assert.Equal(3, data1.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());
            Assert.Equal(3, data1.GetProperty("ListMst_Attribute_Contract").GetArrayLength());

            // 2. Ca lọc OrgID="7200000001" -> 200 OK, ItemCount=1, Lst_Mst_Sys_Config=1, OrgID khớp
            var req2 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_sys_config/index", req2);
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var body2 = await res2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            var data2 = doc2.RootElement.GetProperty("Data");
            Assert.Equal(1, data2.GetProperty("ItemCount").GetInt32());
            Assert.Equal(1, data2.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());
            Assert.Equal("7200000001", data2.GetProperty("Lst_Mst_Sys_Config")[0].GetProperty("OrgID").GetString());

            // 3. Ca lọc OrgID="9999999999" (không tồn tại) -> 200 OK, ItemCount=0, Lst_Mst_Sys_Config=0 (khác số dòng với ca 2)
            var req3 = new StringContent("{\"OrgID\":\"9999999999\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_sys_config/index", req3);
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var body3 = await res3.Content.ReadAsStringAsync();
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            var data3 = doc3.RootElement.GetProperty("Data");
            Assert.Equal(0, data3.GetProperty("ItemCount").GetInt32());
            Assert.Equal(0, data3.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());
            Assert.Equal(3, data3.GetProperty("ListMst_Attribute_Contract").GetArrayLength());

            // 4. Ca lọc NetworkID="1001" -> 200 OK, ItemCount=2, Lst_Mst_Sys_Config=2
            var req4 = new StringContent("{\"NetworkID\":\"1001\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_sys_config/index", req4);
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var body4 = await res4.Content.ReadAsStringAsync();
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            var data4 = doc4.RootElement.GetProperty("Data");
            Assert.Equal(2, data4.GetProperty("ItemCount").GetInt32());
            Assert.Equal(2, data4.GetProperty("Lst_Mst_Sys_Config").GetArrayLength());
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
}
