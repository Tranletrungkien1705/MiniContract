using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (ApprovedAndSignServer)
// Gate:  Contract_ContractController.cs:2629-2933
// API:   ContractContractController.cs:162-230 (WA_Contract_Contract_Approved), 517-585 (WA_Contract_Contract_PartySign)
// BIZ:   QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) -> 32612-33654 (Contract_Contract_ApprovedX),
//        QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) -> 35096-36216 (Contract_Contract_PartySignX)
//
// A.21: gate nhan `string model` (chuoi JSON ContractModel) -> o dich Model la doi tuong JSON (khong phai chuoi long).
//
// KHAC ApprovedAndSignServer_Old (#22): gate :2650-2651 lay fileName = FileList[0].FileName cho CA HAI nhanh
// (Old :2538/:2576 lay khac nhau giua Approved/PartySign). Port giu dung khac biet nay cua nguon (A.18).
//
// GAP — KHONG port phan goi dich vu ngoai (PORT_RULES_MiniContract muc 1 + LESSONS_AGENTS B.7):
//   - UserState.ListMst_OrgSignConfigUI (gate:2634) — session + Mst_OrgSignConfig nguon, khong co o dich.
//   - WA_Common_GetDTime (gate:2676) -> dung DateTime.UtcNow (A.13).
//   - ESign.SignFileNew (gate:2772, 2783) — thu vien ky so server, khong co o dich.
//     (checkTokenidocNet/checkToken da bi gate comment = true (gate:2689-2690), nen port coi nhu luon dung.)
//   - WA_Mst_ParamContract_Get (gate:2733) — API/biz chua co trong excerpt.
//   - SaveContract_ContractElementToPdf (gate:11847-12059) — iTextSharp + VersionUtils.DownloadFile.
//   - HttpContext.Current.Cache[guid + "_RQContract_ContractCache"] (gate:2741).
//   - ReturnApiUserCreateContract (gate:2858, BaseController.cs:6667-6737) — WA_MstSv_Sys_User_Login ngoai.
//   - OS_FileCenter_CreateAndMoveFSX (biz:32800, 35327) — dich vu FileCenter ngoai.
//   - Contract_Checker_CheckDB (biz:32720) — bang/entity chua co o dich.
//   - InosService thong bao (biz:33377-33516) — dich vu INOS ngoai.
//   - Cot khong co tren entity dich: ApprDTimeUTC, ApprBy, ContractFileVersion, EffStatus, InfoToken,
//     ContractFileSpec, ContractFileName (khac ContractFilePath), MST (tren Contract_Contract) -> GAP.
//
// Phan port DUOC (parity 1-1 noi bo biz, giong het #22/#25 da chot):
//   (1) Guard CheckDB "ton tai va ContractStatus thuoc {PENDING, ONPROCESS}" (biz:32688-32695);
//   (2) Guard ContractFileName NOT NULL -> Contract_Contract_Approved_InvalidContractFileName (biz:32755-32766);
//   (3) Guard ContractNo NOT NULL -> Contract_Contract_Approved_ContractNoIsNotNull (biz:32769-32780);
//   (4) UPDATE Contract_Contract SET ContractStatus='ONPROCESS' + ContractFilePath + LogLUDTimeUTC/LogLUBy (biz:32841-32871, 35429-35493);
//   (5) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED' (biz:32974-32991, 35436-35483);
//   (6) Neu moi party CONFIRMED -> ContractStatus='CONFIRMED' (biz:33092-33134, 35555-35597);
//   (7) Ghi Contract_Contract_HistAction FunctionActionType='APPROVED', Desc='Ky Hop dong' (biz:33154-33199, 35615-35649).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractApprovedAndSignServerControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractApprovedAndSignServerControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Duyet va ky hop dong bang chu ky so server (ApprovedAndSignServer — ban LIVE).
    /// Gate: Contract_ContractController.cs:2629-2933
    /// </summary>
    [HttpPost("approvedandsignserver")]
    public async Task<IActionResult> ApprovedAndSignServer(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedAndSignServerReq? req)
    {
        // Gate :2649 — model la chuoi JSON ContractModel (bai hoc A.21); cung chap nhan body phang nhu Old
        Contract_Contract_ApprovedAndSignServerReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Contract_Contract_ApprovedAndSignServerReq>(
                    req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) model = parsed;
            }
            catch
            {
                // Bo qua loi parse — dung cac truong phang cua req
            }
        }

        // Gate :2650-2651 — fileName/filePath lay TU FileList[0] cho CA HAI nhanh (#24; khac #22)
        var fileName = (model?.FileList?.FirstOrDefault()?.FileName ?? "").Trim();

        // Gate :2836, biz:32665/35225 StdParam -> Trim + ToUpper
        var contractCode = StdParam(model?.ContractCode) ?? StdParam(req?.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            // Ma mac dinh biz: TError.ErridQContract.Contract_Contract_Approved (QContract.cs:34112)
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        // Gate :2846, 2840-2841 — FlagOrgCreate/FlagElement/FlagCheckOTP
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);
        var flagCheckOTP = StdFlag(model?.FlagCheckOTP);
        var flagElement = StrValue(model?.FlagElement);

        // Nguoi thuc hien — nguon lay UserState.SysUser.UserCode (gate:2827); dich khong co phien (A.24).
        var waUserCode = StdParam(model?.WAUserCode)
            ?? StdParam(req?.WAUserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // biz:32688-32695(ApprovedX)/35213-35237(PartySignX) Contract_Contract_CheckDB(code, "1" (Yes), "PENDING, ONPROCESS")
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

            // biz:32755-32766(ApprovedX)/35308-35319(PartySignX) ContractFileName NOT NULL
            if (string.IsNullOrEmpty(fileName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", fileName);
            }

            // biz:32769-32780(ApprovedX) ContractNo NOT NULL
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            // biz:32841-32871(ApprovedX)/35429-35493(PartySignX) UPDATE Contract_Contract (chi cac cot dich co)
            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = fileName;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

            // biz:32974-32991(ApprovedX)/35436-35483(PartySignX) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED'
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

            // biz:33092-33134(ApprovedX)/35555-35597(PartySignX) Check PartySign
            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // biz:33154-33199(ApprovedX)/35615-35649(PartySignX) Contract_Contract_HistActionX
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
                FunctionName = flagOrgCreate == "1" ? "Contract_Contract_ApprovedX" : "Contract_Contract_PartySignX",
                FunctionActionType = "APPROVED"
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Gate :2896-2901 — Success=true + AddMessage + RedirectUrl Detail
            var message = "Duyệt hợp đồng '" + contractCode + "' thành công!";
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ContractCode = contractCode,
                    ContractStatus = contract.ContractStatus,
                    FlagOrgCreate = flagOrgCreate,
                    FlagCheckOTP = flagCheckOTP,
                    FlagElement = flagElement,
                    ContractPartyStatus = parties.Select(p => p.ContractPartyStatus).ToList(),
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description,
                    Message = message,
                    RedirectUrl = "/Contract_Contract/Detail?contractcode=" + contractCode
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

    /// <summary>CUtils.StdFlag cua nguon: "1" -> "1", moi gia tri khac ke ca null -> "0".</summary>
    private static string StdFlag(object? value)
    {
        var s = value?.ToString()?.Trim();
        return s == "1" ? "1" : "0";
    }

    /// <summary>CUtils.StrValue cua nguon: null -> "", KHONG Trim/ToUpper.</summary>
    private static string? StrValue(object? value) => value?.ToString() ?? "";
}

/// <summary>
/// Tham so cua gate Contract_ContractController.cs:2629-2933 (ApprovedAndSignServer).
/// </summary>
public class Contract_Contract_ApprovedAndSignServerReq
{
    /// <summary>Chuoi JSON ContractModel (duong dan tho cua gate: `model`).</summary>
    public string? Model { get; set; }

    /// <summary>JSON List Contract_ContractElement (gate :2707) — GAP: dung cho ESign, khong port.</summary>
    public string? ContractElement { get; set; }

    /// <summary>JSON List SignatureRectangle (gate :2706) — GAP: dung cho ESign, khong port.</summary>
    public string? StrSigRec { get; set; }

    /// <summary>Cache key (gate :2741) — GAP: HttpContext.Current.Cache khong co o dich.</summary>
    public string? Guid { get; set; }

    /// <summary>Nguoi thuc hien — nguon lay UserState.SysUser.UserCode (gate:2827); dich khong co phien (A.24).</summary>
    public string? WAUserCode { get; set; }

    // Cac truong ContractModel (gate:2650, 2836, 2837-2841, 2846, 2882) — cho phep body phang nhu Old
    public string? ContractCode { get; set; }
    public string? ContractFileName { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }

    /// <summary>Gate :2650-2651 — FileList[0].FileName/FilePath dung cho CA HAI nhanh.</summary>
    public List<Contract_Contract_ApprovedAndSignServerFile>? FileList { get; set; }
}

/// <summary>Phan tu FileList cua ContractModel (gate :2650-2651).</summary>
public class Contract_Contract_ApprovedAndSignServerFile
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
