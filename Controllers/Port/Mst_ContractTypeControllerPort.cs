using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ContractTypeController.cs — Mst_ContractType
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ContractTypeController.cs:26-90
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstContractTypeController.cs:20-88
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:44050-44141, 44142-44297, 45024-45180
[ApiController]
[Route("api/mst_contracttype")]
public class Mst_ContractTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Mst_ContractTypeControllerPort(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Danh sách / Tìm kiếm phân trang loại hợp đồng.
    /// Gate: Mst_ContractTypeController.cs:26-90 Index(string init = "init", int recordcount = 10, int page = 0)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ContractType_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_ContractTypeController.cs:26, 61-62
            var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
            var pageIndex = req?.Page ?? req?.PageIndex ?? 0;

            long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
            long recordCount = req?.Ft_RecordCount ?? pageSize;

            // 2. Dựng truy vấn lọc theo các cột của Mst_ContractType (Master.cs:45078-45087, 45139-45145)
            var query = _db.Set<Mst_ContractType>().AsNoTracking();

            var networkId = req?.NetworkID?.Trim();
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var orgId = req?.OrgID?.Trim();
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            var contractType = req?.ContractType?.Trim();
            if (!string.IsNullOrEmpty(contractType))
            {
                query = query.Where(x => EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contractType), PortQuery.Esc));
            }

            var contractTypeName = req?.ContractTypeName?.Trim();
            if (!string.IsNullOrEmpty(contractTypeName))
            {
                query = query.Where(x => EF.Functions.Like((x.ContractTypeName ?? "").ToLower(), PortQuery.Contains(contractTypeName), PortQuery.Esc));
            }

            var flagActive = req?.FlagActive?.Trim();
            if (!string.IsNullOrEmpty(flagActive))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            var keyWord = req?.KeyWord?.Trim();
            if (!string.IsNullOrEmpty(keyWord))
            {
                query = query.Where(x => EF.Functions.Like((x.ContractTypeName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)
                                      || EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
            }

            // 3. Sắp xếp: order by mo.NetworkID asc (Master.cs:45086), sau đó theo ContractType để kết quả ổn định
            query = query.OrderBy(x => x.NetworkID).ThenBy(x => x.ContractType);

            // 4. Tổng số dòng (MyCount: Master.cs:45090)
            var itemCount = await query.CountAsync();

            // 5. Phân trang theo mẫu chuẩn LESSON A.12, A.7
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_ContractType>()
                : await query.Skip((int)Math.Min(recordStart, int.MaxValue))
                             .Take((int)Math.Min(recordCount, int.MaxValue))
                             .ToListAsync();

            // 6. Tính PageCount: khi rỗng thì PageCount = 0 (gate :47), có dòng thì tính theo cỡ trang (gate :76)
            var pageCount = 0;
            if (list.Count > 0)
            {
                var ps = pageSize <= 0 ? 10 : pageSize;
                pageCount = (itemCount % ps == 0) ? (itemCount / ps) : (itemCount / ps + 1);
            }

            var pageInfo = new PageInfo<Mst_ContractType>(list, itemCount, pageCount, pageIndex, pageSize);

            return Ok(new
            {
                Success = true,
                Data = pageInfo
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_ContractType_Get", "Exception", ex.Message);
        }
    }
}

public class Mst_ContractType_IndexReq
{
    // Tham số từ Gate MVC Mst_ContractTypeController.cs:26
    public string? Init { get; set; } = "init";
    public int? RecordCount { get; set; }
    public int? Page { get; set; }

    // Các alias phân trang (LESSON A.12, A.14, PortQuery.PageInfo)
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    // Bộ lọc theo các cột của Mst_ContractType (Master.cs:45084-45085, 45139-45145)
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? ContractType { get; set; }
    public string? ContractTypeName { get; set; }
    public string? FlagActive { get; set; }
    public string? KeyWord { get; set; }
}
