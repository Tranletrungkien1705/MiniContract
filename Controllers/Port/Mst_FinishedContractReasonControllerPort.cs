using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_FinishedContractReasonController.cs — Mst_FinishedContractReason
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_FinishedContractReasonController.cs:21-85
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstFinishedContractReasonController.cs:20-88
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:45559-45655, 45656-45812, 46529-46685
[ApiController]
[Route("api/mst_finishedcontractreason")]
public class Mst_FinishedContractReasonControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_FinishedContractReasonControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tìm kiếm và phân trang danh sách nguyên nhân chấm dứt hợp đồng.
    /// Gate: Mst_FinishedContractReasonController.cs:21-85 Index(string init = "init", int recordcount = 10, int page = 0)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_IndexReq? req)
    {
        // 1. Dựng truy vấn cơ sở từ bảng Mst_FinishedContractReason (Master.cs:46581-46592)
        var query = _db.Set<Mst_FinishedContractReason>().AsNoTracking();

        // 2. Bộ lọc điều kiện theo các cột hỗ trợ (Master.cs:46640-46664)
        var networkId = req?.NetworkID?.Trim();
        if (!string.IsNullOrEmpty(networkId))
        {
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
        }

        var reasonCode = req?.ContractFinishReasonCode?.Trim();
        if (!string.IsNullOrEmpty(reasonCode))
        {
            query = query.Where(x => EF.Functions.Like((x.ContractFinishReasonCode ?? "").ToLower(), PortQuery.Equal(reasonCode), PortQuery.Esc));
        }

        var keyWord = req?.KeyWord?.Trim() ?? req?.ContractFinishReasonName?.Trim();
        if (!string.IsNullOrEmpty(keyWord))
        {
            query = query.Where(x => EF.Functions.Like((x.ContractFinishReasonName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
        }

        var flagActive = req?.FlagActive?.Trim();
        if (!string.IsNullOrEmpty(flagActive))
        {
            query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
        }

        var finishType = req?.FinishType?.Trim();
        if (!string.IsNullOrEmpty(finishType))
        {
            query = query.Where(x => EF.Functions.Like((x.FinishType ?? "").ToLower(), PortQuery.Equal(finishType), PortQuery.Esc));
        }

        // 3. Đếm tổng số bản ghi phù hợp (Master.cs:46594-46596: select Count(0) MyCount from ...)
        var itemCount = await query.CountAsync();

        // 4. Sắp xếp: order by mo.NetworkID asc (Master.cs:46591, 46631)
        var ordered = query.OrderBy(x => x.NetworkID).ThenBy(x => x.ContractFinishReasonCode);

        // 5. Tính toán phân trang (Gate: Mst_FinishedContractReasonController.cs:21, 56-57; BIZ: Master.cs:46562-46563, 46604-46605)
        var recordcount = req?.RecordCount ?? req?.Ft_RecordCount ?? req?.Ft_PageSize ?? req?.PageSize ?? 10;
        var page = req?.Page ?? req?.Ft_PageIndex ?? req?.PageIndex ?? 0;

        long recordStart;
        if (req != null && req.Ft_RecordStart != null && req.Page == null && req.Ft_PageIndex == null && req.PageIndex == null)
        {
            recordStart = req.Ft_RecordStart.Value;
            page = recordcount > 0 ? (int)(recordStart / recordcount) : 0;
        }
        else
        {
            recordStart = (long)page * recordcount;
        }

        // 6. Lấy dữ liệu trang (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
        var list = (recordcount <= 0 || recordStart < 0)
            ? new List<Mst_FinishedContractReason>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordcount, int.MaxValue))
                           .ToListAsync();

        // 7. Tính số trang (Gate: Mst_FinishedContractReasonController.cs:71: (itemCount % PageSizeConfig == 0) ? ... : ... + 1)
        const int pageSizeConfig = 10;
        var ps = recordcount > 0 ? recordcount : pageSizeConfig;
        var pageCount = list.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // 8. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
        return Ok(new
        {
            Success = true,
            Data = new PageInfo<Mst_FinishedContractReason>(list, itemCount, pageCount, page, recordcount)
        });
    }
}

/// <summary>
/// DTO tham số đầu vào cho action Index.
/// Hỗ trợ cả tên tham số của tầng Gate MVC (page, recordcount, init) lẫn các tên chuẩn hóa phân trang/lọc.
/// </summary>
public class Mst_FinishedContractReason_IndexReq
{
    // Tham số tầng Gate MVC (Mst_FinishedContractReasonController.cs:21)
    public string? Init { get; set; } = "init";
    public int? RecordCount { get; set; }
    public int? Page { get; set; }

    // Các biến phân trang thay thế
    public int? Ft_RecordCount { get; set; }
    public int? Ft_PageSize { get; set; }
    public int? PageSize { get; set; }
    public int? Ft_PageIndex { get; set; }
    public int? PageIndex { get; set; }
    public long? Ft_RecordStart { get; set; }

    // Các tham số lọc nghiệp vụ
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? ContractFinishReasonName { get; set; }
    public string? KeyWord { get; set; }
    public string? FinishType { get; set; }
    public string? FlagActive { get; set; }
    public string? Ft_WhereClause { get; set; }
}
