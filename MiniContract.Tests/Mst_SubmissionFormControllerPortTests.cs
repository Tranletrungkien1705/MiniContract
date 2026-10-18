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
        new Mst_SubmissionFormMessageSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_SubmissionFormZNSSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_ChannelTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();
        new Mst_BulletinTypeSeeder().SeedAsync(_db).GetAwaiter().GetResult();

        // Thêm kênh không hoạt động để test guard FlagActive
        _db.Set<Mst_ChannelType>().Add(new Mst_ChannelType
        {
            ChannelType = "INACTIVE_CHANNEL",
            ChannelTypeName = "Kênh tạm dừng",
            FlagActive = "0",
            LogLUDTimeUTC = "2024-01-01 10:00:00",
            LogLUBy = "port"
        });
        _db.SaveChanges();

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
    public async Task Detail_WithNullOrEmptyReq_ReturnsEmptyFormAndNullChannel()
    {
        // 1. null request
        var res1 = await _controller.Detail(null);
        var ok1 = Assert.IsType<OkObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(ok1.Value));
        var root1 = doc1.RootElement;

        Assert.True(root1.GetProperty("Success").GetBoolean());
        var data1 = root1.GetProperty("Data");
        Assert.Equal("update", data1.GetProperty("ViewType").GetString());
        Assert.Equal(0, data1.GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());
        Assert.Equal(0, data1.GetProperty("Lst_Mst_SubmissionFormMessage").GetArrayLength());
        Assert.Equal(0, data1.GetProperty("Lst_Mst_SubmissionFormZNS").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, data1.GetProperty("RT_Mst_Channel").ValueKind);
        Assert.Equal(0, data1.GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());

        // 2. empty request {}
        var res2 = await _controller.Detail(new Mst_SubmissionForm_DetailReq());
        var ok2 = Assert.IsType<OkObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
        Assert.True(doc2.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal(0, doc2.RootElement.GetProperty("Data").GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("Data").GetProperty("RT_Mst_Channel").ValueKind);
    }

    [Fact]
    public async Task Detail_NonExistentSubFormCode_ReturnsEmptyFormAndNullChannel()
    {
        var req = new Mst_SubmissionForm_DetailReq { SubFormCode = "NON_EXISTENT_CODE" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("update", data.GetProperty("ViewType").GetString());
        Assert.Equal(0, data.GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());
        Assert.Equal(0, data.GetProperty("Lst_Mst_SubmissionFormMessage").GetArrayLength());
        Assert.Equal(0, data.GetProperty("Lst_Mst_SubmissionFormZNS").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("RT_Mst_Channel").ValueKind);
    }

    [Fact]
    public async Task Detail_EmailSubForm_ReturnsFormMessageAndChannels()
    {
        var req = new Mst_SubmissionForm_DetailReq { SubFormCode = "SF_EMAIL_01" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("update", data.GetProperty("ViewType").GetString());

        var forms = data.GetProperty("Lst_Mst_SubmissionForm");
        Assert.Equal(1, forms.GetArrayLength());
        Assert.Equal("SF_EMAIL_01", forms[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("Mẫu gửi Email 01", forms[0].GetProperty("SubFormName").GetString());
        Assert.Equal("EMAIL", forms[0].GetProperty("ChannelType").GetString());

        var msgs = data.GetProperty("Lst_Mst_SubmissionFormMessage");
        Assert.Equal(1, msgs.GetArrayLength());
        Assert.Equal("SF_EMAIL_01", msgs[0].GetProperty("SubFormCode").GetString());
        Assert.Contains("hợp đồng của quý khách đã được tạo thành công", msgs[0].GetProperty("Message").GetString());

        var zns = data.GetProperty("Lst_Mst_SubmissionFormZNS");
        Assert.Equal(0, zns.GetArrayLength());

        var channel = data.GetProperty("RT_Mst_Channel");
        Assert.Equal(1, channel.GetProperty("Lst_Mst_Channel").GetArrayLength());
        Assert.Equal("7200000001", channel.GetProperty("Lst_Mst_Channel")[0].GetProperty("OrgID").GetString());
        Assert.Equal(1, channel.GetProperty("Lst_Mst_ChannelEmail").GetArrayLength());
        Assert.Equal("support@qcontract.vn", channel.GetProperty("Lst_Mst_ChannelEmail")[0].GetProperty("MailFrom").GetString());

        var lstParam = data.GetProperty("Lst_Mst_ParamContractSubmissForm");
        Assert.Equal(4, lstParam.GetArrayLength());
    }

    [Fact]
    public async Task Detail_ZaloSubForm_ReturnsFormZNSWithParamInfoAndChannels()
    {
        var req = new Mst_SubmissionForm_DetailReq { SubFormCode = "SF_ZALO_01" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("update", data.GetProperty("ViewType").GetString());

        var forms = data.GetProperty("Lst_Mst_SubmissionForm");
        Assert.Equal(1, forms.GetArrayLength());
        Assert.Equal("SF_ZALO_01", forms[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("ZALO", forms[0].GetProperty("ChannelType").GetString());
        Assert.Equal("123456", forms[0].GetProperty("IDZNS").GetString());

        var msgs = data.GetProperty("Lst_Mst_SubmissionFormMessage");
        Assert.Equal(0, msgs.GetArrayLength());

        var zns = data.GetProperty("Lst_Mst_SubmissionFormZNS");
        Assert.Equal(2, zns.GetArrayLength());
        Assert.Equal("contract_code", zns[0].GetProperty("ParamContractCodeZNS").GetString());
        Assert.Equal("CONTRACTCODEOS", zns[0].GetProperty("ParamContractCode").GetString());
        Assert.Equal("ContractCodeOS", zns[0].GetProperty("ParamContractName").GetString());
        Assert.Equal("Contract_Contract", zns[0].GetProperty("ParamContractType").GetString());

        Assert.Equal("payment_amount", zns[1].GetProperty("ParamContractCodeZNS").GetString());
        Assert.Equal("VALPAYMENTED", zns[1].GetProperty("ParamContractCode").GetString());
        Assert.Equal("ValPaymented", zns[1].GetProperty("ParamContractName").GetString());
        Assert.Equal("Contract_ContractParty", zns[1].GetProperty("ParamContractType").GetString());

        var channel = data.GetProperty("RT_Mst_Channel");
        Assert.Equal(1, channel.GetProperty("Lst_Mst_ChannelZalo").GetArrayLength());
        Assert.Equal("1358767413636684272", channel.GetProperty("Lst_Mst_ChannelZalo")[0].GetProperty("ZaloOAID").GetString());

        var lstParam = data.GetProperty("Lst_Mst_ParamContractSubmissForm");
        Assert.Equal(4, lstParam.GetArrayLength());
    }

    [Fact]
    public async Task Detail_FilterWithDifferentOrgId_ReturnsMatchingChannels()
    {
        var req = new Mst_SubmissionForm_DetailReq { SubFormCode = "SF_SMS_01", OrgID = "7200000002" };
        var res = await _controller.Detail(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        var forms = data.GetProperty("Lst_Mst_SubmissionForm");
        Assert.Equal(1, forms.GetArrayLength());
        Assert.Equal("SF_SMS_01", forms[0].GetProperty("SubFormCode").GetString());

        var msgs = data.GetProperty("Lst_Mst_SubmissionFormMessage");
        Assert.Equal(1, msgs.GetArrayLength());
        Assert.Equal("SF_SMS_01", msgs[0].GetProperty("SubFormCode").GetString());

        var channel = data.GetProperty("RT_Mst_Channel");
        Assert.Equal(1, channel.GetProperty("Lst_Mst_Channel").GetArrayLength());
        Assert.Equal("7200000002", channel.GetProperty("Lst_Mst_Channel")[0].GetProperty("OrgID").GetString());
        Assert.Equal("SMS", channel.GetProperty("Lst_Mst_Channel")[0].GetProperty("ChannelTypeContract").GetString());
        Assert.Equal(0, channel.GetProperty("Lst_Mst_ChannelSMS").GetArrayLength());
    }

    [Fact]
    public async Task CreateCopy_WithNullOrEmptyReq_ReturnsInvalidSubFormCodeGuard()
    {
        // 1. null request
        var res1 = await _controller.CreateCopy(null);
        var bad1 = Assert.IsType<BadRequestObjectResult>(res1);
        var doc1 = JsonDocument.Parse(JsonSerializer.Serialize(bad1.Value));
        Assert.False(doc1.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_SubmissionForm_SaveX_InvalidSubFormCode", doc1.RootElement.GetProperty("ErrorCode").GetString());

        // 2. empty request
        var res2 = await _controller.CreateCopy(new Mst_SubmissionForm_CreateCopyReq());
        var bad2 = Assert.IsType<BadRequestObjectResult>(res2);
        var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(bad2.Value));
        Assert.False(doc2.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_SubmissionForm_SaveX_InvalidSubFormCode", doc2.RootElement.GetProperty("ErrorCode").GetString());

        // 3. SubFormCode rỗng
        var res3 = await _controller.CreateCopy(new Mst_SubmissionForm_CreateCopyReq
        {
            Mst_SubmissionForm = new Mst_SubmissionForm { SubFormCode = "" }
        });
        var bad3 = Assert.IsType<BadRequestObjectResult>(res3);
        var doc3 = JsonDocument.Parse(JsonSerializer.Serialize(bad3.Value));
        Assert.False(doc3.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_SubmissionForm_SaveX_InvalidSubFormCode", doc3.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task CreateCopy_WithNonExistentSubFormCode_ReturnsSubFormCodeNotFoundGuard()
    {
        var req = new Mst_SubmissionForm_CreateCopyReq
        {
            Mst_SubmissionForm = new Mst_SubmissionForm
            {
                SubFormCode = "SF_NOT_FOUND",
                ChannelType = "EMAIL",
                BulletinType = "CONTRACT_NOTIFY"
            }
        };
        var res = await _controller.CreateCopy(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_SubmissionForm_CheckDB_SubFormCodeNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task CreateCopy_WithNonExistentChannelType_ReturnsChannelTypeNotFoundGuard()
    {
        var req = new Mst_SubmissionForm_CreateCopyReq
        {
            Mst_SubmissionForm = new Mst_SubmissionForm
            {
                SubFormCode = "SF_EMAIL_01",
                ChannelType = "NON_EXISTENT_CHANNEL",
                BulletinType = "CONTRACT_NOTIFY"
            }
        };
        var res = await _controller.CreateCopy(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ChannelType_CheckDB_ChannelTypeNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task CreateCopy_WithInactiveChannelType_ReturnsChannelTypeFlagActiveNotMatchedGuard()
    {
        var req = new Mst_SubmissionForm_CreateCopyReq
        {
            Mst_SubmissionForm = new Mst_SubmissionForm
            {
                SubFormCode = "SF_EMAIL_01",
                ChannelType = "INACTIVE_CHANNEL",
                BulletinType = "CONTRACT_NOTIFY"
            }
        };
        var res = await _controller.CreateCopy(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_ChannelType_CheckDB_FlagActiveNotMatched", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task CreateCopy_WithNonExistentBulletinType_ReturnsBulletinTypeNotFoundGuard()
    {
        var req = new Mst_SubmissionForm_CreateCopyReq
        {
            Mst_SubmissionForm = new Mst_SubmissionForm
            {
                SubFormCode = "SF_EMAIL_01",
                ChannelType = "EMAIL",
                BulletinType = "NON_EXISTENT_BULLETIN"
            }
        };
        var res = await _controller.CreateCopy(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_BulletinType_CheckDB_BulletinTypeNotFound", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task CreateCopy_WithInactiveBulletinType_ReturnsBulletinTypeFlagActiveNotMatchedGuard()
    {
        var req = new Mst_SubmissionForm_CreateCopyReq
        {
            Mst_SubmissionForm = new Mst_SubmissionForm
            {
                SubFormCode = "SF_EMAIL_01",
                ChannelType = "EMAIL",
                BulletinType = "INACTIVE_BULLETIN"
            }
        };
        var res = await _controller.CreateCopy(req);
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(bad.Value));
        Assert.False(doc.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Erridn.Skycic.QContract.Mst_BulletinType_CheckDB_FlagActiveNotMatched", doc.RootElement.GetProperty("ErrorCode").GetString());
    }

    [Fact]
    public async Task CreateCopy_RoundTrip_UpdateAndReturnClearedSubFormCode()
    {
        var req = new Mst_SubmissionForm_CreateCopyReq
        {
            Mst_SubmissionForm = new Mst_SubmissionForm
            {
                SubFormCode = "SF_EMAIL_01",
                SubFormName = "Mẫu gửi Email 01 Đã Cập Nhật",
                OrgID = "7200000001",
                ChannelType = "EMAIL",
                BulletinType = "CONTRACT_NOTIFY",
                FlagActive = "1"
            },
            Lst_Mst_SubmissionFormMessage = new List<Mst_SubmissionFormMessage>
            {
                new()
                {
                    Message = "Nội dung email mới cập nhật cho mẫu 01"
                }
            }
        };

        var res = await _controller.CreateCopy(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Success").GetBoolean());
        var data = root.GetProperty("Data");
        Assert.Equal("createcopy", data.GetProperty("ViewType").GetString());

        var subForms = data.GetProperty("Lst_Mst_SubmissionForm");
        Assert.Equal(1, subForms.GetArrayLength());
        // SubFormCode đã được xoá để làm mẫu mới
        Assert.Equal("", subForms[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("Mẫu gửi Email 01 Đã Cập Nhật", subForms[0].GetProperty("SubFormName").GetString());
        Assert.Equal("EMAIL", subForms[0].GetProperty("ChannelType").GetString());
        Assert.Equal("CONTRACT_NOTIFY", subForms[0].GetProperty("BulletinType").GetString());

        var msgs = data.GetProperty("Lst_Mst_SubmissionFormMessage");
        Assert.Equal(1, msgs.GetArrayLength());
        Assert.Equal("Nội dung email mới cập nhật cho mẫu 01", msgs[0].GetProperty("Message").GetString());

        var channel = data.GetProperty("RT_Mst_Channel");
        Assert.Equal(1, channel.GetProperty("Lst_Mst_Channel").GetArrayLength());
        Assert.Equal(1, channel.GetProperty("Lst_Mst_ChannelEmail").GetArrayLength());
        Assert.Equal(4, data.GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());

        // Round-trip kiểm tra dữ liệu thật trong DB
        var dbItem = await _db.Set<Mst_SubmissionForm>().FirstOrDefaultAsync(x => x.SubFormCode == "SF_EMAIL_01");
        Assert.NotNull(dbItem);
        Assert.Equal("Mẫu gửi Email 01 Đã Cập Nhật", dbItem.SubFormName);
        Assert.Equal("EMAIL", dbItem.ChannelType);

        var dbMsgs = await _db.Set<Mst_SubmissionFormMessage>().Where(x => x.SubFormCode == "SF_EMAIL_01").ToListAsync();
        Assert.Equal(1, dbMsgs.Count);
        Assert.Equal("Nội dung email mới cập nhật cho mẫu 01", dbMsgs[0].Message);
    }

    [Fact]
    public async Task CreateCopy_WithJsonModelString_RoundTrip_WithZns()
    {
        var modelJson = JsonSerializer.Serialize(new
        {
            Mst_SubmissionForm = new
            {
                SubFormCode = "SF_ZALO_01",
                SubFormName = "Mẫu Zalo Đã Cập Nhật",
                OrgID = "7200000001",
                ChannelType = "ZALO",
                BulletinType = "ZNS_NOTIFY",
                IDZNS = "999999",
                FlagActive = "1"
            },
            Lst_Mst_SubmissionFormZNS = new[]
            {
                new
                {
                    ParamContractCodeZNS = "order_id",
                    SourceDataType = "SYSTEM",
                    ParamContractCode = "CONTRACTCODEOS",
                    ParamValue = "ORD-999"
                }
            }
        });

        var req = new Mst_SubmissionForm_CreateCopyReq { Model = modelJson };
        var res = await _controller.CreateCopy(req);
        var ok = Assert.IsType<OkObjectResult>(res);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var data = doc.RootElement.GetProperty("Data");

        Assert.Equal("createcopy", data.GetProperty("ViewType").GetString());
        var subForms = data.GetProperty("Lst_Mst_SubmissionForm");
        Assert.Equal(1, subForms.GetArrayLength());
        Assert.Equal("", subForms[0].GetProperty("SubFormCode").GetString());
        Assert.Equal("Mẫu Zalo Đã Cập Nhật", subForms[0].GetProperty("SubFormName").GetString());
        Assert.Equal("999999", subForms[0].GetProperty("IDZNS").GetString());

        var znsList = data.GetProperty("Lst_Mst_SubmissionFormZNS");
        Assert.Equal(1, znsList.GetArrayLength());
        Assert.Equal("order_id", znsList[0].GetProperty("ParamContractCodeZNS").GetString());

        // Round-trip kiểm tra DB
        var dbItem = await _db.Set<Mst_SubmissionForm>().FirstOrDefaultAsync(x => x.SubFormCode == "SF_ZALO_01");
        Assert.NotNull(dbItem);
        Assert.Equal("999999", dbItem.IDZNS);
        Assert.Equal("Mẫu Zalo Đã Cập Nhật", dbItem.SubFormName);

        var dbZns = await _db.Set<Mst_SubmissionFormZNS>().Where(x => x.SubFormCode == "SF_ZALO_01").ToListAsync();
        Assert.Equal(1, dbZns.Count);
        Assert.Equal("order_id", dbZns[0].ParamContractCodeZNS);
        Assert.Equal("ORD-999", dbZns[0].ParamValue);
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

            // Ca 9 (Detail): POST body rỗng -> Form rỗng, RT_Mst_Channel = null
            var reqDetail1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resDetail1 = await http.PostAsync("/api/mst_submissionform/detail", reqDetail1);
            var bodyDetail1 = await resDetail1.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resDetail1.StatusCode);
            var docDetail1 = JsonDocument.Parse(bodyDetail1);
            Assert.True(docDetail1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, docDetail1.RootElement.GetProperty("Data").GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, docDetail1.RootElement.GetProperty("Data").GetProperty("RT_Mst_Channel").ValueKind);

            // Ca 10 (Detail): POST subformcode không tồn tại -> Form rỗng, RT_Mst_Channel = null
            var reqDetail2 = new StringContent("{\"SubFormCode\":\"NON_EXISTENT\"}", Encoding.UTF8, "application/json");
            var resDetail2 = await http.PostAsync("/api/mst_submissionform/detail", reqDetail2);
            var bodyDetail2 = await resDetail2.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resDetail2.StatusCode);
            var docDetail2 = JsonDocument.Parse(bodyDetail2);
            Assert.True(docDetail2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(0, docDetail2.RootElement.GetProperty("Data").GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, docDetail2.RootElement.GetProperty("Data").GetProperty("RT_Mst_Channel").ValueKind);

            // Ca 11 (Detail): POST lấy chi tiết mẫu Email "SF_EMAIL_01" -> 1 form, 1 message, channel Email, 4 params
            var reqDetail3 = new StringContent("{\"SubFormCode\":\"SF_EMAIL_01\"}", Encoding.UTF8, "application/json");
            var resDetail3 = await http.PostAsync("/api/mst_submissionform/detail", reqDetail3);
            var bodyDetail3 = await resDetail3.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resDetail3.StatusCode);
            var docDetail3 = JsonDocument.Parse(bodyDetail3);
            Assert.True(docDetail3.RootElement.GetProperty("Success").GetBoolean());
            var dataDetail3 = docDetail3.RootElement.GetProperty("Data");
            Assert.Equal(1, dataDetail3.GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());
            Assert.Equal("SF_EMAIL_01", dataDetail3.GetProperty("Lst_Mst_SubmissionForm")[0].GetProperty("SubFormCode").GetString());
            Assert.Equal(1, dataDetail3.GetProperty("Lst_Mst_SubmissionFormMessage").GetArrayLength());
            Assert.Contains("hợp đồng của quý khách đã được tạo thành công", dataDetail3.GetProperty("Lst_Mst_SubmissionFormMessage")[0].GetProperty("Message").GetString());
            Assert.Equal(0, dataDetail3.GetProperty("Lst_Mst_SubmissionFormZNS").GetArrayLength());
            Assert.Equal(1, dataDetail3.GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_Channel").GetArrayLength());
            Assert.Equal(4, dataDetail3.GetProperty("Lst_Mst_ParamContractSubmissForm").GetArrayLength());

            // Ca 12 (Detail): POST lấy chi tiết mẫu Zalo "SF_ZALO_01" -> 1 form (IDZNS 123456), 2 ZNS items kèm ParamContractName/ParamContractType, channel Zalo
            var reqDetail4 = new StringContent("{\"subformcode\":\"SF_ZALO_01\"}", Encoding.UTF8, "application/json");
            var resDetail4 = await http.PostAsync("/api/mst_submissionform/detail", reqDetail4);
            var bodyDetail4 = await resDetail4.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resDetail4.StatusCode);
            var docDetail4 = JsonDocument.Parse(bodyDetail4);
            Assert.True(docDetail4.RootElement.GetProperty("Success").GetBoolean());
            var dataDetail4 = docDetail4.RootElement.GetProperty("Data");
            Assert.Equal(1, dataDetail4.GetProperty("Lst_Mst_SubmissionForm").GetArrayLength());
            Assert.Equal("SF_ZALO_01", dataDetail4.GetProperty("Lst_Mst_SubmissionForm")[0].GetProperty("SubFormCode").GetString());
            Assert.Equal("123456", dataDetail4.GetProperty("Lst_Mst_SubmissionForm")[0].GetProperty("IDZNS").GetString());
            Assert.Equal(0, dataDetail4.GetProperty("Lst_Mst_SubmissionFormMessage").GetArrayLength());
            Assert.Equal(2, dataDetail4.GetProperty("Lst_Mst_SubmissionFormZNS").GetArrayLength());
            Assert.Equal("ContractCodeOS", dataDetail4.GetProperty("Lst_Mst_SubmissionFormZNS")[0].GetProperty("ParamContractName").GetString());
            Assert.Equal("ValPaymented", dataDetail4.GetProperty("Lst_Mst_SubmissionFormZNS")[1].GetProperty("ParamContractName").GetString());
            Assert.Equal(1, dataDetail4.GetProperty("RT_Mst_Channel").GetProperty("Lst_Mst_ChannelZalo").GetArrayLength());

            // Ca 13 (CreateCopy Guard): POST body rỗng -> 400 InvalidSubFormCode
            var reqCopyGuard1 = new StringContent("{}", Encoding.UTF8, "application/json");
            var resCopyGuard1 = await http.PostAsync("/api/mst_submissionform/createcopy", reqCopyGuard1);
            var bodyCopyGuard1 = await resCopyGuard1.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resCopyGuard1.StatusCode);
            var docCopyGuard1 = JsonDocument.Parse(bodyCopyGuard1);
            Assert.False(docCopyGuard1.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_SubmissionForm_SaveX_InvalidSubFormCode", docCopyGuard1.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 14 (CreateCopy Guard): POST SubFormCode không tồn tại -> 400 SubFormCodeNotFound
            var reqCopyGuard2 = new StringContent("{\"Mst_SubmissionForm\":{\"SubFormCode\":\"SF_NOT_FOUND\",\"ChannelType\":\"EMAIL\",\"BulletinType\":\"CONTRACT_NOTIFY\"}}", Encoding.UTF8, "application/json");
            var resCopyGuard2 = await http.PostAsync("/api/mst_submissionform/createcopy", reqCopyGuard2);
            var bodyCopyGuard2 = await resCopyGuard2.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resCopyGuard2.StatusCode);
            var docCopyGuard2 = JsonDocument.Parse(bodyCopyGuard2);
            Assert.False(docCopyGuard2.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_SubmissionForm_CheckDB_SubFormCodeNotFound", docCopyGuard2.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 15 (CreateCopy Guard): POST ChannelType không tồn tại -> 400 ChannelTypeNotFound
            var reqCopyGuard3 = new StringContent("{\"Mst_SubmissionForm\":{\"SubFormCode\":\"SF_EMAIL_01\",\"ChannelType\":\"UNKNOWN_CHANNEL\",\"BulletinType\":\"CONTRACT_NOTIFY\"}}", Encoding.UTF8, "application/json");
            var resCopyGuard3 = await http.PostAsync("/api/mst_submissionform/createcopy", reqCopyGuard3);
            var bodyCopyGuard3 = await resCopyGuard3.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resCopyGuard3.StatusCode);
            var docCopyGuard3 = JsonDocument.Parse(bodyCopyGuard3);
            Assert.False(docCopyGuard3.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_ChannelType_CheckDB_ChannelTypeNotFound", docCopyGuard3.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 16 (CreateCopy Guard): POST BulletinType không tồn tại -> 400 BulletinTypeNotFound
            var reqCopyGuard4 = new StringContent("{\"Mst_SubmissionForm\":{\"SubFormCode\":\"SF_EMAIL_01\",\"ChannelType\":\"EMAIL\",\"BulletinType\":\"UNKNOWN_BULLETIN\"}}", Encoding.UTF8, "application/json");
            var resCopyGuard4 = await http.PostAsync("/api/mst_submissionform/createcopy", reqCopyGuard4);
            var bodyCopyGuard4 = await resCopyGuard4.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resCopyGuard4.StatusCode);
            var docCopyGuard4 = JsonDocument.Parse(bodyCopyGuard4);
            Assert.False(docCopyGuard4.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("Erridn.Skycic.QContract.Mst_BulletinType_CheckDB_BulletinTypeNotFound", docCopyGuard4.RootElement.GetProperty("ErrorCode").GetString());

            // Ca 17 (CreateCopy Round-trip): POST cập nhật SF_EMAIL_01 thành công, trả về SubFormCode = "" và đọc lại thấy dữ liệu đã được update
            var copyPayload = "{\"Mst_SubmissionForm\":{\"SubFormCode\":\"SF_EMAIL_01\",\"SubFormName\":\"Email 01 Ban Sao\",\"ChannelType\":\"EMAIL\",\"BulletinType\":\"CONTRACT_NOTIFY\",\"FlagActive\":\"1\"},\"Lst_Mst_SubmissionFormMessage\":[{\"Message\":\"Thong bao cap nhat ban sao\"}]}";
            var reqCopySuccess = new StringContent(copyPayload, Encoding.UTF8, "application/json");
            var resCopySuccess = await http.PostAsync("/api/mst_submissionform/createcopy", reqCopySuccess);
            var bodyCopySuccess = await resCopySuccess.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resCopySuccess.StatusCode);
            var docCopySuccess = JsonDocument.Parse(bodyCopySuccess);
            Assert.True(docCopySuccess.RootElement.GetProperty("Success").GetBoolean());
            var copyData = docCopySuccess.RootElement.GetProperty("Data");
            Assert.Equal("createcopy", copyData.GetProperty("ViewType").GetString());
            Assert.Equal("", copyData.GetProperty("Lst_Mst_SubmissionForm")[0].GetProperty("SubFormCode").GetString());
            Assert.Equal("Email 01 Ban Sao", copyData.GetProperty("Lst_Mst_SubmissionForm")[0].GetProperty("SubFormName").GetString());

            // Đọc lại Detail SF_EMAIL_01 trên server để xác nhận round-trip đã lưu vào DB
            var reqVerifyDetail = new StringContent("{\"SubFormCode\":\"SF_EMAIL_01\"}", Encoding.UTF8, "application/json");
            var resVerifyDetail = await http.PostAsync("/api/mst_submissionform/detail", reqVerifyDetail);
            var bodyVerifyDetail = await resVerifyDetail.Content.ReadAsStringAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, resVerifyDetail.StatusCode);
            var docVerifyDetail = JsonDocument.Parse(bodyVerifyDetail);
            Assert.True(docVerifyDetail.RootElement.GetProperty("Success").GetBoolean());
            var verifyForms = docVerifyDetail.RootElement.GetProperty("Data").GetProperty("Lst_Mst_SubmissionForm");
            Assert.Equal(1, verifyForms.GetArrayLength());
            Assert.Equal("Email 01 Ban Sao", verifyForms[0].GetProperty("SubFormName").GetString());
            var verifyMsgs = docVerifyDetail.RootElement.GetProperty("Data").GetProperty("Lst_Mst_SubmissionFormMessage");
            Assert.Equal(1, verifyMsgs.GetArrayLength());
            Assert.Equal("Thong bao cap nhat ban sao", verifyMsgs[0].GetProperty("Message").GetString());
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
