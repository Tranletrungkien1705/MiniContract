using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 AccountController.cs — LoginOS
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\AccountController.cs:215-549 (LoginOS)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:808-826 (InitSSO)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\AccountController.cs:600-611 (strWhereClause_SysUser)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\AccountController.cs:613-624 (strWhereClause_Mst_NNT)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1031-1054 (List_Mst_NNT_Login_Async)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\AccountController.cs:626-637 (strWhereClause_Mst_Sys_Config)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1056-1076 (RT_Mst_Sys_Config_Login_Async)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1163-1188 (List_Mst_CTSRemoteSupplier_Async)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\AccountController.cs:639-658 (strWhereClause_Mst_OrgSignConfig)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1138-1161 (List_Mst_OrgSignConfig_Async)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MasterServerController.cs:20-91 (WA_MstSv_Sys_User_Login)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysUserController.cs:273-344 (WA_Sys_User_GetForCurrentUser)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\MasterServer.cs:744-831 (WAS_MstSv_Sys_User_Login) -> 450-596 (MstSv_Sys_User_Login)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:5527-5619 (WAS_Sys_User_GetForCurrentUser) -> 5299-5525 (Sys_User_GetForCurrentUser)
[ApiController]
[Route("api/account")]
public class AccountControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public AccountControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action LoginOS từ gate AccountController.cs:215-549.
    /// Chuỗi nghiệp vụ:
    /// 1. Kiểm tra guard tham số bắt buộc orgid và parentid (dòng 226).
    ///    Nếu thiếu -> báo lỗi guard tương ứng hoặc Fail ("Account_LoginOS", "Check.orgid_parentid").
    /// 2. Xác định người dùng đăng nhập: nhận qua req.UserCode hoặc header WAUserCode (dòng 296 accSv.GetCurrentUser() - dịch vụ iNOS ngoài -> GAP).
    ///    Nếu không xác định được người dùng -> Fail ("Account_LoginOS", "Check.UserCode").
    /// 3. Truy vấn thông tin người dùng từ Sys_User kết hợp Mst_Department theo System.cs:5397-5423.
    ///    Nếu không tìm thấy trong DB -> Fail ("Account_LoginOS", "Check.UserNotFound", userCode).
    /// 4. Phân quyền Sys_Access / Sys_Object theo System.cs:5431-5463:
    ///    Nếu là QT hệ thống (FlagSysAdmin="1" và FlagActive="1") -> cấp toàn bộ Sys_Object đang hoạt động.
    ///    Ngược lại -> lấy các Sys_Object thuộc các Sys_Group đang hoạt động mà user tham gia.
    /// 5. Lấy thông tin người nộp thuế Mst_NNT theo MST của user (dòng 361-380, strWhereClause_Mst_NNT).
    /// 6. Lấy cấu hình hệ thống Mst_Sys_Config theo parentid (NetworkID) (dòng 382-401, strWhereClause_Mst_Sys_Config).
    /// 7. Lấy danh sách cấu hình chữ ký số Mst_OrgSignConfig theo orgid và FlagActive="1" (dòng 427-451, strWhereClause_Mst_OrgSignConfig).
    /// 8. Lấy danh sách nhà cung cấp CTS remote Mst_CTSRemoteSupplier đang hoạt động (dòng 404-424).
    /// 9. Thiết lập phiên và trả kết quả thành công khớp gate (dòng 524-526):
    ///    { Success = true, RedirectUrl = "/Dashboard/Index?networkId=" + parentId, Data = ... }
    /// </summary>
    [HttpPost("loginos")]
    public async Task<IActionResult> LoginOS([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Account_LoginOSReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (PORT_RULES §3: StdParam cho mã OrgID, NetworkID)
            var orgId = StdParam(req?.OrgId);
            var parentId = StdParam(req?.ParentId);

            // Guard: kiểm tra orgid và parentid không được để trống (AccountController.cs:226)
            if (string.IsNullOrEmpty(orgId) || string.IsNullOrEmpty(parentId))
            {
                if (string.IsNullOrEmpty(orgId) && string.IsNullOrEmpty(parentId))
                {
                    return PortResult.Fail("Account_LoginOS", "Check.orgid_parentid", null);
                }
                if (string.IsNullOrEmpty(orgId))
                {
                    return PortResult.Fail("Account_LoginOS", "Check.orgid", null);
                }
                return PortResult.Fail("Account_LoginOS", "Check.parentid", null);
            }

            // 2. Xác định người dùng đăng nhập (accSv.GetCurrentUser() - AccountController.cs:296; Lesson A.24)
            var userCode = req?.UserCode?.Trim();
            if (string.IsNullOrEmpty(userCode))
            {
                userCode = Request.Headers["WAUserCode"].FirstOrDefault()?.Trim();
            }
            if (string.IsNullOrEmpty(userCode))
            {
                return PortResult.Fail("Account_LoginOS", "Check.UserCode", null);
            }

            // 3. Lấy thông tin user (Sys_User_GetForCurrentUser - System.cs:5397-5423)
            // Tìm theo UserCode hoặc Email (khớp với inosuser.Email truyền vào strWhereClause_SysUser - AccountController.cs:330)
            var user = await _db.Set<Sys_User>()
                .FirstOrDefaultAsync(u => EF.Functions.Like(u.UserCode.ToLower(), userCode.ToLower(), "\\")
                                       || (u.EMail != null && EF.Functions.Like(u.EMail.ToLower(), userCode.ToLower(), "\\")));

            if (user == null)
            {
                return PortResult.Fail("Account_LoginOS", "Check.UserNotFound", userCode);
            }

            // Nối thông tin phòng ban (mdept_DepartmentCode, mdept_DepartmentName) theo System.cs:5417-5418
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

            // 4. Lấy danh sách đối tượng quyền truy cập Sys_Access / Sys_Object theo System.cs:5431-5463
            List<Sys_Object> accessObjects;
            if (user.FlagSysAdmin == "1" && user.FlagActive == "1")
            {
                // Quản trị hệ thống hoạt động: cấp quyền tất cả đối tượng hệ thống đang hoạt động
                accessObjects = await _db.Set<Sys_Object>()
                    .Where(o => o.FlagActive == "1")
                    .OrderBy(o => o.ObjectCode)
                    .ToListAsync();
            }
            else
            {
                // Người dùng thông thường: lấy các đối tượng quyền được gán qua các nhóm đang hoạt động
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

            // 5. Lấy thông tin Mst_NNT (List_Mst_NNT_Login_Async - AccountController.cs:361-380)
            Mst_NNT? nnt = null;
            if (!string.IsNullOrEmpty(user.MST))
            {
                nnt = await _db.Set<Mst_NNT>()
                    .FirstOrDefaultAsync(n => EF.Functions.Like(n.MST.ToLower(), user.MST.ToLower(), "\\"));
            }

            // 6. Lấy cấu hình hệ thống Mst_Sys_Config (RT_Mst_Sys_Config_Login_Async - AccountController.cs:382-401)
            var sysConfig = await _db.Set<Mst_Sys_Config>()
                .FirstOrDefaultAsync(c => EF.Functions.Like(c.NetworkID.ToLower(), parentId.ToLower(), "\\"));

            // 7. Lấy cấu hình chữ ký số Mst_OrgSignConfig (List_Mst_OrgSignConfig_Async - AccountController.cs:427-451)
            var orgSignConfigs = await _db.Set<Mst_OrgSignConfig>()
                .Where(s => EF.Functions.Like(s.OrgID!.ToLower(), orgId.ToLower(), "\\") && s.FlagActive == "1")
                .OrderBy(s => s.AutoID)
                .ToListAsync();

            // 8. Lấy danh sách nhà cung cấp CTS remote Mst_CTSRemoteSupplier (List_Mst_CTSRemoteSupplier_Async - AccountController.cs:404-424)
            var ctsSuppliers = await _db.Set<Mst_CTSRemoteSupplier>()
                .Where(s => s.FlagActive == "1")
                .OrderBy(s => s.SupplierCode)
                .ToListAsync();

            // 9. Trả kết quả thành công khớp gate (AccountController.cs:524-526)
            var redirectUrl = $"/Dashboard/Index?networkId={parentId}";
            return Ok(new
            {
                Success = true,
                RedirectUrl = redirectUrl,
                Data = new
                {
                    NetworkID = parentId,
                    OrgID = nnt?.OrgID ?? orgId,
                    User = user,
                    NNT = nnt,
                    SysConfig = sysConfig,
                    OrgSignConfigs = orgSignConfigs,
                    CTSRemoteSuppliers = ctsSuppliers,
                    AccessObjects = accessObjects
                }
            });
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ trả mã lỗi chuẩn khớp gate SetFailed().AddException(ex) (AccountController.cs:542-548)
            return PortResult.Fail("Account_LoginOS", "Exception", ex.Message);
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
}

public class Account_LoginOSReq
{
    public string? OrgId { get; set; }
    public string? ParentId { get; set; }
    public string? UserCode { get; set; }
    public string? AccessToken { get; set; }
}
