using System.Diagnostics;
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

public class Mst_SubmissionFormControllerPortTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly Mst_SubmissionFormControllerPort _controller;

    public Mst_SubmissionFormControllerPortTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        _db.Database.EnsureCreated();

        // Chạy seeder Mst_SubmissionForm và các bảng liên quan cho Create
        var seeder = new Mst_SubmissionFormSeeder();
        seeder.SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelEmailSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelSMSSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelZaloSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ParamContractSubmissFormSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        _controller = new Mst_SubmissionFormControllerPort(_db);
    }

    public void Dispose()
    {
        _conn.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Index_WithEmptyOrNullBody_ReturnsAllItems()
    {
        // 1. null request
        var result1 = await _controller.Index(null);
        var ok1 = Assert.IsType<OkObjectResult>(result1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var root1 = doc1.RootElement;

        Assert.True(root1.GetProperty("Success").GetBoolean());
        var data1 = root1.GetProperty("Data");
        Assert.Equal(4, data1.GetProperty("ItemCount").GetInt32());
        Assert.Equal(1, data1.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data1.GetProperty("PageIndex").GetInt32());
        Assert.Equal(10, data1.GetProperty("PageSize").GetInt32());

        var list1 = data1.GetProperty("DataList");
        Assert.Equal(4, list1.GetArrayLength());

        // Thứ tự sắp xếp LogLUDTimeUTC desc, SubFormCode asc
        Assert.Equal("SF_SMS_INACTIVE", list1[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("SF_ZALO_01", list1[1].GetProperty("SubFormCode").GetString());
        Assert.Equal("SF_SMS_01", list1[2].GetProperty("SubFormCode").GetString());
        Assert.Equal("SF_EMAIL_01", list1[3].GetProperty("SubFormCode").GetString());

        // 2. new request với init = "init"
        var result2 = await _controller.Index(new Mst_SubmissionForm_IndexReq { Init = "init" });
        var ok2 = Assert.IsType<OkObjectResult>(result2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        Assert.Equal(4, doc2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Index_FilterByChannelType_ReturnsMatchingItems()
    {
        // 1. Lọc EMAIL -> 1 bản ghi
        var reqEmail = new Mst_SubmissionForm_IndexReq { CheckedChannelType = "EMAIL", Init = "search" };
        var resEmail = await _controller.Index(reqEmail);
        var okEmail = Assert.IsType<OkObjectResult>(resEmail);
        var docEmail = JsonDocument.Parse(JsonSerializer.Serialize(okEmail.Value));
        var dataEmail = docEmail.RootElement.GetProperty("Data");
        Assert.Equal(1, dataEmail.GetProperty("ItemCount").GetInt32());
        Assert.Equal("SF_EMAIL_01", dataEmail.GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());

        // 2. Lọc SMS -> 2 bản ghi (SF_SMS_INACTIVE và SF_SMS_01)
        var reqSms = new Mst_SubmissionForm_IndexReq { CheckedChannelType = "SMS", Init = "search" };
        var resSms = await _controller.Index(reqSms);
        var okSms = Assert.IsType<OkObjectResult>(resSms);
        var docSms = JsonDocument.Parse(JsonSerializer.Serialize(okSms.Value));
        var dataSms = docSms.RootElement.GetProperty("Data");
        Assert.Equal(2, dataSms.GetProperty("ItemCount").GetInt32());
        Assert.Equal("SF_SMS_INACTIVE", dataSms.GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("SF_SMS_01", dataSms.GetProperty("DataList")[1].GetProperty("SubFormCode").GetString());

        // 3. Lọc danh sách phân tách dấu phẩy: "EMAIL,ZALO" -> 2 bản ghi
        var reqMulti = new Mst_SubmissionForm_IndexReq { CheckedChannelType = "EMAIL,ZALO", Init = "search" };
        var resMulti = await _controller.Index(reqMulti);
        var okMulti = Assert.IsType<OkObjectResult>(resMulti);
        var docMulti = JsonDocument.Parse(JsonSerializer.Serialize(okMulti.Value));
        var dataMulti = docMulti.RootElement.GetProperty("Data");
        Assert.Equal(2, dataMulti.GetProperty("ItemCount").GetInt32());
    }

    [Fact]
    public async Task Index_FilterByBulletinType_ReturnsMatchingItems()
    {
        // Lọc OTP -> 2 bản ghi (SF_SMS_INACTIVE, SF_SMS_01)
        var req = new Mst_SubmissionForm_IndexReq { CheckedBulletinType = "OTP", Init = "search" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(2, data.GetProperty("ItemCount").GetInt32());

        // Lọc ZNS_NOTIFY -> 1 bản ghi (SF_ZALO_01)
        var reqZns = new Mst_SubmissionForm_IndexReq { CheckedBulletinType = "ZNS_NOTIFY", Init = "search" };
        var resZns = await _controller.Index(reqZns);
        var okZns = Assert.IsType<OkObjectResult>(resZns);
        var docZns = JsonDocument.Parse(JsonSerializer.Serialize(okZns.Value));
        var dataZns = docZns.RootElement.GetProperty("Data");
        Assert.Equal(1, dataZns.GetProperty("ItemCount").GetInt32());
        Assert.Equal("SF_ZALO_01", dataZns.GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());
    }

    [Fact]
    public async Task Index_FilterByStatus_ReturnsMatchingItems()
    {
        // 1. Lọc FlagActive = '1' -> 3 bản ghi
        var reqActive = new Mst_SubmissionForm_IndexReq { CheckedStatusUse = "1", Init = "search" };
        var resActive = await _controller.Index(reqActive);
        var okActive = Assert.IsType<OkObjectResult>(resActive);
        var docActive = JsonDocument.Parse(JsonSerializer.Serialize(okActive.Value));
        var dataActive = docActive.RootElement.GetProperty("Data");
        Assert.Equal(3, dataActive.GetProperty("ItemCount").GetInt32());

        // 2. Lọc FlagActive = '0' -> 1 bản ghi
        var reqInactive = new Mst_SubmissionForm_IndexReq { CheckedStatusUse = "0", Init = "search" };
        var resInactive = await _controller.Index(reqInactive);
        var okInactive = Assert.IsType<OkObjectResult>(resInactive);
        var docInactive = JsonDocument.Parse(JsonSerializer.Serialize(okInactive.Value));
        var dataInactive = docInactive.RootElement.GetProperty("Data");
        Assert.Equal(1, dataInactive.GetProperty("ItemCount").GetInt32());
        Assert.Equal("SF_SMS_INACTIVE", dataInactive.GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());
    }

    [Fact]
    public async Task Index_FilterCombined_ChannelAndStatus()
    {
        // Lọc ChannelType = "SMS" VÀ Status = "1" -> chỉ SF_SMS_01 (SF_SMS_INACTIVE bị loại)
        var req = new Mst_SubmissionForm_IndexReq
        {
            CheckedChannelType = "SMS",
            CheckedStatusUse = "1",
            Init = "search"
        };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(1, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal("SF_SMS_01", data.GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());
    }

    [Fact]
    public async Task Index_FilterNonExistent_ReturnsZeroItems()
    {
        var req = new Mst_SubmissionForm_IndexReq { CheckedChannelType = "VIBER", Init = "search" };
        var res = await _controller.Index(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");
        Assert.Equal(0, data.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, data.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, data.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_Pagination_SkipAndTake()
    {
        // Trang 0, kích thước 2 -> lấy 2 bản ghi đầu
        var reqPage0 = new Mst_SubmissionForm_IndexReq { RecordCount = 2, Page = 0 };
        var resPage0 = await _controller.Index(reqPage0);
        var okPage0 = Assert.IsType<OkObjectResult>(resPage0);
        var docPage0 = JsonDocument.Parse(JsonSerializer.Serialize(okPage0.Value));
        var dataPage0 = docPage0.RootElement.GetProperty("Data");
        Assert.Equal(4, dataPage0.GetProperty("ItemCount").GetInt32());
        Assert.Equal(2, dataPage0.GetProperty("PageCount").GetInt32());
        var listPage0 = dataPage0.GetProperty("DataList");
        Assert.Equal(2, listPage0.GetArrayLength());
        Assert.Equal("SF_SMS_INACTIVE", listPage0[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("SF_ZALO_01", listPage0[1].GetProperty("SubFormCode").GetString());

        // Trang 1, kích thước 2 -> lấy 2 bản ghi sau
        var reqPage1 = new Mst_SubmissionForm_IndexReq { RecordCount = 2, Page = 1 };
        var resPage1 = await _controller.Index(reqPage1);
        var okPage1 = Assert.IsType<OkObjectResult>(resPage1);
        var docPage1 = JsonDocument.Parse(JsonSerializer.Serialize(okPage1.Value));
        var dataPage1 = docPage1.RootElement.GetProperty("Data");
        var listPage1 = dataPage1.GetProperty("DataList");
        Assert.Equal(2, listPage1.GetArrayLength());
        Assert.Equal("SF_SMS_01", listPage1[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("SF_EMAIL_01", listPage1[1].GetProperty("SubFormCode").GetString());
    }

    [Fact]
    public async Task Index_EdgeCases_ZeroRecordCountAndNegativePage()
    {
        // RecordCount = 0 -> DataList rỗng, PageCount = 0, ItemCount vẫn là tổng (LESSON A.7)
        var reqZero = new Mst_SubmissionForm_IndexReq { RecordCount = 0, Page = 0 };
        var resZero = await _controller.Index(reqZero);
        var okZero = Assert.IsType<OkObjectResult>(resZero);
        var docZero = JsonDocument.Parse(JsonSerializer.Serialize(okZero.Value));
        var dataZero = docZero.RootElement.GetProperty("Data");
        Assert.Equal(4, dataZero.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("PageCount").GetInt32());
        Assert.Equal(0, dataZero.GetProperty("DataList").GetArrayLength());

        // Page < 0 -> DataList rỗng (LESSON A.12)
        var reqNeg = new Mst_SubmissionForm_IndexReq { RecordCount = 10, Page = -1 };
        var resNeg = await _controller.Index(reqNeg);
        var okNeg = Assert.IsType<OkObjectResult>(resNeg);
        var docNeg = JsonDocument.Parse(JsonSerializer.Serialize(okNeg.Value));
        var dataNeg = docNeg.RootElement.GetProperty("Data");
        Assert.Equal(4, dataNeg.GetProperty("ItemCount").GetInt32());
        Assert.Equal(0, dataNeg.GetProperty("DataList").GetArrayLength());
    }

    [Fact]
    public async Task Index_FilterBySubFormCode_And_KeyWord()
    {
        // 1. SubFormCode
        var reqCode = new Mst_SubmissionForm_IndexReq { SubFormCode = "SF_EMAIL_01" };
        var resCode = await _controller.Index(reqCode);
        var okCode = Assert.IsType<OkObjectResult>(resCode);
        var docCode = JsonDocument.Parse(JsonSerializer.Serialize(okCode.Value));
        var dataCode = docCode.RootElement.GetProperty("Data");
        Assert.Equal(1, dataCode.GetProperty("ItemCount").GetInt32());
        Assert.Equal("SF_EMAIL_01", dataCode.GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());

        // 2. KeyWord
        var reqKw = new Mst_SubmissionForm_IndexReq { KeyWord = "tạm dừng" };
        var resKw = await _controller.Index(reqKw);
        var okKw = Assert.IsType<OkObjectResult>(resKw);
        var docKw = JsonDocument.Parse(JsonSerializer.Serialize(okKw.Value));
        var dataKw = docKw.RootElement.GetProperty("Data");
        Assert.Equal(1, dataKw.GetProperty("ItemCount").GetInt32());
        Assert.Equal("SF_SMS_INACTIVE", dataKw.GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());
    }

    [Fact]
    public async Task Create_WithEmptyOrNullBody_ReturnsExpectedChannelsAndParams()
    {
        // 1. null request
        var res1 = await _controller.Create(null);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var root1 = doc1.RootElement;

        Assert.True(root1.GetProperty("Success").GetBoolean());
        var data1 = root1.GetProperty("Data");
        Assert.Equal("create", data1.GetProperty("ViewType").GetString());

        var channel1 = data1.GetProperty("RT_Mst_Channel");
        var lstChan1 = channel1.GetProperty("Lst_Mst_Channel");
        Assert.Equal(1, lstChan1.GetArrayLength());
        Assert.Equal("7200000001", lstChan1[0].GetProperty("OrgID").GetString());

        Assert.Equal(1, channel1.GetProperty("Lst_Mst_ChannelEmail").GetArrayLength());
        Assert.Equal(1, channel1.GetProperty("Lst_Mst_ChannelSMS").GetArrayLength());
        Assert.Equal(1, channel1.GetProperty("Lst_Mst_ChannelZalo").GetArrayLength());

        var lstParam1 = data1.GetProperty("Lst_Mst_ParamContractSubmissForm");
        Assert.Equal(4, lstParam1.GetArrayLength());
        Assert.Equal("CONTRACTCODEOS", lstParam1[0].GetProperty("ParamContractCode").GetString());
        Assert.Equal("VALPAYMENTED", lstParam1[1].GetProperty("ParamContractCode").GetString());
        Assert.Equal("VALREMAIN", lstParam1[2].GetProperty("ParamContractCode").GetString());
        Assert.Equal("UNITPRICE", lstParam1[3].GetProperty("ParamContractCode").GetString());

        var subFormModel1 = data1.GetProperty("RT_Mst_SubmissionForm");
        Assert.Equal(0, subFormModel1.GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());

        // 2. empty request {}
        var res2 = await _controller.Create(new Mst_SubmissionForm_CreateReq());
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(1, doc2.RootElement.GetProperty("Data").GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_Channel").GetArrayLength());
        Assert.Equal(4, doc2.RootElement.GetProperty("Data").GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());
    }

    [Fact]
    public async Task Create_FilterByOrgId_7200000002_ReturnsMatchingChannel()
    {
        var req = new Mst_SubmissionForm_CreateReq { OrgID = "7200000002" };
        var res = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        var channel = data.GetProperty("RT_Mst_Channel");
        var lstChan = channel.GetProperty("Lst_Mst_Channel");
        Assert.Equal(1, lstChan.GetArrayLength());
        Assert.Equal("7200000002", lstChan[0].GetProperty("OrgID").GetString());
        Assert.Equal("SMS", lstChan[0].GetProperty("ChannelTypeContract").GetString());

        Assert.Equal(0, channel.GetProperty("Lst_Mst_ChannelEmail").GetArrayLength());
        Assert.Equal(0, channel.GetProperty("Lst_Mst_ChannelSMS").GetArrayLength());
        Assert.Equal(0, channel.GetProperty("Lst_Mst_ChannelZalo").GetArrayLength());

        var lstParam = data.GetProperty("Lst_Mst_ParamContractSubmissForm");
        Assert.Equal(4, lstParam.GetArrayLength());
    }

    [Fact]
    public async Task Create_FilterByNonExistentOrgId_ReturnsEmptyChannels()
    {
        var req = new Mst_SubmissionForm_CreateReq { OrgID = "9999999999" };
        var res = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        var channel = data.GetProperty("RT_Mst_Channel");
        Assert.Equal(0, channel.GetProperty("Lst_Mst_Channel").GetArrayLength());
        Assert.Equal(0, channel.GetProperty("Lst_Mst_ChannelEmail").GetArrayLength());
        Assert.Equal(0, channel.GetProperty("Lst_Mst_ChannelSMS").GetArrayLength());
        Assert.Equal(0, channel.GetProperty("Lst_Mst_ChannelZalo").GetArrayLength());

        var lstParam = data.GetProperty("Lst_Mst_ParamContractSubmissForm");
        Assert.Equal(4, lstParam.GetArrayLength());
    }

    [Fact]
    public async Task Create_FilterByInactiveOrg_7200000003_ReturnsEmptyChannelDueToFlagActive()
    {
        // Org 7200000003 có FlagActive = "0", nên điều kiện FlagActive = "1" sẽ loại bỏ
        var req = new Mst_SubmissionForm_CreateReq { OrgID = "7200000003" };
        var res = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        var channel = data.GetProperty("RT_Mst_Channel");
        Assert.Equal(0, channel.GetProperty("Lst_Mst_Channel").GetArrayLength());

        var lstParam = data.GetProperty("Lst_Mst_ParamContractSubmissForm");
        Assert.Equal(4, lstParam.GetArrayLength());
    }

    [Fact]
    public async Task Create_FilterByFlagActive_0_ReturnsInactiveChannelAndParams()
    {
        var req = new Mst_SubmissionForm_CreateReq { FlagActive = "0", OrgID = "7200000003" };
        var res = await _controller.Create(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        var channel = data.GetProperty("RT_Mst_Channel");
        var lstChan = channel.GetProperty("Lst_Mst_Channel");
        Assert.Equal(1, lstChan.GetArrayLength());
        Assert.Equal("7200000003", lstChan[0].GetProperty("OrgID").GetString());

        var lstParam = data.GetProperty("Lst_Mst_ParamContractSubmissForm");
        Assert.Equal(1, lstParam.GetArrayLength());
        Assert.Equal("INACTIVE_PARAM", lstParam[0].GetProperty("ParamContractCode").GetString());
    }

    [Fact]
    public async Task RealServer_Verification_Port()
    {
        var tempDb = Path.Combine(AppContext.BaseDirectory, $"verify_mst_submissionform_{Guid.NewGuid():N}.db");
        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniContract.dll"));
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "MiniContract.dll"));
        }
        Assert.True(File.Exists(dllPath), $"Cannot find MiniContract.dll at {dllPath}");

        var port = FreePort.Get();
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(dllPath),
        };
        psi.Environment["PORT"] = port.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={tempDb}";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(5) };
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
            Assert.True(started, $"MiniContract server failed to start within 15 seconds on port {port}.");

            // Ca 1: POST body rỗng -> trả về 4 bản ghi
            var req1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var res1 = await http.PostAsync("/api/mst_submissionform/index", req1);
            var body1 = await res1.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res1.StatusCode);
            var doc1 = JsonDocument.Parse(body1);
            Assert.True(doc1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(4, doc1.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // Ca 2: POST bộ lọc 1 (CheckedChannelType = "EMAIL") -> 1 bản ghi
            var req2 = new StringContent("{\"CheckedChannelType\":\"EMAIL\",\"Init\":\"search\"}", Encoding.UTF8, "application/json");
            var res2 = await http.PostAsync("/api/mst_submissionform/index", req2);
            var body2 = await res2.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res2.StatusCode);
            var doc2 = JsonDocument.Parse(body2);
            Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(1, doc2.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal("SF_EMAIL_01", doc2.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());

            // Ca 3: POST bộ lọc 2 (CheckedChannelType = "SMS") -> 2 bản ghi
            var req3 = new StringContent("{\"CheckedChannelType\":\"SMS\",\"Init\":\"search\"}", Encoding.UTF8, "application/json");
            var res3 = await http.PostAsync("/api/mst_submissionform/index", req3);
            var body3 = await res3.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res3.StatusCode);
            var doc3 = JsonDocument.Parse(body3);
            Assert.True(doc3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(2, doc3.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());

            // Ca 4: POST bộ lọc 3 (CheckedStatusUse = "0") -> 1 bản ghi
            var req4 = new StringContent("{\"CheckedStatusUse\":\"0\",\"Init\":\"search\"}", Encoding.UTF8, "application/json");
            var res4 = await http.PostAsync("/api/mst_submissionform/index", req4);
            var body4 = await res4.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, res4.StatusCode);
            var doc4 = JsonDocument.Parse(body4);
            Assert.True(doc4.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(1, doc4.RootElement.GetProperty("Data").GetProperty("ItemCount").GetInt32());
            Assert.Equal("SF_SMS_INACTIVE", doc4.RootElement.GetProperty("Data").GetProperty("DataList")[0].GetProperty("SubFormCode").GetString());

            // Ca 5 (Create): POST body rỗng -> khởi tạo thành công với 1 channel (7200000001) và 4 params
            var reqCreate1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resCreate1 = await http.PostAsync("/api/mst_submissionform/create", reqCreate1);
            var bodyCreate1 = await resCreate1.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resCreate1.StatusCode);
            var docCreate1 = JsonDocument.Parse(bodyCreate1);
            Assert.True(docCreate1.RootElement.GetProperty("Success").GetBoolean());
            var dataCreate1 = docCreate1.RootElement.GetProperty("Data");
            Assert.Equal("create", dataCreate1.GetProperty("ViewType").GetString());
            Assert.Equal(1, dataCreate1.GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_Channel").GetArrayLength());
            Assert.Equal(4, dataCreate1.GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());

            // Ca 6 (Create): POST lọc OrgID = "7200000002" -> 1 channel SMS, 0 email, 4 params
            var reqCreate2 = new StringContent("{\"OrgID\":\"7200000002\"}", Encoding.UTF8, "application/json");
            var resCreate2 = await http.PostAsync("/api/mst_submissionform/create", reqCreate2);
            var bodyCreate2 = await resCreate2.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resCreate2.StatusCode);
            var docCreate2 = JsonDocument.Parse(bodyCreate2);
            Assert.True(docCreate2.RootElement.GetProperty("Success").GetBoolean());
            var dataCreate2 = docCreate2.RootElement.GetProperty("Data");
            Assert.Equal(1, dataCreate2.GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_Channel").GetArrayLength());
            Assert.Equal("SMS", dataCreate2.GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_Channel")[0].GetProperty("ChannelTypeContract").GetString());
            Assert.Equal(0, dataCreate2.GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_ChannelEmail").GetArrayLength());
            Assert.Equal(4, dataCreate2.GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());

            // Ca 7 (Create): POST lọc OrgID không tồn tại "9999999999" -> 0 channel, 4 params
            var reqCreate3 = new StringContent("{\"OrgID\":\"9999999999\"}", Encoding.UTF8, "application/json");
            var resCreate3 = await http.PostAsync("/api/mst_submissionform/create", reqCreate3);
            var bodyCreate3 = await resCreate3.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resCreate3.StatusCode);
            var docCreate3 = JsonDocument.Parse(bodyCreate3);
            Assert.True(docCreate3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, docCreate3.RootElement.GetProperty("Data").GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_Channel").GetArrayLength());
            Assert.Equal(4, docCreate3.RootElement.GetProperty("Data").GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());

            // Ca 8 (Create): POST lọc FlagActive = "0", OrgID = "7200000003" -> 1 channel, 1 param
            var reqCreate4 = new StringContent("{\"FlagActive\":\"0\",\"OrgID\":\"7200000003\"}", Encoding.UTF8, "application/json");
            var resCreate4 = await http.PostAsync("/api/mst_submissionform/create", reqCreate4);
            var bodyCreate4 = await resCreate4.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resCreate4.StatusCode);
            var docCreate4 = JsonDocument.Parse(bodyCreate4);
            Assert.True(docCreate4.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(1, docCreate4.RootElement.GetProperty("Data").GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_Channel").GetArrayLength());
            Assert.Equal(1, docCreate4.RootElement.GetProperty("Data").GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());
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

            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
