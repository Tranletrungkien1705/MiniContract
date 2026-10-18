using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Map_UserInNotifyTypeController.cs — Map_UserInNotifyType
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Map_UserInNotifyTypeController.cs:20-92 (Index)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MapUserInNotifyTypeController.cs:92-160 (WA_Map_UserInNotifyType_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstNNTController.cs:20-89 (WA_Mst_NNT_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:56083-56200 (WAS_Map_UserInNotifyType_Get) -> 56201-56331 (Map_UserInNotifyType_Get) -> 56333-56616 (Map_UserInNotifyType_GetX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:12121-12218 (WAS_Mst_NNT_Get) -> Mst_NNT_Get
[ApiController]
[Route("api/map_userinnotifytype")]
public class Map_UserInNotifyTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Map_UserInNotifyTypeControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Map_UserInNotifyTypeController.cs:20-92.
    /// Chuỗi gọi:
    /// 1. Mst_NNTService.Instance.WA_Mst_NNT_Get (gate dòng 54)
    ///    -> API WA_Mst_NNT_Get -> WAS_Mst_NNT_Get -> Mst_NNT_Get (Master.cs:12121-12218).
    ///    Lấy danh sách đơn vị, gán vào ViewBag.ListMst_NNT.
    /// 2. Dựng biểu thức strWhereClauseSys_UserSearch theo Sys_User (gate dòng 62-66, 203-236).
    /// 3. Map_UserInNotifyTypeService.Instance.WA_Map_UserInNotifyType_Get (gate dòng 90)
    ///    -> API WA_Map_UserInNotifyType_Get -> WAS_Map_UserInNotifyType_Get -> Map_UserInNotifyType_Get -> Map_UserInNotifyType_GetX (Master.cs:56083-56616).
    ///    Lấy thông tin Map_UserInNotifyType, Mst_NotifyType, Mst_ManageNotify.
    /// 4. Trả về View(objRT_Map_UserInNotifyType) với model chứa các danh sách trên (gate dòng 91).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Map_UserInNotifyType_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (LESSONS A.14, A.21, PORT_RULES §3)
            var strMST = StdParam(req?.MST);
            var strUserName = StdStr(req?.Email ?? req?.UserName);
            var strUserCode = StdParam(req?.UserCode);
            var strNotifyType = StdParam(req?.NotifyType);
            var strNetworkID = StdParam(req?.NetworkID);
            var strOrgID = StdParam(req?.OrgID);

            // 2. Chuẩn hoá tham số phân trang (LESSONS A.7, A.12)
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;
            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 3. Lấy danh sách đơn vị Mst_NNT (gate dòng 31-60; BIZ Master.cs:12121-12218)
            var nntQuery = _db.Set<Mst_NNT>().AsNoTracking();
            if (!string.IsNullOrEmpty(strNetworkID))
            {
                nntQuery = nntQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(strNetworkID), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(strOrgID))
            {
                nntQuery = nntQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc));
            }
            var listMst_NNT = await nntQuery.OrderBy(x => x.MST).ToListAsync();

            // 4. Dựng truy vấn Map_UserInNotifyType theo Map_UserInNotifyType_GetX (Master.cs:56389-56594)
            var query = _db.Set<Map_UserInNotifyType>().AsNoTracking();

            if (!string.IsNullOrEmpty(strUserCode))
            {
                query = query.Where(mo => EF.Functions.Like((mo.UserCode ?? "").ToLower(), PortQuery.Equal(strUserCode), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(strNotifyType))
            {
                query = query.Where(mo => EF.Functions.Like((mo.NotifyType ?? "").ToLower(), PortQuery.Equal(strNotifyType), PortQuery.Esc));
            }

            // Lọc theo điều kiện người dùng từ Sys_User (tương ứng strWhereClause_Sys_UserSearch trong gate dòng 62-66, 203-236)
            if (!string.IsNullOrEmpty(strMST) || !string.IsNullOrEmpty(strUserName))
            {
                var users = _db.Set<Sys_User>().AsNoTracking();
                if (!string.IsNullOrEmpty(strMST))
                {
                    users = users.Where(u => EF.Functions.Like((u.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));
                }
                if (!string.IsNullOrEmpty(strUserName))
                {
                    users = users.Where(u => EF.Functions.Like((u.EMail ?? "").ToLower(), PortQuery.Contains(strUserName), PortQuery.Esc)
                                          || EF.Functions.Like((u.UserName ?? "").ToLower(), PortQuery.Contains(strUserName), PortQuery.Esc));
                }
                query = query.Where(mo => users.Any(u => u.UserCode == mo.UserCode));
            }

            // 5. Đếm tổng số bản ghi (Master.cs:56411 select Count(0) MyCount)
            var itemCount = await query.CountAsync();

            // 6. Sắp xếp theo UserCode asc, NotifyType asc (Master.cs:56407 order by mo.UserCode asc)
            var ordered = query.OrderBy(x => x.UserCode).ThenBy(x => x.NotifyType);

            // 7. Lấy danh sách trang (LESSONS A.7, A.12)
            var pagedList = (recordCount <= 0 || recordStart < 0)
                ? new List<Map_UserInNotifyType>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 8. Lấy các loại thông báo Mst_NotifyType liên quan của trang (Master.cs:56466-56486)
            var pagedNotifyTypes = pagedList.Select(x => x.NotifyType).Distinct().ToList();
            var listMst_NotifyType = pagedNotifyTypes.Count == 0
                ? new List<Mst_NotifyType>()
                : await _db.Set<Mst_NotifyType>().AsNoTracking()
                    .Where(x => pagedNotifyTypes.Contains(x.NotifyType))
                    .OrderBy(x => x.NotifyType)
                    .ToListAsync();

            // 9. Lấy thông tin người dùng quản lý thông báo Mst_ManageNotify liên quan của trang (Master.cs:56495-56518)
            var pagedUserCodes = pagedList.Select(x => x.UserCode).Distinct().ToList();
            var listMst_ManageNotify = pagedUserCodes.Count == 0
                ? new List<Mst_ManageNotify>()
                : await _db.Set<Mst_ManageNotify>().AsNoTracking()
                    .Where(x => pagedUserCodes.Contains(x.UserCode))
                    .OrderBy(x => x.UserCode)
                    .ToListAsync();

            // 10. Tính số trang
            var ps = pageSize <= 0 ? 10 : pageSize;
            var pageCount = pagedList.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

            // 11. Đóng gói kết quả khớp RT_Map_UserInNotifyType và ViewBag.ListMst_NNT của gate MVC
            var data = new RT_Map_UserInNotifyType
            {
                MySummaryTable = new MySummaryTable { MyCount = itemCount },
                Lst_Map_UserInNotifyType = pagedList,
                Lst_Mst_NotifyType = listMst_NotifyType,
                Lst_Mst_ManageNotify = listMst_ManageNotify,
                ListMst_NNT = listMst_NNT,
                ItemCount = itemCount,
                PageCount = pageCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Map_UserInNotifyType_Get", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action Index của Map_UserInNotifyTypeController.
/// </summary>
public class Map_UserInNotifyType_IndexReq
{
    public string? MST { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? UserCode { get; set; }
    public string? NotifyType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// Bảng tóm tắt số lượng bản ghi tương ứng MySummaryTable trong WARTBase nguồn.
/// </summary>
public class MySummaryTable
{
    public object MyCount { get; set; } = 0;
}

/// <summary>
/// Cấu trúc kết quả trả về tương ứng RT_Map_UserInNotifyType từ nguồn BIZ và ViewBag.ListMst_NNT từ gate.
/// </summary>
public class RT_Map_UserInNotifyType
{
    public MySummaryTable? MySummaryTable { get; set; }
    public List<Map_UserInNotifyType> Lst_Map_UserInNotifyType { get; set; } = new();
    public List<Mst_NotifyType> Lst_Mst_NotifyType { get; set; } = new();
    public List<Mst_ManageNotify> Lst_Mst_ManageNotify { get; set; } = new();
    public List<Mst_NNT> ListMst_NNT { get; set; } = new();
    public List<Map_UserInNotifyType> DataList => Lst_Map_UserInNotifyType;
    public int ItemCount { get; set; }
    public int PageCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}
