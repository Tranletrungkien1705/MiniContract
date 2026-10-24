using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
using Xunit;

namespace MiniContract.Tests;

/// <summary>
/// #90 DashboardRptController/DashboardSummary — nguồn DashboardRptController.cs:16-93
/// (→ ReportService.Instance.WA_Rpt_ContractForDashboard dòng 74).
/// Excerpt KHÔNG có tầng API/biz (dòng 134), KHÔNG có model/DDL (dòng 127-133), 0 mã lỗi (dòng 137-138)
/// ⇒ endpoint trả 400 GAP.NoBackend, KHÔNG có số liệu để đối chiếu (xem .parity-run\conflicts\90_conflict.md).
/// </summary>
public class DashboardRptControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly DashboardRptControllerPort _controller;

    public DashboardRptControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        _controller = new DashboardRptControllerPort();
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    private static JsonElement ExtractJson(IActionResult result)
    {
        if (result is BadRequestObjectResult bad)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(bad.Value)).RootElement;
        }
        throw new InvalidOperationException("Unexpected result type: " + result?.GetType().Name);
    }

    [Fact]
    public void DashboardSummary_NullBody_ReturnsGapNoBackend()
    {
        // Ca 1: body rỗng — nguồn vẫn gọi WA_Rpt_ContractForDashboard (dòng 74) ⇒ đích trả GAP.NoBackend.
        var root = ExtractJson(_controller.DashboardSummary(null));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.DashboardRpt_DashboardSummary", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("GAP.NoBackend", out var gap));
        Assert.Equal("WA_Rpt_ContractForDashboard", gap.GetString());
    }

    [Fact]
    public void DashboardSummary_WithNetworkId_ReturnsGapNoBackend()
    {
        // Ca 2: có networkid — gate chỉ chuyển tiếp sang dịch vụ ngoài (dòng 63, 74) ⇒ cùng GAP.NoBackend.
        var root = ExtractJson(_controller.DashboardSummary(new DashboardRptControllerPort.DashboardRpt_DashboardSummaryReq
        {
            NetworkID = "1001"
        }));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.DashboardRpt_DashboardSummary", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task RealServer_VerifyDashboardSummaryAsync()
    {
        // Server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm (PORT_RULES §2.6).
        var tempDb = Path.Combine(Path.GetTempPath(), $"verify_dashboardrpt_{Guid.NewGuid():N}.db");
        var dllPath = typeof(DashboardRptControllerPort).Assembly.Location;
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

            // 1. Body rỗng
            var resEmpty = await http.PostAsync("/api/dashboardrpt/dashboardsummary",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(await resEmpty.Content.ReadAsStringAsync());
            Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.DashboardRpt_DashboardSummary",
                docEmpty.RootElement.GetProperty("ErrorCode").GetString());

            // 2. Có networkid
            var resNet = await http.PostAsync("/api/dashboardrpt/dashboardsummary",
                new StringContent(JsonSerializer.Serialize(new { NetworkID = "1001" }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resNet.StatusCode);
            var docNet = JsonDocument.Parse(await resNet.Content.ReadAsStringAsync());
            Assert.False(docNet.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.DashboardRpt_DashboardSummary",
                docNet.RootElement.GetProperty("ErrorCode").GetString());
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
