using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (ApprovedAndSignServer_Old)
// Gate:  Contract_ContractController.cs:2449-2625
//        + BaseController.cs:6667-6737 (ReturnApiUserCreateContract — GAP)
// API:   ContractContractController.cs:162-230 (WA_Contract_Contract_Approved), 517-585 (WA_Contract_Contract_PartySign)
// BIZ:   QContract.cs:34099-34211 (WAS_Contract_Contract_Approved) -> 32612-33654 (Contract_Contract_ApprovedX),
//        QContract.cs:36439-36552 (WAS_Contract_Contract_PartySign) -> 35096-36216 (Contract_Contract_PartySignX)
//        (giống y chuỗi biz đã port ở #22 ApprovedAndSign và #25 ApprovedAndSignRemote).
//
// A.21: gate nhận `string model` (chuỗi JSON ContractModel) -> ở đích Model là đối tượng JSON (không phải chuỗi lồng).
//
// GAP — KHÔNG port phần gọi dịch vụ ngoài (PORT_RULES_MiniContract §1 + LESSONS_AGENTS B.7):
//   - WA_Common_GetDTime (gate:2477) — dịch vụ giờ ngoài -> dùng DateTime.UtcNow (A.13).
//   - ESign.SignFile (gate:2511) — thư viện ký số server, không có ở đích; toàn bộ khối gọi
//     Contract_ContractService.Instance.WA_Contract_Contract_Approved/PartySign của gate NẰM TRONG
//     `if (ESign.SignFile(...) == true)` — vì không chạy được ESign, port coi như luôn tiếp tục (giống cách
//     #22/#25 đã xử lý: bỏ ESign, chạy thẳng phần nghiệp vụ nội bộ).
//   - ReturnApiUserCreateContract (gate:2551, BaseController.cs:6667-6737) — gọi WA_MstSv_Sys_User_Login +
//     WA_Contract_ContractParty_Get, dịch vụ MasterServer ngoài.
//   - OS_FileCenter_CreateAndMoveFSX (biz:32799, 35327) — dịch vụ FileCenter ngoài.
//   - Contract_Checker_CheckDB (biz:32720) — dùng chung mã lỗi Approved_Invalid_UserApproved với guard CheckDB
//     trạng thái hợp đồng (biz:32688-32695) — theo đúng cách #22/#25 đã chốt (build xanh, test xanh).
//   - InosService thông báo (biz:33377-33516) — dịch vụ INOS ngoài.
//   - Cột không có trên entity đích: ApprDTimeUTC, ApprBy, ContractFileVersion, EffStatus, InfoToken → GAP.
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractApprovedAndSignServerOldControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractApprovedAndSignServerOldControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Duyệt &amp; ký hợp đồng bằng chữ ký số server (ApprovedAndSignServer_Old).
    /// Gate: Contract_ContractController.cs:2449-2625
    /// </summary>
    [HttpPost("approvedandsignserver_old")]
    public async Task<IActionResult> ApprovedAndSignServer_Old(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ApprovedAndSignServerOldReq? req)
    {
        // Gate :2492 — model là chuỗi JSON ContractModel (bài học A.21)
        Contract_Contract_ApprovedAndSignServerOldReq? model = req;
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Contract_Contract_ApprovedAndSignServerOldReq>(
                    req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) model = parsed;
            }
            catch
            {
                // Bỏ qua lỗi parse — dùng các trường phẳng của req
            }
        }

        // Gate :2530 — CUtils.StrValue(...ContractCode); biz:3715(ApprovedX)/5155(PartySignX) StdParam -> Trim + ToUpper
        var contractCode = StdParam(model?.ContractCode);
        if (string.IsNullOrEmpty(contractCode))
        {
            // Mã mặc định biz: TError.ErridQContract.Contract_Contract_Approved (QContract.cs:34112)
            return PortResult.Fail("Contract_Contract_Approved", "Check.strJson", null);
        }

        // Gate :2538, 2579, 2580-2581 — StrValue / StdFlag
        var flagOrgCreate = StrValue(model?.FlagOrgCreate);
        var flagCheckOTP = StdFlag(model?.FlagCheckOTP);
        var flagElement = StrValue(model?.FlagElement);

        // Gate :2493, 2532 (nhánh Approved, FlagOrgCreate=="1") — ContractFileName LẤY TỪ FileList[0].FileName;
        // Gate :2576 (nhánh PartySign) — ContractFileName LẤY TỪ trường ContractFileName cấp cao nhất của ContractModel.
        // GIỮ NGUYÊN khác biệt này của nguồn (không "sửa cho đúng ý" — bài học A.18).
        var fileListFileName = (model?.FileList?.FirstOrDefault()?.FileName ?? "").Trim();
        var topContractFileName = (model?.ContractFileName ?? "").Trim();
        var effectiveFileName = flagOrgCreate == "1" ? fileListFileName : topContractFileName;

        // Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:2521, 2561); đích không có phiên (bài học A.24)
        // Null-safe: test gọi thẳng controller (không qua HTTP) thì HttpContext/Request = null (A.24).
        var waUserCode = StdParam(model?.WAUserCode)
            ?? StdParam(req?.WAUserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "PORT";

        var nowStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // biz:32688-32695(ApprovedX)/35213-35237(PartySignX) Contract_Contract_CheckDB(code, "1" (Yes), "PENDING, ONPROCESS")
            // → không tồn tại hoặc status ngoài danh sách ⇒ lỗi.
            //   Mã dùng chung với guard FlagChecker (biz:32744) theo cách #22/#25 đã chốt (excerpt:7991).
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
            if (string.IsNullOrEmpty(effectiveFileName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_InvalidContractFileName",
                    "Check.ContractCode", contractCode,
                    "Check.ContractFileName", effectiveFileName);
            }

            // biz:32769-32780 ContractNo NOT NULL
            if (string.IsNullOrEmpty(contract.ContractNo))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Contract_Contract_Approved_ContractNoIsNotNull",
                    "Check.ContractCode", contractCode,
                    "Check.ContractNo", contract.ContractNo);
            }

            // biz:32914-33017(ApprovedX)/35429-35493(PartySignX) UPDATE Contract_Contract (chỉ các cột đích có)
            contract.ContractStatus = "ONPROCESS";
            contract.ContractFilePath = effectiveFileName;
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

            // biz:33092-33134 Check PartySign: mọi party CONFIRMED (hoặc không có party) → ContractStatus='CONFIRMED'
            var anyNotConfirmed = await _db.Set<Contract_ContractParty>()
                .AnyAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(contractCode), PortQuery.Esc)
                            && !EF.Functions.Like((x.ContractPartyStatus ?? "").ToLower(), PortQuery.Equal("CONFIRMED"), PortQuery.Esc));
            if (!anyNotConfirmed)
            {
                contract.ContractStatus = "CONFIRMED";
                contract.LogLUDTimeUTC = nowStr;
                contract.LogLUBy = waUserCode;
            }

            // biz:33154-33199 Contract_Contract_HistActionX (FunctionActionType="APPROVED", Desc="Ký Hợp đồng")
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

            // Gate :2588-2593 — Success=true + AddMessage + RedirectUrl Detail
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

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    /// <summary>CUtils.StdFlag của nguồn: "1" → "1", mọi giá trị khác kể cả null → "0".</summary>
    private static string StdFlag(object? value)
    {
        var s = value?.ToString()?.Trim();
        return s == "1" ? "1" : "0";
    }

    /// <summary>CUtils.StrValue của nguồn: null -> "", KHÔNG Trim/ToUpper.</summary>
    private static string? StrValue(object? value) => value?.ToString() ?? "";
}

/// <summary>Tham số của gate Contract_ContractController.cs:2449 (ApprovedAndSignServer_Old) — ContractModel.</summary>
public class Contract_Contract_ApprovedAndSignServerOldReq
{
    /// <summary>Chuỗi JSON ContractModel (đường dẫn thô của gate: `model`).</summary>
    public string? Model { get; set; }

    // Các trường ContractModel (gate:2492, 2530-2534, 2574-2576)
    public string? ContractCode { get; set; }

    /// <summary>Gate :2576 — ContractFileName cấp cao nhất của ContractModel (dùng ở nhánh PartySign).</summary>
    public string? ContractFileName { get; set; }
    public string? ContractFileVersion { get; set; }
    public string? SignedFileContent { get; set; }
    public string? FlagElement { get; set; }
    public string? FlagCheckOTP { get; set; }
    public string? FlagOrgCreate { get; set; }

    /// <summary>Gate :2493 — FileList[0].FileName dùng ở nhánh Approved (FlagOrgCreate=="1").</summary>
    public List<Contract_Contract_ApprovedAndSignServerOldFile>? FileList { get; set; }

    /// <summary>Người thực hiện — nguồn lấy UserState.SysUser.UserCode (gate:2521, 2561); đích không có phiên (bài học A.24).</summary>
    public string? WAUserCode { get; set; }
}

/// <summary>Phần tử FileList của ContractModel (gate :2493-2494).</summary>
public class Contract_Contract_ApprovedAndSignServerOldFile
{
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
}
