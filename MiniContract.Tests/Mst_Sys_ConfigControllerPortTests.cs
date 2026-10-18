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
    public async Task Update_NullReq_ReturnsEmptyConfigListMessage()
    {
        var result = await _controller.Update(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Danh sách cấu hình trống!", messages[0].GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
    }

    [Fact]
    public async Task Update_EmptyReq_ReturnsEmptyConfigListMessage()
    {
        var req = new Mst_Sys_Config_UpdateReq();
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("Danh sách cấu hình trống!", messages[0].GetString());
    }

    [Fact]
    public async Task Update_NonExistentOrgID_ReturnsOrganNotFoundFail()
    {
        var req = new Mst_Sys_Config_UpdateReq { OrgID = "9999999999", FlagCurrency = "1" };
        var result = await _controller.Update(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Sys_Config_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
        var p = root.GetProperty("Params");
        Assert.Equal("9999999999", p.GetProperty("Check.OrgID").GetString());
    }

    [Fact]
    public async Task Update_ValidReq_RoundTrip_Success()
    {
        // Ban đầu trên OrgID 7200000001: FlagCurrency="0", FlagColmnConfig="0", FlagOTP="1"
        var req = new Mst_Sys_Config_UpdateReq
        {
            OrgID = "7200000001",
            FlagCurrency = "1",
            FlagColmnConfig = "1",
            FlagOTP = "0"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var messages = root.GetProperty("Messages");
        Assert.Equal("Sửa cấu hình thành công!", messages[0].GetString());

        // Kiểm tra round-trip đọc lại từ DB
        var updated = await _db.Set<Mst_Sys_Config>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(updated);
        Assert.Equal("1", updated.FlagCurrency);
        Assert.Equal("1", updated.FlagColmnConfig);
        Assert.Equal("0", updated.FlagOTP);
        Assert.False(string.IsNullOrEmpty(updated.LogLUDTimeUTC));
        Assert.Equal("port", updated.LogLUBy);
    }

    [Fact]
    public async Task Update_JsonModelString_Success()
    {
        var req = new Mst_Sys_Config_UpdateReq
        {
            Model = "{\"Mst_Sys_Config\":{\"OrgID\":\"7200000001\",\"FlagCurrency\":\"0\",\"FlagColmnConfig\":\"0\",\"FlagOTP\":\"1\"}}"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_Sys_Config>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(updated);
        Assert.Equal("0", updated.FlagCurrency);
        Assert.Equal("0", updated.FlagColmnConfig);
        Assert.Equal("1", updated.FlagOTP);
    }

    [Fact]
    public async Task Update_WithFtColsUpd_OnlyUpdatesSpecifiedColumns()
    {
        // OrgID 7200000002 ban đầu có: FlagCurrency="1", FlagColmnConfig="1", FlagOTP="0"
        var req = new Mst_Sys_Config_UpdateReq
        {
            OrgID = "7200000002",
            FlagOTP = "1",
            FlagCurrency = "0", // Không cập nhật vì Ft_Cols_Upd chỉ định FlagOTP
            Ft_Cols_Upd = "Mst_Sys_Config.FlagOTP"
        };
        var result = await _controller.Update(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_Sys_Config>().FirstOrDefaultAsync(x => x.OrgID == "7200000002");
        Assert.NotNull(updated);
        Assert.Equal("1", updated.FlagOTP); // Cột được chỉ định đã đổi từ 0 thành 1
        Assert.Equal("1", updated.FlagCurrency); // Cột không chỉ định giữ nguyên giá trị ban đầu là 1
        Assert.Equal("1", updated.FlagColmnConfig); // Giữ nguyên
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

            // 5. Ca Update body rỗng: POST {} -> 200 OK, Danh sách cấu hình trống! (Gate guard)
            var req5 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_sys_config/update", req5);
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var body5 = await res5.Content.ReadAsStringAsync();
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Danh sách cấu hình trống!", doc5.RootElement.GetProperty("Messages")[0].GetString());

            // 6. Ca Update guard OrgID không tồn tại -> 400 Bad Request, mã lỗi Mst_Sys_Config_CheckDB_OrganNotFound (Biz guard)
            var req6 = new StringContent("{\"OrgID\":\"9999999999\",\"FlagCurrency\":\"1\"}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_sys_config/update", req6);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res6.StatusCode);
            var body6 = await res6.Content.ReadAsStringAsync();
            var doc6 = JsonDocument.Parse(body6);
            Assert.False(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_Sys_Config_CheckDB_OrganNotFound", doc6.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal("9999999999", doc6.RootElement.GetProperty("Params").GetProperty("Check.OrgID").GetString());

            // 7. Ca Update round-trip thành công trên OrgID="7200000001"
            var req7 = new StringContent("{\"OrgID\":\"7200000001\",\"FlagCurrency\":\"1\",\"FlagColmnConfig\":\"1\",\"FlagOTP\":\"0\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_sys_config/update", req7);
            Assert.Equal(System.Net.HttpStatusCode.OK, res7.StatusCode);
            var body7 = await res7.Content.ReadAsStringAsync();
            var doc7 = JsonDocument.Parse(body7);
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Sửa cấu hình thành công!", doc7.RootElement.GetProperty("Messages")[0].GetString());

            // 8. Đọc lại qua Index để xác nhận giá trị round-trip thật trong DB
            var req8 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_sys_config/index", req8);
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var body8 = await res8.Content.ReadAsStringAsync();
            var doc8 = JsonDocument.Parse(body8);
            var updatedConfig = doc8.RootElement.GetProperty("Data").GetProperty("Lst_Mst_Sys_Config")[0];
            Assert.Equal("1", updatedConfig.GetProperty("FlagCurrency").GetString());
            Assert.Equal("1", updatedConfig.GetProperty("FlagColmnConfig").GetString());
            Assert.Equal("0", updatedConfig.GetProperty("FlagOTP").GetString());
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
