using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ChannelController.cs — Mst_Channel
// Gate: ...\Website\Controllers\Mst_ChannelController.cs:169-247 (Save1), 572-733 (Save), 21-165 (Index), 250-309 (RedirectToMsgSystem), 479-567 (ZaloDisconnect)
// API:  ...\Biz.Web.DEV\Controllers\MstChannelTypeController.cs:376-444
// BIZ:  ...\Biz\MasterData.cs:18938-19034, 19053-19169, 21232-22492
[ApiController]
[Route("api/mst_channel")]
public class Mst_ChannelControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    // Có 2 constructor public nên phải chỉ định cái dùng cho DI, nếu không ASP.NET Core
    // không chọn được và mọi request trả 500.
    [ActivatorUtilitiesConstructor]
    public Mst_ChannelControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // Constructor tiện dụng cho unit test (các action cũ không dùng _config).
    public Mst_ChannelControllerPort(AppDbContext db)
    {
        _db = db;
        _config = null;
    }

    /// <summary>Gate: Mst_ChannelController.cs:572-733 Save(string model)</summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_SaveReq? req)
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

        // Kiểm tra ChannelTypeContract / ChannelTypeOTP (MasterData.cs:21337-21354)
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

        // Kiểm tra mẫu gửi SubForm (MasterData.cs:13779-13838, 21450-21764)
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
            var oldChannels = await _db.Set<Mst_Channel>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_Channel>().RemoveRange(oldChannels);
            var oldEmails = await _db.Set<Mst_ChannelEmail>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelEmail>().RemoveRange(oldEmails);
            var oldSMS = await _db.Set<Mst_ChannelSMS>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelSMS>().RemoveRange(oldSMS);
            var oldZalos = await _db.Set<Mst_ChannelZalo>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelZalo>().RemoveRange(oldZalos);
            await _db.SaveChangesAsync();

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

        return Ok(new
        {
            Success = true,
            Messages = new[] { "Cập nhật cấu hình thành công!" },
            Title = "Cập nhật cấu hình thành công!",
            RedirectUrl = "/Mst_Channel"
        });
    }

    /// <summary>Gate: Mst_ChannelController.cs:169-247 Save1(string model)</summary>
    [HttpPost("save1")]
    public async Task<IActionResult> Save1([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_Save1Req? req)
    {
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Channel_Save1Req>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        var channel = req?.Mst_Channel ?? new Mst_Channel();
        channel.OrgID = orgId;
        channel.NetworkID = StdParam(req?.NetworkID) ?? "1001";

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

        var emailInput = req?.Lst_Mst_ChannelEmail?.FirstOrDefault();
        if (emailInput != null)
        {
            var err = await CheckSubFormAsync(emailInput.SubFormCodeEmailContract);
            if (err != null) return err;
            err = await CheckSubFormAsync(emailInput.SubFormCodeEmailOTP);
            if (err != null) return err;
            err = await CheckSubFormAsync(emailInput.SubFormCodeEmailAccessKey);
            if (err != null) return err;
        }

        var smsInput = req?.Lst_Mst_ChannelSMS?.FirstOrDefault();
        if (smsInput != null)
        {
            var err = await CheckSubFormAsync(smsInput.SubFormCodeContractSMS);
            if (err != null) return err;
            err = await CheckSubFormAsync(smsInput.SubFormCodeSMSOTP);
            if (err != null) return err;
            err = await CheckSubFormAsync(smsInput.SubFormCodeSMSAccessKey);
            if (err != null) return err;
        }

        var zaloInput = req?.Lst_Mst_ChannelZalo?.FirstOrDefault();
        if (zaloInput != null)
        {
            var err = await CheckSubFormAsync(zaloInput.SubFormCodeContractZaloUserId);
            if (err != null) return err;
            err = await CheckSubFormAsync(zaloInput.SubFormCodeContractPhone);
            if (err != null) return err;
            err = await CheckSubFormAsync(zaloInput.SubFormCodeOTP);
            if (err != null) return err;
            err = await CheckSubFormAsync(zaloInput.SubFormCodeAccessKeyZaloUserId);
            if (err != null) return err;
            err = await CheckSubFormAsync(zaloInput.SubFormCodeAccessKeyPhone);
            if (err != null) return err;
        }

        var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var userCode = "port";

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var oldChannels = await _db.Set<Mst_Channel>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_Channel>().RemoveRange(oldChannels);
            var oldEmails = await _db.Set<Mst_ChannelEmail>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelEmail>().RemoveRange(oldEmails);
            var oldSMS = await _db.Set<Mst_ChannelSMS>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelSMS>().RemoveRange(oldSMS);
            var oldZalos = await _db.Set<Mst_ChannelZalo>().Where(x => x.OrgID == orgId).ToListAsync();
            _db.Set<Mst_ChannelZalo>().RemoveRange(oldZalos);
            await _db.SaveChangesAsync();

            channel.OrgID = orgId;
            channel.NetworkID ??= StdParam(req?.NetworkID) ?? "1001";
            channel.ChannelTypeContract = strChannelTypeContract;
            channel.ChannelTypeOTP = strChannelTypeOTP;
            channel.ChannelTypeAccessKey = "EMAIL";
            channel.FlagActive = "1";
            channel.LogLUDTimeUTC = dtimeSys;
            channel.LogLUBy = userCode;
            _db.Set<Mst_Channel>().Add(channel);

            if (emailInput != null)
            {
                emailInput.OrgID = orgId;
                emailInput.SubFormCodeEmailContract = StdParam(emailInput.SubFormCodeEmailContract);
                emailInput.SubFormCodeEmailOTP = StdParam(emailInput.SubFormCodeEmailOTP);
                emailInput.SubFormCodeEmailAccessKey = StdParam(emailInput.SubFormCodeEmailAccessKey);
                emailInput.FlagActive = "1";
                emailInput.LogLUDTimeUTC = dtimeSys;
                emailInput.LogLUBy = userCode;
                _db.Set<Mst_ChannelEmail>().Add(emailInput);
            }

            if (smsInput != null)
            {
                smsInput.OrgID = orgId;
                smsInput.SubFormCodeContractSMS = StdParam(smsInput.SubFormCodeContractSMS);
                smsInput.SubFormCodeSMSOTP = StdParam(smsInput.SubFormCodeSMSOTP);
                smsInput.SubFormCodeSMSAccessKey = StdParam(smsInput.SubFormCodeSMSAccessKey);
                smsInput.FlagActive = "1";
                smsInput.LogLUDTimeUTC = dtimeSys;
                smsInput.LogLUBy = userCode;
                _db.Set<Mst_ChannelSMS>().Add(smsInput);
            }

            if (zaloInput != null)
            {
                zaloInput.OrgID = orgId;
                zaloInput.SubFormCodeContractZaloUserId = StdParam(zaloInput.SubFormCodeContractZaloUserId);
                zaloInput.SubFormCodeContractPhone = StdParam(zaloInput.SubFormCodeContractPhone);
                zaloInput.SubFormCodeOTP = StdParam(zaloInput.SubFormCodeOTP);
                zaloInput.SubFormCodeAccessKeyZaloUserId = StdParam(zaloInput.SubFormCodeAccessKeyZaloUserId);
                zaloInput.SubFormCodeAccessKeyPhone = StdParam(zaloInput.SubFormCodeAccessKeyPhone);
                zaloInput.FlagActive = "1";
                zaloInput.LogLUDTimeUTC = dtimeSys;
                zaloInput.LogLUBy = userCode;
                _db.Set<Mst_ChannelZalo>().Add(zaloInput);
            }

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
            Messages = new[] { "Cập nhật cấu hình kênh thành công!" },
            Title = "Cập nhật cấu hình kênh thành công!",
            RedirectUrl = "/Mst_Channel"
        });
    }

    /// <summary>Gate: Mst_ChannelController.cs:21-165 Index()</summary>
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

        // ZaloGateService gọi dịch vụ ngoài (ZaloGate_API) -> GAP (§1, §3); trả "0" như khi chưa kết nối.
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

    /// <summary>Gate: Mst_ChannelController.cs:479-567 ZaloDisconnect()</summary>
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

    /// <summary>
    /// Sinh URL chuyển hướng sang cổng ZaloGate để bắt đầu luồng kết nối Zalo OA.
    /// Gate: Mst_ChannelController.cs:250-309 RedirectToMsgSystem() — chỉ ghép chuỗi, không truy vấn DB,
    /// không gọi dịch vụ ngoài. accessToken/networkID ở nguồn lấy từ phiên; đích không có phiên nên nhận
    /// qua body TUỲ CHỌN (luật A.14).
    /// </summary>
    [HttpPost("redirecttomsgsystem")]
    public IActionResult RedirectToMsgSystem([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Channel_RedirectToMsgSystemReq? req)
    {
        // Gate :253-258
        var networkID = string.IsNullOrWhiteSpace(req?.NetworkID) ? null : req!.NetworkID!.Trim();
        var accessToken = req?.AccessToken ?? "";

        var resultEntry = new Mst_ChannelJsonResultEntry { Success = false };
        try
        {
            // Gate :268 — "{ZaloGate_API}/{networkID}/"
            var zaloGateApi = _config?["ZaloGate_API"] ?? "";
            var zaloAPI = string.Format("{0}/{1}/", zaloGateApi, networkID ?? "");

            // Gate :270 — "{base}/{Url.Action}"; Url.Action có "/" đầu nên kết quả có "//" — giữ nguyên (parity).
            var baseAddress = GetRequestBaseAddress();
            var returnUrl = string.Format("{0}/{1}", baseAddress, "/Mst_Channel/Index");

            // Gate :271-275 — access_code = CUtils.Base64_Encode(accessToken) (idn.Utils\Utils.cs:153-156)
            var accessCode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(accessToken));
            var paramListObject = new List<KeyValuePair<string, string>>
            {
                new("access_code", accessCode),
                new("returnUrl", returnUrl)
            };

            // Gate :277 — "{zaloAPI}/{zalo}/{connect}"
            var urlConnect = new System.Text.StringBuilder(string.Format("{0}/{1}/{2}", zaloAPI, "zalo", "connect"));

            // Gate :279-284 — RouteValueDictionary -> "k=v" nối "&"
            if (paramListObject.Count > 0)
            {
                var paramString = string.Join("&", paramListObject.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
                urlConnect.AppendFormat("?{0}", paramString);
            }

            // Gate :286, :297-300
            resultEntry.Success = true;
            resultEntry.RedirectUrl = urlConnect.ToString();
            return Ok(resultEntry);
        }
        catch (Exception ex)
        {
            // Gate :302-306. Mã lỗi theo ĐÚNG tên action (luật A.2).
            return PortResult.Fail("Mst_Channel_RedirectToMsgSystem", "Exception", ex.Message);
        }
    }

    /// <summary>BaseController.cs:787-794 — scheme + "://" + host (+ ":port" nếu khác mặc định).</summary>
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

/// <summary>
/// Body của RedirectToMsgSystem — tương ứng networkID/accessToken mà nguồn đọc từ phiên (luật A.14).
/// </summary>
public class Mst_Channel_RedirectToMsgSystemReq
{
    public string? NetworkID { get; set; }
    public string? AccessToken { get; set; }
}

/// <summary>Khuôn JsonResultEntry (Success / RedirectUrl) mà RedirectToMsgSystem trả về.</summary>
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
