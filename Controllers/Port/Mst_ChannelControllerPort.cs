using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ChannelController.cs — Mst_Channel
[ApiController]
[Route("api/mst_channel")]
public class Mst_ChannelControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    [ActivatorUtilitiesConstructor]
    public Mst_ChannelControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public Mst_ChannelControllerPort(AppDbContext db)
    {
        _db = db;
        _config = null;
    }

    /// <summary>Gate: Mst_ChannelController.cs:169-247 Save1.</summary>
    [HttpPost("save1")]
    public Task<IActionResult> Save1([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_Save1Req? req)
        => SaveCore(req, "Cập nhật cấu hình kênh thành công!");

    /// <summary>Gate: Mst_ChannelController.cs:572-733 Save(string model).</summary>
    [HttpPost("save")]
    public Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_SaveReq? req)
        => SaveCore(req, "Cập nhật cấu hình thành công!");

    private async Task<IActionResult> SaveCore(Mst_Channel_SaveReq? req, string successMessage)
    {
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
            catch { }
        }

        var orgId = StdParam(req?.OrgID);
        if (string.IsNullOrEmpty(orgId))
            return PortResult.Fail("Mst_Channel_SaveX_InvalidOrgID", "Check.strOrgID", req?.OrgID);

        var existingChannel = await _db.Set<Mst_Channel>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");
        var existingEmail = await _db.Set<Mst_ChannelEmail>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");
        var existingSMS = await _db.Set<Mst_ChannelSMS>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");
        var existingZalo = await _db.Set<Mst_ChannelZalo>().FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");

        var channel = existingChannel ?? new Mst_Channel { OrgID = orgId, NetworkID = StdParam(req?.NetworkID) ?? "1001", FlagActive = "1" };
        var channelInput = req?.Mst_Channel;
        if (channelInput != null)
        {
            if (channelInput.ChannelTypeContract != null) channel.ChannelTypeContract = channelInput.ChannelTypeContract;
            if (channelInput.ChannelTypeOTP != null) channel.ChannelTypeOTP = channelInput.ChannelTypeOTP;
        }

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

        var sms = existingSMS ?? new Mst_ChannelSMS { OrgID = orgId, FlagActive = "1" };
        var smsInput = req?.Lst_Mst_ChannelSMS?.FirstOrDefault();
        if (smsInput != null)
        {
            if (smsInput.SubFormCodeContractSMS != null) sms.SubFormCodeContractSMS = smsInput.SubFormCodeContractSMS;
            if (smsInput.SubFormCodeSMSOTP != null) sms.SubFormCodeSMSOTP = smsInput.SubFormCodeSMSOTP;
            if (smsInput.SubFormCodeSMSAccessKey != null) sms.SubFormCodeSMSAccessKey = smsInput.SubFormCodeSMSAccessKey;
            if (smsInput.SMSBrandName != null) sms.SMSBrandName = smsInput.SMSBrandName;
        }

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

        var strChannelTypeContract = StdParam(channel.ChannelTypeContract);
        if (string.IsNullOrEmpty(strChannelTypeContract))
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeContract);
        var ctContract = await _db.Set<Mst_ChannelType>().FirstOrDefaultAsync(x => EF.Functions.Like((x.ChannelType ?? "").ToLower(), PortQuery.Equal(strChannelTypeContract), PortQuery.Esc));
        if (ctContract == null)
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeContract);
        if (ctContract.FlagActive != "1")
            return PortResult.Fail("Mst_ChannelType_CheckDB_FlagActiveNotMatched", "Check.ChannelType", strChannelTypeContract, "Check.FlagActiveListToCheck", "1", "DB.FlagActive", ctContract.FlagActive);

        var strChannelTypeOTP = StdParam(channel.ChannelTypeOTP);
        if (string.IsNullOrEmpty(strChannelTypeOTP))
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeOTP);
        var ctOTP = await _db.Set<Mst_ChannelType>().FirstOrDefaultAsync(x => EF.Functions.Like((x.ChannelType ?? "").ToLower(), PortQuery.Equal(strChannelTypeOTP), PortQuery.Esc));
        if (ctOTP == null)
            return PortResult.Fail("Mst_ChannelType_CheckDB_ChannelTypeNotFound", "Check.ChannelType", strChannelTypeOTP);
        if (ctOTP.FlagActive != "1")
            return PortResult.Fail("Mst_ChannelType_CheckDB_FlagActiveNotMatched", "Check.ChannelType", strChannelTypeOTP, "Check.FlagActiveListToCheck", "1", "DB.FlagActive", ctOTP.FlagActive);

        async Task<IActionResult?> CheckSubFormAsync(string? rawCode)
        {
            var code = StdParam(rawCode);
            if (string.IsNullOrEmpty(code)) return null;
            var sf = await _db.Set<Mst_SubmissionForm>().FirstOrDefaultAsync(x => EF.Functions.Like((x.SubFormCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));
            if (sf == null)
                return PortResult.Fail("Mst_SubmissionForm_CheckDB_SubFormCodeNotFound", "Check.SubFormCode", code);
            if (sf.FlagActive != "1")
                return PortResult.Fail("Mst_SubmissionForm_CheckDB_FlagActiveNotMatched", "Check.SubFormCode", code, "Check.FlagActiveListToCheck", "1", "DB.FlagActive", sf.FlagActive);
            return null;
        }

        var err = await CheckSubFormAsync(email.SubFormCodeEmailContract);
        if (err != null) return err;
        err = await CheckSubFormAsync(email.SubFormCodeEmailOTP);
        if (err != null) return err;
        err = await CheckSubFormAsync(email.SubFormCodeEmailAccessKey);
        if (err != null) return err;
        err = await CheckSubFormAsync(sms.SubFormCodeContractSMS);
        if (err != null) return err;
        err = await CheckSubFormAsync(sms.SubFormCodeSMSOTP);
        if (err != null) return err;
        err = await CheckSubFormAsync(sms.SubFormCodeSMSAccessKey);
        if (err != null) return err;
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

        var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var userCode = "port";

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            channel.LogLUDTimeUTC = dtimeSys;
            channel.LogLUBy = userCode;
            channel.FlagActive = "1";
            if (existingChannel == null) _db.Set<Mst_Channel>().Add(channel);

            email.LogLUDTimeUTC = dtimeSys;
            email.LogLUBy = userCode;
            email.FlagActive = "1";
            if (existingEmail == null) _db.Set<Mst_ChannelEmail>().Add(email);

            sms.LogLUDTimeUTC = dtimeSys;
            sms.LogLUBy = userCode;
            sms.FlagActive = "1";
            if (existingSMS == null) _db.Set<Mst_ChannelSMS>().Add(sms);

            zalo.LogLUDTimeUTC = dtimeSys;
            zalo.LogLUBy = userCode;
            zalo.FlagActive = "1";
            if (existingZalo == null) _db.Set<Mst_ChannelZalo>().Add(zalo);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }

        return Ok(new
        {
            Success = true,
            Messages = new[] { successMessage },
            Title = successMessage,
            RedirectUrl = "/Mst_Channel"
        });
    }

    /// <summary>Gate: Mst_ChannelController.cs:21-165 Index().</summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_IndexReq? req)
    {
        var lstSubmissionForm = await _db.Set<Mst_SubmissionForm>()
            .AsNoTracking()
            .Where(x => x.FlagActive == "1")
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ToListAsync();

        var query = _db.Set<Mst_Channel>()
            .AsNoTracking()
            .Where(x => x.FlagActive == "1");

        var orgId = StdParam(req?.OrgID);
        if (!string.IsNullOrEmpty(orgId))
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));

        var networkId = StdParam(req?.NetworkID);
        if (!string.IsNullOrEmpty(networkId))
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));

        var lstChannel = await query.OrderBy(x => x.LogLUDTimeUTC).ToListAsync();

        var orgIds = lstChannel.Select(x => x.OrgID).Distinct().ToList();

        var lstEmail = await _db.Set<Mst_ChannelEmail>().AsNoTracking()
            .Where(x => orgIds.Contains(x.OrgID) && x.FlagActive == "1").OrderBy(x => x.OrgID).ToListAsync();
        var lstSMS = await _db.Set<Mst_ChannelSMS>().AsNoTracking()
            .Where(x => orgIds.Contains(x.OrgID) && x.FlagActive == "1").OrderBy(x => x.OrgID).ToListAsync();
        var lstZalo = await _db.Set<Mst_ChannelZalo>().AsNoTracking()
            .Where(x => orgIds.Contains(x.OrgID) && x.FlagActive == "1").OrderBy(x => x.OrgID).ToListAsync();

        var flagZaloConnect = "0";

        return Ok(new
        {
            Success = true,
            Data = new Mst_Channel_IndexData
            {
                Lst_Mst_Channel = lstChannel,
                Lst_Mst_ChannelZalo = lstZalo,
                Lst_Mst_ChannelSMS = lstSMS,
                Lst_Mst_ChannelEmail = lstEmail,
                RT_Mst_SubmissionForm = new RT_Mst_SubmissionForm { Lst_Mst_SubmissionForm = lstSubmissionForm },
                Lst_Mst_SubmissionForm = lstSubmissionForm,
                FlagZaloConnect = flagZaloConnect
            }
        });
    }

    /// <summary>Gate: Mst_ChannelController.cs:479-567 ZaloDisconnect().</summary>
    [HttpPost("zalodisconnect")]
    public async Task<IActionResult> ZaloDisconnect([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_ZaloDisconnectReq? req)
    {
        var orgId = StdParam(req?.OrgID);
        if (string.IsNullOrEmpty(orgId))
            return PortResult.Fail("Mst_Channel_SaveX_InvalidOrgID", "Check.strOrgID", req?.OrgID);

        var existingZalo = await _db.Set<Mst_ChannelZalo>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) && x.FlagActive == "1");

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

        return Ok(new
        {
            Success = true,
            Messages = new[] { "Ngắt kết nối Zalo thành công!" },
            Title = "Ngắt kết nối Zalo thành công!",
            RedirectUrl = "/Mst_Channel"
        });
    }

    /// <summary>Gate: Mst_ChannelController.cs:250-309 RedirectToMsgSystem().</summary>
    [HttpPost("redirecttomsgsystem")]
    public IActionResult RedirectToMsgSystem([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_RedirectToMsgSystemReq? req)
    {
        var networkID = string.IsNullOrWhiteSpace(req?.NetworkID) ? null : req!.NetworkID!.Trim();
        var accessToken = req?.AccessToken ?? "";

        var resultEntry = new Mst_ChannelJsonResultEntry { Success = false };
        try
        {
            var zaloGateApi = _config?["ZaloGate_API"] ?? "";
            var zaloAPI = string.Format("{0}/{1}/", zaloGateApi, networkID ?? "");

            var baseAddress = GetRequestBaseAddress();
            var returnUrl = string.Format("{0}/{1}", baseAddress, "/Mst_Channel/Index");

            var accessCode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(accessToken));
            var paramListObject = new List<KeyValuePair<string, string>>
            {
                new("access_code", accessCode),
                new("returnUrl", returnUrl)
            };

            var urlConnect = new System.Text.StringBuilder(string.Format("{0}/{1}/{2}", zaloAPI, "zalo", "connect"));

            if (paramListObject.Count > 0)
            {
                var paramString = string.Join("&", paramListObject.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
                urlConnect.AppendFormat("?{0}", paramString);
            }

            resultEntry.Success = true;
            resultEntry.RedirectUrl = urlConnect.ToString();
            return Ok(resultEntry);
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Channel_RedirectToMsgSystem", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Sinh URL redirect_uri sang cổng OAuth Zalo OA. Gate: Mst_ChannelController.cs:434-475 — KHÔNG truy vấn DB, KHÔNG gọi
    /// WA_Mst_Channel_*; chỉ đọc 2 appSettings (ClientDomain, Zalo_App_Id) + ghép chuỗi URL.
    /// </summary>
    [HttpPost("zaloconnect")]
    public IActionResult ZaloConnect([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_ZaloConnectReq? req)
    {
        try
        {
            var separator = new[] { "https://", "http://" };
            var domain = _config?["ClientDomain"];
            var parts = domain!.Split(separator, StringSplitOptions.None);
            var domainNoProtocol = parts[1];

            var appId = req?.ZaloAppId;
            if (string.IsNullOrEmpty(appId))
                appId = _config?["Zalo_App_Id"];

            var url = string.Format(
                "https://oauth.zaloapp.com/v4/oa/permission?app_id={0}&redirect_uri=https%3A%2F%2F{1}%2Fzalo-login-callback",
                appId, domainNoProtocol);

            return Ok(new
            {
                Success = true,
                Url = url,
                Title = new List<string>()
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Channel_ZaloConnect", "Exception", ex.Message);
        }
    }

    private string GetRequestBaseAddress()
    {
        return Request.Scheme + Uri.SchemeDelimiter + Request.Host.Value;
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

public class Mst_Channel_Save1Req : Mst_Channel_SaveReq { }

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

public class Mst_Channel_RedirectToMsgSystemReq
{
    public string? NetworkID { get; set; }
    public string? AccessToken { get; set; }
}

public class Mst_Channel_ZaloConnectReq
{
    public string? ZaloAppId { get; set; }
}

public class Mst_ChannelJsonResultEntry
{
    public bool Success { get; set; }
    public string? RedirectUrl { get; set; }
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
