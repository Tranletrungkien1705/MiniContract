using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (ApprovedAndSignRemote)
// Gate:  Contract_ContractController.cs:2937-3299
// API:   ContractContractController.cs:162-230 (WA_Contract_Contract_Approved), 517-585 (WA_Contract_Contract_PartySign)
// BIZ:   QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) -> 32612-33654 (Contract_Contract_Approved),
//        QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) -> 36218-36437 (Contract_Contract_PartySign)
//
// A.21: gate nhan `string strJson` -> o dich body chinh LA doi tuong JSON (Model = object, khong phai chuoi long).
//
// GAP — KHONG port phan goi dich vu ngoai (PORT_RULES_MiniContract §1 + LESSONS_AGENTS B.7):
//   - ESign.SignFileNew (gate:3071)
//   - SignFileRemote_SingletonSigning / SignFileRemote / RemoteSigningLogin (BaseController.cs:2397-2582)
//   - WA_Common_GetDTime (gate:2978) -> dung DateTime.UtcNow (A.13)
//   - WA_Mst_ParamContract_Get (gate:3032) — API khong co trong Biz.Web (excerpt dong 978)
//   - SaveContract_ContractElementToPdf (gate:11847-12059) — iTextSharp + VersionUtils.DownloadFile
//   - ReturnApiUserCreateContract (gate:3223, BaseController.cs:6667-6737)
//   - HttpContext.Current.Cache[guid + "_RQContract_ContractCache"] (gate:3040)
//   - OS_FileCenter_CreateAndMoveFSX (biz:32799)
//   - Contract_Checker_CheckDB / Contract_ContractDtl / Contract_ContractFiles / Contract_Contract_Base64
//   - Mst_ParamContract / Mst_CTSRemoteSupplier / Mst_OrgSignConfigUI
//
// Phan port DUOC (parity 1-1 phan noi bo):
//   (1) Guard CheckDB "ton tai va ContractStatus ∈ {PENDING, ONPROCESS}" (biz:32686-32695);
//   (2) Guard ContractFileName NOT NULL -> Contract_Contract_Approved_InvalidContractFileName (biz:32755-32766);
//   (3) Guard ContractNo NOT NULL -> Contract_Contract_Approved_ContractNoIsNotNull (biz:32769-32780);
//   (4) UPDATE Contract_Contract SET ContractStatus='ONPROCESS' + ContractFilePath + LogLUDTimeUTC/LogLUBy (biz:32914-33017);
//   (5) UPDATE Contract_ContractParty SET ContractPartyStatus='CONFIRMED' (biz:32974-32991);
//   (6) Neu moi party CONFIRMED -> ContractStatus='CONFIRMED' (biz:42092-42134);
//   (7) Ghi Contract_Contract_HistAction FunctionActionType='APPROVED', Desc='Ky Hop dong' (biz:33154-33199).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractApprovedAndSignRemoteControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractApprovedAndSignRemoteControllerPort(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("approvedandsignremote")]
    public async Task<IActionResult> ApprovedAndSignRemote(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedAndSignRemoteReq? req)
    {
        if (req == null)
        {
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        var model = req.Model;

        var contractCode = StdParam(model?.ContractCode) ?? StdParam(req.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        var fileName = (model?.FileList?.FirstOrDefault()?.FileName ?? "").Trim();
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);
        var flagCheckOTP = StrValue(model?.FlagCheckOTP);
        var flagElement = StrValue(model?.FlagElement);

        // A.14: khong co phien dang nhap o dich -> WAUserCode TUY CHON (body hoac header); KHONG mac dinh tai khoan admin.
        // Null-safe: test goi thang controller (khong qua HTTP) thi HttpContext/Request = null.
        var waUserCode = StdParam(req.WAUserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
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

            if (string.IsNullOrEmpty(fileName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", fileName);
            }

            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = fileName;
            contract.LogLUDTimeUTC = nowStr;
            contract.LogLUBy = waUserCode;

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

            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            var maxVersion = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc))
                .MaxAsync(x => (int?)x.Version) ?? 0;
            var hist = new Contract_Contract_HistAction
            {
                ContractCode = contractCode,
                Version = maxVersion + 1,
                UserCode = waUserCode,
                Description = "Ky Hop dong",
                LogLUDTimeUTC = nowStr,
                LogLUBy = waUserCode,
                FunctionName = flagOrgCreate == "1" ? "Contract_Contract_ApprovedX" : "Contract_Contract_PartySign",
                FunctionActionType = "APPROVED"
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

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
                    Message = $"Duyet hop dong '{contractCode}' thanh cong!",
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

    private static string StrValue(object? value) => value?.ToString() ?? "";
}

/// <summary>Tham so cua gate Contract_ContractController.cs:2937 (ApprovedAndSignRemote).</summary>
public class Contract_Contract_ApprovedAndSignRemoteReq
{
    /// <summary>A.21: gate nhan `string strJson` -> o dich la doi tuong JSON cua ContractModel (khong phai chuoi long).</summary>
    public Contract_Contract_ApprovedAndSignRemoteModel? Model { get; set; }
    public string? ContractElement { get; set; }
    public string? StrSigRec { get; set; }
    public string? Guid { get; set; }
    public string? WAUserCode { get; set; }
    public string? ContractCode { get; set; }
}

/// <summary>ContractModel (gate :2951) — chi khai truong duoc dung.</summary>
public class Contract_Contract_ApprovedAndSignRemoteModel
{
    public List<Contract_Contract_ApprovedAndSignRemoteFile>? FileList { get; set; }
    public string? ContractCode { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FileExtension { get; set; }
}

/// <summary>Phan tu FileList cua ContractModel (gate :2952-2953).</summary>
public class Contract_Contract_ApprovedAndSignRemoteFile
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
