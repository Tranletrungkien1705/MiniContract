using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Sys_AccessController.cs — Sys_Access
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:23-105 (Index)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:110-167 (GetDetailData)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:467-482 (strWhereClause_SysObject)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:484-499 (strWhereClause_SysAccess)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysObjectController.cs:20-91 (WA_Sys_Object_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysAccessController.cs:20-91 (WA_Sys_Access_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:14990-15089 (WAS_Sys_Object_Get) -> 14765-14900 (Sys_Object_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:704-801 (WAS_Sys_Access_Get) -> 445-560 / 221-443 (Sys_Access_Get) -> 44-107 (Sys_Access_CheckDeny)
[ApiController]
[Route("api/sys_access")]
public class Sys_AccessControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Sys_AccessControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Sys_AccessController.cs:23-105.
    /// Chuỗi gọi:
    /// 1. Sys_ObjectService.Instance.WA_Sys_Object_Get (gate dòng 60)
    ///    -> API WA_Sys_Object_Get (SysObjectController.cs:20-91)
    ///    -> BIZ WAS_Sys_Object_Get (System.cs:14990-15089)
    ///    -> Sys_Object_Get (System.cs:14765-14900)
    ///    Lọc Sys_Object theo ObjectType = "MENU" (hoặc req.ObjectType nếu truyền) và FlagActive = "1" (hoặc req.FlagActive nếu truyền), sắp xếp theo ObjectCode asc.
    /// 2. Nếu Lst_Sys_Object != null && Lst_Sys_Object.Count > 0:
    ///    Lấy mã menu đầu tiên: objectCode = Lst_Sys_Object[0].ObjectCode.Trim() (gate dòng 63).
    ///    Hoặc nếu req?.ObjectCode được chỉ định thì dùng req.ObjectCode.
    ///    Sys_AccessService.Instance.WA_Sys_Access_Get (gate dòng 96)
    ///    -> API WA_Sys_Access_Get (SysAccessController.cs:20-91)
    ///    -> BIZ WAS_Sys_Access_Get (System.cs:704-801)
    ///    -> Sys_Access_Get (System.cs:445-560 / 221-443)
    ///    Lọc Sys_Access theo ObjectCode = objectCode, inner join Sys_Access left join Sys_Object on sa.ObjectCode = so.ObjectCode.
    ///    Sắp xếp theo GroupCode asc, ObjectCode asc.
    /// 3. Gate gán: ViewBag.ListSysAccess = listSysAccess; return View(listSysObject); (gate dòng 103-104).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Access_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (Lesson A.14, A.21, PORT_RULES §3)
            var objectType = StdParam(req?.ObjectType) ?? "MENU";
            var flagActive = req?.FlagActive != null ? StdFlag(req.FlagActive) : "1";
            var specifiedObjectCode = StdParam(req?.ObjectCode);
            var groupCode = StdParam(req?.GroupCode);

            // 2. Lấy danh sách Sys_Object (gate dòng 33-64, System.cs:14841-14894)
            // SQL nguồn: select distinct so.ObjectCode into #tbl_Sys_Object_Filter_Draft from Sys_Object so where (1=1) and ... order by so.ObjectCode asc
            var queryObj = _db.Set<Sys_Object>().AsNoTracking();

            if (!string.IsNullOrEmpty(objectType))
            {
                queryObj = queryObj.Where(x => EF.Functions.Like((x.ObjectType ?? "").ToLower(), PortQuery.Equal(objectType), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(flagActive))
            {
                queryObj = queryObj.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            var listSysObject = await queryObj
                .OrderBy(x => x.ObjectCode)
                .ToListAsync();

            var listSysAccess = new List<Sys_Access>();
            var objectCode = "";

            // 3. Nếu danh sách Sys_Object có phần tử -> Lấy danh sách nhóm quyền Sys_Access (gate dòng 61-102)
            // SQL nguồn: select distinct sa.GroupCode, sa.ObjectCode into #tbl_Sys_Access_Filter_Draft from Sys_Access sa where ...
            // inner join Sys_Access sa on ... left join Sys_Object so on sa.ObjectCode = so.ObjectCode (System.cs:332-349)
            if (listSysObject.Count > 0)
            {
                objectCode = !string.IsNullOrEmpty(specifiedObjectCode)
                    ? specifiedObjectCode
                    : (listSysObject[0].ObjectCode ?? "").Trim();

                listSysAccess = await GetSysAccessListAsync(objectCode, groupCode);
            }

            // 4. Trả về kết quả khớp gate MVC: Model là listSysObject, ViewBag là listSysAccess (gate dòng 103-104)
            var data = new Sys_Access_IndexData
            {
                ListSysObject = listSysObject,
                Lst_Sys_Object = listSysObject,
                ListSysAccess = listSysAccess,
                Lst_Sys_Access = listSysAccess,
                ObjectCode = objectCode
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Access_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action GetDetailData từ gate Sys_AccessController.cs:110-167.
    /// Chuỗi gọi:
    /// 1. Khởi tạo sysAccess = new Sys_Access { GroupCode = "", ObjectCode = objectcode } (gate dòng 123-127).
    ///    Tạo điều kiện strWhereClause_SysAccess (gate dòng 128, 484-499).
    /// 2. Sys_AccessService.Instance.WA_Sys_Access_Get (gate dòng 152)
    ///    -> API WA_Sys_Access_Get (SysAccessController.cs:20-91)
    ///    -> BIZ WAS_Sys_Access_Get (System.cs:704-801)
    ///    -> Sys_Access_Get (System.cs:445-560 / 221-443)
    ///    Lọc Sys_Access theo ObjectCode = objectcode (nếu có), inner join Sys_Access left join Sys_Object on sa.ObjectCode = so.ObjectCode.
    ///    Sắp xếp theo GroupCode asc, ObjectCode asc.
    /// 3. Trả về JsonView("ShowDetailData", listSysAccess) (gate dòng 159).
    /// </summary>
    [HttpPost("getdetaildata")]
    public async Task<IActionResult> GetDetailData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Access_GetDetailDataReq? req)
    {
        try
        {
            var objectCode = StdParam(req?.ObjectCode);
            var groupCode = StdParam(req?.GroupCode);

            var listSysAccess = await GetSysAccessListAsync(objectCode, groupCode);

            return Ok(new
            {
                Success = true,
                Data = listSysAccess
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Access_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hàm dùng chung truy vấn phân quyền Sys_Access kết hợp thông tin Sys_Object
    /// theo System.cs:445-560 / 221-443 (Sys_Access_Get).
    /// </summary>
    private async Task<List<Sys_Access>> GetSysAccessListAsync(string? objectCode, string? groupCode = null)
    {
        var queryAccess = from sa in _db.Set<Sys_Access>().AsNoTracking()
                          join so in _db.Set<Sys_Object>().AsNoTracking()
                              on sa.ObjectCode equals so.ObjectCode into g
                          from so in g.DefaultIfEmpty()
                          select new { sa, so };

        if (!string.IsNullOrEmpty(objectCode))
        {
            queryAccess = queryAccess.Where(x => EF.Functions.Like((x.sa.ObjectCode ?? "").ToLower(), PortQuery.Equal(objectCode), PortQuery.Esc));
        }

        if (!string.IsNullOrEmpty(groupCode))
        {
            queryAccess = queryAccess.Where(x => EF.Functions.Like((x.sa.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
        }

        var accessRows = await queryAccess
            .OrderBy(x => x.sa.GroupCode)
            .ThenBy(x => x.sa.ObjectCode)
            .ToListAsync();

        return accessRows.Select(x => new Sys_Access
        {
            GroupCode = x.sa.GroupCode,
            ObjectCode = x.sa.ObjectCode,
            LogLUDTimeUTC = x.sa.LogLUDTimeUTC,
            LogLUBy = x.sa.LogLUBy,
            so_ObjectCode = x.so != null ? x.so.ObjectCode : null,
            so_ObjectName = x.so != null ? x.so.ObjectName : null,
            so_ServiceCode = x.so != null ? x.so.ServiceCode : null,
            so_ObjectType = x.so != null ? x.so.ObjectType : null,
            so_FlagExecModal = x.so != null ? x.so.FlagExecModal : null,
            so_FlagActive = x.so != null ? x.so.FlagActive : null,
        }).ToList();
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action Index của Sys_AccessController.
/// </summary>
public class Sys_Access_IndexReq
{
    public string? FlagActive { get; set; }
    public string? ObjectType { get; set; }
    public string? ObjectCode { get; set; }
    public string? GroupCode { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action GetDetailData của Sys_AccessController.
/// Nhận objectcode từ client (Sys_AccessController.cs:110).
/// </summary>
public class Sys_Access_GetDetailDataReq
{
    public string? ObjectCode { get; set; }
    public string? GroupCode { get; set; }
}

/// <summary>
/// DTO trả về cho action Index của Sys_AccessController.
/// Gồm danh sách menu Sys_Object (Model View) và danh sách phân quyền Sys_Access (ViewBag.ListSysAccess).
/// </summary>
public class Sys_Access_IndexData
{
    public List<Sys_Object> ListSysObject { get; set; } = new();
    public List<Sys_Object> Lst_Sys_Object { get; set; } = new();
    public List<Sys_Access> ListSysAccess { get; set; } = new();
    public List<Sys_Access> Lst_Sys_Access { get; set; } = new();
    public string ObjectCode { get; set; } = "";
}
