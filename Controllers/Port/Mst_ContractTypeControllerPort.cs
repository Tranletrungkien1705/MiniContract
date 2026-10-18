using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ContractTypeController.cs — Mst_ContractType
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ContractTypeController.cs:26-90 (Index), 92-95 (Create GET), 97-146 (Create POST)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstContractTypeController.cs:20-88 (Index), 89-140 (Create)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:43978-44048 (CheckDB), 44299-44548 (WAS/Biz Create), 45024-45180 (Search/Index), 45181-45278 (CreateX)
[ApiController]
[Route("api/mst_contracttype")]
public class Mst_ContractTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_ContractTypeControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
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

    /// <summary>
    /// Tạo mới loại hợp đồng.
    /// Gate: Mst_ContractTypeController.cs:92-95 (GET View), 97-146 Create(string model)
    /// API:  MstContractTypeController.cs:89-140 WA_Mst_ContractType_Create
    /// BIZ:  Master.cs:44299-44548 WAS_Mst_ContractType_Create -> Mst_ContractType_Create:44384-44548 -> Mst_ContractType_CreateX:45181-45278
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ContractType_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc thiếu dữ liệu (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_ContractType_Create", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ContractTypeController.cs:99, 111)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_ContractType_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.NetworkID ??= modelObj.NetworkID;
                    req.ContractType ??= modelObj.ContractType;
                    req.OrgID ??= modelObj.OrgID;
                    req.ContractTypeName ??= modelObj.ContractTypeName;
                    req.Description ??= modelObj.Description;
                    req.FlagActive ??= modelObj.FlagActive;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContractType) && string.IsNullOrWhiteSpace(req.ContractTypeName) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_ContractType_Create", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:45215-45220
        var networkId = req.NetworkID != null ? StdParam(req.NetworkID) : (_config?["Biz_NetworkID"] ?? "1001");
        if (string.IsNullOrEmpty(networkId))
        {
            return PortResult.Fail("Mst_ContractType_Create_InvalidNetworkID", "Check.strNetworkID", req.NetworkID);
        }

        var contractType = StdParam(req.ContractType);
        if (string.IsNullOrEmpty(contractType))
        {
            return PortResult.Fail("Mst_ContractType_Create", "Check.ContractType", req.ContractType);
        }

        var orgId = req.OrgID != null ? StdParam(req.OrgID) : (_config?["Biz_OrgID"] ?? "7200000001");
        var contractTypeName = req.ContractTypeName?.Trim() ?? ""; // Master.cs:45218 string.Format("{0}", objContractTypeName)
        var description = req.Description?.Trim() ?? ""; // Master.cs:45219 string.Format("{0}", objDescription)
        var dtimeSys = DateTime.UtcNow; // Master.cs:44310, 44409, 45266
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port"; // Master.cs:45267

        var entity = new Mst_ContractType
        {
            NetworkID = networkId,
            ContractType = contractType,
            OrgID = orgId ?? "",
            ContractTypeName = contractTypeName,
            Description = description,
            FlagActive = "1", // Master.cs:45265 TConst.Flag.Active
            LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = userCode
        };

        // 3. Mở transaction và kiểm tra trùng lặp khóa chính trong DB (Master.cs:44431, 45240-45247, 43988-44030)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.Set<Mst_ContractType>()
                .FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc) &&
                    EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contractType), PortQuery.Esc));

            if (existing != null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ContractType_CheckDB_OrganExist",
                    "Check.NetworkID", networkId,
                    "Check.ContractType", contractType);
            }

            // 4. Lưu bản ghi mới vào DB (Master.cs:45258-45275)
            _db.Set<Mst_ContractType>().Add(entity);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ContractType_Create", "Exception", ex.Message);
        }

        // 5. Trả kết quả thành công khớp gate MVC (Mst_ContractTypeController.cs:135-138)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Tạo mới loại hợp đồng thành công!" },
            Title = "Tạo mới loại hợp đồng thành công!",
            RedirectUrl = "/Mst_ContractType",
            Data = entity
        });
    }

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    private static string? StdStr(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.Normalize();
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

/// <summary>
/// DTO tham số đầu vào cho action Create.
/// Gate: Mst_ContractTypeController.cs:99 Create(string model)
/// BIZ:  Master.cs:45205-45210
/// </summary>
public class Mst_ContractType_CreateReq
{
    public string? Model { get; set; }
    public string? NetworkID { get; set; }
    public string? ContractType { get; set; }
    public string? OrgID { get; set; }
    public string? ContractTypeName { get; set; }
    public string? Description { get; set; }
    public string? FlagActive { get; set; }
}
