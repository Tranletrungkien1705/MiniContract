using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_FinishedContractReasonController.cs — Mst_FinishedContractReason
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_FinishedContractReasonController.cs:21-85 (Index), 87-90 (Create GET), 92-140 (Create POST)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstFinishedContractReasonController.cs:20-88 (Index), 89-140 (Create)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:45487-45558 (CheckDB), 45814-46046 (WAS/Biz Create), 46529-46685 (Search/Index), 46686-46781 (CreateX)
[ApiController]
[Route("api/mst_finishedcontractreason")]
public class Mst_FinishedContractReasonControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_FinishedContractReasonControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Tìm kiếm và phân trang danh sách nguyên nhân chấm dứt hợp đồng.
    /// Gate: Mst_FinishedContractReasonController.cs:21-85 Index(string init = "init", int recordcount = 10, int page = 0)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_IndexReq? req)
    {
        // 1. Dựng truy vấn cơ sở từ bảng Mst_FinishedContractReason (Master.cs:46581-46592)
        var query = _db.Set<Mst_FinishedContractReason>().AsNoTracking();

        // 2. Bộ lọc điều kiện theo các cột hỗ trợ (Master.cs:46640-46664)
        var networkId = req?.NetworkID?.Trim();
        if (!string.IsNullOrEmpty(networkId))
        {
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
        }

        var reasonCode = req?.ContractFinishReasonCode?.Trim();
        if (!string.IsNullOrEmpty(reasonCode))
        {
            query = query.Where(x => EF.Functions.Like((x.ContractFinishReasonCode ?? "").ToLower(), PortQuery.Equal(reasonCode), PortQuery.Esc));
        }

        var keyWord = req?.KeyWord?.Trim() ?? req?.ContractFinishReasonName?.Trim();
        if (!string.IsNullOrEmpty(keyWord))
        {
            query = query.Where(x => EF.Functions.Like((x.ContractFinishReasonName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
        }

        var flagActive = req?.FlagActive?.Trim();
        if (!string.IsNullOrEmpty(flagActive))
        {
            query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
        }

        var finishType = req?.FinishType?.Trim();
        if (!string.IsNullOrEmpty(finishType))
        {
            query = query.Where(x => EF.Functions.Like((x.FinishType ?? "").ToLower(), PortQuery.Equal(finishType), PortQuery.Esc));
        }

        // 3. Đếm tổng số bản ghi phù hợp (Master.cs:46594-46596: select Count(0) MyCount from ...)
        var itemCount = await query.CountAsync();

        // 4. Sắp xếp: order by mo.NetworkID asc (Master.cs:46591, 46631)
        var ordered = query.OrderBy(x => x.NetworkID).ThenBy(x => x.ContractFinishReasonCode);

        // 5. Tính toán phân trang (Gate: Mst_FinishedContractReasonController.cs:21, 56-57; BIZ: Master.cs:46562-46563, 46604-46605)
        var recordcount = req?.RecordCount ?? req?.Ft_RecordCount ?? req?.Ft_PageSize ?? req?.PageSize ?? 10;
        var page = req?.Page ?? req?.Ft_PageIndex ?? req?.PageIndex ?? 0;

        long recordStart;
        if (req != null && req.Ft_RecordStart != null && req.Page == null && req.Ft_PageIndex == null && req.PageIndex == null)
        {
            recordStart = req.Ft_RecordStart.Value;
            page = recordcount > 0 ? (int)(recordStart / recordcount) : 0;
        }
        else
        {
            recordStart = (long)page * recordcount;
        }

        // 6. Lấy dữ liệu trang (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
        var list = (recordcount <= 0 || recordStart < 0)
            ? new List<Mst_FinishedContractReason>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordcount, int.MaxValue))
                           .ToListAsync();

        // 7. Tính số trang (Gate: Mst_FinishedContractReasonController.cs:71: (itemCount % PageSizeConfig == 0) ? ... : ... + 1)
        const int pageSizeConfig = 10;
        var ps = recordcount > 0 ? recordcount : pageSizeConfig;
        var pageCount = list.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        // 8. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
        return Ok(new
        {
            Success = true,
            Data = new PageInfo<Mst_FinishedContractReason>(list, itemCount, pageCount, page, recordcount)
        });
    }

    /// <summary>
    /// Tạo mới nguyên nhân chấm dứt hợp đồng.
    /// Gate: Mst_FinishedContractReasonController.cs:87-90 (GET View), 92-140 Create(string model)
    /// API:  MstFinishedContractReasonController.cs:89-140 WA_Mst_FinishedContractReason_Create
    /// BIZ:  Master.cs:45814-46046 WAS_Mst_FinishedContractReason_Create -> Mst_FinishedContractReason_Create -> Mst_FinishedContractReason_CreateX:46686-46781
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc thiếu dữ liệu (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Create", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_FinishedContractReasonController.cs:94, 106)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_FinishedContractReason_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.NetworkID ??= modelObj.NetworkID;
                    req.ContractFinishReasonCode ??= modelObj.ContractFinishReasonCode;
                    req.ContractFinishReasonName ??= modelObj.ContractFinishReasonName;
                    req.FinishDescription ??= modelObj.FinishDescription;
                    req.FinishType ??= modelObj.FinishType;
                    req.FlagActive ??= modelObj.FlagActive;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContractFinishReasonCode) && string.IsNullOrWhiteSpace(req.ContractFinishReasonName) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_FinishedContractReason_Create", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:46718-46723
        // NetworkID: ưu tiên từ req, nếu không truyền (null) thì lấy từ cấu hình hoặc mặc định "1001" (gate lấy từ UserState: Mst_FinishedContractReasonController.cs:99, 107)
        var networkId = req.NetworkID != null ? StdParam(req.NetworkID) : (_config?["Biz_NetworkID"] ?? "1001");
        if (string.IsNullOrEmpty(networkId))
        {
            // Master.cs:46731-46741
            return PortResult.Fail("Mst_FinishedContractReason_Create_InvalidNetworkID", "Check.strNetworkID", req.NetworkID);
        }

        var reasonCode = StdParam(req.ContractFinishReasonCode);
        if (string.IsNullOrEmpty(reasonCode))
        {
            return PortResult.Fail("Mst_FinishedContractReason_Create", "Check.ContractFinishReasonCode", req.ContractFinishReasonCode);
        }

        var reasonName = req.ContractFinishReasonName?.Trim() ?? ""; // Master.cs:46720 string.Format("{0}", objContractFinishReasonName)
        var finishType = "FINISHED"; // Master.cs:46721 TConst.ContractFinishType.FINISHED
        var finishDesc = req.FinishDescription?.Trim() ?? ""; // Master.cs:46722 string.Format("{0}", objFinishDescription)
        var dtimeSys = DateTime.UtcNow; // Master.cs:45825, 45916, 46769
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port"; // Master.cs:46770

        var entity = new Mst_FinishedContractReason
        {
            NetworkID = networkId,
            ContractFinishReasonCode = reasonCode,
            ContractFinishReasonName = reasonName,
            FinishType = finishType,
            FinishDescription = finishDesc,
            FlagActive = "1", // Master.cs:46768 TConst.Flag.Active
            LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = userCode
        };

        // 3. Mở transaction và kiểm tra trùng lặp khóa chính trong DB (Master.cs:45934, 46743-46750, 45528-45538)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.Set<Mst_FinishedContractReason>()
                .FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc) &&
                    EF.Functions.Like((x.ContractFinishReasonCode ?? "").ToLower(), PortQuery.Equal(reasonCode), PortQuery.Esc));

            if (existing != null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_FinishedContractReason_CheckDB_OrganExist",
                    "Check.NetworkID", networkId,
                    "Check.ContractFinishReasonCode", reasonCode);
            }

            // 4. Lưu bản ghi mới vào DB (Master.cs:46758-46779)
            _db.Set<Mst_FinishedContractReason>().Add(entity);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_FinishedContractReason_Create", "Exception", ex.Message);
        }

        // 5. Trả kết quả thành công khớp gate MVC (Mst_FinishedContractReasonController.cs:129-132)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Tạo mới nguyên nhân chấm dứt hợp đồng thành công!" },
            Title = "Tạo mới nguyên nhân chấm dứt hợp đồng thành công!",
            RedirectUrl = "/Mst_FinishedContractReason",
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

/// <summary>
/// DTO tham số đầu vào cho action Create.
/// </summary>
public class Mst_FinishedContractReason_CreateReq
{
    public string? Model { get; set; }
    public string? NetworkID { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? ContractFinishReasonName { get; set; }
    public string? FinishType { get; set; }
    public string? FinishDescription { get; set; }
    public string? FlagActive { get; set; }
}

/// <summary>
/// DTO tham số đầu vào cho action Index.
/// Hỗ trợ cả tên tham số của tầng Gate MVC (page, recordcount, init) lẫn các tên chuẩn hóa phân trang/lọc.
/// </summary>
public class Mst_FinishedContractReason_IndexReq
{
    // Tham số tầng Gate MVC (Mst_FinishedContractReasonController.cs:21)
    public string? Init { get; set; } = "init";
    public int? RecordCount { get; set; }
    public int? Page { get; set; }

    // Các biến phân trang thay thế
    public int? Ft_RecordCount { get; set; }
    public int? Ft_PageSize { get; set; }
    public int? PageSize { get; set; }
    public int? Ft_PageIndex { get; set; }
    public int? PageIndex { get; set; }
    public long? Ft_RecordStart { get; set; }

    // Các tham số lọc nghiệp vụ
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? ContractFinishReasonName { get; set; }
    public string? KeyWord { get; set; }
    public string? FinishType { get; set; }
    public string? FlagActive { get; set; }
    public string? Ft_WhereClause { get; set; }
}
