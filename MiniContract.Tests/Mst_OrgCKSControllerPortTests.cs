using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;

namespace MiniContract.Tests;

public class Mst_OrgCKSControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly Mst_OrgCKSControllerPort _controller;

    public Mst_OrgCKSControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder cho Mst_OrgSignConfig
        new Mst_OrgSignConfigSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        var inMemorySettings = new Dictionary<string, string?> {
            {"Biz_NetworkID", "1001"}
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _controller = new Mst_OrgCKSControllerPort(_db, _config);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Create_NullBody_FailsWithCheckStrJson()
    {
        var result = await _controller.Create(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Create", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_EmptyBody_FailsWithCheckStrJson()
    {
        var req = new Mst_OrgCKS_CreateReq();
        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Create", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Create_ConflictMoreThanOneActiveRemote_FailsAndRollsBack()
    {
        // Seeder đã có 1 bản ghi REMOTE active cho 7200000001
        var req = new Mst_OrgCKS_CreateReq
        {
            OrgID = "7200000001",
            SignType = "REMOTE",
            CANumber = "CA_CONFLICT_01",
            FlagActive = "1"
        };

        var result = await _controller.Create(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_MoreThanOneActive", root.GetProperty("ErrorCode").GetString());

        var p = root.GetProperty("Params");
        Assert.Equal("REMOTE", p.GetProperty("Check.SignType").GetString());
        Assert.Equal("7200000001", p.GetProperty("Check.OrgID").GetString());

        // Kiểm tra rollback: DB vẫn chỉ có 1 bản ghi REMOTE active cho 7200000001
        var count = await _db.Set<Mst_OrgSignConfig>()
            .CountAsync(x => x.OrgID == "7200000001" && x.SignType == "REMOTE" && x.FlagActive == "1");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Create_RoundTrip_SuccessAndPersistsToDb()
    {
        var req = new Mst_OrgCKS_CreateReq
        {
            OrgID = "7200000002",
            SignType = "remote", // kiểm tra chuẩn hoá chữ HOA
            CANumber = "ca7200000002",
            CAOrg = "vnpt-ca",
            SupplierCode = "vnpt",
            RemoteSignAgreementUUID = "uuid-test-02",
            RemoteSignPassCode = "pass456",
            AuthenCode = "sms_otp",
            FlagActive = "1"
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        // Đọc lại từ DB để kiểm tra round-trip
        var saved = await _db.Set<Mst_OrgSignConfig>().FirstOrDefaultAsync(x => x.OrgID == "7200000002");
        Assert.NotNull(saved);
        Assert.True(saved.AutoID > 0);
        Assert.Equal("REMOTE", saved.SignType);
        Assert.Equal("CA7200000002", saved.CANumber);
        Assert.Equal("VNPT-CA", saved.CAOrg);
        Assert.Equal("VNPT", saved.SupplierCode);
        Assert.Equal("uuid-test-02", saved.RemoteSignAgreementUUID);
        Assert.Equal("pass456", saved.RemoteSignPassCode);
        Assert.Equal("SMS_OTP", saved.AuthenCode);
        Assert.Equal("1", saved.FlagActive);
        Assert.Equal("port", saved.CreateBy);
        Assert.Equal("1001", saved.NetworkID);
    }

    [Fact]
    public async Task Create_ViaModelJson_Success()
    {
        var jsonModel = JsonSerializer.Serialize(new
        {
            OrgID = "7200000003",
            SignType = "SERVER",
            CANumber = "CASRV01",
            ServerSignFilePath = "/certs/srv01.pfx",
            ServerSignPassword = "secretPassword",
            FlagActive = "1"
        });

        var req = new Mst_OrgCKS_CreateReq
        {
            Model = jsonModel
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var saved = await _db.Set<Mst_OrgSignConfig>().FirstOrDefaultAsync(x => x.OrgID == "7200000003");
        Assert.NotNull(saved);
        Assert.Equal("SERVER", saved.SignType);
        Assert.Equal("CASRV01", saved.CANumber);
        Assert.Equal("/certs/srv01.pfx", saved.ServerSignFilePath);
        Assert.Equal("secretPassword", saved.ServerSignPassword);
    }

    [Fact]
    public async Task Create_UsbTokenAllowsMultipleActive_Success()
    {
        // Seeder đã có 1 bản ghi USBTOKEN active cho 7200000001
        var req = new Mst_OrgCKS_CreateReq
        {
            OrgID = "7200000001",
            SignType = "USBTOKEN",
            CANumber = "CAUSB000002",
            CAOrg = "BKAV-CA",
            FlagActive = "1"
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var count = await _db.Set<Mst_OrgSignConfig>()
            .CountAsync(x => x.OrgID == "7200000001" && x.SignType == "USBTOKEN" && x.FlagActive == "1");
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Create_RemoteInactiveAllowsMultiple_Success()
    {
        // Thêm bản ghi REMOTE với FlagActive = "0" không xung đột với REMOTE FlagActive = "1"
        var req = new Mst_OrgCKS_CreateReq
        {
            OrgID = "7200000001",
            SignType = "REMOTE",
            CANumber = "CAINACTIVE01",
            FlagActive = "0"
        };

        var result = await _controller.Create(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
    }
}
