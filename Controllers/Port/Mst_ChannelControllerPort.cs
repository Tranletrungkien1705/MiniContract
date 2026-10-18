using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ChannelController.cs — Mst_Channel
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ChannelController.cs:572-733 (Save), 21-165 (Index), 479-567 (ZaloDisconnect)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstChannelTypeController.cs:376-444
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\MasterData.cs:18938-19034, 19053-19169, 21232-22492
[ApiController]
[Route("api/mst_channel")]
public class Mst_ChannelControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_ChannelControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lưu cấu hình kênh gửi (Email, SMS, Zalo).
    /// Gate: Mst_ChannelController.cs:572-733 Save(string model)
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_SaveReq? req)
    {
        // 1. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ChannelController.cs:572, 614)
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Channel_SaveReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.OrgID ??= modelObj.OrgID;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.Mst_Channel ??= modelObj.Mst_Channel;
                    req.Lst_Mst_ChannelEmail ??= modelObj.Lst_Mst_ChannelEmail;
                    req.Lst_Mst_ChannelSMS ??= modelObj.Lst_Mst_ChannelSMS;
                    req.Lst_Mst_ChannelZalo ??= modelObj.Lst_Mst_ChannelZalo;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô, để validation phía sau bắt
            }
        }

        // 2. Kiểm tra OrgID bắt buộc (MasterData.cs:21295-21306)
        var orgId = StdParam(req?.OrgID);
        if (string.IsNullOrEmpty(orgId))
        {
            return PortResult.Fail("Mst_Channel_SaveX_InvalidOrgID", "Check.strOrgID", req?.OrgID);
        }

        // 3. Lấy cấu hình kênh hiện có trong DB (Mst_ChannelController.cs:603, 616-655)
        var existingChannel = await _db.Set<Mst_Channel>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");
        var existingEmail = await _db.Set<Mst_ChannelEmail>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");
        var existingSMS = await _db.Set<Mst_ChannelSMS>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");
        var existingZalo = await _db.Set<Mst_ChannelZalo>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");

        // 4. Trộn dữ liệu đầu vào với bản ghi DB (Mst_ChannelController.cs:616-655)
        // Kênh chính
        var channel = existingChannel ?? new Mst_Channel { OrgID = orgId, NetworkID = StdParam(req?.NetworkID) ?? "1001", FlagActive = "1" };
        var channelInput = req?.Mst_Channel;
        if (channelInput != null)
        {
            if (channelInput.ChannelTypeContract != null) channel.ChannelTypeContract = channelInput.ChannelTypeContract;
            if (channelInput.ChannelTypeOTP != null) channel.ChannelTypeOTP = channelInput.ChannelTypeOTP;
        }

        // Kênh Email
        var email = existingEmail ?? new Mst_ChannelEmail { OrgID = orgId, FlagActive = "1" };
        var emailInput = req?.Lst_Mst_ChannelEmail?.FirstOrDefault();
        if (emailInput != null)
        {
            if (emailInput.SubFormCodeEmailContract != null) email.SubFormCodeEmailContract = emailInput.SubFormCodeEmailContract;
            if (emailInput.SubFormCodeEmailOTP != null) email.SubFormCodeEmailOTP = emailInput.SubFormCodeEmailOTP;
            if (emailInput.SubFormCodeEmailAccessKey != null) email.SubFormCodeEmailAccessKey = emailInput.SubFormCodeEmailAccessKey;
            if (emailInput.MailFrom != null) email.MailFrom = emailInput.MailFrom;
            if (emailInput.APIsSendMail != null) email.APIsSendMail = emailInput.APIsSendMail;
            if (emailInput.ApiKeySendMail != null) email.ApiKeySendMail = emailInput.ApiKeySendMail;
            if (emailInput.SolutionCodeSendMail != null) email.SolutionCodeSendMail = emailInput.SolutionCodeSendMail;
            if (emailInput.DisplayNameMailFrom != null) email.DisplayNameMailFrom = emailInput.DisplayNameMailFrom;
        }

        // Kênh SMS
        var sms = existingSMS ?? new Mst_ChannelSMS { OrgID = orgId, FlagActive = "1" };
        var smsInput = req?.Lst_Mst_ChannelSMS?.FirstOrDefault();
        if (smsInput != null)
        {
            if (smsInput.SubFormCodeContractSMS != null) sms.SubFormCodeContractSMS = smsInput.SubFormCodeContractSMS;
            if (smsInput.SubFormCodeSMSOTP != null) sms.SubFormCodeSMSOTP = smsInput.SubFormCodeSMSOTP;
            if (smsInput.SubFormCodeSMSAccessKey != null) sms.SubFormCodeSMSAccessKey = smsInput.SubFormCodeSMSAccessKey;
            if (smsInput.SMSBrandName != null) sms.SMSBrandName = smsInput.SMSBrandName;
        }

        // Kênh Zalo
        var zalo = existingZalo ?? new Mst_ChannelZalo { OrgID = orgId, FlagActive = "1" };
        var zaloInput = req?.Lst_Mst_ChannelZalo?.FirstOrDefault();
        if (zaloInput != null)
        {
            if (zaloInput.SubFormCodeContractPhone != null) zalo.SubFormCodeContractPhone = zaloInput.SubFormCodeContractPhone;
            if (zaloInput.SubFormCodeContractZaloUserId != null) zalo.SubFormCodeContractZaloUserId = zaloInput.SubFormCodeContractZaloUserId;
            if (zaloInput.SubFormCodeOTP != null) zalo.SubFormCodeOTP = zaloInput.SubFormCodeOTP;
            if (zaloInput.SubFormCodeAccessKeyZaloUserId != null) zalo.SubFormCodeAccessKeyZaloUserId = zaloInput.SubFormCodeAccessKeyZaloUserId;
            if (zaloInput.SubFormCodeAccessKeyPhone != null) zalo.SubFormCodeAccessKeyPhone = zaloInput.SubFormCodeAccessKeyPhone;
            if (zaloInput.AppID != null) zalo.AppID = zaloInput.AppID;
            if (zaloInput.AppSecret != null) zalo.AppSecret = zaloInput.AppSecret;
            if (zaloInput.ZaloOAID != null) zalo.ZaloOAID = zaloInput.ZaloOAID;
            if (zaloInput.RefreshToken != null) zalo.RefreshToken = zaloInput.RefreshToken;
            if (zaloInput.AccessToken != null) zalo.AccessToken = zaloInput.AccessToken;
        }

        // 5. Kiểm tra tính hợp lệ nghiệp vụ theo MasterData.cs:21337-21764
        // Kiểm tra ChannelTypeContract (MasterData.cs:21337-21343)
        var strChannelTypeContract = StdParam(channel.ChannelTypeContract);
        if (string.IsNullOrEmpty(strChannelTypeContract))
        {
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeContract);
        }
        var ctContract = await _db.Set<Mst_ChannelType>().FirstOrDefaultAsync(x => EF.Functions.Like((x.ChannelType ?? "").ToLower(), PortQuery.Equal(strChannelTypeContract), PortQuery.Esc));
        if (ctContract == null)
        {
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeContract);
        }
        if (ctContract.FlagActive != "1")
        {
            return PortResult.Fail("Mst_ChannelType_CheckDB_FlagActiveNotMatched",
                "Check.ChannelType", strChannelTypeContract,
                "Check.FlagActiveListToCheck", "1",
                "DB.FlagActive", ctContract.FlagActive);
        }

        // Kiểm tra ChannelTypeOTP (MasterData.cs:21348-21354)
        var strChannelTypeOTP = StdParam(channel.ChannelTypeOTP);
        if (string.IsNullOrEmpty(strChannelTypeOTP))
        {
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeOTP);
        }
        var ctOTP = await _db.Set<Mst_ChannelType>().FirstOrDefaultAsync(x => EF.Functions.Like((x.ChannelType ?? "").ToLower(), PortQuery.Equal(strChannelTypeOTP), PortQuery.Esc));
        if (ctOTP == null)
        {
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeOTP);
        }
        if (ctOTP.FlagActive != "1")
        {
            return PortResult.Fail("Mst_ChannelType_CheckDB_FlagActiveNotMatched",
                "Check.ChannelType", strChannelTypeOTP,
                "Check.FlagActiveListToCheck", "1",
                "DB.FlagActive", ctOTP.FlagActive);
        }

        // Kiểm tra mẫu gửi SubForm (MasterData.cs:13779-13838, 21450-21764)
        async Task<IActionResult?> CheckSubFormAsync(string? rawCode)
        {
            var code = StdParam(rawCode);
            if (string.IsNullOrEmpty(code)) return null;
            var sf = await _db.Set<Mst_SubmissionForm>().FirstOrDefaultAsync(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));
            if (sf == null)
            {
                return PortResult.Fail("Mst_SubmissionForm_CheckDB_SubFormCodeNotFound", "Check.SubFormCode", code);
            }
            if (sf.FlagActive != "1")
            {
                return PortResult.Fail("Mst_SubmissionForm_CheckDB_FlagActiveNotMatched",
                    "Check.SubFormCode", code,
                    "Check.FlagActiveListToCheck", "1",
                    "DB.FlagActive", sf.FlagActive);
            }
            return null;
        }

        // SubForm Email
        var err = await CheckSubFormAsync(email.SubFormCodeEmailContract);
        if (err != null) return err;
        err = await CheckSubFormAsync(email.SubFormCodeEmailOTP);
        if (err != null) return err;
        err = await CheckSubFormAsync(email.SubFormCodeEmailAccessKey);
        if (err != null) return err;

        // SubForm SMS
        err = await CheckSubFormAsync(sms.SubFormCodeContractSMS);
        if (err != null) return err;
        err = await CheckSubFormAsync(sms.SubFormCodeSMSOTP);
        if (err != null) return err;
        err = await CheckSubFormAsync(sms.SubFormCodeSMSAccessKey);
        if (err != null) return err;

        // SubForm Zalo
        err = await CheckSubFormAsync(zalo.SubFormCodeContractZaloUserId);
        if (err != null) return err;
        err = await CheckSubFormAsync(zalo.SubFormCodeContractPhone);
        if (err != null) return err;
        err = await CheckSubFormAsync(zalo.SubFormCodeOTP);
        if (err != null) return err;
        err = await CheckSubFormAsync(zalo.SubFormCodeAccessKeyZaloUserId);
        if (err != null) return err;
        err = await CheckSubFormAsync(zalo.SubFormCodeAccessKeyPhone);
        if (err != null) return err;

        // 6. Lưu vào DB trong transaction (MasterData.cs:22094-22330)
        var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var userCode = "port";

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Xoá bản ghi cũ của OrgID (MasterData.cs:22098-22139)
            var oldChannels = await _db.Set<Mst_Channel>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_Channel>().RemoveRange(oldChannels);

            var oldEmails = await _db.Set<Mst_ChannelEmail>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelEmail>().RemoveRange(oldEmails);

            var oldSMS = await _db.Set<Mst_ChannelSMS>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelSMS>().RemoveRange(oldSMS);

            var oldZalos = await _db.Set<Mst_ChannelZalo>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelZalo>().RemoveRange(oldZalos);

            await _db.SaveChangesAsync();

            // Thêm mới các bản ghi kênh đã cập nhật (MasterData.cs:22146-22275)
            channel.OrgID = orgId;
            channel.NetworkID ??= StdParam(req?.NetworkID) ?? "1001";
            channel.ChannelTypeContract = strChannelTypeContract;
            channel.ChannelTypeOTP = strChannelTypeOTP;
            channel.ChannelTypeAccessKey = "EMAIL";
            channel.FlagActive = "1";
            channel.LogLUDTimeUTC = dtimeSys;
            channel.LogLUBy = userCode;
            _db.Set<Mst_Channel>().Add(channel);

            email.OrgID = orgId;
            email.SubFormCodeEmailContract = StdParam(email.SubFormCodeEmailContract);
            email.SubFormCodeEmailOTP = StdParam(email.SubFormCodeEmailOTP);
            email.SubFormCodeEmailAccessKey = StdParam(email.SubFormCodeEmailAccessKey);
            email.FlagActive = "1";
            email.LogLUDTimeUTC = dtimeSys;
            email.LogLUBy = userCode;
            _db.Set<Mst_ChannelEmail>().Add(email);

            sms.OrgID = orgId;
            sms.SubFormCodeContractSMS = StdParam(sms.SubFormCodeContractSMS);
            sms.SubFormCodeSMSOTP = StdParam(sms.SubFormCodeSMSOTP);
            sms.SubFormCodeSMSAccessKey = StdParam(sms.SubFormCodeSMSAccessKey);
            sms.FlagActive = "1";
            sms.LogLUDTimeUTC = dtimeSys;
            sms.LogLUBy = userCode;
            _db.Set<Mst_ChannelSMS>().Add(sms);

            zalo.OrgID = orgId;
            zalo.SubFormCodeContractZaloUserId = StdParam(zalo.SubFormCodeContractZaloUserId);
            zalo.SubFormCodeContractPhone = StdParam(zalo.SubFormCodeContractPhone);
            zalo.SubFormCodeOTP = StdParam(zalo.SubFormCodeOTP);
            zalo.SubFormCodeAccessKeyZaloUserId = StdParam(zalo.SubFormCodeAccessKeyZaloUserId);
            zalo.SubFormCodeAccessKeyPhone = StdParam(zalo.SubFormCodeAccessKeyPhone);
            zalo.FlagActive = "1";
            zalo.LogLUDTimeUTC = dtimeSys;
            zalo.LogLUBy = userCode;
            _db.Set<Mst_ChannelZalo>().Add(zalo);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }

        // 7. Trả kết quả thành công khớp gate (Mst_ChannelController.cs:722-725)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Cập nhật cấu hình thành công!" },
            Title = "Cập nhật cấu hình thành công!",
            RedirectUrl = "/Mst_Channel"
        });
    }

    /// <summary>
    /// Lấy thông tin cấu hình kênh gửi (Email, SMS, Zalo) và danh sách mẫu gửi.
    /// Gate: Mst_ChannelController.cs:21-165 Index()
    /// BIZ: MasterData.cs:18384-18780 Mst_Channel_GetX; 14059-14200 Mst_SubmissionForm_GetX
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_IndexReq? req)
    {
        // 1. Lấy danh sách mẫu gửi (Mst_SubmissionForm) có FlagActive = '1'
        // Gate: Mst_ChannelController.cs:41 stringWhereClauseMst_SubmissionForm = "Mst_SubmissionForm.FlagActive = '1'"
        // BIZ: MasterData.cs:14169-14200 order by msf.LogLUDTimeUTC desc
        var lstSubmissionForm = await _db.Set<Mst_SubmissionForm>()
            .AsNoTracking()
            .Where(x => x.FlagActive == "1")
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ToListAsync();

        // 2. Lấy danh sách kênh cấu hình Mst_Channel
        // Gate: Mst_ChannelController.cs:104-127 Ft_WhereClause = "Mst_Channel.OrgID = " + orgID + " and Mst_Channel.FlagActive = '1'"
        // BIZ: MasterData.cs:18441-18506 order by mc.LogLUDTimeUTC asc
        var query = _db.Set<Mst_Channel>()
            .AsNoTracking()
            .Where(x => x.FlagActive == "1");

        var orgId = StdParam(req?.OrgID);
        if (!string.IsNullOrEmpty(orgId))
        {
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
        }

        var networkId = StdParam(req?.NetworkID);
        if (!string.IsNullOrEmpty(networkId))
        {
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
        }

        var lstChannel = await query.OrderBy(x => x.LogLUDTimeUTC).ToListAsync();

        // 3. Lấy thông tin kênh chi tiết tương ứng (Email, SMS, Zalo) theo OrgID
        // BIZ: MasterData.cs:18511-18570
        var orgIds = lstChannel.Select(x => x.OrgID).Distinct().ToList();

        var lstEmail = await _db.Set<Mst_ChannelEmail>()
            .AsNoTracking()
            .Where(x => orgIds.Contains(x.OrgID) && x.FlagActive == "1")
            .OrderBy(x => x.OrgID)
            .ToListAsync();

        var lstSMS = await _db.Set<Mst_ChannelSMS>()
            .AsNoTracking()
            .Where(x => orgIds.Contains(x.OrgID) && x.FlagActive == "1")
            .OrderBy(x => x.OrgID)
            .ToListAsync();

        var lstZalo = await _db.Set<Mst_ChannelZalo>()
            .AsNoTracking()
            .Where(x => orgIds.Contains(x.OrgID) && x.FlagActive == "1")
            .OrderBy(x => x.OrgID)
            .ToListAsync();

        // 4. Kết nối Zalo (Gate: Mst_ChannelController.cs:75-104, 129-147)
        // ZaloGateService gọi dịch vụ ngoài (ZaloGate_API) -> GAP (§1, §3), bỏ qua và trả flagZaloConnect = "0"
        var flagZaloConnect = "0";

        // 5. Trả kết quả khớp Gate (Mst_ChannelController.cs:141, 66, 154)
        return Ok(new
        {
            Success = true,
            Data = new Mst_Channel_IndexData
            {
                Lst_Mst_Channel = lstChannel,
                Lst_Mst_ChannelZalo = lstZalo,
                Lst_Mst_ChannelSMS = lstSMS,
                Lst_Mst_ChannelEmail = lstEmail,
                RT_Mst_SubmissionForm = new RT_Mst_SubmissionForm
                {
                    Lst_Mst_SubmissionForm = lstSubmissionForm
                },
                Lst_Mst_SubmissionForm = lstSubmissionForm,
                FlagZaloConnect = flagZaloConnect
            }
        });
    }

    /// <summary>
    /// Ngắt kết nối kênh Zalo (xoá trắng token và thông tin Zalo OA).
    /// Gate: Mst_ChannelController.cs:479-567 ZaloDisconnect()
    /// API: MstChannelTypeController.cs:91-140 (WA_Mst_Channel_Get), 376-444 (WA_Mst_Channel_Save)
    /// BIZ: MasterData.cs:18814-18935 (WAS_Mst_Channel_Get), 18938-19034 (WAS_Mst_Channel_Save)
    /// </summary>
    [HttpPost("zalodisconnect")]
    public async Task<IActionResult> ZaloDisconnect([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_ZaloDisconnectReq? req)
    {
        // 1. Kiểm tra OrgID bắt buộc (MasterData.cs:21295-21306)
        var orgId = StdParam(req?.OrgID);
        if (string.IsNullOrEmpty(orgId))
        {
            return PortResult.Fail("Mst_Channel_SaveX_InvalidOrgID", "Check.strOrgID", req?.OrgID);
        }

        // 2. Lấy kênh cấu hình Mst_ChannelZalo hiện có của OrgID
        // Gate: Mst_ChannelController.cs:496-520 Ft_WhereClause = "Mst_Channel.OrgID = " + orgID + " and Mst_Channel.FlagActive = '1'"
        var existingZalo = await _db.Set<Mst_ChannelZalo>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");

        // 3. Nếu có cấu hình Zalo, thực hiện ngắt kết nối bằng cách xoá trắng các token và OAID
        // Gate: Mst_ChannelController.cs:520-551
        if (existingZalo != null)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var userCode = "port";

                existingZalo.AccessToken = "";
                existingZalo.RefreshToken = "";
                existingZalo.ZaloOAID = "";
                existingZalo.AppID = "";
                existingZalo.LogLUDTimeUTC = dtimeSys;
                existingZalo.LogLUBy = userCode;

                var existingChannel = await _db.Set<Mst_Channel>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");
                if (existingChannel != null)
                {
                    existingChannel.LogLUDTimeUTC = dtimeSys;
                    existingChannel.LogLUBy = userCode;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // 4. Trả kết quả thành công khớp Gate (Mst_ChannelController.cs:553-559)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Ngắt kết nối Zalo thành công!" },
            Title = "Ngắt kết nối Zalo thành công!",
            RedirectUrl = "/Mst_Channel"
        });
    }

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }
}

public class Mst_Channel_SaveReq
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? Model { get; set; }
    public Mst_Channel? Mst_Channel { get; set; }
    public List<Mst_ChannelEmail>? Lst_Mst_ChannelEmail { get; set; }
    public List<Mst_ChannelSMS>? Lst_Mst_ChannelSMS { get; set; }
    public List<Mst_ChannelZalo>? Lst_Mst_ChannelZalo { get; set; }
}

public class Mst_Channel_IndexReq
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

public class Mst_Channel_ZaloDisconnectReq
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

public class RT_Mst_Channel
{
    public List<Mst_Channel> Lst_Mst_Channel { get; set; } = new();
    public List<Mst_ChannelZalo> Lst_Mst_ChannelZalo { get; set; } = new();
    public List<Mst_ChannelSMS> Lst_Mst_ChannelSMS { get; set; } = new();
    public List<Mst_ChannelEmail> Lst_Mst_ChannelEmail { get; set; } = new();
}

public class RT_Mst_SubmissionForm
{
    public List<Mst_SubmissionForm> Lst_Mst_SubmissionForm { get; set; } = new();
}

public class Mst_Channel_IndexData : RT_Mst_Channel
{
    public RT_Mst_SubmissionForm? RT_Mst_SubmissionForm { get; set; }
    public List<Mst_SubmissionForm> Lst_Mst_SubmissionForm { get; set; } = new();
    public string FlagZaloConnect { get; set; } = "0";
}

