using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_AreaController.cs — Mst_Area
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_AreaController.cs:22-125 (Index), 131-197 (Export), 348-445 (Detail)
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
    /// Danh sách / Tìm kiếm phân trang khu vực thị trường.
    /// Gate: Mst_AreaController.cs:22-125 Index(string init = "init", int recordcount = 10, int page = 0)
    /// API:  MstAreaController.cs:19-89 WA_Mst_Area_Get
    /// BIZ:  Customer.cs:101-189 (WAS_Mst_Area_Get), 190-316 (Mst_Area_Get), 318-474 (Mst_Area_GetX)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Area_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_AreaController.cs:22, 73-74:
            // recordcount mặc định 10, page mặc định 0.
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart;
            long recordCount;
            if (req?.Ft_RecordStart != null)
            {
                recordStart = req.Ft_RecordStart.Value;
                recordCount = req.Ft_RecordCount ?? pageSize;
            }
            else
            {
                recordStart = (long)pageIndex * pageSize;
                recordCount = req?.Ft_RecordCount ?? pageSize;
            }

            // 2. Dựng truy vấn cơ sở từ bảng Mst_Area (Customer.cs:377, 417)
            var query = BuildQuery(req?.OrgID, req?.NetworkID, req?.FlagActive, req?.AreaCode);

            // 3. Đếm tổng số bản ghi (Customer.cs:384: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 4. Lấy dữ liệu trang (Customer.cs:393-394, LESSON A.7, A.12)
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Area>()
                : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                             .Take((int)Math.Min(recordCount, int.MaxValue))
                             .ToListAsync();

            // 5. Tính số trang (Gate: Mst_AreaController.cs:92: (itemCount % recordcount == 0) ? itemCount / recordcount : itemCount / recordcount + 1)
            var pageCount = 0;
            if (list.Count > 0 && pageSize > 0)
            {
                pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
            }

            // 6. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
            return Ok(new
            {
                Success = true,
                Data = new PageInfo<Mst_Area>(list, itemCount, pageCount, pageIndex, pageSize)
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Area_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xuất danh sách khu vực ra file Excel.
    /// Gate: Mst_AreaController.cs:131-197 Export()
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Area_ExportReq? req)
    {
        // 1. Dựng truy vấn theo strWhereClause_Mst_Area (Mst_AreaController.cs:666-689) và Customer.cs:370-424
        var query = BuildQuery(req?.OrgID, req?.NetworkID, req?.FlagActive, req?.AreaCode);

        // 2. Phân trang: Ft_RecordStart / Ft_RecordCount (Customer.cs:351-352, 393-394, LESSON A.12)
        var recordStart = req?.Ft_RecordStart ?? 0;
        var recordCount = req?.Ft_RecordCount ?? int.MaxValue;
        var list = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_Area>()
            : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                         .Take((int)Math.Min(recordCount, int.MaxValue))
                         .ToListAsync();

        // 3. Kiểm tra danh sách rỗng (Mst_AreaController.cs:184-187)
        if (list.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Title = "Không có dữ liệu!",
                CheckSuccess = "1"
            });
        }

        // 4. Xuất Excel và trả URL (Mst_AreaController.cs:175-183)
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

    /// <summary>
    /// Xem chi tiết vùng thị trường (Mst_Area).
    /// Gate: Mst_AreaController.cs:348-445 Detail(string areaCode)
    /// API:  MstAreaController.cs:19-89 WA_Mst_Area_Get
    /// BIZ:  Customer.cs:101-189 (WAS_Mst_Area_Get), 190-316 (Mst_Area_Get), 318-474 (Mst_Area_GetX)
    /// </summary>
    [HttpPost("detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Area_DetailReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào areaCode (Gate: Mst_AreaController.cs:348, 404)
            var areaCode = StdParam(req?.AreaCode);
            var orgId = StdParam(req?.OrgID) ?? StdParam(Request?.Headers["OrgID"].FirstOrDefault());
            var networkId = StdParam(req?.NetworkID) ?? StdParam(Request?.Headers["NetworkID"].FirstOrDefault());

            // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
            if (string.IsNullOrEmpty(areaCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Area_DetailReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    areaCode = StdParam(modelObj?.AreaCode);
                    orgId ??= StdParam(modelObj?.OrgID);
                    networkId ??= StdParam(modelObj?.NetworkID);
                }
                catch
                {
                    areaCode = StdParam(req.Model);
                }
            }

            // 2. Nếu areaCode rỗng, gate trả về View(new Mst_Area()) (Gate: Mst_AreaController.cs:404, 444)
            if (string.IsNullOrEmpty(areaCode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Area_DetailData?)null
                });
            }

            // 3. Dựng truy vấn tìm bản ghi Mst_Area theo AreaCode (Gate: Mst_AreaController.cs:406-438; Customer.cs:377, 417)
            var query = BuildQuery(orgId, networkId, null, areaCode);
            var item = await query.FirstOrDefaultAsync();

            if (item == null)
            {
                // Gate: dòng 438-444 -> nếu không tìm thấy trả về View(new Mst_Area())
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Area_DetailData?)null
                });
            }

            // 4. Lấy danh sách vùng thị trường cha để phục vụ dropdown (Gate: Mst_AreaController.cs:362-402: ListMst_AreaExt)
            var parentQuery = BuildQuery(item.OrgID, item.NetworkID, null, null);
            var listExt = await parentQuery.ToListAsync();

            var detailData = new Mst_Area_DetailData(item)
            {
                ListMst_AreaExt = listExt
            };

            return Ok(new
            {
                Success = true,
                Data = detailData
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Area_Get", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpper();
    }

    private static string? StdStr(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static string StdFlag(string? value)
    {
        return value?.Trim() == "1" ? "1" : "0";
    }

    private IQueryable<Mst_Area> BuildQuery(string? orgId, string? networkId, string? flagActive, string? areaCode)
    {
        var query = _db.Set<Mst_Area>().AsNoTracking();

        var oId = orgId?.Trim();
        if (!string.IsNullOrEmpty(oId))
        {
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(oId), PortQuery.Esc));
        }

        var nId = networkId?.Trim();
        if (!string.IsNullOrEmpty(nId))
        {
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(nId), PortQuery.Esc));
        }

        var fActive = flagActive?.Trim();
        if (!string.IsNullOrEmpty(fActive))
        {
            query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(fActive), PortQuery.Esc));
        }

        var aCode = areaCode?.Trim();
        if (!string.IsNullOrEmpty(aCode))
        {
            query = query.Where(x => EF.Functions.Like((x.AreaCode ?? "").ToLower(), PortQuery.Equal(aCode), PortQuery.Esc));
        }

        // Sắp xếp: order by mo.OrgID asc, t.MyIdxSeq asc (Customer.cs:380, 420)
        return query.OrderBy(x => x.OrgID).ThenBy(x => x.AreaCode);
    }
}

public class Mst_Area_IndexReq
{
    public string? Init { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
    public string? AreaCode { get; set; }
    public string? AreaName { get; set; }
    public string? KeyWord { get; set; }
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

public class Mst_Area_DetailReq
{
    public string? AreaCode { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? Model { get; set; }
}

public class Mst_Area_DetailData : Mst_Area
{
    public List<Mst_Area>? ListMst_AreaExt { get; set; }

    public Mst_Area_DetailData() { }

    public Mst_Area_DetailData(Mst_Area src)
    {
        OrgID = src.OrgID;
        AreaCode = src.AreaCode;
        NetworkID = src.NetworkID;
        AreaCodeParent = src.AreaCodeParent;
        AreaBUCode = src.AreaBUCode;
        AreaBUPattern = src.AreaBUPattern;
        AreaLevel = src.AreaLevel;
        AreaName = src.AreaName;
        AreaDesc = src.AreaDesc;
        FlagActive = src.FlagActive;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;
    }
}
