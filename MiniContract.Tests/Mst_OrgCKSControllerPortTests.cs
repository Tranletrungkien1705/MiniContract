using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Controllers.Port;
using MiniContract.Data;
using MiniContract.Models.Port;
using Xunit;
using Xunit.Abstractions;

namespace MiniContract.Tests;

public class Mst_OrgCKSControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly Mst_OrgCKSControllerPort _controller;

    public Mst_OrgCKSControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
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

    [Fact]
    public async Task Edit_NullBody_FailsWithCheckStrJson()
    {
        var result = await _controller.Edit(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Update", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_EmptyBody_FailsWithCheckStrJson()
    {
        var req = new Mst_OrgCKS_EditReq();
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Update", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Edit_NotFoundAutoId_FailsWithOrganNotFound()
    {
        var req = new Mst_OrgCKS_EditReq
        {
            AutoID = 999999,
            SignType = "USBTOKEN",
            OrgID = "7200000001"
        };
        var result = await _controller.Edit(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());

        var p = root.GetProperty("Params");
        Assert.Equal(999999, p.GetProperty("Check.AutoID").GetInt64());
    }

    [Fact]
    public async Task Edit_ConflictMoreThanOneActiveRemote_FailsAndRollsBack()
    {
        // Bản ghi 3 trong Seeder có OrgID = 7200000001, SignType = REMOTE, FlagActive = 0
        var inactive = await _db.Set<Mst_OrgSignConfig>()
            .FirstAsync(x => x.OrgID == "7200000001" && x.SignType == "REMOTE" && x.FlagActive == "0");

        var req = new Mst_OrgCKS_EditReq
        {
            AutoID = inactive.AutoID,
            OrgID = "7200000001",
            SignType = "REMOTE",
            FlagActive = "1"
        };

        var result = await _controller.Edit(req);
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
    public async Task Edit_UsbToken_RoundTrip_SuccessAndPersistsToDb()
    {
        // Lấy bản ghi USBTOKEN từ seeder
        var usbRecord = await _db.Set<Mst_OrgSignConfig>()
            .FirstAsync(x => x.OrgID == "7200000001" && x.SignType == "USBTOKEN");

        var req = new Mst_OrgCKS_EditReq
        {
            AutoID = usbRecord.AutoID,
            OrgID = "7200000001",
            SignType = "usbtoken",
            CAOrg = "fpt-ca",
            CAEffDTimeUTCStart = "2026-06-01 00:00:00",
            CAEffDTimeUTCEnd = "2027-06-01 00:00:00",
            FlagActive = "1"
        };

        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        // Đọc lại từ DB để kiểm tra round-trip
        var updated = await _db.Set<Mst_OrgSignConfig>().FindAsync(usbRecord.AutoID);
        Assert.NotNull(updated);
        Assert.Equal("USBTOKEN", updated.SignType);
        Assert.Equal("FPT-CA", updated.CAOrg);
        Assert.Equal("2026-06-01 00:00:00", updated.CAEffDTimeUTCStart);
        Assert.Equal("2027-06-01 00:00:00", updated.CAEffDTimeUTCEnd);
        Assert.Equal("1", updated.FlagActive);
        Assert.Equal("port", updated.UpdateBy);
        Assert.NotNull(updated.UpdateDTimeUTC);
    }

    [Fact]
    public async Task Edit_ViaModelJson_Success()
    {
        var usbRecord = await _db.Set<Mst_OrgSignConfig>()
            .FirstAsync(x => x.OrgID == "7200000001" && x.SignType == "USBTOKEN");

        var jsonModel = JsonSerializer.Serialize(new
        {
            AutoID = usbRecord.AutoID,
            OrgID = "7200000001",
            SignType = "USBTOKEN",
            CAOrg = "BKAV-CA-NEW",
            CAEffDTimeUTCStart = "2026-07-01 00:00:00",
            CAEffDTimeUTCEnd = "2027-07-01 00:00:00",
            FlagActive = "1"
        });

        var req = new Mst_OrgCKS_EditReq
        {
            Model = jsonModel
        };

        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_OrgSignConfig>().FindAsync(usbRecord.AutoID);
        Assert.NotNull(updated);
        Assert.Equal("BKAV-CA-NEW", updated.CAOrg);
    }

    [Fact]
    public async Task Edit_Remote_SuccessAndPersistsToDb()
    {
        var remoteRecord = await _db.Set<Mst_OrgSignConfig>()
            .FirstAsync(x => x.OrgID == "7200000001" && x.SignType == "REMOTE" && x.FlagActive == "1");

        var req = new Mst_OrgCKS_EditReq
        {
            AutoID = remoteRecord.AutoID,
            OrgID = "7200000001",
            SignType = "remote",
            SupplierCode = "viettel",
            RemoteSignAgreementUUID = "uuid-viettel-edited",
            RemoteSignPassCode = "pass-edited-999",
            AuthenCode = "email_otp",
            FlagActive = "1"
        };

        var result = await _controller.Edit(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var updated = await _db.Set<Mst_OrgSignConfig>().FindAsync(remoteRecord.AutoID);
        Assert.NotNull(updated);
        Assert.Equal("VIETTEL", updated.SupplierCode);
        Assert.Equal("uuid-viettel-edited", updated.RemoteSignAgreementUUID);
        Assert.Equal("pass-edited-999", updated.RemoteSignPassCode);
        Assert.Equal("EMAIL_OTP", updated.AuthenCode);
    }

    [Fact]
    public async Task Delete_NullBody_FailsWithCheckStrJson()
    {
        var result = await _controller.Delete(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Delete", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_EmptyBody_FailsWithCheckStrJson()
    {
        var req = new Mst_OrgCKS_DeleteReq();
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Delete", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_NotFoundAutoId_FailsWithOrganNotFound()
    {
        var req = new Mst_OrgCKS_DeleteReq
        {
            AutoID = 999999,
            OrgID = "7200000001"
        };
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());

        var p = root.GetProperty("Params");
        Assert.Equal(999999, p.GetProperty("Check.AutoID").GetInt64());
    }

    [Fact]
    public async Task Delete_ZeroOrNegativeAutoId_FailsWithOrganNotFound()
    {
        var req = new Mst_OrgCKS_DeleteReq
        {
            AutoID = 0
        };
        var result = await _controller.Delete(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_OrganNotFound", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Delete_RoundTrip_SuccessAndRemovesFromDb()
    {
        // Tạo bản ghi mới để xóa
        var entity = new Mst_OrgSignConfig
        {
            OrgID = "7200000099",
            SignType = "USBTOKEN",
            CANumber = "CA_DEL_01",
            CAOrg = "VNPT-CA",
            FlagActive = "1"
        };
        _db.Set<Mst_OrgSignConfig>().Add(entity);
        await _db.SaveChangesAsync();

        var autoId = entity.AutoID;
        Assert.True(autoId > 0);

        var req = new Mst_OrgCKS_DeleteReq
        {
            AutoID = autoId,
            OrgID = "7200000099"
        };

        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        // Kiểm tra round-trip: bản ghi không còn trong DB
        var inDb = await _db.Set<Mst_OrgSignConfig>().FindAsync(autoId);
        Assert.Null(inDb);
    }

    [Fact]
    public async Task Delete_ViaModelJson_Success()
    {
        // Tạo bản ghi mới để xóa qua Model JSON
        var entity = new Mst_OrgSignConfig
        {
            OrgID = "7200000099",
            SignType = "REMOTE",
            CANumber = "CA_DEL_02",
            FlagActive = "1"
        };
        _db.Set<Mst_OrgSignConfig>().Add(entity);
        await _db.SaveChangesAsync();

        var autoId = entity.AutoID;

        var jsonModel = JsonSerializer.Serialize(new
        {
            AutoID = autoId,
            OrgID = "7200000099"
        });

        var req = new Mst_OrgCKS_DeleteReq
        {
            Model = jsonModel
        };

        var result = await _controller.Delete(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        // Kiểm tra bản ghi đã bị xóa khỏi DB
        var inDb = await _db.Set<Mst_OrgSignConfig>().FindAsync(autoId);
        Assert.Null(inDb);
    }

    [Fact]
    public async Task RealServer_Verification_Port8544()
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
        var freePort = FreePort.Get(); psi.Environment["PORT"] = freePort.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{freePort}"), Timeout = TimeSpan.FromSeconds(3) };
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
            Assert.True(started, "MiniContract server failed to start within 15 seconds on port 8544.");

            // Ca 1: POST body rong -> 400 Bad Request voi ma loi Erridn.Skycic.QContract.Mst_OrgSignConfig_Update
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_orgcks/edit", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ1 (Empty body): {body1}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.False(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Update", doc1.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 2: POST AutoID khong ton tai (999999) -> 400 Bad Request voi ma loi OrganNotFound
            var req2 = new StringContent("{\"AutoID\": 999999, \"SignType\": \"USBTOKEN\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_orgcks/edit", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ2 (AutoID 999999 NotFound): {body2}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            Assert.False(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_OrganNotFound", doc2.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 3: POST sua AutoID 3 (REMOTE FlagActive 0) thanh 1 -> 400 Bad Request do xung dot >1 active REMOTE
            var req3 = new StringContent("{\"AutoID\": 3, \"OrgID\": \"7200000001\", \"SignType\": \"REMOTE\", \"FlagActive\": \"1\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_orgcks/edit", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ3 (Conflict MoreThanOneActive): {body3}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            Assert.False(doc3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_MoreThanOneActive", doc3.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 4: POST round-trip sua USBTOKEN AutoID 2 -> 200 OK voi Success = true va gia tri moi duoc luu
            var req4 = new StringContent("{\"AutoID\": 2, \"OrgID\": \"7200000001\", \"SignType\": \"USBTOKEN\", \"CAOrg\": \"VERIFY-USB-CA\", \"CAEffDTimeUTCStart\": \"2026-06-01 00:00:00\", \"CAEffDTimeUTCEnd\": \"2027-06-01 00:00:00\", \"FlagActive\": \"1\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_orgcks/edit", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ4 (Round-trip USBTOKEN AutoID 2): {body4}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("VERIFY-USB-CA", doc4.RootElement.GetProperty("Data").GetProperty("CAOrg").GetString());

            // Ca 5: POST round-trip sua qua chuoi Model JSON
            var modelJson = JsonSerializer.Serialize("{\"AutoID\":2,\"SignType\":\"USBTOKEN\",\"CAOrg\":\"MODEL-JSON-CA\",\"FlagActive\":\"1\"}");
            var req5 = new StringContent($"{{\"Model\": {modelJson}}}", Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_orgcks/edit", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ5 (Round-trip via Model JSON): {body5}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            var doc5 = JsonDocument.Parse(body5);
            Assert.True(doc5.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("MODEL-JSON-CA", doc5.RootElement.GetProperty("Data").GetProperty("CAOrg").GetString());

            // Ca 6 (Delete - Body rong): POST /api/mst_orgcks/delete voi {} -> 400 Bad Request voi ma loi Mst_OrgSignConfig_Delete
            var req6 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_orgcks/delete", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ6 (Delete empty body): {body6}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            Assert.False(doc6.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_Delete", doc6.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 7 (Delete - NotFound): POST /api/mst_orgcks/delete voi AutoID 999999 -> 400 Bad Request voi ma loi OrganNotFound
            var req7 = new StringContent("{\"AutoID\": 999999, \"OrgID\": \"7200000001\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_orgcks/delete", req7);
            var body7 = await res7.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ7 (Delete AutoID 999999 NotFound): {body7}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res7.StatusCode);
            var doc7 = JsonDocument.Parse(body7);
            Assert.False(doc7.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_OrganNotFound", doc7.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 8 (Delete - Round-trip): POST /api/mst_orgcks/delete voi AutoID 2 (USBTOKEN) -> 200 OK voi Success = true
            var req8 = new StringContent("{\"AutoID\": 2, \"OrgID\": \"7200000001\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_orgcks/delete", req8);
            var body8 = await res8.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ8 (Delete round-trip AutoID 2): {body8}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var doc8 = JsonDocument.Parse(body8);
            Assert.True(doc8.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa chữ ký số thành công!", doc8.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 9 (Delete - Verify da bi xoa): POST /api/mst_orgcks/delete lai AutoID 2 -> 400 Bad Request OrganNotFound
            var req9 = new StringContent("{\"AutoID\": 2, \"OrgID\": \"7200000001\"}", Encoding.UTF8, "application/json");
            var res9 = await http.PostAsync("/api/mst_orgcks/delete", req9);
            var body9 = await res9.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ9 (Delete AutoID 2 again -> NotFound): {body9}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res9.StatusCode);
            var doc9 = JsonDocument.Parse(body9);
            Assert.False(doc9.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_OrgSignConfig_CheckDB_OrganNotFound", doc9.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 10 (Delete - Via Model JSON): POST /api/mst_orgcks/delete voi Model JSON -> 200 OK
            var delModelJson = JsonSerializer.Serialize("{\"AutoID\":3,\"OrgID\":\"7200000001\"}");
            var req10 = new StringContent($"{{\"Model\": {delModelJson}}}", Encoding.UTF8, "application/json");
            var res10 = await http.PostAsync("/api/mst_orgcks/delete", req10);
            var body10 = await res10.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ10 (Delete via Model JSON AutoID 3): {body10}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res10.StatusCode);
            var doc10 = JsonDocument.Parse(body10);
            Assert.True(doc10.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Xóa chữ ký số thành công!", doc10.RootElement.GetProperty("Messages")[0].GetString());
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
