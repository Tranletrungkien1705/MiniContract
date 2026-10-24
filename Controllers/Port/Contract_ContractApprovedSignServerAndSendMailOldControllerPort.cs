using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;
using System.Text.Json;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (ApprovedSignServerAndSendMail_Old)
// Gate: Contract_ContractController.cs:3980-4324
//   + ham phu: BaseController.cs:6667-6737 (ReturnApiUserCreateContract),
//     Contract_ContractController.cs:7980-7995 (strWhereClause_Contract_ContractSearch),
//     BaseController.cs:6138-6144 (RT_Contract_Contract_Get),
//     Contract_ContractController.cs:19707-19733 (ContractResult),
//     Contract_ContractController.cs:12194-12364 (ContractPartySend)
// API: ContractContractController.cs:162-230 (WA_Contract_Contract_Approved),
//      ContractContractController.cs:517-585 (WA_Contract_Contract_PartySign),
//      ContractContractController.cs:375-443 (WA_Contract_Contract_Get)
// BIZ: QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) → 32612-33654 (Contract_Contract_ApprovedX)
//      QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) → 35096-36216 (Contract_Contract_PartySignX)
//      (cùng chuỗi biz đã port ở #22 ApprovedAndSignServer_Old và #25 ApprovedSignAndSendMail)
//
// A.21: gate nhận `string model` (chuỗi JSON ContractModel) → đích nhận JSON tương ứng (Model hoặc phẳng).
//
// GAP — không port phần gọi dịch vụ ngoài (PORT_RULES_MiniContract §1 + LESSONS B.7):
//   - objSignConfig = UserState.ListMst_OrgSignConfigUI (gate:3985) — không có phiên (A.24) → bỏ, không cần cho nghiệp vụ.
//   - WA_Common_GetDTime (gate:4010-4019) — dịch vụ giờ ngoài → DateTime.UtcNow (A.13).
//   - ESign.SignFile (gate:4045) — thư viện ký số server; toàn bộ khối gọi
//     WA_Contract_Contract_Approved/PartySign nằm trong `if (ESign.SignFile(...) == true)`. Port coi như
//     luôn tiếp tục (giống cách #22/#25 đã chốt: bỏ ESign, chạy thẳng phần nghiệp vụ nội bộ).
//   - ReturnApiUserCreateContract (gate:4084, BaseController.cs:6667-6737) — WA_MstSv_Sys_User_Login +
//     WA_Contract_ContractParty_Get, dịch vụ MasterServer ngoài.
//   - ContractPartySend (gate:4170) — EmailChannel/SMSChannel/ZaloChannel + SendMailGun/RenderRazorViewToString ngoài.
//   - Session[guid] = listContract_Result (gate:4168) — không có session ở đích; không cần cho kết quả HTTP.
//   - Xoá file tempCA (gate:4311-4320) — không có file tạm (bỏ ESign).
//   - Cột không có trên entity đích: ContractFileVersion, ApprDTimeUTC, ApprBy, EffStatus, InfoToken,
//     ContractFileSpec → GAP (giống #25).
//   - Contract_Checker_CheckDB (biz:32720) — dùng chung mã lỗi với guard CheckDB trạng thái (biz:32688-32695)
//     theo đúng cách #22/#25 đã chốt.
//
// Phần port ĐƯỢC (parity 1-1 nội bộ — trùng với #25 ApprovedSignAndSendMail):
//   (1) Contract_Contract_CheckDB (biz:32688-32695): tồn tại + ContractStatus ∈ {PENDING, ONPROCESS}
//       → mã Contract_Contract_Approved_Invalid_UserApproved;
//   (2) Guard ContractFileName NOT NULL → Contract_Contract_Approved_InvalidContractFileName;
//   (3) Guard ContractNo NOT NULL → Contract_Contract_Approved_ContractNoIsNotNull;
//   (4) UPDATE Contract_Contract SET ContractStatus='ONPROCESS' + ContractFilePath + LogLUDTimeUTC/LogLUBy;
//   (5) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED';
//   (6) Nếu mọi party CONFIRMED ⇒ ContractStatus='CONFIRMED';
//   (7) Ghi Contract_Contract_HistAction: Approved (flagOrgCreate=='1') → FunctionActionType='APPROVED' +
//       FunctionName='Contract_Contract_ApprovedX'; ngược lại → 'PartySign' + 'Contract_Contract_PartySignX';
//   (8) Search lại Contract_Contract theo ContractCode sau ghi (gate:4124-4159).
//
// KHÁC BIỆT so với #25 (ApprovedSignAndSendMail): ContractFileName LUÔN lấy từ FileList[0].FileName
// (gate :4026, 4066) cho CẢ HAI nhánh Approved và PartySign — không lấy từ trường phẳng ContractFileName.
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractApprovedSignServerAndSendMailOldControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractApprovedSignServerAndSendMailOldControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("approvedsignserverandsendmail_old")]
    public async Task<IActionResult> ApprovedSignServerAndSendMail_Old(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedSignServerAndSendMailOldReq? req)
    {
        // Gate :4025 — Newtonsoft.Deserialize<ContractModel>(model); ở đích Model là object JSON (A.21)
        Contract_Contract_ApprovedSignServerAndSendMailOldReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Contract_Contract_ApprovedSignServerAndSendMailOldReq>(
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

        // Gate :4026, 4066 — fileName LUÔN lấy từ FileList[0].FileName
        var fileName = (model?.FileList?.FirstOrDefault()?.FileName ?? "").Trim();

        // Gate :4067-4068 — FlagElement/FlagCheckOTP của Contract_Contract (CUtils.StrValue)
        var flagElement = StrValue(model?.FlagElement);
        var flagCheckOTP = StrValue(model?.FlagCheckOTP);

        // Gate :4072 — flagOrgCreate = StrValue(ContractModel.FlagOrgCreate)
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);
        var isApprovedPath = flagOrgCreate == "1"; // gate :4073 — "hợp đồng đi"

        // Người thực hiện — nguồn UserState.SysUser.UserCode (gate:4055); đích không có phiên (A.24)
        var waUserCode = StdParam(model?.WAUserCode)
            ?? StdParam(req?.WAUserCode)
            ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

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
            if (string.IsNullOrEmpty(fileName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", fileName);
            }

            // biz:32769-32780 ContractNo NOT NULL
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            // biz:32914-33017 (ApprovedX) / 35426-35497 (PartySignX) UPDATE Contract_Contract
            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = fileName;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

            // UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED'
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

            // biz:33092-33134 / 35555-35597 — mọi party CONFIRMED (hoặc không có party) ⇒ ContractStatus='CONFIRMED'
            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // biz:33154-33199 (ApprovedX) / 35615-35649 (PartySignX) — Contract_Contract_HistAction
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

            // Gate :4124-4159 — Search lại Contract_Contract theo ContractCode (strWhereClause_Contract_ContractSearch)
            // GAP: ContractPartySend (gate:4170) — gửi mail/SMS/Zalo ngoài.
            var sendResult = await _db.Set<Contract_Contract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            await tx.CommitAsync();

            // Gate :4290-4293 — message "Duyệt hợp đồng '<code>' thành công!" + RedirectUrl Detail
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ContractCode = contractCode,
                    ContractStatus = contract.ContractStatus,
                    ContractFilePath = contract.ContractFilePath,
                    FlagOrgCreate = flagOrgCreate,
                    FlagElement = flagElement,
                    FlagCheckOTP = flagCheckOTP,
                    ContractPartyStatus = parties.Select(p => p.ContractPartyStatus).ToList(),
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    SendResultFound = sendResult != null,
                    SendResultContractStatus = sendResult?.ContractStatus,
                    Message = $"Duyệt hợp đồng '{contractCode}' thành công!",
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

    /// <summary>CUtils.StdParam của nguồn: Trim + ToUpper, rỗng → null.</summary>
    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    /// <summary>CUtils.StrValue của nguồn: null → "", KHÔNG Trim/ToUpper.</summary>
    private static string StrValue(object? value) => value?.ToString() ?? "";
}

/// <summary>
/// Tham số của gate Contract_ContractController.cs:3980 (ApprovedSignServerAndSendMail_Old) — ContractModel.
/// Client có thể gửi `Model` (chuỗi JSON ContractModel) HOẶC gửi phẳng các trường (A.21).
/// </summary>
public class Contract_Contract_ApprovedSignServerAndSendMailOldReq
{
    /// <summary>Chuỗi JSON ContractModel (gate :4025).</summary>
    public string? Model { get; set; }

    // Các trường ContractModel (gate :4064-4072)
    public string? ContractCode { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }

    /// <summary>FileList của ContractModel — fileName lấy từ FileList[0].FileName (gate :4026).</summary>
    public List<Contract_Contract_ApprovedSignServerAndSendMailOldFile>? FileList { get; set; }

    /// <summary>Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:4055); đích không có phiên (A.24).</summary>
    public string? WAUserCode { get; set; }
}

/// <summary>Phần tử FileList của ContractModel (gate :4026-4027).</summary>
public class Contract_Contract_ApprovedSignServerAndSendMailOldFile
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
