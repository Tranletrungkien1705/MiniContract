using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MiniContract.Controllers.Port;
using Xunit;

namespace MiniContract.Tests;

// Test hạng mục #136 — Mst_ChannelController/ZaloConnect (pure gate logic: config + URL builder).
// Dùng in-memory IConfiguration vì HTTP harness không chèn được env var (GAP khai báo trong TOM TAT).
public class Mst_Channel_ZaloConnectTests
{
    private static Mst_ChannelControllerPort Ctrl(Dictionary<string, string?> cfg)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(cfg).Build();
        return new Mst_ChannelControllerPort(null!, config);
    }

    [Fact]
    public void ZaloConnect_WithBodyAppId_UsesBodyAppId()
    {
        // Ca 1 debai: body ZaloAppId="9876543210" + ClientDomain=https://...
        var c = Ctrl(new()
        {
            ["ClientDomain"] = "https://qcontract.ecore.vn",
            ["Zalo_App_Id"] = "1234567890"
        });
        var result = c.ZaloConnect(new Mst_Channel_ZaloConnectReq { ZaloAppId = "9876543210" });
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(
            "https://oauth.zaloapp.com/v4/oa/permission?app_id=9876543210&redirect_uri=https%3A%2F%2Fqcontract.ecore.vn%2Fzalo-login-callback",
            doc.RootElement.GetProperty("Url").GetString());
        Assert.Equal(0, doc.RootElement.GetProperty("Title").GetArrayLength());
    }

    [Fact]
    public void ZaloConnect_NoBodyAppId_FallsBackToConfig()
    {
        // Ca 2 debai: body {} → app_id lấy từ config Zalo_App_Id
        var c = Ctrl(new()
        {
            ["ClientDomain"] = "https://qcontract.ecore.vn",
            ["Zalo_App_Id"] = "1234567890"
        });
        var result = c.ZaloConnect(new Mst_Channel_ZaloConnectReq());
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Equal(
            "https://oauth.zaloapp.com/v4/oa/permission?app_id=1234567890&redirect_uri=https%3A%2F%2Fqcontract.ecore.vn%2Fzalo-login-callback",
            doc.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void ZaloConnect_NullReq_FallsBackToConfig()
    {
        // Ca 2b debai: body null cũng được
        var c = Ctrl(new()
        {
            ["ClientDomain"] = "https://qcontract.ecore.vn",
            ["Zalo_App_Id"] = "1234567890"
        });
        var result = c.ZaloConnect(null);
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Contains("app_id=1234567890", doc.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void ZaloConnect_HttpDomain_RedirectUriStillHttps()
    {
        // Ca 3 debai: ClientDomain=http://test.local → redirect_uri vẫn https%3A%2F%2F
        var c = Ctrl(new()
        {
            ["ClientDomain"] = "http://test.local",
            ["Zalo_App_Id"] = "1234567890"
        });
        var result = c.ZaloConnect(new Mst_Channel_ZaloConnectReq { ZaloAppId = "abc" });
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Equal(
            "https://oauth.zaloapp.com/v4/oa/permission?app_id=abc&redirect_uri=https%3A%2F%2Ftest.local%2Fzalo-login-callback",
            doc.RootElement.GetProperty("Url").GetString());
    }

    [Fact]
    public void ZaloConnect_NoProtocolDomain_Returns400()
    {
        // Ca 4 debai: ClientDomain không protocol → StrSplit [1] ném → 400
        var c = Ctrl(new() { ["ClientDomain"] = "qcontract.ecore.vn" });
        var result = c.ZaloConnect(new Mst_Channel_ZaloConnectReq());
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Channel_ZaloConnect", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public void ZaloConnect_NullDomain_Returns400()
    {
        // Ca 5 debai: ClientDomain null/empty → 400
        var c = Ctrl(new());
        var result = c.ZaloConnect(new Mst_Channel_ZaloConnectReq());
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.Equal("Erridn.Skycic.QContract.Mst_Channel_ZaloConnect", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public void ZaloConnect_EmptyDomain_Returns400()
    {
        var c = Ctrl(new() { ["ClientDomain"] = "" });
        var result = c.ZaloConnect(new Mst_Channel_ZaloConnectReq());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void ZaloConnect_BothAppIdsEmpty_SuccessWithEmptyAppId()
    {
        // Ca 6 debai: cả zaloappid và Zalo_App_Id đều rỗng → app_id= (rỗng), Success=true
        var c = Ctrl(new() { ["ClientDomain"] = "https://qcontract.ecore.vn" });
        var result = c.ZaloConnect(new Mst_Channel_ZaloConnectReq());
        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Contains("app_id=&redirect_uri=", doc.RootElement.GetProperty("Url").GetString());
    }
}
