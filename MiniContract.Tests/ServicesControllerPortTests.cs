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
/// #256 ServicesController/ContractPartySend — nguồn ServicesController.cs:417-585
/// (→ TClientService.*.Instance.WA_*_Get + SendChannel -> Mailgate/SMS/ZaloOA dòng 1206-1612).
/// Excerpt dòng 1617: KHÔNG có tầng API/Biz.Web, KHÔNG có hàm BIZ, KHÔNG có DDL.
/// ⇒ endpoint trả 400 GAP.NoBackend, không có số liệu để đối chiếu (xem .parity-run\conflicts\256_conflict.md).
/// </summary>
public class ServicesControllerPortTests : IDisposable
{
    // Marker ghi trong repo (MiniContract.Tests/TestResults) để có bằng chứng THẬT test đã chạy hết:
    // đầu ra `dotnet test` bị công cụ cắt mất dòng tổng kết.
    private static readonly string MarkerDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..", "TestResults"));

    private static void MarkDone(string name)
    {
        Directory.CreateDirectory(MarkerDir);
        File.WriteAllText(Path.Combine(MarkerDir, "s256_" + name + ".ok"), DateTime.UtcNow.ToString("o"));
    }

    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly ServicesControllerPort _controller;

    public ServicesControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        _controller = new ServicesControllerPort();
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
    public void ContractPartySend_NullBody_ReturnsGapNoBackend()
    {
        // Ca 1: body rỗng — nguồn ném "strJson is null" (dòng 571); đích trả GAP.NoBackend vì không có backend.
        var root = ExtractJson(_controller.ContractPartySend(null));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("GAP.NoBackend", out var gap));
        Assert.Contains("WA_Contract_Contract_Get", gap.GetString());
        MarkDone("nullbody");
    }

    [Fact]
    public void ContractPartySend_WithContract_ReturnsGapNoBackend()
    {
        // Ca 2: body có Lst_Contract_Contract — nguồn đi vào SendChannel (dịch vụ ngoài) ⇒ cùng GAP.NoBackend.
        var root = ExtractJson(_controller.ContractPartySend(new ServicesControllerPort.Services_ContractPartySendReq
        {
            NetworkID = "1001",
            FlagSendPartyCreate = "1",
            Lst_Contract_Contract = new() { new() { ContractCode = "CTR001" } },
        }));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend", root.GetProperty("ErrorCode").GetString());
        MarkDone("contract");
    }

    [Fact]
    public async Task RealServer_VerifyContractPartySendAsync()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"verify_services_{Guid.NewGuid():N}.db");
        var dllPath = typeof(ServicesControllerPort).Assembly.Location;
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
            var resEmpty = await http.PostAsync("/api/services/contractpartysend",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(await resEmpty.Content.ReadAsStringAsync());
            Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend",
                docEmpty.RootElement.GetProperty("ErrorCode").GetString());

            // 2. Body có hợp đồng
            var resContract = await http.PostAsync("/api/services/contractpartysend",
                new StringContent(JsonSerializer.Serialize(new
                {
                    NetworkID = "1001",
                    FlagSendPartyCreate = "1",
                    Lst_Contract_Contract = new[] { new { ContractCode = "CTR001" } },
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resContract.StatusCode);
            var docContract = JsonDocument.Parse(await resContract.Content.ReadAsStringAsync());
            Assert.False(docContract.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend",
                docContract.RootElement.GetProperty("ErrorCode").GetString());

            MarkDone("realserver");
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
