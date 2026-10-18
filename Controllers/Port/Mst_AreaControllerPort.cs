using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_AreaController.cs — Mst_Area
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_AreaController.cs:131-197
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstAreaController.cs:19-89
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Customer\Customer.cs:101-474
[ApiController]
[Route("api/mst_area")]
public class Mst_AreaControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    // Từ điển cột xuất excel theo GetImportDicColums() (Mst_AreaController.cs:199-210)
    private static readonly Dictionary<string, string> DicColNames = new()
    {
        { "AreaCode", "Mã khu vực" },
        { "AreaName", "Tên khu vực" },
        { "AreaDesc", "Mô tả" },
        { "AreaCodeParent", "Mã khu vực cha" },
        { "FlagActive", "Trạng thái" }
    };

    public Mst_AreaControllerPort(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Xuất danh sách khu vực ra file Excel.
    /// Gate: Mst_AreaController.cs:131-197 Export()
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Area_ExportReq? req)
    {
        // 1. Dựng truy vấn theo strWhereClause_Mst_Area (Mst_AreaController.cs:666-689) và Customer.cs:370-424
        var query = _db.Set<Mst_Area>().AsNoTracking();

        var orgId = req?.OrgID?.Trim();
        if (!string.IsNullOrEmpty(orgId))
        {
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
        }

        var networkId = req?.NetworkID?.Trim();
        if (!string.IsNullOrEmpty(networkId))
        {
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
        }

        var flagActive = req?.FlagActive?.Trim();
        if (!string.IsNullOrEmpty(flagActive))
        {
            query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
        }

        var areaCode = req?.AreaCode?.Trim();
        if (!string.IsNullOrEmpty(areaCode))
        {
            query = query.Where(x => EF.Functions.Like((x.AreaCode ?? "").ToLower(), PortQuery.Equal(areaCode), PortQuery.Esc));
        }

        // 2. Sắp xếp: order by mo.OrgID asc, t.MyIdxSeq asc (Customer.cs:380, 420)
        query = query.OrderBy(x => x.OrgID).ThenBy(x => x.AreaCode);

        // 3. Phân trang: Ft_RecordStart / Ft_RecordCount (Customer.cs:351-352, 393-394, LESSON A.12)
        var recordStart = req?.Ft_RecordStart ?? 0;
        var recordCount = req?.Ft_RecordCount ?? int.MaxValue;
        var list = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_Area>()
            : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                         .Take((int)Math.Min(recordCount, int.MaxValue))
                         .ToListAsync();

        // 4. Kiểm tra danh sách rỗng (Mst_AreaController.cs:184-187)
        if (list.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Title = "Không có dữ liệu!",
                CheckSuccess = "1"
            });
        }

        // 5. Xuất Excel và trả URL (Mst_AreaController.cs:175-183)
        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_Area));
            PortExcel.ExportToExcelFromList(list, DicColNames, filePath, nameof(Mst_Area));

            return Ok(new
            {
                Success = true,
                Title = "Đã xuất excel thành công!",
                CheckSuccess = "1",
                strUrl = virtualPath
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Area_Export", "Exception", ex.Message);
        }
    }
}

public class Mst_Area_ExportReq
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
    public string? AreaCode { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
}
