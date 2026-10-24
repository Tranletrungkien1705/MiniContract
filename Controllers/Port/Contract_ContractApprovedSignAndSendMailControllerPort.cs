using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;
using System.Text.Json;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (ApprovedSignAndSendMail)
// Gate: Contract_ContractController.cs:3579-3975 (ApprovedSignAndSendMail)
// API:  ContractContractController.cs:162-230 (WA_Contract_Contract_Approved),
//       ContractContractController.cs:517-585 (WA_Contract_Contract_PartySign)
// BIZ:  QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) → 32612-33654 (Contract_Contract_ApprovedX)
//       QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) → 36218-36437 → 35096-36216 (Contract_Contract_PartySignX)
//
// A.21: gate nhận `string model` (JSON ContractModel), `contractelement`, `strsigrec` → đích nhận JSON tương ứng.
//
// GAP — KHÔNG port phần gọi dịch vụ ngoài (PORT_RULES §1 + LESSONS B.7):
//   - ESign.SignFileNew (gate:3687) — ký số USB token, thư viện ngoài.
//   - WA_Common_GetDTime (gate:3620) — dịch vụ giờ ngoài → DateTime.UtcNow (A.13).
//   - ReturnApiUserCreateContract (gate:3756, BaseController.cs:6667-6737) — WA_MstSv_Sys_User_Login + WA_Contract_ContractParty_Get (MasterServer ngoài).
//   - ContractPartySend → EmailChannel/SMSChannel/ZaloChannel → SendMailGun / RenderRazorViewToString (gate:3844, :12223-12363, BaseController.cs:467-478) — gửi mail/SMS/Zalo ngoài.
//   - OS_FileCenter_CreateAndMoveFSX (biz:32799) — FileCenter ngoài.
//   - Sys_User_GetAbilityViewOfUser (biz:32656) — auth QContract nội bộ, đích không có.
//   - Contract_Checker_CheckDB (biz:32720) — không tra được UserName từ SysUser (đích không có phiên).
//   - OS_MstSvTVANService.Instance.* / OS_Contract_Contract.Instance.* (biz:34841, 34869, 35784) — đối tác liên thông ngoài.
//   - Contract_ContractParty_UpdEmailSend (gate:3953) — external mail.
//   - Contract_ContractElement_UpdateX (biz:36305) — entity đích không có cột SignTypeCTS/TimeOfSigning/ElementType.
//   - Cột không có ở entity đích: ContractFileVersion, ApprDTimeUTC, ApprBy, EffStatus, InfoToken, ContractFileSpec → không cập nhật.
//
// Phần port ĐƯỢC (parity 1-1 nội bộ):
//   (1) Contract_Contract_CheckDB (biz:32688-32695): tồn tại + ContractStatus ∈ {PENDING, ONPROCESS}
//       → mã Contract_Contract_Approved_Invalid_UserApproved;
//   (2) Guard ContractFileName NOT NULL → Contract_Contract_Approved_InvalidContractFileName (biz:32755-32766);
//   (3) Guard ContractNo NOT NULL → Contract_Contract_Approved_ContractNoIsNotNull (biz:32769-32780);
//   (4) UPDATE Contract_Contract SET ContractStatus='ONPROCESS' + ContractFilePath + LogLUDTimeUTC/LogLUBy (biz:32914-33017, 35426-35597);
//   (5) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED' (biz:32974-32991, 35436-35497);
//   (6) Nếu mọi party CONFIRMED ⇒ ContractStatus='CONFIRMED' (biz:33092-33134, 35555-35597);
//   (7) Ghi Contract_Contract_HistAction FunctionActionType='APPROVED' (flagOrgCreate='1') hoặc 'PartySign'
//       (biz:33154-33199, 35615-35649);
//   (8) Search lại Contract_Contract theo ContractCode sau ghi (gate:3798-3833).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractApprovedSignAndSendMailControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractApprovedSignAndSendMailControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("approvedsignandsendmail")]
    public async Task<IActionResult> ApprovedSignAndSendMail(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedSignAndSendMailReq? req)
    {
        // Gate :3594 — model có thể là chuỗi JSON (A.21); hoặc trường phẳng
        Contract_Contract_ApprovedSignAndSendMailReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Contract_Contract_ApprovedSignAndSendMailReq>(
                    req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) model = parsed;
            }
            catch { /* bỏ qua lỗi parse — dùng trường phẳng */ }
        }

        var contractCode = StdParam(model?.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            // Mã mặc định biz: TError.ErridQContract.Contract_Contract_Approved (QContract.cs:34112)
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        // Gate :3733-3736 — các trường từ ContractModel
        var contractFileName = (model?.ContractFileName ?? "").Trim();
        var flagCheckOTP = StdFlag(model?.FlagCheckOTP);
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);

        // Gate :3694-3701 — tokenInfo lấy từ SignType DIGITAL/SHORT
        var tokenInfo = "";
        if (req?.StrSigRec != null && req.StrSigRec.Count > 0)
        {
            var objSignatureDigital = req.StrSigRec.FirstOrDefault(it =>
                string.Equals((it.SignType ?? "").ToUpperInvariant(), "DIGITAL", StringComparison.Ordinal)
                || string.Equals((it.SignType ?? "").ToUpperInvariant(), "SHORT", StringComparison.Ordinal));
            if (objSignatureDigital != null) tokenInfo = (objSignatureDigital.TokenInfo?.ToString() ?? "").Trim();
        }

        // Người thực hiện — nguồn UserState.SysUser.UserCode (gate:3722); đích không có phiên (A.24)
        // Null-safe: test gọi controller trực tiếp có thể có Request = null
        var waUserCode = StdParam(model?.WAUserCode)
            ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var isApprovedPath = flagOrgCreate == "1"; // gate :3745 — "hợp đồng đi"

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // biz:32688-32695 Contract_Contract_CheckDB(code, "1", "PENDING, ONPROCESS")
            var contract = await _db.Set<Contract_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            var status = (contract?.ContractStatus ?? "").ToUpperInvariant();
            var inStatusList = status == "PENDING" || status == "ONPROCESS";
            if (contract == null || !inStatusList)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_Invalid_UserApproved",
                    "Check.ContractCode", contractCode,
                    "Check.ContractStatus", contract?.ContractStatus);
            }

            // biz:32755-32766 ContractFileName NOT NULL
            if (string.IsNullOrEmpty(contractFileName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", contractFileName);
            }

            // biz:32769-32780 ContractNo NOT NULL
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            // biz:32914-33017 / 35426-35597 UPDATE Contract_Contract
            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = contractFileName;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

            // biz:32974-32991 / 35436-35497 UPDATE Contract_ContractParty
            var parties = await _db.Set<Contract_ContractParty>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                .ToListAsync();
            foreach (var p in parties)
            {
                p.ContractPartyStatus = "CONFIRMED";
                p.LogLUDTimeUTC = nowStr;
                p.LogLUBy = waUserCode;
            }
            await _db.SaveChangesAsync();

            // biz:33092-33134 / 35555-35597 — mọi party CONFIRMED ⇒ ContractStatus='CONFIRMED'
            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // biz:33154-33199 (Approved) / 35615-35649 (PartySign) — HistAction
            var histType = isApprovedPath ? "APPROVED" : "PartySign";
            var maxVersion = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                .MaxAsync(x => (int?)x.Version) ?? 0;
            var hist = new Contract_Contract_HistAction
            {
                ContractCode = contractCode,
                Version = maxVersion + 1,
                UserCode = waUserCode,
                Description = "Ký Hợp đồng",
                LogLUDTimeUTC = nowStr,
                LogLUBy = waUserCode,
                FunctionName = isApprovedPath ? "Contract_Contract_ApprovedX" : "Contract_Contract_PartySignX",
                FunctionActionType = histType
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);
            await _db.SaveChangesAsync();

            // Gate :3798-3833 — search Contract_Contract theo ContractCode sau ghi
            var sendResult = await _db.Set<Contract_Contract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            // Gate :3844 — ContractPartySend (gửi mail/SMS/Zalo) — GAP: dịch vụ ngoài.

            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ContractCode = contractCode,
                    ContractStatus = contract.ContractStatus,
                    ContractFilePath = contract.ContractFilePath,
                    FlagOrgCreate = flagOrgCreate,
                    FlagCheckOTP = flagCheckOTP,
                    InfoToken = tokenInfo,
                    ContractPartyStatus = parties.Select(p => p.ContractPartyStatus).ToList(),
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    SendResultFound = sendResult != null,
                    SendResultContractStatus = sendResult?.ContractStatus,
                    Message = $"Ký và gửi hợp đồng '{contractCode}' thành công!",
                    RedirectUrl = $"/Contract_Contract/Detail?contractcode={contractCode}"
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Contract_Contract_Approved", "Exception", ex.Message);
        }
    }

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    private static string StdFlag(object? value)
    {
        var s = value?.ToString()?.Trim();
        return s == "1" ? "1" : "0";
    }

    private static string StrValue(object? value) => value?.ToString() ?? "";
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:3579 (ApprovedSignAndSendMail).
/// Client có thể gửi `Model` (chuỗi JSON ContractModel) + `ContractElement`/`StrSigRec` (JSON) như gate,
/// HOẶC gửi phẳng các trường (bài học A.21 — KHÔNG bịa body khác nguồn).
/// </summary>
public class Contract_Contract_ApprovedSignAndSendMailReq
{
    /// <summary>Chuỗi JSON ContractModel (gate:3594).</summary>
    public string? Model { get; set; }

    // Các trường ContractModel (gate:3731-3737)
    public string? ContractCode { get; set; }
    public string? ContractFileName { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }
    public string? OtpCode { get; set; }

    /// <summary>FileList của ContractModel (gate:3675-3676) — GAP: chỉ dùng cho ESign (bỏ).</summary>
    public List<Contract_Contract_ApprovedSignAndSendMail_FileItem>? FileList { get; set; }

    /// <summary>Danh sách Contract_ContractElement (gate:3600).</summary>
    public List<Contract_ContractElementItem>? ContractElement { get; set; }

    /// <summary>Danh sách SignatureRectangle (gate:3599).</summary>
    public List<Contract_SignatureRectangleItem>? StrSigRec { get; set; }

    /// <summary>Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:3722); đích không có phiên (bài học A.24).</summary>
    public string? WAUserCode { get; set; }
}

/// <summary>Phần tử FileList của ContractModel (gate:3675-3676).</summary>
public class Contract_Contract_ApprovedSignAndSendMail_FileItem
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
