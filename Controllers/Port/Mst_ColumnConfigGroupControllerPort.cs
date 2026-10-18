using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ColumnConfigGroupController.cs — Mst_ColumnConfigGroup
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ColumnConfigGroupController.cs:19-96 (Index), 97-178 (DoSearch)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstColumnConfigGroupController.cs:19-75
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:52079-52275, 52908-53064
[ApiController]
[Route("api/mst_columnconfiggroup")]
public class Mst_ColumnConfigGroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_ColumnConfigGroupControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Mst_ColumnConfigGroupController.cs:19-96.
    /// Chuỗi gọi: gate :19-96 -> WA_Mst_ColumnConfigGroup_Get (:79) -> WAS_Mst_ColumnConfigGroup_Get -> Mst_ColumnConfigGroup_Get -> Mst_ColumnConfigGroup_GetX (Master.cs:52908-53064).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ColumnConfigGroup_IndexReq? req)
    {
        // 1. Phân trang: Gate dòng 20-22, 71-72:
        // page mặc định 0, recordcount mặc định 10.
        // Ft_RecordStart = (page * recordcount).ToString(), Ft_RecordCount = recordcount.ToString()
        int pageIndex = req?.Page ?? req?.PageIndex ?? 0;
        int pageSize = req?.Recordcount ?? req?.PageSize ?? 10;
        long recordStart = (long)pageIndex * pageSize;
        long recordCount = pageSize;

        // 2. Gọi GetXAsync tương ứng Mst_ColumnConfigGroup_GetX (Master.cs:52908-53064)
        var (itemCount, dataList) = await GetXAsync(req, recordStart, recordCount);

        // 3. Tính PageCount theo logic tầng gate Mst_ColumnConfigGroupController.cs:80-92:
        // if (objRT_Mst_ColumnConfigGroupCur.Lst_Mst_ColumnConfigGroup != null && objRT_Mst_ColumnConfigGroupCur.Lst_Mst_ColumnConfigGroup.Count > 0)
        // {
        //     pageInfo.DataList = objRT_Mst_ColumnConfigGroupCur.Lst_Mst_ColumnConfigGroup;
        //     itemCount = objRT_Mst_ColumnConfigGroupCur.MySummaryTable == null ? 0 : Convert.ToInt32(objRT_Mst_ColumnConfigGroupCur.MySummaryTable.MyCount);
        // }
        // pageCount = (itemCount % recordcount == 0) ? itemCount / recordcount : itemCount / recordcount + 1;
        // Khi dataList rỗng (hoặc pageSize <= 0) thì pageCount = 0 (bài học LESSON A.7, A.12).
        var pageCount = 0;
        if (dataList.Count > 0 && pageSize > 0)
        {
            pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
        }

        var pageInfo = new PageInfo<Mst_ColumnConfigGroup>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    /// <summary>
    /// Port 1:1 action DoSearch từ gate Mst_ColumnConfigGroupController.cs:97-178.
    /// Chuỗi gọi: gate :97-178 -> WA_Mst_ColumnConfigGroup_Get (:154) -> WAS_Mst_ColumnConfigGroup_Get -> Mst_ColumnConfigGroup_Get -> Mst_ColumnConfigGroup_GetX (Master.cs:52908-53064).
    /// </summary>
    [HttpPost("dosearch")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ColumnConfigGroup_DoSearchReq? req)
    {
        // 1. Phân trang: Gate dòng 98-100, 147-148:
        // page mặc định 0, recordcount mặc định 10.
        // Ft_RecordStart = (Convert.ToInt32(page) * recordcount).ToString(), Ft_RecordCount = recordcount.ToString()
        int pageIndex = req?.Page ?? req?.PageIndex ?? 0;
        int pageSize = req?.Recordcount ?? req?.PageSize ?? 10;
        long recordStart = (long)pageIndex * pageSize;
        long recordCount = pageSize;

        // 2. Gọi GetXAsync tương ứng Mst_ColumnConfigGroup_GetX (Master.cs:52908-53064)
        var (itemCount, dataList) = await GetXAsync(req, recordStart, recordCount);

        // 3. Tính PageCount theo logic tầng gate Mst_ColumnConfigGroupController.cs:155-167:
        // if (objRT_Mst_ColumnConfigGroupCur.Lst_Mst_ColumnConfigGroup != null && objRT_Mst_ColumnConfigGroupCur.Lst_Mst_ColumnConfigGroup.Count > 0)
        // {
        //     pageInfo.DataList = objRT_Mst_ColumnConfigGroupCur.Lst_Mst_ColumnConfigGroup;
        //     itemCount = objRT_Mst_ColumnConfigGroupCur.MySummaryTable == null ? 0 : Convert.ToInt32(objRT_Mst_ColumnConfigGroupCur.MySummaryTable.MyCount);
        // }
        // pageCount = (itemCount % recordcount == 0) ? itemCount / recordcount : itemCount / recordcount + 1;
        // Khi dataList rỗng (hoặc pageSize <= 0) thì pageCount = 0 (bài học LESSON A.7, A.12).
        var pageCount = 0;
        if (dataList.Count > 0 && pageSize > 0)
        {
            pageCount = (itemCount % pageSize == 0) ? (itemCount / pageSize) : (itemCount / pageSize + 1);
        }

        var pageInfo = new PageInfo<Mst_ColumnConfigGroup>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new
        {
            Success = true,
            Data = pageInfo
        });
    }

    /// <summary>
    /// Thực thi truy vấn theo Mst_ColumnConfigGroup_GetX (Master.cs:52908-53064).
    /// </summary>
    private async Task<(int itemCount, List<Mst_ColumnConfigGroup> dataList)> GetXAsync(
        Mst_ColumnConfigGroup_IndexReq? req,
        long recordStart,
        long recordCount)
    {
        var query = _db.Set<Mst_ColumnConfigGroup>().AsNoTracking();

        // Lọc theo strWhereClause_Mst_ColumnConfigGroup (Mst_ColumnConfigGroupController.cs:512-539):
        var code = req?.ColumnConfigGrpCode?.Trim();
        if (!string.IsNullOrEmpty(code))
        {
            query = query.Where(x => EF.Functions.Like((x.ColumnConfigGrpCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));
        }

        var name = req?.ColumnGrpName?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(x => EF.Functions.Like((x.ColumnGrpName ?? "").ToLower(), PortQuery.Contains(name), PortQuery.Esc));
        }

        var flagActive = req?.FlagActive?.Trim();
        if (!string.IsNullOrEmpty(flagActive))
        {
            query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
        }

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

        // Đếm tổng số bản ghi: select Count(0) MyCount (Master.cs:52974)
        var itemCount = await query.CountAsync();

        // Sắp xếp: order by mo.ColumnConfigGrpCode asc (Master.cs:52970)
        query = query.OrderBy(x => x.ColumnConfigGrpCode).ThenBy(x => x.OrgID);

        // Phân trang: (Master.cs:52941-52942, 52983-52984; LESSON A.12)
        var dataList = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_ColumnConfigGroup>()
            : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                         .Take((int)Math.Min(recordCount, int.MaxValue))
                         .ToListAsync();

        return (itemCount, dataList);
    }
}

public class Mst_ColumnConfigGroup_IndexReq
{
    public string? Init { get; set; } = "init";
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? Recordcount { get; set; }
    public int? PageSize { get; set; }
    public string? ColumnConfigGrpCode { get; set; }
    public string? ColumnGrpName { get; set; }
    public string? FlagActive { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}

public class Mst_ColumnConfigGroup_DoSearchReq : Mst_ColumnConfigGroup_IndexReq
{
}
