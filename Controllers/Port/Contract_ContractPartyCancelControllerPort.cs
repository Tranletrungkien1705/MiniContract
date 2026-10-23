using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract (Contract_ContractParty_Cancel)
// Gate:  Contract_ContractController.cs:1987-2030 (Contract_ContractParty_Cancel)
//        + BaseController.cs:6667-6737 (ReturnApiUserCreateContract)
//        + BaseController.cs:6739-6760 (strWhereClause_Contract_ContractParty)
// API:   ContractContractPartyController.cs:20-88 (WA_Contract_ContractParty_Cancel)
// BIZ:   zTemp.cs:3056-3129 (WAS_Contract_ContractParty_Cancel)
//        -> zTemp.cs:2675-2862 (Contract_ContractParty_Cancel)
//        -> zTemp.cs:1911-2674 (Contract_ContractParty_CancelX)
//        + QContract.cs:9313-9378 (Contract_Contract_CheckDB)
//        + QContract.cs:39692-39762 (Contract_ContractParty_CheckDB)
//        + QContract.cs:40201-40360 (Contract_Contract_HistActionX)
//
// A.21: gate nhận `string contractcode` (hoặc body JSON); ở đích body là DTO phẳng/lồng.
//
// GAP — KHÔNG port phần gọi dịch vụ ngoài (PORT_RULES_MiniContract §1 + LESSONS_AGENTS B.7):
//   - ReturnApiUserCreateContract (gate:1998, BaseController.cs:6667-6737) — gọi WA_Contract_ContractParty_Get và WA_MstSv_Sys_User_Login
//   - WA_Contract_ContractParty_Cancel (gate:2016) — gọi HTTP sang apiUserCreateContract nếu có
//   - OSiNOSSv.NotificationService.CreateNotifications / SendNotification (biz:2568, 2637) — gửi thông báo qua iNOS / Firebase push
//   - Contract_Contract_GetInfoPartyX & Contract_Contract_SendContractToPartyX_New20221101 (biz:2781-2807) — dev nguồn đã comment lại
//
// Phần port ĐƯỢC (parity 1-1 phần nội bộ):
//   (1) Guard CheckDB Contract: tồn tại và ContractStatus = 'ONPROCESS' (QContract.cs:9313-9378, zTemp.cs:1966-1972);
//   (2) Guard CheckDB ContractParty: tồn tại theo ContractCode + MST (QContract.cs:39692-39762, zTemp.cs:1974-1981);
//   (3) UPDATE Contract_Contract SET ContractStatus = 'CANCELED', CancelDTimeUTC, LogLUDTimeUTC, LogLUBy (zTemp.cs:2038-2060, 2102-2125);
//   (4) UPDATE Contract_ContractParty SET ContractPartyStatus = 'CANCELED', CancelDTimeUTC, CancelBy, LogLUDTimeUTC, LogLUBy (zTemp.cs:2067-2095, 2127-2163);
//   (5) Ghi Contract_Contract_HistActionX FunctionActionType = 'PartyCancel', Desc = 'Hủy hợp đồng' (zTemp.cs:2210-2226, QContract.cs:40201-40360).
[ApiController]
[Route("api/contract_contract")]
public class Contract_ContractPartyCancelControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Contract_ContractPartyCancelControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Hủy hợp đồng từ phía đối tác ký (Contract_ContractParty_Cancel).
    /// Gate: Contract_ContractController.cs:1987-2030
    /// API:  ContractContractPartyController.cs:20-88 WA_Contract_ContractParty_Cancel
    /// BIZ:  zTemp.cs:1911-2674 Contract_ContractParty_CancelX
    /// </summary>
    [HttpPost("contract_contractparty_cancel")]
    public async Task<IActionResult> Contract_ContractParty_Cancel(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Contract_Contract_ContractPartyCancelReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc null (bài học A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Contract_ContractParty_Cancel", "Check.strJson", null);
        }

        var contractCode = StdParam(req.ContractCode) ?? StdParam(req.Contractcode);

        // Hỗ trợ trường hợp client truyền model JSON dạng chuỗi
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                using var doc = JsonDocument.Parse(req.Model);
                if (string.IsNullOrEmpty(contractCode))
                {
                    if (doc.RootElement.TryGetProperty("ContractCode", out var ccProp))
                    {
                        contractCode = StdParam(ccProp.GetString());
                    }
                    else if (doc.RootElement.TryGetProperty("contractcode", out var ccProp2))
                    {
                        contractCode = StdParam(ccProp2.GetString());
                    }
                }
                if (string.IsNullOrEmpty(req.MST) && doc.RootElement.TryGetProperty("MST", out var mstProp))
                {
                    req.MST = mstProp.GetString();
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrEmpty(contractCode))
        {
            return PortResult.Fail("Contract_ContractParty_Cancel", "Check.strJson", null);
        }

        var strContractCode = contractCode;
        var strMST = StdParam(req.MST);

        // A.14 / A.24: không có phiên đăng nhập ở đích -> WAUserCode tuỳ chọn (body hoặc header); mặc định "port"
        var waUserCode = StdParam(req.WAUserCode)
            ?? StdParam(req.UserCode)
            ?? StdParam(HttpContext?.Request?.Headers["WAUserCode"].FirstOrDefault())
            ?? "port";

        // 2. Kiểm tra DB: Contract_Contract_CheckDB (zTemp.cs:1966-1972, QContract.cs:9313-9378)
        var contract = await _db.Set<Contract_Contract>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode), PortQuery.Esc));

        if (contract == null)
        {
            // QContract.cs:9345: TError.ErridQContract.Contract_Contract_CheckDB_OrganNotFound
            return PortResult.Fail("Contract_Contract_CheckDB_OrganNotFound", "Check.ContractCode", strContractCode);
        }

        // QContract.cs:9365-9376: kiểm tra ContractStatus thuộc danh sách "ONPROCESS"
        if (!string.Equals(contract.ContractStatus, "ONPROCESS", StringComparison.OrdinalIgnoreCase))
        {
            // QContract.cs:9373: TError.ErridQContract.Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched
            return PortResult.Fail("Contract_Contract_CheckDB_CheckDB_ContractStatusNotMatched",
                "Check.ContractCode", strContractCode,
                "Check.strContractListToCheck", "ONPROCESS",
                "DB.ContractStatus", contract.ContractStatus);
        }

        // 3. Kiểm tra DB: Contract_ContractParty_CheckDB (zTemp.cs:1974-1981, QContract.cs:39692-39762)
        // Nếu strMST chưa có (gate lấy từ UserState.Mst_NNT.MST), tìm party của hợp đồng
        if (string.IsNullOrEmpty(strMST))
        {
            var defaultParty = await _db.Set<Contract_ContractParty>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode), PortQuery.Esc))
                .FirstOrDefaultAsync();
            if (defaultParty != null)
            {
                strMST = defaultParty.MST;
            }
        }

        var party = await _db.Set<Contract_ContractParty>()
            .FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode), PortQuery.Esc) &&
                EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST ?? ""), PortQuery.Esc));

        if (party == null)
        {
            // QContract.cs:39728: TError.ErridQContract.Contract_ContractParty_CheckDB_OrganNotFound
            return PortResult.Fail("Contract_ContractParty_CheckDB_OrganNotFound",
                "Check.ContractCode", strContractCode,
                "Check.MST", strMST);
        }

        var strCustomerNameParty = party.CustomerName ?? "";

        // 4. Mở Transaction cập nhật dữ liệu (zTemp.cs:2719, 2817)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // Cập nhật Contract_Contract (zTemp.cs:2038-2060, 2102-2125)
            contract.ContractStatus = "CANCELED";
            contract.CancelDTimeUTC = dtimeSys;
            contract.LogLUDTimeUTC = dtimeSys;
            contract.LogLUBy = waUserCode;

            // Cập nhật Contract_ContractParty (zTemp.cs:2067-2095, 2127-2163)
            party.ContractPartyStatus = "CANCELED";
            party.CancelDTimeUTC = dtimeSys;
            party.CancelBy = waUserCode;
            party.LogLUDTimeUTC = dtimeSys;
            party.LogLUBy = waUserCode;

            // Ghi lịch sử Contract_Contract_HistActionX (zTemp.cs:2210-2226, QContract.cs:40201-40360)
            var lastHist = await _db.Set<Contract_Contract_HistAction>()
                .Where(x => EF.Functions.Like((x.ContractCode ?? "").ToLower(), PortQuery.Equal(strContractCode), PortQuery.Esc))
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

            int nextVersion = (lastHist?.Version ?? 0) + 1;

            var hist = new Contract_Contract_HistAction
            {
                ContractCode = strContractCode,
                Version = nextVersion,
                UserCode = waUserCode,
                UserName = strCustomerNameParty,
                Description = "Hủy hợp đồng",
                LogLUDTimeUTC = dtimeSys,
                LogLUBy = waUserCode,
                Remark = "",
                FunctionName = "Contract_ContractParty_CancelX",
                FunctionActionType = "PartyCancel",
                FunctionRemark = ""
            };
            _db.Set<Contract_Contract_HistAction>().Add(hist);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // GAP: Bỏ qua dịch vụ ngoài bắn Notification iNOS / Firebase push (zTemp.cs:2568, 2637)

            // 5. Trả kết quả thành công khớp gate MVC Contract_ContractController.cs:2018-2022
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Hủy hợp đồng thành công" },
                RedirectUrl = $"/Contract_Contract/Detail?contractcode={strContractCode}",
                Data = new
                {
                    ContractCode = strContractCode,
                    ContractStatus = contract.ContractStatus,
                    ContractPartyStatus = party.ContractPartyStatus,
                    CancelDTimeUTC = contract.CancelDTimeUTC,
                    CancelBy = party.CancelBy,
                    HistActionVersion = hist.Version,
                    HistActionType = hist.FunctionActionType,
                    HistActionDescription = hist.Description
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Contract_ContractParty_Cancel", "Exception", ex.Message);
        }
    }

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }
}

/// <summary>
/// Tham số đầu vào cho action Contract_ContractParty_Cancel.
/// Gate: Contract_ContractController.cs:1987 (string contractcode).
/// Hỗ trợ cả ContractCode/contractcode, MST, WAUserCode, Model dạng chuỗi.
/// </summary>
public class Contract_Contract_ContractPartyCancelReq
{
    public string? Contractcode { get; set; }
    public string? ContractCode { get; set; }
    public string? MST { get; set; }
    public string? WAUserCode { get; set; }
    public string? UserCode { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? Model { get; set; }
}
