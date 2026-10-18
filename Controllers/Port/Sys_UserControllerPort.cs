using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Sys_UserController.cs — Sys_User
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:25-110 (Index)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:1962-1982 (strWhereClause_SysUserName)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysUserController.cs:199-270 (WA_Sys_User_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:7011-7123 (WAS_Sys_User_Get) -> 6635-7009 (Sys_User_Get_New20191104)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.Mix.cs:30-79 (Sys_User_GetAbilityViewOfUser)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.Mix.cs:1990-2067 (zzzzClauseSelect_Mst_NNT_ViewAbility_Get_20220823)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.Mix.cs:631-1114 (Sys_User_CheckAuthorize - iNOS account service ngoài -> GAP)
[ApiController]
[Route("api/sys_user")]
public class Sys_UserControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    // Hằng số che mật khẩu từ D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Constants\Const.Main.BE.cs:13
    private const string Default_PasswordMask = "*********";

    public Sys_UserControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Sys_UserController.cs:25-110.
    /// Chuỗi gọi:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NGUOI_DUNG") (gate dòng 27-31).
    /// 2. Khởi tạo phân trang: recordcount = 10, page = 0, gán init = "search" (gate dòng 36).
    /// 3. Dựng điều kiện tìm kiếm từ username qua strWhereClause_SysUserName(objSys_User) (gate dòng 54-61, 1962-1982):
    ///    UserName like '%username%' or PhoneNo like '%username%' or EMail like '%username%'.
    /// 4. Đóng gói RQ_Sys_User (gate dòng 62-83) và gọi Sys_UserService.Instance.WA_Sys_User_Get
    ///    -> API WA_Sys_User_Get (SysUserController.cs:199-270)
    ///    -> BIZ WAS_Sys_User_Get (System.cs:7011-7123)
    ///    -> BIZ Sys_User_Get_New20191104 (System.cs:6635-7009).
    /// 5. Các bước nghiệp vụ tầng BIZ:
    ///    - Kiểm tra quyền xem của người dùng: Sys_User_GetAbilityViewOfUser(strWAUserCode) (System.cs:6733, System.Mix.cs:30-79).
    ///      Nếu người dùng strWAUserCode không tồn tại -> ném lỗi Sys_User_BizInvalidUserAbility.
    ///    - Xác định phạm vi đơn vị #tbl_Mst_NNT_ViewAbility qua zzzzClauseSelect_Mst_NNT_ViewAbility_Get_20220823 (System.Mix.cs:1990-2067):
    ///      Nếu DealerType == "NPP" -> lấy các Mst_NNT có DealerType = 'NPP'.
    ///      Ngược lại -> lấy các Mst_NNT theo MSTBUPattern like drAbilityOfUser["MNNTMSTBUPattern"].
    ///    - Lọc Sys_User inner join #tbl_Mst_NNT_ViewAbility theo MST, left join Mst_Department, Sys_UserInGroup, Sys_Group (System.cs:474-492).
    ///    - Lọc theo strWhereClause (nếu có username thì tìm theo UserName/PhoneNo/EMail).
    ///    - Đếm tổng số bản ghi MyCount (= itemCount) (System.cs:495).
    ///    - Phân trang: Skip(page * recordcount).Take(recordcount) (System.cs:504-505).
    ///    - Thay thế UserPassword bằng Default_PasswordMask ("*********") (System.cs:6816).
    ///    - Điền mdept_DepartmentCode, mdept_DepartmentName từ Mst_Department và OrgID từ Mst_NNT (System.cs:6837-6846).
    /// 6. Gate gán: pageCount, pageSize, pageIndex, itemCount và trả về View(pageInfo) (gate dòng 94-109).
    /// </summary>
    [HttpPost("index")]
    [HttpPost("search")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_User_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Sys_UserController.cs:25:
            var recordcount = req?.RecordCount ?? req?.PageSize ?? 10;
            var page = req?.Page ?? req?.PageIndex ?? 0;

            // 2. Chuẩn hoá các tham số lọc (PORT_RULES §3, LESSONS_AGENTS A.14)
            var waUserCode = StdParam(req?.WAUserCode)
                ?? StdParam(req?.UserCode)
                ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault());

            var keyword = StdStr(req?.UserName) ?? StdStr(req?.KeyWord);
            var reqUserCode = StdParam(req?.UserCode);
            var reqMST = StdParam(req?.MST);
            var reqDeptCode = StdParam(req?.DepartmentCode);
            var reqNetworkId = StdParam(req?.NetworkID);
            var reqOrgId = StdParam(req?.OrgID);
            var reqFlagActive = req?.FlagActive != null ? StdFlag(req.FlagActive) : null;

            // 3. Kiểm tra quyền xem của người dùng thực hiện (Sys_User_GetAbilityViewOfUser, System.Mix.cs:30-79)
            // Nếu có waUserCode được truyền (từ header hoặc body), phải kiểm tra tồn tại trong Sys_User
            IQueryable<string>? visibleMstsQuery = null;
            if (!string.IsNullOrEmpty(waUserCode))
            {
                var callerUser = await _db.Set<Sys_User>().AsNoTracking()
                    .FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc));

                if (callerUser == null)
                {
                    // Guard: Mã lỗi TError.ErridQContract.Sys_User_BizInvalidUserAbility (System.Mix.cs:74)
                    return PortResult.Fail("Sys_User_BizInvalidUserAbility");
                }

                // Xác định phạm vi ViewAbility (zzzzClauseSelect_Mst_NNT_ViewAbility_Get_20220823, System.Mix.cs:1990-2067)
                var callerMST = StdParam(callerUser.MST);
                var callerNnt = !string.IsNullOrEmpty(callerMST)
                    ? await _db.Set<Mst_NNT>().AsNoTracking()
                        .FirstOrDefaultAsync(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(callerMST), PortQuery.Esc))
                    : null;

                if (callerNnt != null && string.Equals(callerNnt.DealerType?.Trim(), "NPP", StringComparison.OrdinalIgnoreCase))
                {
                    visibleMstsQuery = _db.Set<Mst_NNT>().AsNoTracking()
                        .Where(n => EF.Functions.Like((n.DealerType ?? "").ToLower(), PortQuery.Equal("NPP"), PortQuery.Esc))
                        .Select(n => n.MST);
                }
                else if (callerNnt != null && !string.IsNullOrEmpty(callerNnt.MSTBUPattern))
                {
                    var pattern = PortQuery.Contains(callerNnt.MSTBUPattern);
                    visibleMstsQuery = _db.Set<Mst_NNT>().AsNoTracking()
                        .Where(n => EF.Functions.Like((n.MSTBUPattern ?? "").ToLower(), pattern, PortQuery.Esc))
                        .Select(n => n.MST);
                }
                else if (!string.IsNullOrEmpty(callerMST))
                {
                    visibleMstsQuery = _db.Set<Mst_NNT>().AsNoTracking()
                        .Where(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(callerMST), PortQuery.Esc))
                        .Select(n => n.MST);
                }
            }

            // 4. Dựng truy vấn cơ sở từ bảng Sys_User (System.cs:478-492)
            var query = _db.Set<Sys_User>().AsNoTracking();

            // Áp dụng phạm vi ViewAbility nếu có
            if (visibleMstsQuery != null)
            {
                query = query.Where(u => visibleMstsQuery.Contains(u.MST ?? ""));
            }

            // Lọc theo username: strWhereClause_SysUserName (Sys_UserController.cs:1962-1982)
            // UserName like '%username%' or PhoneNo like '%username%' or EMail like '%username%'
            if (!string.IsNullOrEmpty(keyword))
            {
                var kwPattern = PortQuery.Contains(keyword);
                query = query.Where(u =>
                    EF.Functions.Like((u.UserName ?? "").ToLower(), kwPattern, PortQuery.Esc) ||
                    EF.Functions.Like((u.PhoneNo ?? "").ToLower(), kwPattern, PortQuery.Esc) ||
                    EF.Functions.Like((u.EMail ?? "").ToLower(), kwPattern, PortQuery.Esc));
            }

            // Các bộ lọc chi tiết nếu được chỉ định
            if (!string.IsNullOrEmpty(reqMST))
            {
                query = query.Where(u => EF.Functions.Like((u.MST ?? "").ToLower(), PortQuery.Equal(reqMST), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(reqDeptCode))
            {
                query = query.Where(u => EF.Functions.Like((u.DepartmentCode ?? "").ToLower(), PortQuery.Equal(reqDeptCode), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(reqNetworkId))
            {
                query = query.Where(u => EF.Functions.Like((u.NetworkID ?? "").ToLower(), PortQuery.Equal(reqNetworkId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(reqOrgId))
            {
                query = query.Where(u => EF.Functions.Like((u.OrgID ?? "").ToLower(), PortQuery.Equal(reqOrgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(reqFlagActive))
            {
                query = query.Where(u => EF.Functions.Like((u.FlagActive ?? "").ToLower(), PortQuery.Equal(reqFlagActive), PortQuery.Esc));
            }

            // 5. Sắp xếp theo UserCode asc (System.cs:491)
            var ordered = query.OrderBy(u => u.UserCode);

            // 6. Đếm tổng số bản ghi (Summary: select Count(0) MyCount from #tbl_Sys_User_Filter_Draft, System.cs:495)
            var itemCount = await ordered.CountAsync();

            // 7. Phân trang dữ liệu (System.cs:504-505, LESSONS_AGENTS A.12)
            var recordStart = (long)page * recordcount;
            var list = (recordcount <= 0 || recordStart < 0)
                ? new List<Sys_User>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordcount, int.MaxValue))
                               .ToListAsync();

            // 8. Bổ sung thông tin cột nối và che mật khẩu (System.cs:6816, 6837-6846)
            if (list.Count > 0)
            {
                // Danh sách phòng ban để nối thông tin mdept_DepartmentCode, mdept_DepartmentName
                var deptCodes = list
                    .Where(u => !string.IsNullOrEmpty(u.DepartmentCode))
                    .Select(u => u.DepartmentCode!.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                var depts = await _db.Set<Mst_Department>().AsNoTracking()
                    .Where(d => deptCodes.Contains(d.DepartmentCode.ToUpper()))
                    .ToDictionaryAsync(d => d.DepartmentCode.Trim().ToUpperInvariant(), d => d);

                // Danh sách NNT để lấy OrgID (System.cs:6839)
                var msts = list
                    .Where(u => !string.IsNullOrEmpty(u.MST))
                    .Select(u => u.MST!.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                var nnts = await _db.Set<Mst_NNT>().AsNoTracking()
                    .Where(n => msts.Contains(n.MST.ToUpper()))
                    .ToDictionaryAsync(n => n.MST.Trim().ToUpperInvariant(), n => n);

                foreach (var user in list)
                {
                    // Che mật khẩu bằng hằng số Default_PasswordMask (System.cs:6816)
                    user.UserPassword = Default_PasswordMask;

                    // Nối phòng ban (System.cs:6837-6838)
                    var uDeptCode = user.DepartmentCode?.Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(uDeptCode) && depts.TryGetValue(uDeptCode, out var dept))
                    {
                        user.mdept_DepartmentCode = dept.DepartmentCode;
                        user.mdept_DepartmentName = dept.DepartmentName;
                    }

                    // Nối OrgID từ Mst_NNT (System.cs:6839)
                    var uMST = user.MST?.Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(uMST) && nnts.TryGetValue(uMST, out var nnt))
                    {
                        user.OrgID = nnt.OrgID ?? user.OrgID;
                    }
                }
            }

            // 9. Tính số trang (Gate: Sys_UserController.cs:94: (itemCount % recordcount == 0) ? itemCount / recordcount : itemCount / recordcount + 1)
            const int pageSizeConfig = 10;
            var ps = recordcount > 0 ? recordcount : pageSizeConfig;
            var pageCount = list.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

            // 10. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSONS_AGENTS A.5, A.11)
            return Ok(new
            {
                Success = true,
                Data = new PageInfo<Sys_User>(list, itemCount, pageCount, page, recordcount)
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_User_Get", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().ToUpperInvariant();
    }

    private static string? StdStr(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().Normalize();
    }

    private static string StdFlag(string? s)
    {
        return s?.Trim() == "1" ? "1" : "0";
    }
}

/// <summary>
/// Model nhận tham số tìm kiếm danh sách người dùng cho action Index.
/// Khớp với chữ ký gate: Index(string username, string init = "init", int recordcount = 10, int page = 0)
/// và các trường lọc mở rộng trong RQ_Sys_User.
/// </summary>
public class Sys_User_IndexReq
{
    public string? UserName { get; set; }
    public string? KeyWord { get; set; }
    public string? UserCode { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? MST { get; set; }
    public string? DepartmentCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? FlagActive { get; set; }
    public string? Init { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public string? WAUserCode { get; set; }
}
