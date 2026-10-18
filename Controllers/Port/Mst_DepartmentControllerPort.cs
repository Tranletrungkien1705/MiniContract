using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_DepartmentController.cs — Mst_Department
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_DepartmentController.cs:24-87 Index(string init = "init", int recordcount = 10, int page = 0)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstDepartmentController.cs:20-89 WA_Mst_Department_Get
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:35737-35835 (WAS_Mst_Department_Get), 35836-35980 (Mst_Department_Get), 36641-36794 (Mst_Department_GetX)
[ApiController]
[Route("api/mst_department")]
public class Mst_DepartmentControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_DepartmentControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Danh sách / Tìm kiếm phân trang phòng ban.
    /// Gate: Mst_DepartmentController.cs:24-87 Index(string init = "init", int recordcount = 10, int page = 0)
    /// API:  MstDepartmentController.cs:20-89 WA_Mst_Department_Get
    /// BIZ:  Master.cs:35737-35835 (WAS_Mst_Department_Get), 35836-35980 (Mst_Department_Get), 36641-36794 (Mst_Department_GetX)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Department_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_DepartmentController.cs:24:
            var recordcount = req?.RecordCount ?? req?.PageSize ?? 10;
            var page = req?.Page ?? req?.PageIndex ?? 0;

            // 2. Kiểm tra cờ init: nếu init == "search" (Gate: Mst_DepartmentController.cs:39, 75-78),
            // gate gán lại init = "init", không gọi backend và trả DataList rỗng, ItemCount = 0, PageCount = 0.
            if (string.Equals(req?.Init, "search", StringComparison.OrdinalIgnoreCase))
            {
                var emptyPage = new PageInfo<Mst_Department>(new List<Mst_Department>(), 0, 0, page, recordcount);
                return Ok(new
                {
                    Success = true,
                    Data = emptyPage
                });
            }

            // 3. Dựng truy vấn cơ sở từ bảng Mst_Department (Master.cs:36698)
            var query = _db.Set<Mst_Department>().AsNoTracking();

            // 4. Lọc theo các cột của Mst_Department (Master.cs:36749-36773 BuildWhere)
            var deptCode = req?.DepartmentCode?.Trim();
            if (!string.IsNullOrEmpty(deptCode))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(deptCode), PortQuery.Esc));
            }

            var deptName = req?.DepartmentName?.Trim();
            if (!string.IsNullOrEmpty(deptName))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentName ?? "").ToLower(), PortQuery.Contains(deptName), PortQuery.Esc));
            }

            var keyWord = req?.KeyWord?.Trim();
            if (!string.IsNullOrEmpty(keyWord))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)
                                      || EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
            }

            var flagActive = req?.FlagActive?.Trim();
            if (!string.IsNullOrEmpty(flagActive))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            var mst = req?.MST?.Trim();
            if (!string.IsNullOrEmpty(mst))
            {
                query = query.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            var networkId = req?.NetworkID?.Trim();
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var parentCode = req?.DepartmentCodeParent?.Trim();
            if (!string.IsNullOrEmpty(parentCode))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentCodeParent ?? "").ToLower(), PortQuery.Equal(parentCode), PortQuery.Esc));
            }

            var buCode = req?.DepartmentBUCode?.Trim();
            if (!string.IsNullOrEmpty(buCode))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentBUCode ?? "").ToLower(), PortQuery.Equal(buCode), PortQuery.Esc));
            }

            var deptLevel = req?.DepartmentLevel?.Trim();
            if (!string.IsNullOrEmpty(deptLevel))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentLevel ?? "").ToLower(), PortQuery.Equal(deptLevel), PortQuery.Esc));
            }

            // 5. Đếm tổng số bản ghi (Master.cs:36705: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 6. Sắp xếp: order by mdept.DepartmentCode asc (Master.cs:36701, 36740)
            var ordered = query.OrderBy(x => x.DepartmentCode);

            // 7. Xác định vị trí phân trang (Gate: Mst_DepartmentController.cs:56-57; BIZ: Master.cs:36673-36674, 36714-36715)
            long recordStart;
            long recordCount;
            if (req != null && req.Ft_RecordStart != null)
            {
                recordStart = req.Ft_RecordStart.Value;
                recordCount = req.Ft_RecordCount ?? recordcount;
            }
            else
            {
                recordStart = (long)page * recordcount;
                recordCount = recordcount;
            }

            // 8. Lấy dữ liệu trang (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Department>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 9. Tính số trang (Gate: Mst_DepartmentController.cs:71: (itemCount % PageSizeConfig == 0) ? ... : ... + 1)
            const int pageSizeConfig = 10;
            var ps = recordcount > 0 ? recordcount : pageSizeConfig;
            var pageCount = list.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

            // 10. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
            return Ok(new
            {
                Success = true,
                Data = new PageInfo<Mst_Department>(list, itemCount, pageCount, page, recordcount)
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Department_Get", "Exception", ex.Message);
        }
    }
}

/// <summary>
/// DTO tham số cho action Index của Mst_DepartmentController.
/// </summary>
public class Mst_Department_IndexReq
{
    // Tham số tầng gate Mst_DepartmentController.cs:24
    public string? Init { get; set; } = "init";
    public int? RecordCount { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }

    // Tham số lọc và phân trang tầng BIZ Master.cs:36649-36655
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
    public string? Ft_WhereClause { get; set; }

    // Các cột lọc bảng Mst_Department (Master.cs:36753-36759)
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public string? MST { get; set; }
    public string? NetworkID { get; set; }
    public string? DepartmentCodeParent { get; set; }
    public string? DepartmentBUCode { get; set; }
    public string? DepartmentLevel { get; set; }
}
