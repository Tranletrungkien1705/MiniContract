using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Sys_GroupController.cs — Sys_Group
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:22-102 (Index)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:1093-1104 (strWhereClause_SysGroupName)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysGroupController.cs:20-91 (WA_Sys_Group_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:2283-2397 (WAS_Sys_Group_Get) -> 1966-2281 (Sys_Group_Get_New20191102)
[ApiController]
[Route("api/sys_group")]
public class Sys_GroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Sys_GroupControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Sys_GroupController.cs:22-102.
    /// Chuỗi gọi:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NHOM_NGUOI_DUNG") (gate dòng 24-28).
    /// 2. Gán init = "search" (gate dòng 33).
    /// 3. Khởi tạo PageInfo&lt;Sys_Group&gt; với itemCount = 0, pageCount = 0 (gate dòng 40-45).
    /// 4. Dựng strWhereClause theo Sys_Group.GroupName: strWhereClause_SysGroupName (gate dòng 49-55, 1093-1104).
    /// 5. Gọi Sys_GroupService.Instance.WA_Sys_Group_Get (gate dòng 79)
    ///    -> API WA_Sys_Group_Get (SysGroupController.cs:20-91)
    ///    -> BIZ WAS_Sys_Group_Get (System.cs:2283-2397)
    ///    -> Sys_Group_Get_New20191102 (System.cs:1966-2281).
    ///    SQL: select distinct sg.GroupCode into #tbl_Sys_Group_Filter_Draft from Sys_Group sg ... order by sg.GroupCode asc.
    ///    Phân trang: #tbl_Sys_Group_Filter với MyIdxSeq between nFilterRecordStart and nFilterRecordEnd.
    ///    Lấy dữ liệu: select t.MyIdxSeq, sg.* from #tbl_Sys_Group_Filter t inner join Sys_Group sg on t.GroupCode = sg.GroupCode order by t.MyIdxSeq asc.
    /// 6. Gán pageInfo.DataList, pageInfo.ItemCount, pageInfo.PageCount (gate dòng 80-88, 96-99).
    /// 7. Trả về View(pageInfo) (gate dòng 101).
    /// </summary>
    [HttpPost("index")]
    [HttpPost("search")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Sys_GroupController.cs:22, 69-70
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Chuẩn hoá tham số tìm kiếm (LESSONS A.14, A.21, PORT_RULES §3)
            var groupName = StdStr(req?.GroupName);
            var groupCode = StdParam(req?.GroupCode);
            var flagActive = req?.FlagActive != null ? StdFlag(req.FlagActive) : null;
            var mst = StdParam(req?.MST);
            var keyWord = StdStr(req?.KeyWord);

            // 3. Dựng truy vấn cơ sở từ bảng Sys_Group (System.cs:2081, 2133)
            var query = _db.Set<Sys_Group>().AsNoTracking();

            // 4. Lọc theo GroupName (Gate dòng 49-54, 1093-1104: strWhereClause_SysGroupName)
            if (!string.IsNullOrEmpty(groupName))
            {
                query = query.Where(x => EF.Functions.Like((x.GroupName ?? "").ToLower(), PortQuery.Contains(groupName), PortQuery.Esc));
            }

            // Lọc theo GroupCode nếu có (System.cs:2181-2184 htSpCols)
            if (!string.IsNullOrEmpty(groupCode))
            {
                query = query.Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
            }

            // Lọc theo FlagActive nếu có (System.cs:2181-2184 htSpCols)
            if (!string.IsNullOrEmpty(flagActive))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            // Lọc theo MST nếu có (System.cs:2083: inner join #tbl_Mst_NNT_ViewAbility on sg.MST = t_MstNNT_View.MST)
            if (!string.IsNullOrEmpty(mst))
            {
                query = query.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            // Lọc theo KeyWord nếu có
            if (!string.IsNullOrEmpty(keyWord))
            {
                query = query.Where(x => EF.Functions.Like((x.GroupName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)
                                      || EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
            }

            // 5. Sắp xếp: order by sg.GroupCode asc (System.cs:2091)
            query = query.OrderBy(x => x.GroupCode);

            // 6. Tổng số dòng (Summary: select Count(0) MyCount from #tbl_Sys_Group_Filter_Draft, System.cs:2095)
            var itemCount = await query.CountAsync();

            // 7. Phân trang theo mẫu chuẩn LESSON A.12, A.7
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Sys_Group>()
                : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                             .Take((int)Math.Min(recordCount, int.MaxValue))
                             .ToListAsync();

            // 8. Tính PageCount: khi rỗng hoặc itemCount == 0 thì PageCount = 0 (Gate dòng 45), có dòng thì tính theo cỡ trang (Gate dòng 87)
            var pageCount = 0;
            if (list.Count > 0 && pageSize > 0)
            {
                pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
            }

            var pageInfo = new PageInfo<Sys_Group>(list, itemCount, pageCount, pageIndex, pageSize);

            return Ok(new
            {
                Success = true,
                Data = pageInfo
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Group_Get", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action Index của Sys_GroupController.
/// </summary>
public class Sys_Group_IndexReq
{
    // Tham số từ Gate Sys_GroupController.cs:22 Index(string groupname, string init = "init", int recordcount = 10, int page = 0)
    public string? GroupName { get; set; }

    public string? Init { get; set; }

    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }

    public int? Page { get; set; }
    public int? PageIndex { get; set; }

    // Tham số phân trang tầng BIZ System.cs:1978-1979
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Tham số lọc bổ sung theo BIZ htSpCols (System.cs:2181-2184)
    public string? GroupCode { get; set; }
    public string? FlagActive { get; set; }
    public string? MST { get; set; }
    public string? KeyWord { get; set; }
}
