using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 HomeController.cs — Index & About
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\HomeController.cs:27-134 (Index)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\HomeController.cs:136-141 (About)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:2985-3336 (InitNetworkData)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:747-758 (RedirectActionToLogin)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:808-826 (InitSSO)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\HomeController.cs:253-264 (strWhereClause_SysUser)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\HomeController.cs:265-276 (strWhereClause_Mst_NNT)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1031-1036 (List_Mst_NNT_Login_Async)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\HomeController.cs:277-288 (strWhereClause_Mst_Sys_Config)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1056-1061 (RT_Mst_Sys_Config_Login_Async)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1163-1188 (List_Mst_CTSRemoteSupplier_Async)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1138-1161 (List_Mst_OrgSignConfig_Async)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MasterServerController.cs:20-91 (WA_MstSv_Sys_User_Login)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysUserController.cs:273-344 (WA_Sys_User_GetForCurrentUser)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\OSInosController.cs:168-239 (WA_OS_Inos_Org_GetMyOrgList)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\MasterServer.cs:744-831 (WAS_MstSv_Sys_User_Login) -> 450-596 (MstSv_Sys_User_Login)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:5527-5619 (WAS_Sys_User_GetForCurrentUser) -> 5299-5525 (Sys_User_GetForCurrentUser)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\OS.Inos.cs:2766-2847 (WAS_OS_Inos_Org_GetMyOrgList) -> 345-452 (OS_Inos_Org_GetMyOrgList) -> 2212-2315 (Inos_OrgService_GetMyOrgListX_New20200207)
[ApiController]
[Route("api/home")]
public class HomeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public HomeControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate HomeController.cs:27-134.
    /// Chuỗi nghiệp vụ:
    /// 1. Kiểm tra xác thực User.Identity.IsAuthenticated (HomeController.cs:29).
    ///    Nếu chưa xác thực: SignOut và RedirectActionToLogin() -> chuyển hướng về /Account/Login.
    /// 2. Nhánh 1: Nếu có RouteNetworkId > 0 (HomeController.cs:39):
    ///    Gọi InitNetworkData() (BaseController.cs:2985-3336):
    ///    - Xác định user đăng nhập qua req.UserCode hoặc header WAUserCode (iNOS accSv.GetCurrentUser() -> GAP).
    ///    - Nếu không thấy user -> InitNetworkData trả về false -> RedirectActionToLogin().
    ///    - Lấy Sys_User, nối thông tin Mst_Department.
    ///    - Lấy quyền truy cập Sys_Access / Sys_Object (Admin -> toàn bộ; thường -> theo nhóm).
    ///    - Lấy thông tin Mst_NNT theo MST của user.
    ///    - Lấy cấu hình hệ thống Mst_Sys_Config theo NetworkID.
    ///    - Lấy cấu hình chữ ký số Mst_OrgSignConfig theo OrgID và FlagActive="1".
    ///    - Lấy danh sách Mst_CTSRemoteSupplier hoạt động.
    ///    - Khởi tạo thành công -> Redirect sang Dashboard (/Dashboard/Index?networkId={RouteNetworkId}).
    /// 3. Nhánh 2: Nếu RouteNetworkId <= 0 và có phiên/accessToken (HomeController.cs:55-119):
    ///    - Kiểm tra RawUrl: nếu khác "/" -> Redirect về "/" (HomeController.cs:76-80).
    ///    - Lấy danh sách tổ chức người dùng (WA_OS_Inos_Org_GetMyOrgList -> Mst_Org trong local DB).
    ///    - Phân loại tổ chức gốc (ParentId == "0") và tổ chức con (ParentId != "0").
    ///    - Nếu danh sách chỉ có 1 tổ chức -> RedirectToAction("index", new { networkId = list[0].Id }).
    ///    - Nếu danh sách có nhiều tổ chức -> trả về View(listOS_Inos_Org) (hoặc danh sách JSON).
    /// 4. Nhánh 3: Không có accessToken / không có phiên -> RedirectActionToLogin().
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Home_IndexReq? req)
    {
        try
        {
            var rawUrl = req?.RawUrl?.Trim() ?? "/";

            // 1. Kiểm tra xác thực (HomeController.cs:29, 127-133)
            // Xác định người dùng qua req.UserCode, header WAUserCode hoặc req.AccessToken
            var userCode = req?.UserCode?.Trim();
            if (string.IsNullOrEmpty(userCode))
            {
                userCode = Request?.Headers["WAUserCode"].FirstOrDefault()?.Trim();
            }

            var accessToken = req?.AccessToken?.Trim();
            if (string.IsNullOrEmpty(accessToken) && Request?.Headers != null && Request.Headers.TryGetValue("AccessToken", out var hToken))
            {
                accessToken = hToken.FirstOrDefault()?.Trim();
            }

            bool isAuthenticated = req?.IsAuthenticated ?? (!string.IsNullOrEmpty(userCode) || !string.IsNullOrEmpty(accessToken));

            string redirectUrl;

            // Nếu chưa xác thực -> Chuyển hướng tới trang đăng nhập (RedirectActionToLogin)
            if (!isAuthenticated)
            {
                redirectUrl = $"/Account/Login?redirectUrl={Uri.EscapeDataString(rawUrl)}";
                return Ok(new
                {
                    Success = true,
                    RedirectUrl = redirectUrl,
                    Data = (object?)null
                });
            }

            // 2. Kiểm tra RouteNetworkId (HomeController.cs:39, BaseController.cs:987-1007)
            var networkIdStr = req?.NetworkId ?? req?.RouteNetworkId?.ToString();
            var networkId = StdParam(networkIdStr);
            long routeNetworkId = 0;
            if (!string.IsNullOrEmpty(networkId))
            {
                long.TryParse(networkId, out routeNetworkId);
            }

            if (routeNetworkId > 0)
            {
                // Gọi InitNetworkData (BaseController.cs:2985-3336)
                if (string.IsNullOrEmpty(userCode))
                {
                    // Không xác định được người dùng -> InitNetworkData trả về false -> chuyển hướng tới Login
                    redirectUrl = $"/Account/Login?redirectUrl={Uri.EscapeDataString(rawUrl)}";
                    return Ok(new
                    {
                        Success = true,
                        RedirectUrl = redirectUrl,
                        Data = (object?)null
                    });
                }

                // Lấy thông tin người dùng từ Sys_User (Sys_User_GetForCurrentUser - System.cs:5397-5423)
                var user = await _db.Set<Sys_User>()
                    .FirstOrDefaultAsync(u => EF.Functions.Like(u.UserCode.ToLower(), userCode.ToLower(), "\\")
                                           || (u.EMail != null && EF.Functions.Like(u.EMail.ToLower(), userCode.ToLower(), "\\")));

                if (user == null)
                {
                    // Người dùng không tồn tại -> InitNetworkData trả về false -> chuyển hướng tới Login
                    redirectUrl = $"/Account/Login?redirectUrl={Uri.EscapeDataString(rawUrl)}";
                    return Ok(new
                    {
                        Success = true,
                        RedirectUrl = redirectUrl,
                        Data = (object?)null
                    });
                }

                // Nối thông tin phòng ban (System.cs:5417-5418)
                if (!string.IsNullOrEmpty(user.DepartmentCode))
                {
                    var dept = await _db.Set<Mst_Department>()
                        .FirstOrDefaultAsync(d => EF.Functions.Like(d.DepartmentCode.ToLower(), user.DepartmentCode.ToLower(), "\\"));
                    if (dept != null)
                    {
                        user.mdept_DepartmentCode = dept.DepartmentCode;
                        user.mdept_DepartmentName = dept.DepartmentName;
                    }
                }

                // Lấy quyền truy cập Sys_Access / Sys_Object (System.cs:5431-5463)
                List<Sys_Object> accessObjects;
                if (user.FlagSysAdmin == "1" && user.FlagActive == "1")
                {
                    accessObjects = await _db.Set<Sys_Object>()
                        .Where(o => o.FlagActive == "1")
                        .OrderBy(o => o.ObjectCode)
                        .ToListAsync();
                }
                else
                {
                    var userGroups = await _db.Set<Sys_UserInGroup>()
                        .Where(ug => EF.Functions.Like(ug.UserCode.ToLower(), user.UserCode.ToLower(), "\\"))
                        .Select(ug => ug.GroupCode)
                        .ToListAsync();

                    var activeGroups = await _db.Set<Sys_Group>()
                        .Where(g => g.FlagActive == "1" && userGroups.Contains(g.GroupCode))
                        .Select(g => g.GroupCode)
                        .ToListAsync();

                    var objectCodes = await _db.Set<Sys_Access>()
                        .Where(a => activeGroups.Contains(a.GroupCode))
                        .Select(a => a.ObjectCode)
                        .Distinct()
                        .ToListAsync();

                    accessObjects = await _db.Set<Sys_Object>()
                        .Where(o => o.FlagActive == "1" && objectCodes.Contains(o.ObjectCode))
                        .OrderBy(o => o.ObjectCode)
                        .ToListAsync();
                }

                // Lấy thông tin Mst_NNT (BaseController.cs:3163-3183)
                Mst_NNT? nnt = null;
                if (!string.IsNullOrEmpty(user.MST))
                {
                    nnt = await _db.Set<Mst_NNT>()
                        .FirstOrDefaultAsync(n => EF.Functions.Like(n.MST.ToLower(), user.MST.ToLower(), "\\"));
                }

                // Lấy cấu hình hệ thống Mst_Sys_Config theo NetworkID (BaseController.cs:3185-3204)
                var sysConfig = await _db.Set<Mst_Sys_Config>()
                    .FirstOrDefaultAsync(c => EF.Functions.Like(c.NetworkID.ToLower(), networkId!.ToLower(), "\\"));

                // Xác định OrgID (BaseController.cs:3049)
                var orgId = StdParam(req?.OrgId) ?? user.OrgID ?? nnt?.OrgID ?? "0";

                // Lấy cấu hình chữ ký số Mst_OrgSignConfig (BaseController.cs:3230-3254)
                var orgSignConfigs = await _db.Set<Mst_OrgSignConfig>()
                    .Where(s => EF.Functions.Like(s.OrgID!.ToLower(), orgId.ToLower(), "\\") && s.FlagActive == "1")
                    .OrderBy(s => s.AutoID)
                    .ToListAsync();

                // Lấy danh sách Mst_CTSRemoteSupplier (BaseController.cs:3207-3227)
                var ctsSuppliers = await _db.Set<Mst_CTSRemoteSupplier>()
                    .Where(s => s.FlagActive == "1")
                    .OrderBy(s => s.SupplierCode)
                    .ToListAsync();

                // Chuyển hướng tới Dashboard khi InitNetworkData thành công (HomeController.cs:50-52)
                redirectUrl = $"/Dashboard/Index?networkId={networkId}";
                return Ok(new
                {
                    Success = true,
                    RedirectUrl = redirectUrl,
                    Data = new
                    {
                        NetworkID = networkId,
                        OrgID = orgId,
                        User = user,
                        NNT = nnt,
                        SysConfig = sysConfig,
                        OrgSignConfigs = orgSignConfigs,
                        CTSRemoteSuppliers = ctsSuppliers,
                        AccessObjects = accessObjects
                    }
                });
            }

            // 3. Nhánh RouteNetworkId <= 0 (HomeController.cs:55-119)
            // Kiểm tra RawUrl: nếu khác "/" chuyển hướng về "/" (HomeController.cs:76-80)
            if (!string.IsNullOrEmpty(rawUrl) && !rawUrl.Equals("/") && !rawUrl.Equals("/api/home/index", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    Success = true,
                    RedirectUrl = "/",
                    Data = (object?)null
                });
            }

            // Lấy danh sách tổ chức người dùng (WA_OS_Inos_Org_GetMyOrgList -> Mst_Org)
            var orgQuery = _db.Set<Mst_Org>().AsQueryable();
            if (!string.IsNullOrEmpty(req?.NetworkIdFilter))
            {
                var netFilter = StdParam(req.NetworkIdFilter)!;
                orgQuery = orgQuery.Where(o => EF.Functions.Like(o.NetworkID!.ToLower(), netFilter.ToLower(), "\\"));
            }
            if (!string.IsNullOrEmpty(req?.OrgId))
            {
                var orgFilter = StdParam(req.OrgId)!;
                orgQuery = orgQuery.Where(o => EF.Functions.Like(o.OrgID.ToLower(), orgFilter.ToLower(), "\\"));
            }

            var orgList = await orgQuery.OrderBy(o => o.OrgID).ToListAsync();

            // Phân loại tổ chức gốc và tổ chức con theo HomeController.cs:99-112
            var listOS_Inos_Org = (from item in orgList
                                   where (string.IsNullOrEmpty(item.OrgParent) || item.OrgParent.Trim().Equals("0"))
                                   select item).ToList();

            var listOS_Inos_OrgSub = (from item in orgList
                                      where (!string.IsNullOrEmpty(item.OrgParent) && !item.OrgParent.Trim().Equals("0"))
                                      select item).ToList();

            if (listOS_Inos_OrgSub.Count > 0)
            {
                foreach (var item in listOS_Inos_OrgSub)
                {
                    var parent = listOS_Inos_Org.FirstOrDefault(it =>
                        !string.IsNullOrEmpty(it.OrgID) &&
                        !string.IsNullOrEmpty(item.OrgParent) &&
                        it.OrgID.Trim().Equals(item.OrgParent.Trim()));

                    if (parent == null)
                    {
                        listOS_Inos_Org.Add(item);
                    }
                }
            }

            // Nếu chỉ có đúng 1 tổ chức -> Chuyển hướng tới index với networkId tương ứng (HomeController.cs:114-117)
            if (listOS_Inos_Org.Count == 1)
            {
                var targetNetId = listOS_Inos_Org[0].NetworkID ?? listOS_Inos_Org[0].OrgID;
                redirectUrl = $"/Dashboard/Index?networkId={targetNetId}";
                return Ok(new
                {
                    Success = true,
                    RedirectUrl = redirectUrl,
                    Data = listOS_Inos_Org
                });
            }

            // Nếu có nhiều tổ chức hoặc không có -> Trả về danh sách tổ chức (HomeController.cs:119)
            return Ok(new
            {
                Success = true,
                RedirectUrl = (string?)null,
                Data = listOS_Inos_Org
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Home_Index", "Exception", ex.Message);
        }
    }

    #region Helper chuẩn hoá tham số theo quy ước nguồn (PORT_RULES §3)
    private static string? StdParam(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    }

    private static string? StdStr(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
    }

    private static string StdFlag(string? s)
    {
        return s?.Trim() == "1" ? "1" : "0";
    }
    #endregion

    /// <summary>
    /// Port 1:1 action About từ gate HomeController.cs:136-141.
    /// Chuỗi nghiệp vụ:
    /// 1. Gán ViewBag.Message = "Your application description page." (HomeController.cs:138).
    /// 2. Trả về View() (HomeController.cs:140).
    /// Ở tầng API port: trả về { Success = true, Title = "About", Message = "Your application description page.", Data = new { Title = "About", Message = "Your application description page." } }.
    /// </summary>
    [HttpPost("about")]
    public IActionResult About([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Home_AboutReq? req = null)
    {
        return Ok(new
        {
            Success = true,
            Title = "About",
            Message = "Your application description page.",
            Data = new
            {
                Title = "About",
                Message = "Your application description page."
            }
        });
    }
}

public class Home_IndexReq
{
    public string? NetworkId { get; set; }
    public long? RouteNetworkId { get; set; }
    public string? OrgId { get; set; }
    public string? UserCode { get; set; }
    public string? AccessToken { get; set; }
    public bool? IsAuthenticated { get; set; }
    public string? RawUrl { get; set; }
    public string? NetworkIdFilter { get; set; }
}

public class Home_AboutReq
{
}
