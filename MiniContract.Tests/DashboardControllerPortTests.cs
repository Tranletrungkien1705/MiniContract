using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class DashboardControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly DashboardControllerPort _controller;

    public DashboardControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Seed dữ liệu mẫu: Sys_User (Order 101), Contract_Contract (Order 211), Contract_ContractUser (Order 217)
        new Sys_UserSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Contract_ContractUserSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new DashboardControllerPort(_db);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private static JsonElement ExtractJson(IActionResult result)
    {
        if (result is OkObjectResult ok)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;
        }
        if (result is BadRequestObjectResult bad)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(bad.Value)).RootElement;
        }
        throw new InvalidOperationException("Unexpected result type: " + result?.GetType().Name);
    }

    [Fact]
    public async Task DashboardLicense_NullReq_ReturnsSuccessWithDefaultCounts()
    {
        // Ca 1: Body rỗng (null) -> waUserCode rỗng:
        // ContractCreatePending = 1 (HD0003 có ContractStatus="ONPROCESS")
        // PendingForUser = 0
        // PendingForOther = 3 (HD0001, HD0003, HD0004 có UserSignSatus="PENDING")
        var result = await _controller.DashboardLicense(null);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetArrayLength());

        var item0 = data[0];
        Assert.Equal("ContractCreatePending", item0.GetProperty("ReportType").GetString());
        Assert.Equal(1, item0.GetProperty("TotalQty").GetDouble());

        var item1 = data[1];
        Assert.Equal("PendingForUser", item1.GetProperty("ReportType").GetString());
        Assert.Equal(0, item1.GetProperty("TotalQty").GetDouble());

        var item2 = data[2];
        Assert.Equal("PendingForOther", item2.GetProperty("ReportType").GetString());
        Assert.Equal(3, item2.GetProperty("TotalQty").GetDouble());

        var listInv = root.GetProperty("ListInvoice_License");
        Assert.Equal(0, listInv.GetArrayLength());
    }

    [Fact]
    public async Task DashboardLicense_UserU01_ReturnsExpectedCountsAndMST()
    {
        // Ca 2: Người dùng U01 (admin@solution.hn)
        // ContractCreatePending = 1 (HD0003)
        // PendingForUser = 2 (HD0001, HD0003 có U01 ký PENDING)
        // PendingForOther = 2 (HD0003, HD0004 có U02 ký PENDING)
        // MST tự động tra từ Sys_User của U01 là "0100000001"
        var req = new Dashboard_DashboardLicenseReq
        {
            WAUserCode = "U01"
        };
        var result = await _controller.DashboardLicense(req);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("0100000001", root.GetProperty("MST").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal(3, data.GetArrayLength());

        Assert.Equal("ContractCreatePending", data[0].GetProperty("ReportType").GetString());
        Assert.Equal(1, data[0].GetProperty("TotalQty").GetDouble());

        Assert.Equal("PendingForUser", data[1].GetProperty("ReportType").GetString());
        Assert.Equal(2, data[1].GetProperty("TotalQty").GetDouble());

        Assert.Equal("PendingForOther", data[2].GetProperty("ReportType").GetString());
        Assert.Equal(2, data[2].GetProperty("TotalQty").GetDouble());
    }

    [Fact]
    public async Task DashboardLicense_UserU02_ReturnsDifferentCounts()
    {
        // Ca 3: Người dùng U02 (tester@solution.hn) - bộ lọc khác ra số lượng khác
        // ContractCreatePending = 1 (HD0003)
        // PendingForUser = 2 (HD0003, HD0004 có U02 ký PENDING)
        // PendingForOther = 2 (HD0001, HD0003 có U01 ký PENDING)
        var req = new Dashboard_DashboardLicenseReq
        {
            UserCode = "U02",
            MST = "CUSTOM_MST"
        };
        var result = await _controller.DashboardLicense(req);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("CUSTOM_MST", root.GetProperty("MST").GetString());

        var data = root.GetProperty("Data");
        Assert.Equal("PendingForUser", data[1].GetProperty("ReportType").GetString());
        Assert.Equal(2, data[1].GetProperty("TotalQty").GetDouble());

        Assert.Equal("PendingForOther", data[2].GetProperty("ReportType").GetString());
        Assert.Equal(2, data[2].GetProperty("TotalQty").GetDouble());
    }

    [Fact]
    public async Task DashboardLicense_UserU03_ReturnsZeroPendingForUser()
    {
        // Ca 4: Người dùng U03 không có hợp đồng nào chờ ký:
        // ContractCreatePending = 1
        // PendingForUser = 0
        // PendingForOther = 3 (HD0001, HD0003, HD0004)
        var req = new Dashboard_DashboardLicenseReq
        {
            WAUserCode = "U03"
        };
        var result = await _controller.DashboardLicense(req);
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal("PendingForUser", data[1].GetProperty("ReportType").GetString());
        Assert.Equal(0, data[1].GetProperty("TotalQty").GetDouble());

        Assert.Equal("PendingForOther", data[2].GetProperty("ReportType").GetString());
        Assert.Equal(3, data[2].GetProperty("TotalQty").GetDouble());
    }

    [Fact]
    public async Task DashboardLicense_HeaderUserCode_ReadsFromHeaders()
    {
        // Ca 5: Truyền user code qua HTTP Header thay vì body
        _controller.ControllerContext.HttpContext.Request.Headers["WAUserCode"] = "U01";

        var result = await _controller.DashboardLicense(new Dashboard_DashboardLicenseReq());
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal("PendingForUser", data[1].GetProperty("ReportType").GetString());
        Assert.Equal(2, data[1].GetProperty("TotalQty").GetDouble());
    }

    [Fact]
    public async Task DashboardLicense_EmptyDatabase_ReturnsZeros()
    {
        // Ca 6: Kiểm tra với DB trống
        using var emptyConn = new SqliteConnection("DataSource=:memory:");
        emptyConn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(emptyConn).Options;
        using var emptyDb = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        emptyDb.Database.EnsureCreated();

        var emptyCtrl = new DashboardControllerPort(emptyDb);
        emptyCtrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await emptyCtrl.DashboardLicense(new Dashboard_DashboardLicenseReq { WAUserCode = "U01" });
        var root = ExtractJson(result);

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        Assert.Equal(0, data[0].GetProperty("TotalQty").GetDouble());
        Assert.Equal(0, data[1].GetProperty("TotalQty").GetDouble());
        Assert.Equal(0, data[2].GetProperty("TotalQty").GetDouble());
    }

    [Fact]
    public async Task RealServer_VerifyDashboardLicenseAsync()
    {
        // Chạy kiểm thử trên server thật với FreePort.Get() và SQLite tạm (PORT_RULES §2.6)
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_dashboard_{Guid.NewGuid():N}.db");
        var dllPath = typeof(DashboardControllerPort).Assembly.Location;
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
                try
                {
                    var res = await http.GetAsync("/healthz");
                    if (res.IsSuccessStatusCode)
                    {
                        started = true;
                        break;
                    }
                }
                catch
                {
                    await Task.Delay(500);
                }
            }
            Assert.True(started, "Real server did not start within 15 seconds.");

            // 1. Gọi /api/dashboard/dashboardlicense với body rỗng {}
            var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var resEmpty = await http.PostAsync("/api/dashboard/dashboardlicense", emptyContent);
            Assert.True(resEmpty.IsSuccessStatusCode);
            var jsonEmpty = await resEmpty.Content.ReadAsStringAsync();
            var docEmpty = JsonDocument.Parse(jsonEmpty);
            Assert.True(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            var dataEmpty = docEmpty.RootElement.GetProperty("Data");
            Assert.Equal(1, dataEmpty[0].GetProperty("TotalQty").GetDouble());
            Assert.Equal(0, dataEmpty[1].GetProperty("TotalQty").GetDouble());
            Assert.Equal(3, dataEmpty[2].GetProperty("TotalQty").GetDouble());

            // 2. Gọi /api/dashboard/dashboardlicense với user U01
            var u01Payload = JsonSerializer.Serialize(new { WAUserCode = "U01" });
            var u01Content = new StringContent(u01Payload, Encoding.UTF8, "application/json");
            var resU01 = await http.PostAsync("/api/dashboard/dashboardlicense", u01Content);
            Assert.True(resU01.IsSuccessStatusCode);
            var jsonU01 = await resU01.Content.ReadAsStringAsync();
            var docU01 = JsonDocument.Parse(jsonU01);
            Assert.True(docU01.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("0100000001", docU01.RootElement.GetProperty("MST").GetString());
            var dataU01 = docU01.RootElement.GetProperty("Data");
            Assert.Equal(1, dataU01[0].GetProperty("TotalQty").GetDouble());
            Assert.Equal(2, dataU01[1].GetProperty("TotalQty").GetDouble());
            Assert.Equal(2, dataU01[2].GetProperty("TotalQty").GetDouble());

            // 3. Gọi /api/dashboard/dashboardlicense với user U02 (bộ lọc khác)
            var u02Payload = JsonSerializer.Serialize(new { UserCode = "U02", MST = "CUSTOM_MST" });
            var u02Content = new StringContent(u02Payload, Encoding.UTF8, "application/json");
            var resU02 = await http.PostAsync("/api/dashboard/dashboardlicense", u02Content);
            Assert.True(resU02.IsSuccessStatusCode);
            var jsonU02 = await resU02.Content.ReadAsStringAsync();
            var docU02 = JsonDocument.Parse(jsonU02);
            Assert.True(docU02.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("CUSTOM_MST", docU02.RootElement.GetProperty("MST").GetString());
            var dataU02 = docU02.RootElement.GetProperty("Data");
            Assert.Equal(1, dataU02[0].GetProperty("TotalQty").GetDouble());
            Assert.Equal(2, dataU02[1].GetProperty("TotalQty").GetDouble());
            Assert.Equal(2, dataU02[2].GetProperty("TotalQty").GetDouble());

            // 4. Gọi /api/dashboard/dashboardlicense với user U03 (bộ lọc khác)
            var u03Payload = JsonSerializer.Serialize(new { WAUserCode = "U03" });
            var u03Content = new StringContent(u03Payload, Encoding.UTF8, "application/json");
            var resU03 = await http.PostAsync("/api/dashboard/dashboardlicense", u03Content);
            Assert.True(resU03.IsSuccessStatusCode);
            var jsonU03 = await resU03.Content.ReadAsStringAsync();
            var docU03 = JsonDocument.Parse(jsonU03);
            Assert.True(docU03.RootElement.GetProperty("Success").GetBoolean());
            var dataU03 = docU03.RootElement.GetProperty("Data");
            Assert.Equal(1, dataU03[0].GetProperty("TotalQty").GetDouble());
            Assert.Equal(0, dataU03[1].GetProperty("TotalQty").GetDouble());
            Assert.Equal(3, dataU03[2].GetProperty("TotalQty").GetDouble());
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
                if (File.Exists(tempDb))
                {
                    File.Delete(tempDb);
                }
            }
            catch { }
        }
    }
}
