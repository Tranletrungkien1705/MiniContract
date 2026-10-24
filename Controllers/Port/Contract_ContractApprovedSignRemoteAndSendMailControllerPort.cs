using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (ApprovedSignRemoteAndSendMail)
// Gate: Contract_ContractController.cs:4794-5319 (ApprovedSignRemoteAndSendMail)
// API:  ContractContractController.cs:162-230 (WA_Contract_Contract_Approved),
//       ContractContractController.cs:517-585 (WA_Contract_Contract_PartySign)
// BIZ:  QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) → 33656-33873 (Contract_Contract_Approved)
//       → 32612-33654 (Contract_Contract_ApprovedX)
//       QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) → 36218-36437 (Contract_Contract_PartySign)
//       → 35096-36216 (Contract_Contract_PartySignX)
//
// GAP — KHÔNG port phần gọi dịch vụ ngoài (PORT_RULES_MiniContract §1 + LESSONS B.7):
//   - ESign.SignFileNew (gate:4931) — ký USB token, thư viện ngoài.
//   - SignFileRemote_SingletonSigning / SignFileRemote / RemoteSigningLogin (BaseController.cs:2397-2582) — EFYCA remote signing.
//   - WA_Common_GetDTime (gate:4837) — dịch vụ giờ ngoài → dùng DateTime.UtcNow (A.13).
//   - WA_Mst_ParamContract_Get (gate:4891) — API ngoài.
//   - SaveContract_ContractElementToPdf (gate:11847-12059) — iTextSharp + VersionUtils.DownloadFile.
//   - HttpContext.Current.Cache[guid + "_RQContract_ContractCache"] (gate:4899) — session cache.
//   - ReturnApiUserCreateContract (gate:5077, BaseController.cs:6667-6737) — MasterServer ngoài.
//   - ContractPartySend → EmailChannel/SMSChannel/ZaloChannel (gate:5166) — gửi mail/SMS/Zalo ngoài.
//   - OS_FileCenter_CreateAndMoveFSX (biz:32799) — FileCenter ngoài.
//   - Sys_User_GetAbilityViewOfUser (biz:32656) — auth QContract nội bộ, đích không có.
//   - Contract_Checker_CheckDB (biz:32720) — không tra được UserName từ SysUser.
//   - Contract_ContractElement_UpdateX (biz:41450-41856) — dùng dsData + SignTypeCTS/TimeOfSigning (đích không có cột).
//   - Phần OS_* (đối tác liên thông ngoài) trong Contract_Contract_ApprovedX / Contract_Contract_PartySignX.
//   - Cột không có ở entity đích: ContractFileVersion/ContractFileName (Contract_Contract),
//     FlagCreatePartnerSignFirst (Contract_ContractParty), EffStatus (Contract_Contract) → không cập nhật.
//
// Phần port ĐƯỢC (parity 1-1 nội bộ):
//   (1) Guard Contract_Contract_CheckDB (biz:32688-32695 / 35211-35235):
//       Approved path → {PENDING, ONPROCESS}; PartySign path → {ONPROCESS} (FlagCreatePartnerSignFirst null).
//       Vi phạm → Contract_Contract_Approved_Invalid_UserApproved.
//   (2) Guard ContractFileName NOT NULL → Contract_Contract_Approved_InvalidContractFileName (biz:32755-32766);
//   (3) Guard ContractNo NOT NULL → Contract_Contract_Approved_ContractNoIsNotNull (biz:32769-32780);
//   (4) UPDATE Contract_Contract SET ContractStatus='ONPROCESS' + ContractFilePath + LogLUDTimeUTC/LogLUBy
//       (biz:32914-33017 / 35513-35551);
//   (5) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED' (biz:32974-32991 / 35536-35597);
//   (6) Nếu mọi party CONFIRMED (kể cả không có party) ⇒ ContractStatus='CONFIRMED' (biz:33092-33134 / 35555-35597);
//   (7) Ghi Contract_Contract_HistAction FunctionActionType='APPROVED' (flagOrgCreate='1') hoặc 'PartySign'
//       (biz:33154-33199 / 35615-35649);
//   (8) Search lại Contract_Contract theo ContractCode (gate:5120-5154).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractApprovedSignRemoteAndSendMailControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractApprovedSignRemoteAndSendMailControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("approvedsignremoteandsendmail")]
    public async Task<IActionResult> ApprovedSignRemoteAndSendMail(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedSignRemoteAndSendMailReq? req)
    {
        if (req == null)
        {
            // Biz mặc định của action Approved (QContract.cs:34112 Contract_Contract_Approved).
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        // A.21: gate nhận `string model`; đích nhận cả JSON object (khuyến nghị) lẫn JSON string (client cũ).
        Contract_Contract_ApprovedSignRemoteAndSendMailModel? model = null;
        if (req.Model.HasValue)
        {
            var je = req.Model.Value;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            if (je.ValueKind == JsonValueKind.Object)
            {
                model = JsonSerializer.Deserialize<Contract_Contract_ApprovedSignRemoteAndSendMailModel>(je.GetRawText(), opts);
            }
            else if (je.ValueKind == JsonValueKind.String)
            {
                var jsonStr = je.GetString();
                if (!string.IsNullOrWhiteSpace(jsonStr))
                {
                    model = JsonSerializer.Deserialize<Contract_Contract_ApprovedSignRemoteAndSendMailModel>(jsonStr, opts);
                }
            }
        }

        var contractCode = StdParam(model?.ContractCode) ?? StdParam(req.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        // gate:4810 — fileName = FileList[0].FileName (null-safe).
        var contractFileName = (model?.FileList?.FirstOrDefault()?.FileName ?? req.ContractFileName ?? "").Trim();
        var flagCheckOTP = StdFlag(model?.FlagCheckOTP ?? req.FlagCheckOTP);
        var flagOrgCreate = StrValue(model?.FlagOrgCreate ?? req.FlagOrgCreate);
        var isApprovedPath = flagOrgCreate == "1"; // gate:5066

        // A.14: đích không có phiên đăng nhập → WAUserCode tuỳ chọn (body/header), KHÔNG mặc định admin.
        var waUserCode = StdParam(req.WAUserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // biz:32688-32695 (Approved) / 35211-35235 (PartySign).
            // Approved path: Contract_Contract_CheckDB(code, "1", "PENDING, ONPROCESS");
            // PartySign path: nếu FlagCreatePartnerSignFirst="1" thì "PENDING, ONPROCESS", ngược lại "ONPROCESS".
            // Gate :5097-5111 KHÔNG set FlagCreatePartnerSignFirst ⇒ null ⇒ nhánh else ⇒ "ONPROCESS".
            var contract = await _db.Set<Contract_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));
            var status = (contract?.ContractStatus ?? "").ToUpperInvariant();
            var inStatusList = isApprovedPath
                ? (status == "PENDING" || status == "ONPROCESS")
                : (status == "ONPROCESS");
            if (contract == null || !inStatusList)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_Invalid_UserApproved",
                    "Check.ContractCode", contractCode,
                    "Check.ContractStatus", contract?.ContractStatus);
            }

            // biz:32755-32766
            if (string.IsNullOrEmpty(contractFileName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", contractFileName);
            }

            // biz:32769-32780
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            // biz:32914-33017 / 35513-35551 — UPDATE Contract_Contract.
            // GAP: ContractFileName, ContractFileVersion không có cột trong entity đích.
            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = contractFileName;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

            // biz:32974-32991 / 35536-35597 — UPDATE Contract_ContractParty.
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

            // biz:33092-33134 / 35555-35597 — mọi party CONFIRMED (hoặc không có party) ⇒ ContractStatus='CONFIRMED'.
            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // biz:33154-33199 (Approved) / 35615-35649 (PartySign) — HistAction.
            // biz:35632 — UserName lấy từ Contract_ContractParty.CustomerName của đối tác (nếu có).
            var histType = isApprovedPath ? "APPROVED" : "PartySign";
            var histFuncName = isApprovedPath ? "Contract_Contract_ApprovedX" : "Contract_Contract_PartySignX";
            var customerName = parties.FirstOrDefault(p => !string.IsNullOrEmpty(p.CustomerName))?.CustomerName;
            var maxVersion = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                .MaxAsync(x => (int?)x.Version) ?? 0;
            var hist = new Contract_Contract_HistAction
            {
                ContractCode = contractCode,
                Version = maxVersion + 1,
                UserCode = waUserCode,
                UserName = customerName ?? waUserCode,
                Description = "Ký Hợp đồng",
                LogLUDTimeUTC = nowStr,
                LogLUBy = waUserCode,
                FunctionName = histFuncName,
                FunctionActionType = histType
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);
            await _db.SaveChangesAsync();

            // gate:5120-5154 — search lại Contract_Contract theo ContractCode.
            var sendResult = await _db.Set<Contract_Contract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            // gate:5166 ContractPartySend — GAP: gửi mail/SMS/Zalo là dịch vụ ngoài.

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
                    ContractPartyStatus = parties.Select(p => p.ContractPartyStatus).ToList(),
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    SendResultFound = sendResult != null,
                    SendResultContractStatus = sendResult?.ContractStatus,
                    // gate:5286-5287
                    Message = $"Duyệt hợp đồng '{contractCode}' thành công!",
                    RedirectUrl = $"/Contract_Contract/Detail?contractcode={contractCode}"
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Biz mặc định của action Approved (QContract.cs:34112 Contract_Contract_Approved).
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
/// Tham số của gate Contract_ContractController.cs:4794 (ApprovedSignRemoteAndSendMail).
/// A.21: `string model`/`string contractelement`/`string strsigrec` của nguồn là JSON (đối tượng) — đích nhận cả
/// JSON object lẫn JSON string để tương thích; riêng 2 trường sau là GAP (chỉ ESign/PDF/remote signing dùng).
/// </summary>
public class Contract_Contract_ApprovedSignRemoteAndSendMailReq
{
    /// <summary>ContractModel — nhận cả JSON object (khuyến nghị) lẫn JSON string.</summary>
    public JsonElement? Model { get; set; }

    /// <summary>JSON Contract_ContractElement[] (gate:4794) — GAP: chỉ dùng cho UpdateX/PDF ngoài.</summary>
    public JsonElement? ContractElement { get; set; }

    /// <summary>JSON SignatureRectangle[] (gate:4794) — GAP: chỉ dùng cho ESign/remote signing ngoài.</summary>
    public JsonElement? StrSigRec { get; set; }

    /// <summary>Khoá cache session (gate:4899) — GAP: đích không có HttpContext.Cache.</summary>
    public string? Guid { get; set; }

    /// <summary>A.14: người thực hiện — nguồn UserState.SysUser.UserCode (gate:4787); đích không có phiên.</summary>
    public string? WAUserCode { get; set; }

    // Trường phẳng fallback (nếu client gửi trực tiếp không qua Model).
    public string? ContractCode { get; set; }
    public string? ContractFileName { get; set; }
    public string? FlagOrgCreate { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagElement { get; set; }
}

/// <summary>ContractModel (gate:4809) — chỉ khai trường mà port này dùng.</summary>
public class Contract_Contract_ApprovedSignRemoteAndSendMailModel
{
    public List<Contract_Contract_ApprovedSignRemoteAndSendMailFile>? FileList { get; set; }
    public string? ContractCode { get; set; }
    public string? ContractFileName { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }
    public string? FileExtension { get; set; }
}

/// <summary>FileList[0] của ContractModel (gate:4810-4811).</summary>
public class Contract_Contract_ApprovedSignRemoteAndSendMailFile
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
