using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_Sys_ConfigController.cs — Mst_Sys_Config
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_Sys_ConfigController.cs:21-90 (Index)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstAttributeContractController.cs:20-88 (WA_Mst_Attribute_Contract_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstSysConfigController.cs:20-88 (WA_Mst_Sys_Config_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7224-7312 (WAS_Mst_Sys_Config_Get) -> 7313-7439 (Mst_Sys_Config_Get) -> 7441-7597 (Mst_Sys_Config_GetX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7974-8070 (WAS_Mst_Attribute_Contract_Get) -> 8071-8227 (Mst_Attribute_Contract_Get) -> 8923-9052 (Mst_Attribute_Contract_GetX)
[ApiController]
[Route("api/mst_sys_config")]
public class Mst_Sys_ConfigControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_Sys_ConfigControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Mst_Sys_ConfigController.cs:21-90.
    /// Chuỗi gọi:
    /// 1. Mst_Attribute_ContractService.Instance.WA_Mst_Attribute_Contract_Get (gate dòng 53)
    ///    -> API WA_Mst_Attribute_Contract_Get -> WAS_Mst_Attribute_Contract_Get -> Mst_Attribute_Contract_Get -> Mst_Attribute_Contract_GetX (QContract.cs:8923-9052)
    ///    Lấy danh sách thuộc tính hợp đồng, gán vào ViewBag.ListMst_Attribute_Contract.
    /// 2. Mst_Sys_ConfigService.Instance.WA_Mst_Sys_Config_Get (gate dòng 83)
    ///    -> API WA_Mst_Sys_Config_Get -> WAS_Mst_Sys_Config_Get -> Mst_Sys_Config_Get -> Mst_Sys_Config_GetX (QContract.cs:7441-7597)
    ///    Lấy thông tin cấu hình hệ thống theo OrgID.
    /// 3. Nếu tìm thấy cấu hình (Lst_Mst_Sys_Config.Count > 0): trả về View(model), ngược lại trả về View() (gate dòng 85-89).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Sys_Config_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (Lesson A.14, A.21, PORT_RULES §3)
            var orgId = StdParam(req?.OrgID);
            var networkId = StdParam(req?.NetworkID);

            // 2. Lấy thông tin thuộc tính hợp đồng Mst_Attribute_Contract (QContract.cs:8923-9052, gate dòng 31-59)
            // SQL nguồn: select distinct mo.AttributeContractCode from Mst_Attribute_Contract mo order by mo.AttributeContractCode asc
            var queryAttr = _db.Set<Mst_Attribute_Contract>().AsNoTracking();
            if (!string.IsNullOrEmpty(networkId))
            {
                queryAttr = queryAttr.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listMst_Attribute_Contract = await queryAttr
                .OrderBy(x => x.AttributeContractCode)
                .ToListAsync();

            // 3. Lấy thông tin cấu hình hệ thống Mst_Sys_Config (QContract.cs:7441-7597, gate dòng 61-89)
            // SQL nguồn: select distinct mo.NetworkID, mo.OrgID from Mst_Sys_Config mo where ... order by mo.NetworkID asc
            var queryConfig = _db.Set<Mst_Sys_Config>().AsNoTracking();
            if (!string.IsNullOrEmpty(orgId))
            {
                queryConfig = queryConfig.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                queryConfig = queryConfig.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // Sắp xếp theo NetworkID asc (QContract.cs:7503)
            queryConfig = queryConfig.OrderBy(x => x.NetworkID);

            var itemCount = await queryConfig.CountAsync();
            var listMst_Sys_Config = await queryConfig.ToListAsync();

            // 4. Trả về kết quả khớp gate MVC: Model là Mst_Sys_Config (hoặc Lst_Mst_Sys_Config) và ViewBag chứa ListMst_Attribute_Contract (Mst_Sys_ConfigController.cs:58, 85-89)
            var data = new Mst_Sys_Config_IndexData
            {
                Mst_Sys_Config = listMst_Sys_Config.FirstOrDefault(),
                Lst_Mst_Sys_Config = listMst_Sys_Config,
                ListMst_Attribute_Contract = listMst_Attribute_Contract,
                Lst_Mst_Attribute_Contract = listMst_Attribute_Contract,
                ItemCount = itemCount
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Sys_Config_Get", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action Index của Mst_Sys_ConfigController.
/// </summary>
public class Mst_Sys_Config_IndexReq
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? Recordcount { get; set; }
    public int? PageSize { get; set; }
    public int? Ft_RecordStart { get; set; }
    public int? Ft_RecordCount { get; set; }
    public string? Ft_WhereClause { get; set; }
}

/// <summary>
/// DTO trả về cho action Index của Mst_Sys_ConfigController.
/// Bao gồm thông tin cấu hình hệ thống (Model View) và danh sách thuộc tính hợp đồng (ViewBag.ListMst_Attribute_Contract).
/// </summary>
public class Mst_Sys_Config_IndexData
{
    public Mst_Sys_Config? Mst_Sys_Config { get; set; }
    public List<Mst_Sys_Config> Lst_Mst_Sys_Config { get; set; } = new();
    public List<Mst_Attribute_Contract> ListMst_Attribute_Contract { get; set; } = new();
    public List<Mst_Attribute_Contract> Lst_Mst_Attribute_Contract { get; set; } = new();
    public int ItemCount { get; set; }
}
