using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 ESignController.cs — Contract_ContractParty_Cancel
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ESignController.cs:2058-2096 (Contract_ContractParty_Cancel)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\ContractContractPartyController.cs:20-88 (WA_Contract_ContractParty_Cancel)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\zTemp.cs:3056-3129 (WAS_Contract_ContractParty_Cancel)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\zTemp.cs:2675-2862 (Contract_ContractParty_Cancel)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\zTemp.cs:1911-2674 (Contract_ContractParty_CancelX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9313-9378 (Contract_Contract_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:39692-39762 (Contract_ContractParty_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:40201-40360 (Contract_Contract_HistActionX)
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
