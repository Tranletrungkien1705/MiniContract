using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 ESignController.cs — Contract_ContractParty_Cancel & ShowModalPopupPartySign_QRCode & ReturnResultPage
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ESignController.cs:2058-2096 (Contract_ContractParty_Cancel)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ESignController.cs:2100-2236 (ShowModalPopupPartySign_QRCode)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ESignController.cs:2239-2303 (ReturnResultPage)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\ContractContractPartyController.cs:20-88 (WA_Contract_ContractParty_Cancel)
//       ClientService: Contract_ContractVerifyOtpService.cs (WA_Contract_ContractVerifyOtp_Save)
//       ClientService: Contract_ContractService.cs (WA_Contract_Contract_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\zTemp.cs:3056-3129 (WAS_Contract_ContractParty_Cancel)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\zTemp.cs:2675-2862 (Contract_ContractParty_Cancel)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\zTemp.cs:1911-2674 (Contract_ContractParty_CancelX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9313-9378 (Contract_Contract_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:39692-39762 (Contract_ContractParty_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:40201-40360 (Contract_Contract_HistActionX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:42095-42310 (Contract_ContractVerifyOtp_SaveX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:10526-10787 (WAS_Contract_Contract_Get),
//       9380-9575 (Contract_Contract_Get), 9747-10524 (Contract_Contract_GetX)
[ApiController]
[Route("api/esign")]
public class ESignControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public ESignControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Hủy hợp đồng từ phía đối tác ký (Contract_ContractParty_Cancel).
    /// Gate: ESignController.cs:2058-2096
    /// API:  ContractContractPartyController.cs:20-88 WA_Contract_ContractParty_Cancel
    /// BIZ:  zTemp.cs:1911-2674 Contract_ContractParty_CancelX
    /// </summary>
    [HttpPost("contract_contractparty_cancel")]
    public async Task<IActionResult> Contract_ContractParty_Cancel(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ESign_Contract_ContractParty_CancelReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc null (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Contract_ContractParty_Cancel", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' và 'user' như gate MVC (ESignController.cs:2058, 2061-2062)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                using var doc = JsonDocument.Parse(req.Model);
                if (doc.RootElement.TryGetProperty("ContractCode", out var ccProp))
                {
                    req.ContractCode ??= ccProp.GetString();
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (!string.IsNullOrWhiteSpace(req.User))
        {
            try
            {
                using var doc = JsonDocument.Parse(req.User);
                if (doc.RootElement.TryGetProperty("MST", out var mstProp))
                {
                    req.MST ??= mstProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("OrgID", out var orgProp))
                {
                    req.OrgID ??= orgProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("NetworkID", out var netProp))
                {
                    req.NetworkID ??= netProp.GetString();
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContractCode) &&
            string.IsNullOrWhiteSpace(req.MST) &&
            string.IsNullOrWhiteSpace(req.Model) &&
            string.IsNullOrWhiteSpace(req.User))
        {
            return PortResult.Fail("Contract_ContractParty_Cancel", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào bằng StdParam theo nguồn zTemp.cs:1954-1955
        var strContractCode = StdParam(req.ContractCode);
        var strMST = StdParam(req.MST);
        var strWAUserCode = StdParam(req.WAUserCode ?? req.Email ?? Request?.Headers["WAUserCode"].FirstOrDefault() ?? Request?.Headers["Email"].FirstOrDefault() ?? "port") ?? "port";

        // 3. Kiểm tra DB: Contract_Contract_CheckDB (zTemp.cs:1966-1972, QContract.cs:9313-9378)
        var contract = await _db.Set<Contract_Contract>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc));

        if (contract == null)
        {
            // QContract.cs:9345: TError.ErridQContract.Contract_Contract_CheckDB_OrganNotFound
            return PortResult.Fail("Contract_Contract_CheckDB_OrganNotFound", "Check.ContractCode", strContractCode);
        }

        // QContract.cs:9365-9376: kiểm tra ContractStatus thuộc danh sách "ONPROCESS"
        if (!string.Equals(contract.ContractStatus, "ONPROCESS", StringComparison.OrdinalIgnoreCase))
        {
            return PortResult.Fail("Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched",
                "Check.ContractCode", strContractCode,
                "Check.strContractListToCheck", "ONPROCESS",
                "DB.ContractStatus", contract.ContractStatus);
        }

        // 4. Kiểm tra DB: Contract_ContractParty_CheckDB (zTemp.cs:1974-1981, QContract.cs:39692-39762)
        var party = await _db.Set<Contract_ContractParty>()
            .FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc) &&
                EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST ?? ""), PortQuery.Esc));

        if (party == null)
        {
            // QContract.cs:39728: TError.ErridQContract.Contract_ContractParty_CheckDB_OrganNotFound
            return PortResult.Fail("Contract_ContractParty_CheckDB_OrganNotFound",
                "Check.ContractCode", strContractCode,
                "Check.MST", strMST);
        }

        var strCustomerNameParty = party.CustomerName ?? "";

        // 5. Mở Transaction cập nhật dữ liệu (zTemp.cs:2719, 2817)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // Cập nhật Contract_Contract (zTemp.cs:2038-2060, 2102-2125)
            contract.ContractStatus = "CANCELED";
            contract.CancelDTimeUTC = dtimeSys;
            contract.LogLUDTimeUTC = dtimeSys;
            contract.LogLUBy = strWAUserCode;

            // Cập nhật Contract_ContractParty (zTemp.cs:2067-2095, 2127-2163)
            party.ContractPartyStatus = "CANCELED";
            party.CancelDTimeUTC = dtimeSys;
            party.CancelBy = strWAUserCode;
            party.LogLUDTimeUTC = dtimeSys;
            party.LogLUBy = strWAUserCode;

            // Ghi lịch sử Contract_Contract_HistActionX (zTemp.cs:2210-2226, QContract.cs:40201-40360)
            var lastHist = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode ?? ""), PortQuery.Esc))
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

            int nextVersion = (lastHist?.Version ?? 0) + 1;

            var hist = new Contract_Contract_HistAction
            {
                ContractCode = strContractCode ?? "",
                Version = nextVersion,
                UserCode = strWAUserCode,
                UserName = strCustomerNameParty,
                Description = "Hủy hợp đồng",
                LogLUDTimeUTC = dtimeSys,
                LogLUBy = strWAUserCode,
                Remark = "",
                FunctionName = "Contract_ContractParty_CancelX",
                FunctionActionType = "PartyCancel",
                FunctionRemark = ""
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // GAP: Bỏ qua dịch vụ ngoài bắn Notification iNOS / Firebase push (zTemp.cs:2568, 2637)

            // 6. Trả kết quả thành công khớp gate MVC ESignController.cs:2084-2088
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Hủy hợp đồng thành công" },
                RedirectUrl = ""
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Contract_ContractParty_Cancel", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hiển thị popup và sinh mã OTP gửi qua email cho khách hàng ký số (ShowModalPopupPartySign_QRCode).
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ESignController.cs:2100-2236
    /// API:  ClientService gọi WA_Contract_ContractVerifyOtp_Save
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:42095-42310 (Contract_ContractVerifyOtp_SaveX)
    /// </summary>
    [HttpPost("showmodalpopuppartysign_qrcode")]
    public async Task<IActionResult> ShowModalPopupPartySign_QRCode(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ESign_ShowModalPopupPartySign_QRCodeReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc null (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Contract_ContractVerifyOtp_Save", "Check.strJson", null);
        }

        // Hỗ trợ client gửi chuỗi JSON 'objContractParty' và 'objUserTemp' như gate MVC (ESignController.cs:2112, 2115)
        var strContractCode = req.ContractCode;
        var strCustomerCodeSys = req.CustomerCodeSys;
        var strCustomerEmail = req.CustomerEmail;
        var strCustomerName = req.CustomerName;
        var strNetworkID = req.NetworkID;
        var strOrgID = req.OrgID;
        var strTimeOTP = req.TimeOTP;

        if (!string.IsNullOrWhiteSpace(req.ObjContractParty))
        {
            try
            {
                using var doc = JsonDocument.Parse(req.ObjContractParty);
                if (doc.RootElement.TryGetProperty("CustomerCodeSys", out var ccsProp))
                    strCustomerCodeSys ??= ccsProp.GetString();
                if (doc.RootElement.TryGetProperty("CustomerEmail", out var ceProp))
                    strCustomerEmail ??= ceProp.GetString();
                if (doc.RootElement.TryGetProperty("CustomerName", out var cnProp))
                    strCustomerName ??= cnProp.GetString();
                if (doc.RootElement.TryGetProperty("ContractCode", out var ccProp))
                    strContractCode ??= ccProp.GetString();
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (!string.IsNullOrWhiteSpace(req.ObjUserTemp))
        {
            try
            {
                using var doc = JsonDocument.Parse(req.ObjUserTemp);
                if (doc.RootElement.TryGetProperty("NetworkID", out var netProp))
                    strNetworkID ??= netProp.GetString();
                if (doc.RootElement.TryGetProperty("OrgID", out var orgProp))
                    strOrgID ??= orgProp.GetString();
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(strContractCode) &&
            string.IsNullOrWhiteSpace(strCustomerCodeSys) &&
            string.IsNullOrWhiteSpace(req.ObjContractParty) &&
            string.IsNullOrWhiteSpace(req.ObjUserTemp) &&
            string.IsNullOrWhiteSpace(req.TimeOTP))
        {
            return PortResult.Fail("Contract_ContractVerifyOtp_Save", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào bằng StdParam theo nguồn QContract.cs:42136-42138
        var normContractCode = StdParam(strContractCode);
        var normUserCodeSign = StdParam(strCustomerCodeSys);
        var strWAUserCode = StdParam(req.WAUserCode ?? req.Email ?? Request?.Headers["WAUserCode"].FirstOrDefault() ?? Request?.Headers["Email"].FirstOrDefault() ?? "port") ?? "port";

        // Gate ESignController.cs:2120:
        // if (contractcode != "" && objContractParty != "" && objUserTemp != "" && (timeOTP == "" || timeOTP == "0"))
        // Nếu timeOTP khác rỗng và khác "0", client đang đếm ngược OTP cũ -> không sinh OTP mới, trả thành công luôn
        var isTimeOtpZeroOrEmpty = string.IsNullOrEmpty(strTimeOTP) || strTimeOTP == "0";
        if (!isTimeOtpZeroOrEmpty)
        {
            return Ok(new
            {
                Success = true,
                Messages = "Ký thành công"
            });
        }

        // Guard 1: Contract_ContractVerifyOtp_SaveX_InvalidTContractCode (QContract.cs:42147-42157)
        if (string.IsNullOrEmpty(normContractCode))
        {
            return PortResult.Fail("Contract_ContractVerifyOtp_SaveX_InvalidTContractCode", "Check.strContractCode", normContractCode);
        }

        // Sinh mã OTP (6 ký tự hex ngẫu nhiên như CUtils.GetRandomHexNumber(6) tại ESignController.cs:2122 hoặc lấy từ req nếu truyền trực tiếp)
        var strOtpCode = StdParam(req.OtpCode) ?? GenerateRandomHex(6);

        var dtimeSys = DateTime.UtcNow;
        var strCreateDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
        var strEndDate = dtimeSys.AddMinutes(2).ToString("yyyy-MM-dd HH:mm:ss");

        // Guard 2: Kiểm tra timeout (QContract.cs:42163-42193)
        // select t.* from Contract_ContractVerifyOtp where ContractCode = @strContractCode and OtpCode = @strOtpCode and EndDate >= @strCreateDTimeUTC
        var existingOtp = await _db.Set<Contract_ContractVerifyOtp>()
            .FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(normContractCode), PortQuery.Esc) &&
                EF.Functions.Like((x.OtpCode ?? "").ToLower(), PortQuery.Equal(strOtpCode), PortQuery.Esc) &&
                x.EndDate != null && string.Compare(x.EndDate, strCreateDTimeUTC) >= 0);

        if (existingOtp != null)
        {
            return PortResult.Fail("Contract_ContractVerifyOtp_SaveX_NotTimeOut", "Check.strContractCode", normContractCode);
        }

        // 3. Mở Transaction lưu DB: Xoá OTP cũ cùng (ContractCode, UserCodeSign) rồi chèn OTP mới (QContract.cs:42231-42294)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var oldOtps = await _db.Set<Contract_ContractVerifyOtp>()
                .Where(x =>
                    EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(normContractCode), PortQuery.Esc) &&
                    (string.IsNullOrEmpty(normUserCodeSign) || EF.Functions.Like((x.UserCodeSign ?? "").ToLower(), PortQuery.Equal(normUserCodeSign), PortQuery.Esc)))
                .ToListAsync();

            if (oldOtps.Count > 0)
            {
                _db.Set<Contract_ContractVerifyOtp>().RemoveRange(oldOtps);
            }

            var newOtp = new Contract_ContractVerifyOtp
            {
                OtpCode = strOtpCode,
                ContractCode = normContractCode,
                UserCodeSign = normUserCodeSign ?? "",
                CreateDate = strCreateDTimeUTC,
                EndDate = strEndDate,
                FlagActive = "1",
                LogLUDTimeUTC = strCreateDTimeUTC,
                LogLUBy = strWAUserCode
            };
            _db.Set<Contract_ContractVerifyOtp>().Add(newOtp);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // GAP: Bỏ qua dịch vụ ngoài gọi RptSv_Contract_ContractService lấy mẫu email và MailGun gửi email (ESignController.cs:2177-2221)

            // 4. Trả kết quả thành công khớp gate MVC ESignController.cs:2226
            return Ok(new
            {
                Success = true,
                Messages = "Ký thành công"
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Contract_ContractVerifyOtp_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Trang kết quả trả về sau khi ký số (ReturnResultPage).
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ESignController.cs:2239-2303
    /// API/ClientService: WA_Contract_Contract_Get
    /// BIZ: QContract.cs:9747-10524 Contract_Contract_GetX
    /// </summary>
    [HttpPost("returnresultpage")]
    [HttpGet("returnresultpage")]
    public async Task<IActionResult> ReturnResultPage(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ESign_ReturnResultPageReq? req)
    {
        try
        {
            var message = req?.Message ?? Request?.Query["message"].FirstOrDefault();
            var contractCode = req?.ContractCode ?? Request?.Query["contractCode"].FirstOrDefault() ?? Request?.Query["contractcode"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    using var doc = JsonDocument.Parse(req.Model);
                    if (doc.RootElement.TryGetProperty("Message", out var msgProp) || doc.RootElement.TryGetProperty("message", out msgProp))
                        message ??= msgProp.GetString();
                    if (doc.RootElement.TryGetProperty("ContractCode", out var ccProp) || doc.RootElement.TryGetProperty("contractCode", out ccProp))
                        contractCode ??= ccProp.GetString();
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            // Nhánh 1: Ký thành công (ESignController.cs:2243)
            if (string.Equals(message, "success", StringComparison.OrdinalIgnoreCase))
            {
                var domain = "";
                var urlFile = "";

                var normContractCode = StdParam(contractCode);
                if (!string.IsNullOrEmpty(normContractCode))
                {
                    var contract = await _db.Set<Contract_Contract>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(normContractCode), PortQuery.Esc));

                    if (contract != null && !string.IsNullOrEmpty(contract.ContractFilePath))
                    {
                        urlFile = contract.ContractFilePath.Replace("\\", "/");
                    }
                }

                var messageText = "Tài liệu đã được ký thành công";
                var subMessageText = "Bạn sẽ nhận được bản sao cuối cùng của tài liệu";

                var data = new ESign_ReturnResultPageData
                {
                    View = "Success",
                    ViewName = "Success",
                    MessageText = messageText,
                    SubMessageText = subMessageText,
                    UrlFile = urlFile,
                    Domain = domain
                };

                return Ok(new
                {
                    Success = true,
                    Data = data,
                    Messages = new[] { messageText },
                    MessageText = messageText,
                    SubMessageText = subMessageText,
                    UrlFile = urlFile,
                    View = "Success",
                    ViewName = "Success"
                });
            }
            // Nhánh 2: Không tìm thấy đối tác ký (ESignController.cs:2291)
            else if (string.Equals(message, "error-signer-not-valid", StringComparison.OrdinalIgnoreCase))
            {
                var messageText = "Không tìm thấy thông tin đối tác ký";
                var subMessageText = "";

                var data = new ESign_ReturnResultPageData
                {
                    View = "Notification",
                    ViewName = "Notification",
                    MessageText = messageText,
                    SubMessageText = subMessageText,
                    UrlFile = "",
                    Domain = ""
                };

                return Ok(new
                {
                    Success = true,
                    Data = data,
                    Messages = new[] { messageText },
                    MessageText = messageText,
                    SubMessageText = subMessageText,
                    UrlFile = "",
                    View = "Notification",
                    ViewName = "Notification"
                });
            }
            // Nhánh 3: Phiên ký hết hạn hoặc các trường hợp khác / mặc định (ESignController.cs:2297)
            else
            {
                var messageText = "Phiên ký đã hết hạn";
                var subMessageText = "";

                var data = new ESign_ReturnResultPageData
                {
                    View = "Notification",
                    ViewName = "Notification",
                    MessageText = messageText,
                    SubMessageText = subMessageText,
                    UrlFile = "",
                    Domain = ""
                };

                return Ok(new
                {
                    Success = true,
                    Data = data,
                    Messages = new[] { messageText },
                    MessageText = messageText,
                    SubMessageText = subMessageText,
                    UrlFile = "",
                    View = "Notification",
                    ViewName = "Notification"
                });
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Contract_Contract_Get", "Exception", ex.Message);
        }
    }

    private static readonly Random _rnd = new();
    private static string GenerateRandomHex(int digits)
    {
        byte[] buffer = new byte[(digits + 1) / 2];
        _rnd.NextBytes(buffer);
        var hex = Convert.ToHexString(buffer);
        return hex.Substring(0, digits);
    }

    private static string? StdParam(object? v)
    {
        if (v == null) return null;
        var s = v.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }
}

/// <summary>
/// DTO nhận tham số yêu cầu hủy hợp đồng từ đối tác ký số.
/// </summary>
public class ESign_Contract_ContractParty_CancelReq
{
    // Tham số phẳng
    public string? ContractCode { get; set; }
    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? WAUserCode { get; set; }
    public string? Email { get; set; }

    // Hỗ trợ tham số lồng/chuỗi như gate MVC ESignController.cs:2058
    public string? Model { get; set; }
    public string? User { get; set; }
    public string? Guid { get; set; }
}

/// <summary>
/// DTO nhận tham số yêu cầu sinh OTP ký số qua mã QR / popup.
/// </summary>
public class ESign_ShowModalPopupPartySign_QRCodeReq
{
    // Tham số phẳng
    public string? ContractCode { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? TimeOTP { get; set; }
    public string? Guid { get; set; }
    public string? OtpCode { get; set; }
    public string? WAUserCode { get; set; }
    public string? Email { get; set; }

    // Hỗ trợ tham số chuỗi JSON như gate MVC ESignController.cs:2100
    public string? ObjContractParty { get; set; }
    public string? ObjUserTemp { get; set; }
}

/// <summary>
/// DTO nhận tham số cho trang kết quả ký ReturnResultPage.
/// Gate: ESignController.cs:2239 ReturnResultPage(string message, string contractCode, string networkId, string accessToken, string guid)
/// </summary>
public class ESign_ReturnResultPageReq
{
    public string? Message { get; set; }
    public string? ContractCode { get; set; }
    public string? NetworkId { get; set; }
    public string? AccessToken { get; set; }
    public string? Guid { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO trả về kết quả cho action ReturnResultPage.
/// Tương ứng ViewBag (MessageText, SubMessageText, UrlFile) và View (Success hoặc Notification).
/// </summary>
public class ESign_ReturnResultPageData
{
    public string? View { get; set; }
    public string? ViewName { get; set; }
    public string? MessageText { get; set; }
    public string? SubMessageText { get; set; }
    public string? UrlFile { get; set; }
    public string? Domain { get; set; }
}
