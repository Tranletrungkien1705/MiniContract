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
/// #256 ServicesController/ContractPartySend (nguồn ServicesController.cs:417-585)
/// #257 ServicesController/ContractPartySend_SSO (nguồn ServicesController.cs:587-756)
/// #258 ServicesController/ContractSend (nguồn ServicesController.cs:759-926)
/// #259 ServicesController/ContractResend (nguồn ServicesController.cs:927-1074)
/// Cả bốn: TClientService.*.Instance.WA_*_Get + iNOS SSO + MasterServer + SendChannel/ResendChannel
/// (Mailgate/SMS/ZaloOA). Excerpt #259 dòng 2066: KHÔNG có tầng API/Biz.Web; dòng 2070: KHÔNG có mã TError;
/// DDL chỉ tham chiếu master nguồn.
/// ⇒ cả bốn endpoint trả 400 GAP.NoBackend; xem .parity-run\conflicts\256_conflict.md … 259_conflict.md.
/// </summary>
public class ServicesControllerPortTests : IDisposable
{
    // Marker để có bằng chứng THẬT test đã chạy hết (stdout của `dotnet test` hay bị cắt).
    private static readonly string MarkerDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..", "TestResults"));

    private static void MarkDone(string name)
    {
        Directory.CreateDirectory(MarkerDir);
        File.WriteAllText(Path.Combine(MarkerDir, "s257_" + name + ".ok"), DateTime.UtcNow.ToString("o"));
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
            return JsonDocument.Parse(JsonSerializer.Serialize(bad.Value)).RootElement;
        throw new InvalidOperationException("Unexpected result type: " + result?.GetType().Name);
    }

    // ================= #256 ContractPartySend =================

    [Fact]
    public void ContractPartySend_NullBody_ReturnsGapNoBackend()
    {
        var root = ExtractJson(_controller.ContractPartySend(null));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("GAP.NoBackend", out var gap));
        Assert.Contains("WA_Contract_Contract_Get", gap.GetString());
        MarkDone("s256_nullbody");
    }

    [Fact]
    public void ContractPartySend_WithContract_ReturnsGapNoBackend()
    {
        var root = ExtractJson(_controller.ContractPartySend(new ServicesControllerPort.Services_ContractPartySendReq
        {
            NetworkID = "1001",
            FlagSendPartyCreate = "1",
            Lst_Contract_Contract = new() { new() { ContractCode = "CTR001" } },
        }));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend", root.GetProperty("ErrorCode").GetString());
        MarkDone("s256_contract");
    }

    // ================= #257 ContractPartySend_SSO =================

    [Fact]
    public void ContractPartySend_SSO_NullBody_ReturnsGapNoBackend()
    {
        // Ca 1: body rỗng — nguồn ném "strJson is null" (ServicesController.cs:742); đích trả GAP.NoBackend.
        var root = ExtractJson(_controller.ContractPartySend_SSO(null));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend_SSO", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("GAP.NoBackend", out var gap));
        Assert.Contains("CheckAuthorize", gap.GetString());
        MarkDone("s257_nullbody");
    }

    [Fact]
    public void ContractPartySend_SSO_WithContract_ReturnsGapNoBackend()
    {
        // Ca 2: body có AccessToken + Lst_Contract_Contract — nguồn đi qua SSO iNOS + SendChannel (dịch vụ ngoài).
        var root = ExtractJson(_controller.ContractPartySend_SSO(new ServicesControllerPort.Services_ContractPartySend_SSOReq
        {
            AccessToken = "sso-tok",
            NetworkID = "1001",
            FlagSendPartyCreate = "1",
            Lst_Contract_Contract = new() { new() { ContractCode = "CTR001" } },
        }));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend_SSO", root.GetProperty("ErrorCode").GetString());
        MarkDone("s257_contract");
    }

    // ================= #259 ContractResend =================

    [Fact]
    public void ContractResend_NullBody_ReturnsGapNoBackend()
    {
        // Ca 1: body rỗng — nguồn ném "strJson is null" (ServicesController.cs:1058-1061); đích trả GAP.NoBackend.
        var root = ExtractJson(_controller.ContractResend(null));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractResend", root.GetProperty("ErrorCode").GetString());
        Assert.True(root.GetProperty("Params").TryGetProperty("GAP.NoBackend", out var gap));
        Assert.Contains("ResendChannel", gap.GetString());
        MarkDone("s259_nullbody");
    }

    [Fact]
    public void ContractResend_WithContractAndSendHist_ReturnsGapNoBackend()
    {
        // Ca 2: body có RQ_Contract_Contract + Lst_Contract_SendHist — nguồn đi qua SSO iNOS + ContractData_Get +
        // ResendChannel(WA_Mst_Channel_Get/SubmissionForm/ContractTypeDtl + Email/Zalo/SMS) — dịch vụ ngoài.
        var root = ExtractJson(_controller.ContractResend(new ServicesControllerPort.Services_ContractResendReq
        {
            RQ_Contract_Contract = new()
            {
                AccessToken = "sso-tok",
                NetworkID = "1001",
                Lst_Contract_Contract = new() { new() { ContractCode = "CTR001" } },
            },
            Lst_Contract_SendHist = new()
            {
                new() { ChannelType = "EMAIL", PartyCode = "PARTY_A", UserToken = "ut1", InfoReceive = "a@b.c" },
            },
        }));
        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Services_ContractResend", root.GetProperty("ErrorCode").GetString());
        MarkDone("s259_contract");
    }

    // ================= Real-server verify (cả #256/#257/#259) =================

    [Fact]
    public async Task RealServer_VerifyContractPartySendAndSSOAsync()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"verify_services_{Guid.NewGuid():N}.db");
        var dllPath = typeof(ServicesControllerPort).Assembly.Location;
        if (!File.Exists(dllPath))
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath))
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
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
                    if (res.IsSuccessStatusCode) { started = true; break; }
                }
                catch { await Task.Delay(500); }
            }
            Assert.True(started, "Real server did not start within 15 seconds.");

            // #256: body rỗng + body có hợp đồng
            var resEmpty = await http.PostAsync("/api/services/contractpartysend",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resEmpty.StatusCode);
            var docEmpty = JsonDocument.Parse(await resEmpty.Content.ReadAsStringAsync());
            Assert.False(docEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend",
                docEmpty.RootElement.GetProperty("ErrorCode").GetString());

            var resContract = await http.PostAsync("/api/services/contractpartysend",
                new StringContent(JsonSerializer.Serialize(new
                {
                    NetworkID = "1001",
                    FlagSendPartyCreate = "1",
                    Lst_Contract_Contract = new[] { new { ContractCode = "CTR001" } },
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resContract.StatusCode);
            var docContract = JsonDocument.Parse(await resContract.Content.ReadAsStringAsync());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend",
                docContract.RootElement.GetProperty("ErrorCode").GetString());

            // #257: body rỗng
            var resSsoEmpty = await http.PostAsync("/api/services/contractpartysend_sso",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resSsoEmpty.StatusCode);
            var docSsoEmpty = JsonDocument.Parse(await resSsoEmpty.Content.ReadAsStringAsync());
            Assert.False(docSsoEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend_SSO",
                docSsoEmpty.RootElement.GetProperty("ErrorCode").GetString());

            // #257: body có AccessToken + hợp đồng
            var resSsoContract = await http.PostAsync("/api/services/contractpartysend_sso",
                new StringContent(JsonSerializer.Serialize(new
                {
                    AccessToken = "sso-tok",
                    NetworkID = "1001",
                    FlagSendPartyCreate = "1",
                    Lst_Contract_Contract = new[] { new { ContractCode = "CTR001" } },
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resSsoContract.StatusCode);
            var docSsoContract = JsonDocument.Parse(await resSsoContract.Content.ReadAsStringAsync());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractPartySend_SSO",
                docSsoContract.RootElement.GetProperty("ErrorCode").GetString());

            // #259: body rỗng + body đầy đủ
            var resResendEmpty = await http.PostAsync("/api/services/contractresend",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resResendEmpty.StatusCode);
            var docResendEmpty = JsonDocument.Parse(await resResendEmpty.Content.ReadAsStringAsync());
            Assert.False(docResendEmpty.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractResend",
                docResendEmpty.RootElement.GetProperty("ErrorCode").GetString());
            Assert.True(docResendEmpty.RootElement.GetProperty("Params").TryGetProperty("GAP.NoBackend", out _));

            var resResendFull = await http.PostAsync("/api/services/contractresend",
                new StringContent(JsonSerializer.Serialize(new
                {
                    RQ_Contract_Contract = new
                    {
                        AccessToken = "sso-tok",
                        NetworkID = "1001",
                        Lst_Contract_Contract = new[] { new { ContractCode = "CTR001" } },
                    },
                    Lst_Contract_SendHist = new[]
                    {
                        new { ChannelType = "EMAIL", PartyCode = "PARTY_A", UserToken = "ut1", InfoReceive = "a@b.c" },
                    },
                }), Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, resResendFull.StatusCode);
            var docResendFull = JsonDocument.Parse(await resResendFull.Content.ReadAsStringAsync());
            Assert.Equal("Erridn.Skycic.QContract.Services_ContractResend",
                docResendFull.RootElement.GetProperty("ErrorCode").GetString());

            MarkDone("s257_realserver");
        }
        finally
        {
            try
            {
                if (!proc.HasExited) { proc.Kill(true); proc.WaitForExit(3000); }
            }
            catch { }
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
