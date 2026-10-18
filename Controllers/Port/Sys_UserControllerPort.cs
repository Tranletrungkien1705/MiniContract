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
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:114-196 (Create GET View)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:200-367 (Create POST)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:373-464 (LoadMstCustomer)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:468-473 (StopService)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:477-632 (Update GET View)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:636-725 (Edit POST)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:1936-1947 (strWhereClause_SysUser)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:1962-1982 (strWhereClause_SysUserName)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:1984-2020 (strWhereClause_Mst_NNT)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_UserController.cs:2060-2087 (strWhereClause_Mst_Customer)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:4026-4060 (List_Mst_NNT)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:5417-5427 (WA_Mst_Customer_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:5497-5525 (List_Mst_Department_Task)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:5532-5565 (List_Mst_DepartmentExt_Async)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysUserController.cs:199-270 (WA_Sys_User_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstNNTController.cs:20-89 (WA_Mst_NNT_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstDepartmentController.cs:20-89 (WA_Mst_Department_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstCustomerController.cs:92-162 (WA_Mst_Customer_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysUserController.cs:421-492 (WA_Sys_User_Create)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysUserController.cs:569-640 (WA_Sys_User_Update)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:7011-7123 (WAS_Sys_User_Get) -> 6635-7009 (Sys_User_Get_New20191104)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:12121-12218 (WAS_Mst_NNT_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:35737-35835 (WAS_Mst_Department_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Customer\Customer.cs:1464-1612 (WAS_Mst_Customer_Get) -> 996-1462 (Mst_Customer_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:9907-9950 (WAS_Sys_User_Create) -> 7863-8027 (Sys_User_Create_New20200110)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:11620-11715 (WAS_Sys_User_Update) -> 11201-11618 (Sys_User_Update_New20231115)
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

    /// <summary>
    /// Port 1:1 action Create từ gate Sys_UserController.cs:114-196 (GET View Create) và 200-367 (POST Create).
    /// Chuỗi gọi GET Create:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NGUOI_DUNG_TAO_MOI") (gate dòng 116-120).
    /// 2. Lấy UserState: waMST, isSysadmin (FlagSysAdmin), isParent = (NetworkID == OrgID) (gate dòng 121-134).
    /// 3. Mã số thuế Mst_NNT (gate dòng 136-155):
    ///    - Nếu isSysadmin == "0" (Inactive): strWhereClause_Mst_NNT theo MST, OrgID, FlagActive = "1" (gate dòng 145-148, 1984-2020).
    ///    - Nếu isSysadmin == "1": strWhereClause_Mst_NNT = null (lấy tất cả NNT active, gate dòng 149-152).
    ///    - Gọi Mst_NNTService.Instance.WA_Mst_NNT_Get -> API WA_Mst_NNT_Get (MstNNTController.cs:20-89)
    ///      -> BIZ WAS_Mst_NNT_Get (Master.cs:12121-12218) -> Mst_NNT_Get.
    /// 4. Danh sách phòng ban Mst_Department (gate dòng 157-193):
    ///    - Điều kiện: TblMst_Department.FlagActive = '1' (gate dòng 158-166).
    ///    - Gọi List_Mst_DepartmentExt_Async (gate dòng 190, BaseController.cs:5532-5565)
    ///      -> List_Mst_Department_Task (BaseController.cs:5497-5525)
    ///      -> API WA_Mst_Department_Get (MstDepartmentController.cs:20-89)
    ///      -> BIZ WAS_Mst_Department_Get (Master.cs:35737-35835) -> Mst_Department_Get.
    ///    - Dựng cây Mst_DepartmentExtension.ToGroupBaseTree và làm phẳng có phân cấp thụt lề HLevel qua Mst_DepartmentExtension.ToFlatGroupBaseTree (BaseController.cs:5557-5562).
    /// 5. Trả về View(new Sys_User()) kèm ViewBag.ListMst_NNTCur, ViewBag.ListMst_Department, ViewBag.IsParent (gate dòng 193-195).
    ///
    /// Chuỗi gọi POST Create (lưu tài khoản người dùng mới khi có dữ liệu người dùng, Sys_UserController.cs:200-367):
    /// 6. BIZ WAS_Sys_User_Create (System.cs:9907-9950) -> Sys_User_Create_New20200110 (System.cs:7863-8027):
    ///    - Kiểm tra UserCode không rỗng (Sys_User_Create_InvalidUserCode).
    ///    - Kiểm tra trùng UserCode (Sys_User_CheckDB_UserExist).
    ///    - Kiểm tra MST thuộc Mst_NNT (Mst_NNT_CheckDB_NNTNotFound).
    ///    - Mở transaction, lưu Sys_User mới, commit.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_User_CreateReq? req)
    {
        // 1. Phân biệt nhánh lưu dữ liệu người dùng (POST Save) và nhánh khởi tạo màn hình (GET View Create)
        var isSaveRequest = req != null && (
            string.Equals(req.Save, "1", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrWhiteSpace(req.Model) ||
            (!string.IsNullOrWhiteSpace(req.UserCode) && req.IsInit != true && !string.Equals(req.Init, "init", StringComparison.OrdinalIgnoreCase))
        );

        if (isSaveRequest)
        {
            return await CreateUserCoreAsync(req!);
        }

        return await InitCreateViewAsync(req);
    }

    private async Task<IActionResult> InitCreateViewAsync(Sys_User_CreateReq? req)
    {
        try
        {
            // 1. Chuẩn hoá các tham số ngữ cảnh người dùng gọi (Gate Sys_UserController.cs:121-134)
            var waUserCode = StdParam(req?.WAUserCode)
                ?? StdParam(req?.UserCodeCaller)
                ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault());

            Sys_User? callerUser = null;
            if (!string.IsNullOrEmpty(waUserCode))
            {
                callerUser = await _db.Set<Sys_User>().AsNoTracking()
                    .FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc));
            }

            var waMST = StdParam(req?.MST) ?? StdParam(callerUser?.MST) ?? "0100000001";
            var waOrgId = StdParam(req?.OrgID) ?? StdParam(callerUser?.OrgID) ?? _config?["Biz_OrgID"] ?? "7200000001";
            var networkId = StdParam(req?.NetworkID) ?? StdParam(callerUser?.NetworkID) ?? _config?["Biz_NetworkID"] ?? "1001";

            // Xác định isSysadmin (Gate Sys_UserController.cs:128)
            var isSysadmin = !string.IsNullOrEmpty(req?.IsSysAdmin)
                ? StdFlag(req.IsSysAdmin)
                : (!string.IsNullOrEmpty(req?.FlagSysAdmin)
                    ? StdFlag(req.FlagSysAdmin)
                    : (callerUser?.FlagSysAdmin == "1" ? "1" : "0"));

            // 2. Xác định isParent = NetworkID == OrgID (Gate Sys_UserController.cs:129-134)
            var isParent = string.Equals(networkId, waOrgId, StringComparison.OrdinalIgnoreCase);

            // 3. Truy vấn danh sách Mst_NNT (Gate Sys_UserController.cs:136-155, BIZ Master.cs:12121-12218)
            var nntQuery = _db.Set<Mst_NNT>().AsNoTracking()
                .Where(n => n.FlagActive == "1");

            if (isSysadmin == "0")
            {
                // Khi không phải SysAdmin: lọc theo MST và OrgID của người dùng hiện tại (Sys_UserController.cs:1984-2020)
                if (!string.IsNullOrEmpty(waMST))
                {
                    nntQuery = nntQuery.Where(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(waMST), PortQuery.Esc));
                }
                if (!string.IsNullOrEmpty(waOrgId))
                {
                    nntQuery = nntQuery.Where(n => EF.Functions.Like((n.OrgID ?? "").ToLower(), PortQuery.Equal(waOrgId), PortQuery.Esc));
                }
            }

            var listMst_NNTCur = await nntQuery.OrderBy(n => n.MST).ToListAsync();

            // 4. Truy vấn danh sách Mst_Department với FlagActive = '1' (Gate Sys_UserController.cs:157-193, BIZ Master.cs:35737-35835)
            var deptListRaw = await _db.Set<Mst_Department>().AsNoTracking()
                .Where(d => d.FlagActive == "1")
                .OrderBy(d => d.DepartmentCode)
                .ToListAsync();

            // Chuyển đổi sang Mst_DepartmentExt và tổ chức cây phân cấp thụt lề HLevel (BaseController.cs:5532-5565)
            var listMst_DepartmentExt = new List<Mst_DepartmentExt>();
            foreach (var item in deptListRaw)
            {
                listMst_DepartmentExt.Add(new Mst_DepartmentExt
                {
                    DepartmentCode = item.DepartmentCode,
                    NetworkID = item.NetworkID,
                    DepartmentCodeParent = item.DepartmentCodeParent,
                    DepartmentBUCode = item.DepartmentBUCode,
                    DepartmentBUPattern = item.DepartmentBUPattern,
                    DepartmentLevel = item.DepartmentLevel,
                    MST = item.MST,
                    DepartmentName = item.DepartmentName,
                    FlagActive = item.FlagActive,
                    LogLUDTimeUTC = item.LogLUDTimeUTC,
                    LogLUBy = item.LogLUBy
                });
            }

            var tree = Mst_DepartmentExtension.ToGroupBaseTree(listMst_DepartmentExt);
            var flatTree = Mst_DepartmentExtension.ToFlatGroupBaseTree(tree);

            // 5. Trả về kết quả khởi tạo màn hình khớp ViewBag và Model của gate MVC (Sys_UserController.cs:193-195)
            return Ok(new
            {
                Success = true,
                Data = new Sys_User_CreateInitData
                {
                    IsParent = isParent,
                    ListMst_NNTCur = listMst_NNTCur,
                    ListMst_Department = flatTree,
                    Sys_User = new Sys_User()
                }
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("WAS_Mst_Department_Get", "Exception", ex.Message);
        }
    }

    private async Task<IActionResult> CreateUserCoreAsync(Sys_User_CreateReq req)
    {
        // 1. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Sys_UserController.cs:200, 213)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_User_CreateReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.UserCode ??= modelObj.UserCode;
                    req.UserName ??= modelObj.UserName;
                    req.UserPassword ??= modelObj.UserPassword;
                    req.PhoneNo ??= modelObj.PhoneNo;
                    req.EMail ??= modelObj.EMail;
                    req.MST ??= modelObj.MST;
                    req.DepartmentCode ??= modelObj.DepartmentCode;
                    req.Position ??= modelObj.Position;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.FlagSysAdmin ??= modelObj.FlagSysAdmin;
                    req.FlagNNTAdmin ??= modelObj.FlagNNTAdmin;
                    req.FlagDLAdmin ??= modelObj.FlagDLAdmin;
                    req.OrgID ??= modelObj.OrgID;
                    req.NetworkID ??= modelObj.NetworkID;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        // 2. Chuẩn hoá tham số đầu vào theo BIZ System.cs:8846-8860
        var strUserCode = StdParam(req.UserCode);
        if (string.IsNullOrEmpty(strUserCode))
        {
            return PortResult.Fail("Sys_User_Create_InvalidUserCode", "Check.strUserCode", req.UserCode);
        }

        var strUserName = req.UserName != null ? string.Format("{0}", req.UserName).Trim() : strUserCode;
        var strPassword = !string.IsNullOrWhiteSpace(req.UserPassword) ? req.UserPassword.Trim() : (_config?["PassDefault"] ?? "123456");
        var strPhoneNo = req.PhoneNo != null ? string.Format("{0}", req.PhoneNo).Trim() : "";
        var strEmail = !string.IsNullOrWhiteSpace(req.EMail) ? req.EMail.Trim() : strUserCode;
        var strMST = StdParam(req.MST) ?? "0100000001";
        var strDeptCode = StdParam(req.DepartmentCode);
        var strPosition = req.Position != null ? string.Format("{0}", req.Position).Trim() : "";
        var strFlagActive = req.FlagActive != null ? StdFlag(req.FlagActive) : "1";
        var strFlagSysAdmin = req.FlagSysAdmin != null ? StdFlag(req.FlagSysAdmin) : "0";
        var strFlagNNTAdmin = req.FlagNNTAdmin != null ? StdFlag(req.FlagNNTAdmin) : "0";
        var strFlagDLAdmin = "0"; // Gate Sys_UserController.cs:214: objSys_UserInput.FlagDLAdmin = "0"

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Guard: Kiểm tra trùng mã UserCode (System.cs:8877-8883 Sys_User_CheckDB với FlagExist="0")
            var userExists = await _db.Set<Sys_User>().AnyAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(strUserCode), PortQuery.Esc));
            if (userExists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_CheckDB_UserExist", "Check.UserCode", strUserCode);
            }

            // Guard: Kiểm tra tồn tại MST trong Mst_NNT (System.cs:8911-8918 Mst_NNT_CheckDB với FlagExist="1", FlagActive="1")
            var nnt = await _db.Set<Mst_NNT>().FirstOrDefaultAsync(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));
            if (nnt == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMST);
            }

            var networkId = StdParam(req.NetworkID) ?? nnt.NetworkID ?? _config?["Biz_NetworkID"] ?? "1001";
            var orgId = StdParam(req.OrgID) ?? nnt.OrgID ?? _config?["Biz_OrgID"] ?? "7200000001";

            var newUser = new Sys_User
            {
                UserCode = strUserCode,
                UserName = strUserName,
                UserPassword = strPassword,
                PhoneNo = strPhoneNo,
                EMail = strEmail,
                MST = strMST,
                DepartmentCode = strDeptCode,
                Position = strPosition,
                NetworkID = networkId,
                OrgID = orgId,
                FlagActive = strFlagActive,
                FlagSysAdmin = strFlagSysAdmin,
                FlagNNTAdmin = strFlagNNTAdmin,
                FlagDLAdmin = strFlagDLAdmin,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            _db.Set<Sys_User>().Add(newUser);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Messages = new[] { "Tạo tài khoản thành công!" },
                RedirectUrl = "/Sys_User",
                Data = newUser
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_User_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action LoadMstCustomer từ gate Sys_UserController.cs:373-464.
    /// Chuỗi gọi:
    /// 1. Lấy UserState: waUserCode, waMST, isSysadmin = FlagSysAdmin, isParent = (NetworkID == OrgID) (gate dòng 375-389).
    /// 2. Khởi tạo đối tượng Mst_NNT với MST = mst, FlagActive = "1", OrgID = UserState.Mst_NNT.OrgID (gate dòng 392-397).
    /// 3. Dựng strWhereClauseMst_NNTCur qua strWhereClause_Mst_NNT (Sys_UserController.cs:1984-2020):
    ///    - Nếu isSysadmin == "0": lọc theo MST = mst, FlagActive = "1", OrgID = UserState.Mst_NNT.OrgID.
    ///    - Nếu isSysadmin == "1": strWhereClauseMst_NNTCur = null (lấy toàn bộ Mst_NNT hoạt động).
    ///    - Gọi List_Mst_NNT(strWhereClauseMst_NNTCur) -> API WA_Mst_NNT_Get -> BIZ WAS_Mst_NNT_Get (gate dòng 408).
    /// 4. Tìm objMst_NNT = listMst_NNTCur.Where(x => x.MST.Equals(mst)).FirstOrDefault() (gate dòng 415).
    /// 5. Nếu objMst_NNT != null:
    ///    - Dựng strWhereClause_Mst_Customer (Sys_UserController.cs:2060-2087) với FlagDealer = "1", OrgID = objMst_NNT.OrgID, FlagActive = "1".
    ///    - Gọi Mst_CustomerService.Instance.WA_Mst_Customer_Get -> API WA_Mst_Customer_Get (MstCustomerController.cs:92-162)
    ///      -> BIZ WAS_Mst_Customer_Get (Customer.cs:1464-1612) -> Mst_Customer_Get.
    ///    - Lấy danh sách Lst_Mst_Customer đưa vào listMst_Customer (gate dòng 446-450).
    /// 6. Trả về JsonView("LoadMst_Customer", listMst_Customer) (gate dòng 456).
    ///    Khi không tìm thấy objMst_NNT hoặc body rỗng: listMst_Customer rỗng [].
    /// 7. Bắt ngoại lệ nếu có: trả về lỗi với mã mặc định TError.ErridQContract.WAS_Mst_Customer_Get (Customer.cs:1481).
    /// </summary>
    [HttpPost("loadmstcustomer")]
    [HttpPost("loadmst_customer")]
    public async Task<IActionResult> LoadMstCustomer([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_User_LoadMstCustomerReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (PORT_RULES §3)
            var strMST = StdParam(req?.MST) ?? StdParam(Request?.Query["mst"].FirstOrDefault());
            var waUserCode = StdParam(req?.WAUserCode) ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault());

            Sys_User? callerUser = null;
            if (!string.IsNullOrEmpty(waUserCode))
            {
                callerUser = await _db.Set<Sys_User>().AsNoTracking()
                    .FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc));
            }

            var waOrgId = StdParam(req?.OrgID) ?? StdParam(callerUser?.OrgID) ?? _config?["Biz_OrgID"] ?? "7200000001";
            var networkId = StdParam(req?.NetworkID) ?? StdParam(callerUser?.NetworkID) ?? _config?["Biz_NetworkID"] ?? "1001";

            // Xác định isSysadmin (Gate Sys_UserController.cs:381)
            var isSysadmin = !string.IsNullOrEmpty(req?.IsSysAdmin)
                ? StdFlag(req.IsSysAdmin)
                : (!string.IsNullOrEmpty(req?.FlagSysAdmin)
                    ? StdFlag(req.FlagSysAdmin)
                    : (callerUser?.FlagSysAdmin == "1" ? "1" : "0"));

            // 2. Truy vấn danh sách Mst_NNT (Gate Sys_UserController.cs:391-408)
            var nntQuery = _db.Set<Mst_NNT>().AsNoTracking()
                .Where(n => n.FlagActive == "1");

            if (isSysadmin == "0")
            {
                // Khi không phải SysAdmin: lọc theo OrgID của đơn vị người dùng hiện tại (Sys_UserController.cs:1984-2020)
                if (!string.IsNullOrEmpty(waOrgId))
                {
                    nntQuery = nntQuery.Where(n => EF.Functions.Like((n.OrgID ?? "").ToLower(), PortQuery.Equal(waOrgId), PortQuery.Esc));
                }
                if (!string.IsNullOrEmpty(strMST))
                {
                    nntQuery = nntQuery.Where(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));
                }
            }

            var listMst_NNTCur = await nntQuery.ToListAsync();

            // 3. Tìm objMst_NNT = listMst_NNTCur.Where(x => x.MST.Equals(mst)).FirstOrDefault() (Gate Sys_UserController.cs:415)
            var listMst_Customer = new List<Mst_Customer>();
            if (!string.IsNullOrEmpty(strMST) && listMst_NNTCur.Count > 0)
            {
                var objMst_NNT = listMst_NNTCur.FirstOrDefault(x => string.Equals(x.MST, strMST, StringComparison.OrdinalIgnoreCase));
                if (objMst_NNT != null)
                {
                    // 4. Truy vấn danh sách Mst_Customer với FlagDealer = '1', OrgID = objMst_NNT.OrgID, FlagActive = '1'
                    // (Gate Sys_UserController.cs:418-450, BIZ Customer.cs:1464-1612)
                    var targetOrgId = StdParam(objMst_NNT.OrgID);
                    var custQuery = _db.Set<Mst_Customer>().AsNoTracking()
                        .Where(c => c.FlagActive == "1" && c.FlagDealer == "1");

                    if (!string.IsNullOrEmpty(targetOrgId))
                    {
                        custQuery = custQuery.Where(c => EF.Functions.Like((c.OrgID ?? "").ToLower(), PortQuery.Equal(targetOrgId), PortQuery.Esc));
                    }

                    listMst_Customer = await custQuery.OrderBy(c => c.CustomerCodeSys).ToListAsync();
                }
            }

            // 5. Trả về cấu trúc Success / Data chứa List<Mst_Customer> (Gate: JsonView("LoadMst_Customer", listMst_Customer))
            return Ok(new
            {
                Success = true,
                Data = listMst_Customer
            });
        }
        catch (Exception ex)
        {
            // BIZ Customer.cs:1481: strErrorCodeDefault = TError.ErridQContract.WAS_Mst_Customer_Get
            return PortResult.Fail("WAS_Mst_Customer_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action StopService từ gate Sys_UserController.cs:468-473.
    /// Chuỗi gọi:
    /// 1. Action HttpGet StopService trả về View() thông báo tạm dừng dịch vụ.
    ///    Không gọi API/Biz nào, không truy vấn cơ sở dữ liệu, không có tham số đầu vào.
    /// 2. Ở API Port: trả về Success = true, Data = null theo chuẩn PortJsonResultFilter.
    /// </summary>
    [HttpGet("stopservice")]
    [HttpPost("stopservice")]
    public IActionResult StopService([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_User_StopServiceReq? req = null)
    {
        return Ok(new
        {
            Success = true,
            Data = (object?)null
        });
    }

    /// <summary>
    /// Port 1:1 action Update từ gate Sys_UserController.cs:477-632.
    /// Chuỗi gọi:
    /// 1. SysAccessCheckDeny(UserState, "MENU_QL_NGUOI_DUNG_SUA") (gate dòng 479-483).
    /// 2. Lấy UserState: waUserCode, waMST, isSysadmin = FlagSysAdmin, isParent = (NetworkID == OrgID) (gate dòng 484-498).
    /// 3. Nếu usercode không rỗng:
    ///    - Dựng strWhereClause = strWhereClause_SysUser(usercode) -> Sys_User.UserCode = usercode (gate dòng 502-503, 1936-1947).
    ///    - Đóng gói RQ_Sys_User và gọi Sys_UserService.Instance.WA_Sys_User_Get (gate dòng 505-528)
    ///      -> API WA_Sys_User_Get (SysUserController.cs:199-270)
    ///      -> BIZ WAS_Sys_User_Get (System.cs:7011-7123)
    ///      -> BIZ Sys_User_Get_New20191104 (System.cs:6635-7009).
    ///    - Tầng BIZ:
    ///      + Kiểm tra quyền xem người dùng Sys_User_GetAbilityViewOfUser(strWAUserCode) (System.Mix.cs:30-79).
    ///        Nếu strWAUserCode không tồn tại -> ném lỗi Sys_User_BizInvalidUserAbility (System.Mix.cs:74).
    ///      + Xác định phạm vi đơn vị ViewAbility (zzzzClauseSelect_Mst_NNT_ViewAbility_Get_20220823, System.Mix.cs:1990-2067).
    ///      + Lọc Sys_User theo UserCode và MST trong phạm vi ViewAbility (System.cs:6755-6848).
    ///      + Che mật khẩu bằng Default_PasswordMask ("*********") (System.cs:6816).
    ///      + Nối phòng ban mdept_DepartmentCode, mdept_DepartmentName và OrgID từ Mst_NNT (System.cs:6837-6846).
    /// 4. Nếu tìm thấy người dùng (Lst_Sys_User.Count > 0) (gate dòng 530-629):
    ///    - Danh sách NNT (ViewBag.ListMst_NNTCur):
    ///      + Nếu isSysadmin == "0": lọc theo strWhereClause_Mst_NNT (MST = waMST, FlagActive = "1", OrgID = waOrgId) (gate dòng 541-544, 1984-2020).
    ///      + Nếu isSysadmin == "1": strWhereClauseMst_NNTCur = null (lấy toàn bộ NNT active) (gate dòng 545-548).
    ///      + Gọi List_Mst_NNT -> API WA_Mst_NNT_Get -> BIZ WAS_Mst_NNT_Get (Master.cs:12121-12218).
    ///    - Danh sách phòng ban (ViewBag.ListMst_Department):
    ///      + Lọc FlagActive = "1" (gate dòng 558-562).
    ///      + Gọi List_Mst_DepartmentExt_Async (BaseController.cs:5532-5565), dựng cây Mst_DepartmentExtension.ToGroupBaseTree và làm phẳng có thụt lề HLevel qua Mst_DepartmentExtension.ToFlatGroupBaseTree.
    ///    - Danh sách khách hàng đại lý (ViewBag.ListMst_Customer):
    ///      + Lọc theo strWhereClause_Mst_Customer với FlagDealer = "1", OrgID = waOrgId, FlagActive = "1" (gate dòng 589-596, 2060-2087).
    ///      + Gọi WA_Mst_Customer_Get -> BIZ WAS_Mst_Customer_Get (Customer.cs:1464-1612).
    ///    - Trả về View(objRT_Sys_UserCur.Lst_Sys_User[0]) kèm ViewBag.
    ///      Ở API Port trả về Success = true, Data = Sys_User_UpdateData.
    /// 5. Nếu không tìm thấy người dùng hoặc usercode rỗng:
    ///    - Trả về View(new Sys_User()) (gate dòng 631).
    ///      Ở API Port trả về Success = true, Data = null.
    /// 6. Bắt ngoại lệ catch (Exception ex) -> PortResult.Fail("Sys_User_Get", "Exception", ex.Message).
    /// </summary>
    [HttpGet("update")]
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_User_UpdateReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào usercode (Gate: Sys_UserController.cs:477, 500)
            var userCode = StdParam(req?.UserCode);
            if (string.IsNullOrEmpty(userCode) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("usercode", out var qUserCode))
            {
                userCode = StdParam(qUserCode.ToString());
            }

            // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
            if (string.IsNullOrEmpty(userCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_User_UpdateReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    userCode = StdParam(modelObj?.UserCode);
                }
                catch
                {
                    userCode = StdParam(req.Model);
                }
            }

            // 2. Nếu usercode rỗng hoặc null, gate trả về View(new Sys_User()) (Sys_UserController.cs:500, 631)
            if (string.IsNullOrEmpty(userCode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Sys_User_UpdateData?)null
                });
            }

            // 3. Chuẩn hoá thông tin ngữ cảnh người dùng gọi (Gate: Sys_UserController.cs:484-498)
            var waUserCode = StdParam(req?.WAUserCode)
                ?? StdParam(req?.UserCodeCaller)
                ?? StdParam(Request?.Headers["WAUserCode"].FirstOrDefault());

            Sys_User? callerUser = null;
            IQueryable<string>? visibleMstsQuery = null;
            if (!string.IsNullOrEmpty(waUserCode))
            {
                callerUser = await _db.Set<Sys_User>().AsNoTracking()
                    .FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc));

                if (callerUser == null)
                {
                    // Guard: Mã lỗi TError.ErridQContract.Sys_User_BizInvalidUserAbility (System.Mix.cs:74)
                    return PortResult.Fail("Sys_User_BizInvalidUserAbility");
                }

                // Xác định phạm vi ViewAbility (zzzzClauseSelect_Mst_NNT_ViewAbility_Get_20220823, System.Mix.cs:1990-2067)
                var callerMSTFromUser = StdParam(callerUser.MST);
                var callerNnt = !string.IsNullOrEmpty(callerMSTFromUser)
                    ? await _db.Set<Mst_NNT>().AsNoTracking()
                        .FirstOrDefaultAsync(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(callerMSTFromUser), PortQuery.Esc))
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
                else if (!string.IsNullOrEmpty(callerMSTFromUser))
                {
                    visibleMstsQuery = _db.Set<Mst_NNT>().AsNoTracking()
                        .Where(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(callerMSTFromUser), PortQuery.Esc))
                        .Select(n => n.MST);
                }
            }

            var waMST = StdParam(req?.MST) ?? StdParam(callerUser?.MST) ?? "0100000001";
            var waOrgId = StdParam(req?.OrgID) ?? StdParam(callerUser?.OrgID) ?? _config?["Biz_OrgID"] ?? "7200000001";
            var networkId = StdParam(req?.NetworkID) ?? StdParam(callerUser?.NetworkID) ?? _config?["Biz_NetworkID"] ?? "1001";

            // Xác định isSysadmin (Gate: Sys_UserController.cs:490, 492)
            var isSysadmin = !string.IsNullOrEmpty(req?.IsSysAdmin)
                ? StdFlag(req.IsSysAdmin)
                : (!string.IsNullOrEmpty(req?.FlagSysAdmin)
                    ? StdFlag(req.FlagSysAdmin)
                    : (callerUser?.FlagSysAdmin == "1" ? "1" : "0"));

            // Xác định isParent = NetworkID == OrgID (Gate: Sys_UserController.cs:494-498)
            var isParent = string.Equals(networkId, waOrgId, StringComparison.OrdinalIgnoreCase);

            // 4. Truy vấn người dùng theo UserCode (Gate: Sys_UserController.cs:503-530, BIZ: System.cs:6755-6848)
            var userQuery = _db.Set<Sys_User>().AsNoTracking();
            if (visibleMstsQuery != null)
            {
                userQuery = userQuery.Where(u => visibleMstsQuery.Contains(u.MST ?? ""));
            }

            var user = await userQuery.FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(userCode), PortQuery.Esc));

            // Nếu không tìm thấy, gate trả về View(new Sys_User()) (Gate: Sys_UserController.cs:530, 631)
            if (user == null)
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Sys_User_UpdateData?)null
                });
            }

            // Che mật khẩu bằng hằng số Default_PasswordMask (System.cs:6816)
            user.UserPassword = Default_PasswordMask;

            // Nối thông tin phòng ban (System.cs:6837-6838)
            var uDeptCode = StdParam(user.DepartmentCode);
            if (!string.IsNullOrEmpty(uDeptCode))
            {
                var dept = await _db.Set<Mst_Department>().AsNoTracking()
                    .FirstOrDefaultAsync(d => EF.Functions.Like((d.DepartmentCode ?? "").ToLower(), PortQuery.Equal(uDeptCode), PortQuery.Esc));
                if (dept != null)
                {
                    user.mdept_DepartmentCode = dept.DepartmentCode;
                    user.mdept_DepartmentName = dept.DepartmentName;
                }
            }

            // Nối OrgID từ Mst_NNT (System.cs:6839)
            var uMST = StdParam(user.MST);
            if (!string.IsNullOrEmpty(uMST))
            {
                var nnt = await _db.Set<Mst_NNT>().AsNoTracking()
                    .FirstOrDefaultAsync(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(uMST), PortQuery.Esc));
                if (nnt != null)
                {
                    user.OrgID = nnt.OrgID ?? user.OrgID;
                }
            }

            // 5. Truy vấn danh sách Mst_NNT (Gate: Sys_UserController.cs:533-550, BIZ: Master.cs:12121-12218)
            var nntQuery = _db.Set<Mst_NNT>().AsNoTracking()
                .Where(n => n.FlagActive == "1");

            if (isSysadmin == "0")
            {
                // Khi không phải SysAdmin: lọc theo MST và OrgID của người dùng hiện tại (Sys_UserController.cs:1984-2020)
                if (!string.IsNullOrEmpty(waMST))
                {
                    nntQuery = nntQuery.Where(n => EF.Functions.Like((n.MST ?? "").ToLower(), PortQuery.Equal(waMST), PortQuery.Esc));
                }
                if (!string.IsNullOrEmpty(waOrgId))
                {
                    nntQuery = nntQuery.Where(n => EF.Functions.Like((n.OrgID ?? "").ToLower(), PortQuery.Equal(waOrgId), PortQuery.Esc));
                }
            }

            var listMst_NNTCur = await nntQuery.OrderBy(n => n.MST).ToListAsync();

            // 6. Truy vấn danh sách Mst_Department với FlagActive = '1' (Gate: Sys_UserController.cs:553-587, BIZ: Master.cs:35737-35835)
            var deptListRaw = await _db.Set<Mst_Department>().AsNoTracking()
                .Where(d => d.FlagActive == "1")
                .OrderBy(d => d.DepartmentCode)
                .ToListAsync();

            var listMst_DepartmentExt = new List<Mst_DepartmentExt>();
            foreach (var item in deptListRaw)
            {
                listMst_DepartmentExt.Add(new Mst_DepartmentExt
                {
                    DepartmentCode = item.DepartmentCode,
                    NetworkID = item.NetworkID,
                    DepartmentCodeParent = item.DepartmentCodeParent,
                    DepartmentBUCode = item.DepartmentBUCode,
                    DepartmentBUPattern = item.DepartmentBUPattern,
                    DepartmentLevel = item.DepartmentLevel,
                    MST = item.MST,
                    DepartmentName = item.DepartmentName,
                    FlagActive = item.FlagActive,
                    LogLUDTimeUTC = item.LogLUDTimeUTC,
                    LogLUBy = item.LogLUBy
                });
            }

            var tree = Mst_DepartmentExtension.ToGroupBaseTree(listMst_DepartmentExt);
            var flatTree = Mst_DepartmentExtension.ToFlatGroupBaseTree(tree);

            // 7. Truy vấn danh sách Mst_Customer với FlagDealer = '1', OrgID = waOrgId, FlagActive = '1'
            // (Gate: Sys_UserController.cs:589-624, BIZ: Customer.cs:1464-1612)
            var custQuery = _db.Set<Mst_Customer>().AsNoTracking()
                .Where(c => c.FlagActive == "1" && c.FlagDealer == "1");

            if (!string.IsNullOrEmpty(waOrgId))
            {
                custQuery = custQuery.Where(c => EF.Functions.Like((c.OrgID ?? "").ToLower(), PortQuery.Equal(waOrgId), PortQuery.Esc));
            }

            var listMst_Customer = await custQuery.OrderBy(c => c.CustomerCodeSys).ToListAsync();

            // 8. Đóng gói kết quả khớp gate MVC: Model là Sys_User, ViewBag chứa ListMst_NNTCur, ListMst_Department, ListMst_Customer, IsParent
            // (Gate: Sys_UserController.cs:550, 623, 626, 628)
            var inosBaseUrl = _config?["InosBaseUrl"];
            var updateData = new Sys_User_UpdateData(user, isParent, listMst_NNTCur, flatTree, listMst_Customer, inosBaseUrl);

            return Ok(new
            {
                Success = true,
                Data = updateData
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_User_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Edit từ gate Sys_UserController.cs:636-725.
    /// Chuỗi gọi:
    /// 1. Deserialize model -> Sys_User (gate dòng 648).
    /// 2. Kiểm tra if (objSys_UserInput != null && !CUtils.IsNullOrEmpty(objSys_UserInput.UserCode)) (gate dòng 649):
    ///    Nếu rỗng: trả về Success = true, AddMessage("Mã người dùng trống!") (gate dòng 720-721).
    /// 3. Dựng strWhereClause = strWhereClause_SysUser(UserCode) (gate dòng 651-652, 1936-1947).
    ///    Gọi Sys_UserService.Instance.WA_Sys_User_Get(objRQ_Sys_User, addressAPIs) (gate dòng 675)
    ///    -> API WA_Sys_User_Get (SysUserController.cs:199-270)
    ///    -> BIZ WAS_Sys_User_Get (System.cs:7011-7123)
    ///    -> BIZ Sys_User_Get_New20191104 (System.cs:6635-7009).
    /// 4. Kiểm tra if (objRT_Sys_UserCur.Lst_Sys_User != null && objRT_Sys_UserCur.Lst_Sys_User.Count > 0) (gate dòng 676):
    ///    Nếu không thấy: trả về Success = true, AddMessage("Mã người dùng '" + objSys_UserInput.UserCode + "' không có trong hệ thống!") (gate dòng 714-715).
    /// 5. Nếu tìm thấy:
    ///    - Sao chép các thuộc tính từ objSys_UserInput: UserName, PhoneNo, Position, DepartmentCode, FlagSysAdmin, FlagNNTAdmin, FlagViewOrgCreate, FlagViewOtherOrgCreate, FlagActive (gate dòng 678-686).
    ///    - Dựng strFt_Cols_Upd (gate dòng 688-698).
    ///    - Đóng gói objRQ_Sys_User và gọi Sys_UserService.Instance.WA_Sys_User_Update(objRQ_Sys_User, addressAPIs) (gate dòng 706)
    ///      -> API WA_Sys_User_Update (SysUserController.cs:569-640)
    ///      -> BIZ WAS_Sys_User_Update (System.cs:11620-11715)
    ///      -> BIZ Sys_User_Update_New20231115 (System.cs:11201-11618).
    /// 6. BIZ Sys_User_Update_New20231115:
    ///    - Mở transaction: _cf.db.BeginTransaction() (System.cs:11269).
    ///    - Chuẩn hoá tham số đầu vào: strUserCode, strUserName, strPhoneNo, strPosition, strDepartmentCode, các cờ FlagSysAdmin, FlagNNTAdmin, FlagActive, FlagViewOrgCreate, FlagViewOtherOrgCreate (System.cs:11310-11327).
    ///    - Xác định bUpd_* từ strFt_Cols_Upd (System.cs:11330-11342).
    ///    - Gọi Sys_User_CheckDB (System.cs:11348-11354, 3552-3611).
    ///    - Biz Guard: nếu bUpd_UserName && strUserName.Length < 1 -> ném TError.ErridQContract.Sys_User_Update_InvalidUserName (System.cs:11356-11365).
    ///    - Cập nhật các cột trong DB (System.cs:11390-11408).
    ///    - Bỏ qua dịch vụ ngoài Inos_AccountService_EditProfileX, VelocaPortal (System.cs:11423-11562) -> GAP.
    ///    - Commit transaction: TDALUtils.DBUtils.CommitSafety(_cf.db) (System.cs:11580).
    /// 7. Trả về kết quả khớp gate MVC (Sys_UserController.cs:708-710):
    ///    Success = true, Messages = ["Sửa người dùng thành công!"], RedirectUrl = Url.Action("Index"), Data = existing.
    /// </summary>
    [HttpPost("edit")]
    [HttpPost("save")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_User_EditReq? req)
    {
        // 1. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Sys_UserController.cs:636, 648)
        if (!string.IsNullOrWhiteSpace(req?.Model))
        {
            try
            {
                var modelObj = System.Text.Json.JsonSerializer.Deserialize<Sys_User_EditReq>(req.Model, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.UserCode ??= modelObj.UserCode;
                    req.UserName ??= modelObj.UserName;
                    req.PhoneNo ??= modelObj.PhoneNo;
                    req.Position ??= modelObj.Position;
                    req.DepartmentCode ??= modelObj.DepartmentCode;
                    req.FlagSysAdmin ??= modelObj.FlagSysAdmin;
                    req.FlagNNTAdmin ??= modelObj.FlagNNTAdmin;
                    req.FlagViewOrgCreate ??= modelObj.FlagViewOrgCreate;
                    req.FlagViewOtherOrgCreate ??= modelObj.FlagViewOtherOrgCreate;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.FlagDLAdmin ??= modelObj.FlagDLAdmin;
                    req.EMail ??= modelObj.EMail;
                    req.MST ??= modelObj.MST;
                    req.Avatar ??= modelObj.Avatar;
                    req.Ft_Cols_Upd ??= modelObj.Ft_Cols_Upd;
                    req.WAUserCode ??= modelObj.WAUserCode;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.OrgID ??= modelObj.OrgID;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        // Gate guard 1: Kiểm tra model và UserCode không được rỗng (Sys_UserController.cs:649-650, 720-721)
        var rawUserCode = req?.UserCode;
        if (string.IsNullOrEmpty(rawUserCode) && HttpContext?.Request?.Query != null && HttpContext.Request.Query.TryGetValue("usercode", out var qUserCode))
        {
            rawUserCode = qUserCode.ToString();
        }

        if (req == null || string.IsNullOrWhiteSpace(rawUserCode))
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Mã người dùng trống!" },
                Title = "Mã người dùng trống!"
            });
        }

        var userCode = StdParam(rawUserCode);
        if (string.IsNullOrEmpty(userCode))
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Mã người dùng trống!" },
                Title = "Mã người dùng trống!"
            });
        }

        // Gate guard 2: Tra cứu bản ghi người dùng hiện có trong DB qua WA_Sys_User_Get (Sys_UserController.cs:651-676, 714-716)
        var existing = await _db.Set<Sys_User>()
            .FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(userCode), PortQuery.Esc));

        if (existing == null)
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { $"Mã người dùng '{rawUserCode}' không có trong hệ thống!" },
                Title = $"Mã người dùng '{rawUserCode}' không có trong hệ thống!"
            });
        }

        // Xác định các cột cập nhật theo gate (Sys_UserController.cs:688-698)
        var defaultCols = "SYS_USER.USERNAME,SYS_USER.PHONENO,SYS_USER.POSITION,SYS_USER.DEPARTMENTCODE,SYS_USER.FLAGSYSADMIN,SYS_USER.FLAGNNTADMIN,SYS_USER.FLAGACTIVE,SYS_USER.FLAGVIEWORGCREATE,SYS_USER.FLAGVIEWOTHERORGCREATE,";
        var strFt_Cols_Upd = (StdParam(req.Ft_Cols_Upd) ?? defaultCols).ToUpperInvariant();

        bool bUpd_UserName = strFt_Cols_Upd.Contains("SYS_USER.USERNAME") || strFt_Cols_Upd.Contains("USERNAME");
        bool bUpd_PhoneNo = strFt_Cols_Upd.Contains("SYS_USER.PHONENO") || strFt_Cols_Upd.Contains("PHONENO");
        bool bUpd_Position = strFt_Cols_Upd.Contains("SYS_USER.POSITION") || strFt_Cols_Upd.Contains("POSITION");
        bool bUpd_DepartmentCode = strFt_Cols_Upd.Contains("SYS_USER.DEPARTMENTCODE") || strFt_Cols_Upd.Contains("DEPARTMENTCODE");
        bool bUpd_FlagSysAdmin = strFt_Cols_Upd.Contains("SYS_USER.FLAGSYSADMIN") || strFt_Cols_Upd.Contains("FLAGSYSADMIN");
        bool bUpd_FlagNNTAdmin = strFt_Cols_Upd.Contains("SYS_USER.FLAGNNTADMIN") || strFt_Cols_Upd.Contains("FLAGNNTADMIN");
        bool bUpd_FlagActive = strFt_Cols_Upd.Contains("SYS_USER.FLAGACTIVE") || strFt_Cols_Upd.Contains("FLAGACTIVE");
        bool bUpd_FlagViewOrgCreate = strFt_Cols_Upd.Contains("SYS_USER.FLAGVIEWORGCREATE") || strFt_Cols_Upd.Contains("FLAGVIEWORGCREATE");
        bool bUpd_FlagViewOtherOrgCreate = strFt_Cols_Upd.Contains("SYS_USER.FLAGVIEWOTHERORGCREATE") || strFt_Cols_Upd.Contains("FLAGVIEWOTHERORGCREATE");
        bool bUpd_EMail = strFt_Cols_Upd.Contains("SYS_USER.EMAIL") || strFt_Cols_Upd.Contains("EMAIL");
        bool bUpd_Avatar = strFt_Cols_Upd.Contains("SYS_USER.AVATAR") || strFt_Cols_Upd.Contains("AVATAR");
        bool bUpd_FlagDLAdmin = strFt_Cols_Upd.Contains("SYS_USER.FLAGDLADMIN") || strFt_Cols_Upd.Contains("FLAGDLADMIN");

        // Chuẩn hoá dữ liệu cập nhật theo System.cs:11313-11327
        var strUserName = req.UserName != null ? string.Format("{0}", req.UserName).Trim() : (existing.UserName ?? "");
        var strPhoneNo = req.PhoneNo != null ? string.Format("{0}", req.PhoneNo).Trim() : (existing.PhoneNo ?? "");
        var strPosition = req.Position != null ? string.Format("{0}", req.Position) : (existing.Position ?? "");
        var strDepartmentCode = req.DepartmentCode != null ? StdParam(req.DepartmentCode) : existing.DepartmentCode;
        var strFlagSysAdmin = req.FlagSysAdmin != null ? StdFlag(req.FlagSysAdmin) : (existing.FlagSysAdmin ?? "0");
        var strFlagNNTAdmin = req.FlagNNTAdmin != null ? StdFlag(req.FlagNNTAdmin) : (existing.FlagNNTAdmin ?? "0");
        var strFlagActive = req.FlagActive != null ? StdFlag(req.FlagActive) : (existing.FlagActive ?? "1");
        var strFlagViewOrgCreate = req.FlagViewOrgCreate != null ? StdFlag(req.FlagViewOrgCreate) : (existing.FlagViewOrgCreate ?? "0");
        var strFlagViewOtherOrgCreate = req.FlagViewOtherOrgCreate != null ? StdFlag(req.FlagViewOtherOrgCreate) : (existing.FlagViewOtherOrgCreate ?? "0");
        var strEMail = req.EMail != null ? string.Format("{0}", req.EMail).Trim() : (existing.EMail ?? "");
        var strAvatar = req.Avatar != null ? string.Format("{0}", req.Avatar) : (existing.ACAvatar ?? "");
        var strFlagDLAdmin = req.FlagDLAdmin != null ? StdFlag(req.FlagDLAdmin) : (existing.FlagDLAdmin ?? "0");

        // Mở transaction cập nhật (System.cs:11269)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Biz guard: Nếu cập nhật UserName mà strUserName.Length < 1 -> ném Sys_User_Update_InvalidUserName (System.cs:11356-11365)
            if (bUpd_UserName && strUserName.Length < 1)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Sys_User_Update_InvalidUserName", "Check.strUserName", req.UserName ?? "");
            }

            if (bUpd_UserName) existing.UserName = strUserName;
            if (bUpd_PhoneNo) existing.PhoneNo = strPhoneNo;
            if (bUpd_Position) existing.Position = strPosition;
            if (bUpd_DepartmentCode) existing.DepartmentCode = strDepartmentCode;
            if (bUpd_FlagSysAdmin) existing.FlagSysAdmin = strFlagSysAdmin;
            if (bUpd_FlagNNTAdmin) existing.FlagNNTAdmin = strFlagNNTAdmin;
            if (bUpd_FlagActive) existing.FlagActive = strFlagActive;
            if (bUpd_FlagViewOrgCreate) existing.FlagViewOrgCreate = strFlagViewOrgCreate;
            if (bUpd_FlagViewOtherOrgCreate) existing.FlagViewOtherOrgCreate = strFlagViewOtherOrgCreate;
            if (bUpd_EMail) existing.EMail = strEMail;
            if (bUpd_Avatar) existing.ACAvatar = strAvatar;
            if (bUpd_FlagDLAdmin) existing.FlagDLAdmin = strFlagDLAdmin;

            var dtimeSys = DateTime.UtcNow;
            var callerCode = Request?.Headers["WAUserCode"].FirstOrDefault() ?? req.WAUserCode ?? "port";

            existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
            existing.LogLUBy = callerCode;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            _db.Entry(existing).State = EntityState.Detached;

            // Che mật khẩu trước khi trả về
            existing.UserPassword = Default_PasswordMask;

            // Trả kết quả thành công khớp gate MVC (Sys_UserController.cs:708-710)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Sửa người dùng thành công!" },
                Title = "Sửa người dùng thành công!",
                RedirectUrl = "/Sys_User",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_User_Update", "Exception", ex.Message);
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
/// Model nhận tham số cho action Create (cả khởi tạo màn hình và lưu tài khoản).
/// Gate: Sys_UserController.cs:114-196 (GET Create), 200-367 (POST Create).
/// </summary>
public class Sys_User_CreateReq
{
    // Cờ khởi tạo màn hình tạo mới
    public bool? IsInit { get; set; }
    public string? Init { get; set; }

    // Tham số lọc/ngữ cảnh người dùng gọi (Gate: Sys_UserController.cs:114-196)
    public string? WAUserCode { get; set; }
    public string? UserCodeCaller { get; set; }
    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? IsSysAdmin { get; set; }
    public string? FlagActive { get; set; }

    // Tham số khi POST Create lưu tài khoản (Gate: Sys_UserController.cs:200-367)
    public string? Model { get; set; }
    public string? FlagExist { get; set; }
    public string? UserCode { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Position { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
    public string? FlagDLAdmin { get; set; }
    public string? FlagViewOrgCreate { get; set; }
    public string? FlagViewOtherOrgCreate { get; set; }
    public string? Save { get; set; }
}

/// <summary>
/// Model kết quả khởi tạo màn hình tạo mới người dùng.
/// Tương ứng ViewBag.IsParent, ViewBag.ListMst_NNTCur, ViewBag.ListMst_Department và Model Sys_User.
/// Gate: Sys_UserController.cs:134, 154, 193, 195.
/// </summary>
public class Sys_User_CreateInitData
{
    public bool IsParent { get; set; }
    public List<Mst_NNT> ListMst_NNTCur { get; set; } = new();
    public List<Mst_DepartmentExt> ListMst_Department { get; set; } = new();
    public Sys_User Sys_User { get; set; } = new();
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

/// <summary>
/// Model nhận tham số cho action LoadMstCustomer của Sys_UserController.
/// Gate: Sys_UserController.cs:373 LoadMstCustomer(string mst)
/// </summary>
public class Sys_User_LoadMstCustomerReq
{
    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? IsSysAdmin { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? WAUserCode { get; set; }
}

/// <summary>
/// Model nhận tham số cho action StopService của Sys_UserController.
/// Gate: Sys_UserController.cs:469 StopService() (không nhận tham số).
/// </summary>
public class Sys_User_StopServiceReq
{
}

/// <summary>
/// Model nhận tham số cho action Update của Sys_UserController.
/// Gate: Sys_UserController.cs:477 Update(string usercode)
/// </summary>
public class Sys_User_UpdateReq
{
    public string? UserCode { get; set; }
    public string? Model { get; set; }
    public string? WAUserCode { get; set; }
    public string? UserCodeCaller { get; set; }
    public string? MST { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? IsSysAdmin { get; set; }
    public string? FlagSysAdmin { get; set; }
}

/// <summary>
/// Model kết quả trả về cho màn hình cập nhật người dùng.
/// Khớp với Model Sys_User và ViewBag (ListMst_NNTCur, ListMst_Department, ListMst_Customer, IsParent, InosBaseUrl).
/// Gate: Sys_UserController.cs:498, 550, 623, 626, 628.
/// </summary>
public class Sys_User_UpdateData : Sys_User
{
    public bool IsParent { get; set; }
    public string? InosBaseUrl { get; set; }
    public List<Mst_NNT> ListMst_NNTCur { get; set; } = new();
    public List<Mst_DepartmentExt> ListMst_Department { get; set; } = new();
    public List<Mst_Customer> ListMst_Customer { get; set; } = new();
    public Sys_User? Sys_User { get; set; }

    public Sys_User_UpdateData() { }

    public Sys_User_UpdateData(Sys_User src, bool isParent, List<Mst_NNT> nnts, List<Mst_DepartmentExt> depts, List<Mst_Customer> custs, string? inosBaseUrl = null)
    {
        UserCode = src.UserCode;
        NetworkID = src.NetworkID;
        OrgID = src.OrgID;
        UserName = src.UserName;
        UserPassword = src.UserPassword;
        PhoneNo = src.PhoneNo;
        EMail = src.EMail;
        MST = src.MST;
        DepartmentCode = src.DepartmentCode;
        Position = src.Position;
        FlagActive = src.FlagActive;
        FlagSysAdmin = src.FlagSysAdmin;
        FlagNNTAdmin = src.FlagNNTAdmin;
        FlagDLAdmin = src.FlagDLAdmin;
        FlagViewOrgCreate = src.FlagViewOrgCreate;
        FlagViewOtherOrgCreate = src.FlagViewOtherOrgCreate;
        FlagBG = src.FlagBG;
        InosAccessToken = src.InosAccessToken;
        AuthorizeDTimeStart = src.AuthorizeDTimeStart;
        ACId = src.ACId;
        ACAvatar = src.ACAvatar;
        ACEmail = src.ACEmail;
        ACLanguage = src.ACLanguage;
        ACName = src.ACName;
        ACPhone = src.ACPhone;
        ACTimeZone = src.ACTimeZone;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;
        mdept_DepartmentCode = src.mdept_DepartmentCode;
        mdept_DepartmentName = src.mdept_DepartmentName;

        IsParent = isParent;
        ListMst_NNTCur = nnts;
        ListMst_Department = depts;
        ListMst_Customer = custs;
        InosBaseUrl = inosBaseUrl;
        Sys_User = src;
    }
}

/// <summary>
/// Model nhận tham số cho action Edit/Save của Sys_UserController.
/// Gate: Sys_UserController.cs:636 Edit(string model)
/// </summary>
public class Sys_User_EditReq
{
    public string? Model { get; set; }
    public string? UserCode { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNo { get; set; }
    public string? Position { get; set; }
    public string? DepartmentCode { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
    public string? FlagViewOrgCreate { get; set; }
    public string? FlagViewOtherOrgCreate { get; set; }
    public string? FlagActive { get; set; }
    public string? FlagDLAdmin { get; set; }
    public string? EMail { get; set; }
    public string? MST { get; set; }
    public string? Avatar { get; set; }
    public string? Ft_Cols_Upd { get; set; }
    public string? WAUserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
}


