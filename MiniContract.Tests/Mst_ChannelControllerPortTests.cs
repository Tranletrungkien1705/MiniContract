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

public class Mst_ChannelControllerPortTests : IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_ChannelControllerPort _controller;

    public Mst_ChannelControllerPortTests(ITestOutputHelper? output = null)
    {
        _output = output;
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder cho các bảng kênh và danh mục liên quan
        new Mst_ChannelTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_SubmissionFormSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_OASeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelEmailSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelSMSSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelZaloSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_ChannelControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Save_EmptyOrgID_FailsWithInvalidOrgID()
    {
        var req = new Mst_Channel_SaveReq { OrgID = "" };
        var result = await _controller.Save(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_Channel_SaveX_InvalidOrgID", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Save_InvalidChannelType_FailsWithChannelTypeNotFound()
    {
        var req = new Mst_Channel_SaveReq
        {
            OrgID = "7200000001",
            Mst_Channel = new Mst_Channel
            {
                ChannelTypeContract = "INVALID_TYPE",
                ChannelTypeOTP = "SMS"
            }
        };
        var result = await _controller.Save(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ChannelType_CheckDB_ChannelTypeNotFound", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Save_InvalidSubFormCode_FailsWithSubFormCodeNotFound()
    {
        var req = new Mst_Channel_SaveReq
        {
            OrgID = "7200000001",
            Mst_Channel = new Mst_Channel
            {
                ChannelTypeContract = "EMAIL",
                ChannelTypeOTP = "SMS"
            },
            Lst_Mst_ChannelEmail = new List<Mst_ChannelEmail>
            {
                new() { SubFormCodeEmailContract = "INVALID_SUBFORM" }
            }
        };
        var result = await _controller.Save(req);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(badResult.Value));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_SubmissionForm_CheckDB_SubFormCodeNotFound", root.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task Save_RoundTrip_SuccessAndUpdatesDatabase()
    {
        var req = new Mst_Channel_SaveReq
        {
            OrgID = "7200000001",
            NetworkID = "1001",
            Mst_Channel = new Mst_Channel
            {
                ChannelTypeContract = "ZALO",
                ChannelTypeOTP = "EMAIL"
            },
            Lst_Mst_ChannelEmail = new List<Mst_ChannelEmail>
            {
                new()
                {
                    SubFormCodeEmailContract = "SF_EMAIL_01",
                    SubFormCodeEmailOTP = "SF_EMAIL_01",
                    MailFrom = "test_save@qcontract.vn"
                }
            },
            Lst_Mst_ChannelSMS = new List<Mst_ChannelSMS>
            {
                new()
                {
                    SubFormCodeContractSMS = "SF_SMS_01",
                    SubFormCodeSMSOTP = "SF_SMS_01",
                    SMSBrandName = "BrandTest"
                }
            },
            Lst_Mst_ChannelZalo = new List<Mst_ChannelZalo>
            {
                new()
                {
                    SubFormCodeContractPhone = "SF_ZALO_01",
                    SubFormCodeOTP = "SF_ZALO_01",
                    ZaloOAID = "1358767413636684272"
                }
            }
        };

        var result = await _controller.Save(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        Assert.Equal("Cập nhật cấu hình thành công!", root.GetProperty("Title").GetString());

        // Kiểm tra round-trip trong DB
        var channel = await _db.Set<Mst_Channel>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(channel);
        Assert.Equal("ZALO", channel.ChannelTypeContract);
        Assert.Equal("EMAIL", channel.ChannelTypeOTP);

        var email = await _db.Set<Mst_ChannelEmail>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(email);
        Assert.Equal("SF_EMAIL_01", email.SubFormCodeEmailContract);
        Assert.Equal("test_save@qcontract.vn", email.MailFrom);

        var sms = await _db.Set<Mst_ChannelSMS>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(sms);
        Assert.Equal("SF_SMS_01", sms.SubFormCodeContractSMS);
        Assert.Equal("BrandTest", sms.SMSBrandName);

        var zalo = await _db.Set<Mst_ChannelZalo>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(zalo);
        Assert.Equal("SF_ZALO_01", zalo.SubFormCodeContractPhone);
    }

    [Fact]
    public async Task Save_ViaModelJsonString_Success()
    {
        var modelPayload = new Mst_Channel_SaveReq
        {
            OrgID = "7200000001",
            NetworkID = "1001",
            Mst_Channel = new Mst_Channel
            {
                ChannelTypeContract = "SMS",
                ChannelTypeOTP = "SMS"
            }
        };

        var req = new Mst_Channel_SaveReq
        {
            Model = JsonSerializer.Serialize(modelPayload)
        };

        var result = await _controller.Save(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());

        var channel = await _db.Set<Mst_Channel>().FirstOrDefaultAsync(x => x.OrgID == "7200000001");
        Assert.NotNull(channel);
        Assert.Equal("SMS", channel.ChannelTypeContract);
        Assert.Equal("SMS", channel.ChannelTypeOTP);
    }

    [Fact]
    public async Task Index_EmptyBody_ReturnsAllActiveChannelsAndSubmissionForms()
    {
        var result = await _controller.Index(null);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        // 2 active channels seeded (7200000001, 7200000002) - 7200000003 is FlagActive=0 so excluded
        var channels = data.GetProperty("Lst_Mst_Channel");
        Assert.Equal(2, channels.GetArrayLength());

        // 3 submission forms seeded
        var forms = data.GetProperty("Lst_Mst_SubmissionForm");
        Assert.Equal(3, forms.GetArrayLength());

        // Zalo external service skipped -> flagZaloConnect is "0"
        Assert.Equal("0", data.GetProperty("FlagZaloConnect").GetString());
    }

    [Fact]
    public async Task Index_FilterByOrgId_ReturnsMatchingChannel()
    {
        var req = new Mst_Channel_IndexReq { OrgID = "7200000001" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        var channels = data.GetProperty("Lst_Mst_Channel");
        Assert.Equal(1, channels.GetArrayLength());
        Assert.Equal("7200000001", channels[0].GetProperty("OrgID").GetString());

        var emails = data.GetProperty("Lst_Mst_ChannelEmail");
        Assert.Equal(1, emails.GetArrayLength());
        Assert.Equal("7200000001", emails[0].GetProperty("OrgID").GetString());

        var sms = data.GetProperty("Lst_Mst_ChannelSMS");
        Assert.Equal(1, sms.GetArrayLength());
        Assert.Equal("7200000001", sms[0].GetProperty("OrgID").GetString());

        var zalo = data.GetProperty("Lst_Mst_ChannelZalo");
        Assert.Equal(1, zalo.GetArrayLength());
        Assert.Equal("7200000001", zalo[0].GetProperty("OrgID").GetString());
    }

    [Fact]
    public async Task Index_FilterByNonExistentOrgId_ReturnsZeroChannels()
    {
        var req = new Mst_Channel_IndexReq { OrgID = "NON_EXISTENT_ORG" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        var channels = data.GetProperty("Lst_Mst_Channel");
        Assert.Equal(0, channels.GetArrayLength());

        var emails = data.GetProperty("Lst_Mst_ChannelEmail");
        Assert.Equal(0, emails.GetArrayLength());

        // Submission forms are still returned
        var forms = data.GetProperty("Lst_Mst_SubmissionForm");
        Assert.Equal(3, forms.GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterByInactiveOrgId_ReturnsZeroChannels()
    {
        // 7200000003 is seeded with FlagActive="0"
        var req = new Mst_Channel_IndexReq { OrgID = "7200000003" };
        var result = await _controller.Index(req);
        var okResult = Assert.IsType<OkObjectResult>(result);

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");

        var channels = data.GetProperty("Lst_Mst_Channel");
        Assert.Equal(0, channels.GetArrayLength());
    }

    [Fact]
    public async Task RealServer_Verification_Port8538()
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
        psi.Environment["PORT"] = "8538";
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8538"), Timeout = TimeSpan.FromSeconds(3) };
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

            // Request 1: POST empty OrgID -> Guard Fail 400 Mst_Channel_SaveX_InvalidOrgID
            var req1 = new StringContent("{\"OrgID\":\"\"}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_channel/save", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ1: {body1}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res1.StatusCode);
            Assert.Contains("Mst_Channel_SaveX_InvalidOrgID", body1);

            // Request 2: POST invalid ChannelType -> Guard Fail 400 Mst_ChannelType_CheckDB_ChannelTypeNotFound
            var req2 = new StringContent("{\"OrgID\":\"7200000001\",\"Mst_Channel\":{\"ChannelTypeContract\":\"INVALID\"}}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_channel/save", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ2: {body2}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res2.StatusCode);
            Assert.Contains("Mst_ChannelType_CheckDB_ChannelTypeNotFound", body2);

            // Request 3: POST invalid SubFormCode -> Guard Fail 400 Mst_SubmissionForm_CheckDB_SubFormCodeNotFound
            var req3 = new StringContent("{\"OrgID\":\"7200000001\",\"Mst_Channel\":{\"ChannelTypeContract\":\"EMAIL\",\"ChannelTypeOTP\":\"SMS\"},\"Lst_Mst_ChannelEmail\":[{\"SubFormCodeEmailContract\":\"INVALID_CODE\"}]}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_channel/save", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ3: {body3}");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res3.StatusCode);
            Assert.Contains("Mst_SubmissionForm_CheckDB_SubFormCodeNotFound", body3);

            // Request 4: POST round-trip save -> 200 OK Cập nhật cấu hình thành công!
            var validPayload = new
            {
                OrgID = "7200000001",
                NetworkID = "1001",
                Mst_Channel = new { ChannelTypeContract = "ZALO", ChannelTypeOTP = "EMAIL" },
                Lst_Mst_ChannelEmail = new[] { new { SubFormCodeEmailContract = "SF_EMAIL_01", SubFormCodeEmailOTP = "SF_EMAIL_01", MailFrom = "notify@qcontract.vn" } },
                Lst_Mst_ChannelSMS = new[] { new { SubFormCodeContractSMS = "SF_SMS_01", SubFormCodeSMSOTP = "SF_SMS_01", SMSBrandName = "QContractBrand" } },
                Lst_Mst_ChannelZalo = new[] { new { SubFormCodeContractPhone = "SF_ZALO_01", SubFormCodeOTP = "SF_ZALO_01", ZaloOAID = "1358767413636684272" } }
            };
            var req4 = new StringContent(JsonSerializer.Serialize(validPayload), Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_channel/save", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ4: {body4}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            Assert.Contains("Cập nhật cấu hình thành công!", body4);

            // Request 5: POST via 'model' string -> 200 OK
            var modelPayload = new
            {
                OrgID = "7200000001",
                NetworkID = "1001",
                Mst_Channel = new { ChannelTypeContract = "SMS", ChannelTypeOTP = "SMS" }
            };
            var req5 = new StringContent(JsonSerializer.Serialize(new { Model = JsonSerializer.Serialize(modelPayload) }), Encoding.UTF8, "application/json");
            var res5 = await http.PostAsync("/api/mst_channel/save", req5);
            var body5 = await res5.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ5: {body5}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res5.StatusCode);
            Assert.Contains("Cập nhật cấu hình thành công!", body5);

            // Request 6: POST /api/mst_channel/index with empty body -> 200 OK, returns active channels and submission forms
            var req6 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res6 = await http.PostAsync("/api/mst_channel/index", req6);
            var body6 = await res6.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ6: {body6}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res6.StatusCode);
            var doc6 = JsonDocument.Parse(body6);
            Assert.True(doc6.RootElement.GetProperty("Success").GetBoolean());
            var data6 = doc6.RootElement.GetProperty("Data");
            Assert.True(data6.GetProperty("Lst_Mst_Channel").GetArrayLength() >= 2);
            Assert.True(data6.GetProperty("Lst_Mst_SubmissionForm").GetArrayLength() >= 3);
            Assert.Equal("0", data6.GetProperty("FlagZaloConnect").GetString());

            // Request 7: POST /api/mst_channel/index with OrgID='7200000001' -> 200 OK, returns 1 channel
            var req7 = new StringContent("{\"OrgID\":\"7200000001\"}", Encoding.UTF8, "application/json");
            var res7 = await http.PostAsync("/api/mst_channel/index", req7);
            var body7 = await res7.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ7: {body7}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res7.StatusCode);
            var doc7 = JsonDocument.Parse(body7);
            Assert.True(doc7.RootElement.GetProperty("Success").GetBoolean());
            var data7 = doc7.RootElement.GetProperty("Data");
            Assert.Equal(1, data7.GetProperty("Lst_Mst_Channel").GetArrayLength());
            Assert.Equal("7200000001", data7.GetProperty("Lst_Mst_Channel")[0].GetProperty("OrgID").GetString());

            // Request 8: POST /api/mst_channel/index with non-existent OrgID -> 200 OK, returns 0 channels
            var req8 = new StringContent("{\"OrgID\":\"NON_EXISTENT_ORG\"}", Encoding.UTF8, "application/json");
            var res8 = await http.PostAsync("/api/mst_channel/index", req8);
            var body8 = await res8.Content.ReadAsStringAsync();
            _output?.WriteLine($"REQ8: {body8}");
            Assert.Equal(System.Net.HttpStatusCode.OK, res8.StatusCode);
            var doc8 = JsonDocument.Parse(body8);
            Assert.True(doc8.RootElement.GetProperty("Success").GetBoolean());
            var data8 = doc8.RootElement.GetProperty("Data");
            Assert.Equal(0, data8.GetProperty("Lst_Mst_Channel").GetArrayLength());
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
