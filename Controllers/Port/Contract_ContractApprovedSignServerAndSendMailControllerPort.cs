using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;
using System.Text.Json;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (ApprovedSignServerAndSendMail — bản LIVE)
// Gate: Contract_ContractController.cs:4328-4790
// API: ContractContractController.cs:162-230 (WA_Contract_Contract_Approved),
//      ContractContractController.cs:517-585 (WA_Contract_Contract_PartySign),
//      ContractContractController.cs:375-443 (WA_Contract_Contract_Get — re-search)
// BIZ: QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) -> 32612-33654 (Contract_Contract_ApprovedX)
//      QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) -> 36218-36437 (Contract_Contract_PartySignX)
//
// A.21: gate nhận `string model` (chuỗi JSON ContractModel) → đích nhận Model (object JSON) HOẶC trường phẳng.
//
// KHÁC BIỆT so với bản `_Old` (gate:3980-4324, #24):
//   Ban LIVE dùng ContractFileName KHÁC NHAU giữa 2 nhánh:
//     * Approved (flagOrgCreate=="1", gate:4531): ContractFileName = fileName (FileList[0].FileName)
//     * PartySign (gate:4578): ContractFileName = objContractModelInput.ContractFileName (trường phẳng)
//
// GAP — không port dịch vụ ngoài (PORT_RULES_MiniContract §1 + LESSONS B.7):
//   - UserState.ListMst_OrgSignConfigUI (gate:4333) — session nguồn.
//   - WA_Common_GetDTime (gate:4378) → DateTime.UtcNow (A.13).
//   - ESign.CheckToken/SignFileNew (gate:4389-4390, 4474, 4485) — checkToken bị comment = true (gate:4391-4392).
//   - WA_Mst_ParamContract_Get (gate:4433), ElementTypeValue_Get (gate:4454),
//     SaveContract_ContractElementToPdf (gate:4457) — API/iTextSharp ngoài.
//   - Cache[guid + "_RQContract_ContractCache"] (gate:4441).
//   - ReturnApiUserCreateContract (gate:4553) — MasterServer ngoài.
//   - Session[guid] + ContractPartySend (gate:4634-4637) — session + gửi mail.
//   - Xoá file tempCA (gate:4776-4786) — bỏ ESign.
//   - Cột không có trên entity đích: ContractFileSpec, InfoToken, FlagElement, FlagCheckOTP (Contract_Contract);
//     ContractFileVersion, ContractFileName, InfoToken (Contract_ContractParty).
//   - SignatureRectangle / SignTypeCTS / TimeOfSigning / tokenInfo (gate:4493-4511) — GAP (ESign).
//
// Phần port ĐƯỢC (parity 1-1 — giống #22/#24/#25):
//   (1) Contract_Contract_CheckDB: tồn tại + ContractStatus ∈ {PENDING, ONPROCESS} → ..._Invalid_UserApproved;
//   (2) Guard ContractFileName NOT NULL → ..._InvalidContractFileName;
//   (3) Guard ContractNo NOT NULL → ..._ContractNoIsNotNull;
//   (4) UPDATE Contract_Contract SET ContractStatus='ONPROCESS' + ContractFilePath + LogLUDTimeUTC/LogLUBy;
//   (5) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED';
//   (6) Nếu mọi party CONFIRMED ⇒ ContractStatus='CONFIRMED';
//   (7) Ghi Contract_Contract_HistAction: 'APPROVED'/ApprovedX hoặc 'PartySign'/PartySignX;
//   (8) Search lại Contract_Contract theo ContractCode (gate:4591-4621).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractApprovedSignServerAndSendMailControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractApprovedSignServerAndSendMailControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("approvedsignserverandsendmail")]
    public async Task<IActionResult> ApprovedSignServerAndSendMail(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedSignServerAndSendMailReq? req)
    {
        // Gate :4350 — Newtonsoft.Deserialize<ContractModel>(model); ở đích Model là object JSON (A.21)
        Contract_Contract_ApprovedSignServerAndSendMailReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Contract_Contract_ApprovedSignServerAndSendMailReq>(
                    req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) model = parsed;
            }
            catch { /* bỏ qua lỗi parse — dùng trường phẳng */ }
        }

        // biz:32665/35225 StdParam (Trim + ToUpper)
        var contractCode = StdParam(model?.ContractCode) ?? StdParam(req?.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            // Mã mặc định biz: TError.ErridQContract.Contract_Contract_Approved (QContract.cs:34112)
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        // Gate :4351 — fileName LUÔN lấy từ FileList[0].FileName
        var fileName = model?.FileList?.FirstOrDefault()?.FileName ?? "";

        // Gate :4541-4542 — flagOrgCreate quyết định nhánh Approved / PartySign
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);
        var isApprovedPath = flagOrgCreate == "1";

        // Gate :4531 (Approved) → ContractFileName = fileName;
        //      :4578 (PartySign) → ContractFileName = objContractModelInput.ContractFileName
        var contractFileNameForWrite = isApprovedPath ? fileName : StrValue(model?.ContractFileName);

        // Người thực hiện — nguồn UserState.SysUser.UserCode (gate:4520); đích không có phiên (A.24)
        var waUserCode = StdParam(model?.WAUserCode)
            ?? StdParam(req?.WAUserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault())
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

            // biz:32755-32766 ContractFileName NOT NULL (giá trị theo nhánh)
            if (string.IsNullOrEmpty(contractFileNameForWrite))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", contractFileNameForWrite);
            }

            // biz:32769-32780 ContractNo NOT NULL
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            // biz:32841-32871 (ApprovedX) / 35429-35493 (PartySignX) UPDATE Contract_Contract
            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = contractFileNameForWrite;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

            // biz:32974-32991 (ApprovedX) / 35436-35483 (PartySignX) UPDATE Contract_ContractParty
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
                FunctionActionType = isApprovedPath ? "APPROVED" : "PartySign"
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);
            await _db.SaveChangesAsync();

            // Gate :4591-4621 — Search lại Contract_Contract theo ContractCode
            // GAP: ContractPartySend (gate:4637) — gửi mail/SMS/Zalo ngoài.
            var sendResult = await _db.Set<Contract_Contract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc));

            await tx.CommitAsync();

            // Gate :4755-4760 — Success=true + AddMessage + RedirectUrl Detail
            // nguồn dùng CUtils.StrValue(objContractModelInput.ContractCode) (raw, không StdParam).
            var rawContractCode = StrValue(model?.ContractCode);
            var message = "Duyệt hợp đồng '" + rawContractCode + "' thành công!";
            var redirectUrl = "/Contract_Contract/Detail?contractcode=" + rawContractCode;

            return Ok(new
            {
                Success = true,
                Message = new[] { new { Key = "contract_contract", Value = message } },
                RedirectUrl = redirectUrl,
                Detail = (string?)null,
                ErrorCode = (string?)null,
                Data = new
                {
                    ContractCode = contractCode,
                    ContractStatus = contract.ContractStatus,
                    ContractFilePath = contract.ContractFilePath,
                    FlagOrgCreate = flagOrgCreate,
                    ContractPartyStatus = parties.Select(p => p.ContractPartyStatus).ToList(),
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    SendResultFound = sendResult != null,
                    SendResultContractStatus = sendResult?.ContractStatus,
                    Message = message,
                    RedirectUrl = redirectUrl
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
/// Tham số của gate Contract_ContractController.cs:4328-4790 (ApprovedSignServerAndSendMail — bản LIVE).
/// Client có thể gửi `Model` (chuỗi JSON ContractModel) HOẶC gửi phẳng các trường (A.21).
/// </summary>
public class Contract_Contract_ApprovedSignServerAndSendMailReq
{
    /// <summary>Chuỗi JSON ContractModel (gate :4350).</summary>
    public string? Model { get; set; }

    /// <summary>JSON List Contract_ContractElement (gate :4408) — GAP: dùng cho ESign, không port.</summary>
    public string? ContractElement { get; set; }

    /// <summary>JSON List SignatureRectangle (gate :4407) — GAP: dùng cho ESign, không port.</summary>
    public string? StrSigRec { get; set; }

    /// <summary>Cache key (gate :4441) — GAP: HttpContext.Current.Cache không có ở đích.</summary>
    public string? Guid { get; set; }

    /// <summary>Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:4520); đích không có phiên (A.24).</summary>
    public string? WAUserCode { get; set; }

    // Các trường ContractModel (gate:4529-4534, 4569-4579)
    public string? ContractCode { get; set; }
    public string? ContractFileName { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }

    /// <summary>Gate :4351-4352 — FileList[0].FileName dùng cho CẢ HAI nhánh (guard + Approved path).</summary>
    public List<Contract_Contract_ApprovedSignServerAndSendMailFile>? FileList { get; set; }
}

/// <summary>Phần tử FileList của ContractModel (gate :4351-4352).</summary>
public class Contract_Contract_ApprovedSignServerAndSendMailFile
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
