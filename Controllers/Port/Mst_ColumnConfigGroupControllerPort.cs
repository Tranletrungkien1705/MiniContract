using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ColumnConfigGroupController.cs — Mst_ColumnConfigGroup
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ColumnConfigGroupController.cs:19-96 (Index), 97-178 (DoSearch), 181-184 (Create GET), 185-233 (Create POST), 239-290 (Update)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstColumnConfigGroupController.cs:19-75 (Index), 89-153 (Create)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:52007-52078 (CheckDB), 52296-52499 (WAS/Biz Create), 52908-53064 (GetX), 53065-53174 (CreateX)
[ApiController]
[Route("api/mst_columnconfiggroup")]
public class Mst_ColumnConfigGroupControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_ColumnConfigGroupControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
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

    /// <summary>
    /// Tạo mới nhóm cấu hình cột.
    /// Gate: Mst_ColumnConfigGroupController.cs:181-184 (GET View), 185-233 Create(string model)
    /// API:  MstColumnConfigGroupController.cs:89-153 WA_Mst_ColumnConfigGroup_Create
    /// BIZ:  Master.cs:52007-52078 (Mst_ColumnConfigGroup_CheckDB), 52296-52499 (WAS_Mst_ColumnConfigGroup_Create -> Mst_ColumnConfigGroup_Create), 53065-53174 (Mst_ColumnConfigGroup_CreateX)
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ColumnConfigGroup_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc thiếu dữ liệu (LESSON A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_ColumnConfigGroup_Create", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ColumnConfigGroupController.cs:187, 197)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_ColumnConfigGroup_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.OrgID ??= modelObj.OrgID;
                    req.ColumnConfigGrpCode ??= modelObj.ColumnConfigGrpCode;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.ColumnGrpName ??= modelObj.ColumnGrpName;
                    req.ColumnGrpFormat ??= modelObj.ColumnGrpFormat;
                    req.ColumnGrpDesc ??= modelObj.ColumnGrpDesc;
                    req.FlagActive ??= modelObj.FlagActive;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ColumnConfigGrpCode) && string.IsNullOrWhiteSpace(req.OrgID) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_ColumnConfigGroup_Create", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:53098-53106
        var code = StdParam(req.ColumnConfigGrpCode);
        var orgId = StdParam(req.OrgID) ?? StdParam(Request?.Headers["OrgID"].FirstOrDefault());
        var name = req.ColumnGrpName != null ? req.ColumnGrpName.ToString() : "";
        var format = StdParam(req.ColumnGrpFormat);
        var desc = req.ColumnGrpDesc != null ? req.ColumnGrpDesc.ToString() : "";
        var networkId = StdParam(req.NetworkID) ?? _config?["Biz_NetworkID"] ?? "1001";

        // 3. Guard: kiểm tra mã nhóm cấu hình không được rỗng (Master.cs:53112-53121)
        if (string.IsNullOrEmpty(code))
        {
            return PortResult.Fail("Mst_ColumnConfigGroup_Create_InvalidColumnConfigGrpCode", "Check.strColumnConfigGrpCode", req.ColumnConfigGrpCode);
        }

        // 4. Mở transaction và kiểm tra DB (Master.cs:52411, 52462, 52470)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Kiểm tra trùng lặp theo (ColumnConfigGrpCode, OrgID) trong DB (Master.cs:52048-52059, 53124-53131)
            if (!string.IsNullOrEmpty(orgId))
            {
                var existing = await _db.Set<Mst_ColumnConfigGroup>()
                    .FirstOrDefaultAsync(x =>
                        EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc) &&
                        EF.Functions.Like((x.ColumnConfigGrpCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));

                if (existing != null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_ColumnConfigGroup_CheckDB_ColumnConfigGrpCodeExist",
                        "Check.ColumnConfigGrpCode", req.ColumnConfigGrpCode,
                        "Check.OrgID", req.OrgID);
                }
            }

            // Guard: kiểm tra OrgID không được rỗng (Master.cs:53133-53144)
            if (string.IsNullOrEmpty(orgId))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ColumnConfigGroup_Create_Invalid_OrgID_NotNull",
                    "Check.strColumnConfigGrpCode", code,
                    "Check.strOrgID", orgId);
            }

            var dtimeSys = DateTime.UtcNow;
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

            var entity = new Mst_ColumnConfigGroup
            {
                OrgID = orgId,
                ColumnConfigGrpCode = code,
                NetworkID = networkId,
                ColumnGrpName = name,
                ColumnGrpFormat = format,
                ColumnGrpDesc = desc,
                FlagActive = "1", // Master.cs:53161 TConst.Flag.Active
                LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = userCode
            };

            _db.Set<Mst_ColumnConfigGroup>().Add(entity);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // 5. Trả kết quả thành công khớp gate MVC (Mst_ColumnConfigGroupController.cs:222-225)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Tạo mới nhóm cấu hình thành công!" },
                Title = "Tạo mới nhóm cấu hình thành công!",
                RedirectUrl = "/Mst_ColumnConfigGroup",
                Data = entity
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ColumnConfigGroup_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Update từ gate Mst_ColumnConfigGroupController.cs:239-290.
    /// Chuỗi gọi: gate :239-290 -> WA_Mst_ColumnConfigGroup_Get (:283) -> WAS_Mst_ColumnConfigGroup_Get -> Mst_ColumnConfigGroup_Get -> Mst_ColumnConfigGroup_GetX (Master.cs:52908-53064).
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ColumnConfigGroup_UpdateReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào ColumnConfigGrpCode (Gate: Mst_ColumnConfigGroupController.cs:239, 252)
            var code = StdParam(req?.ColumnConfigGrpCode);
            var orgId = StdParam(req?.OrgID) ?? StdParam(Request?.Headers["OrgID"].FirstOrDefault());
            var networkId = StdParam(req?.NetworkID) ?? StdParam(Request?.Headers["NetworkID"].FirstOrDefault());

            // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
            if (string.IsNullOrEmpty(code) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_ColumnConfigGroup_UpdateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    code = StdParam(modelObj?.ColumnConfigGrpCode);
                    orgId = StdParam(modelObj?.OrgID) ?? orgId;
                    networkId = StdParam(modelObj?.NetworkID) ?? networkId;
                }
                catch
                {
                    code = StdParam(req.Model);
                }
            }

            // 2. Nếu mã rỗng, gate trả về View(new Mst_ColumnConfigGroup()) (Mst_ColumnConfigGroupController.cs:252, 289)
            if (string.IsNullOrEmpty(code))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_ColumnConfigGroup?)null
                });
            }

            // 3. Gọi GetXAsync tương ứng WA_Mst_ColumnConfigGroup_Get (Gate :283; Master.cs:52908-53064)
            var queryReq = new Mst_ColumnConfigGroup_IndexReq
            {
                ColumnConfigGrpCode = code,
                OrgID = orgId,
                NetworkID = networkId
            };
            var (_, list) = await GetXAsync(queryReq, 0, 1);
            var entity = list.FirstOrDefault();

            // 4. Nếu tìm thấy trả về bản ghi đầu tiên, nếu không trả null (Gate :284-289)
            return Ok(new
            {
                Success = true,
                Data = entity
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_ColumnConfigGroup_Update", "Exception", ex.Message);
        }
    }

    private static string? StdParam(object? value)
    {
        var s = value?.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }
}

public class Mst_ColumnConfigGroup_CreateReq
{
    public string? Model { get; set; }
    public string? OrgID { get; set; }
    public string? ColumnConfigGrpCode { get; set; }
    public string? NetworkID { get; set; }
    public string? ColumnGrpName { get; set; }
    public string? ColumnGrpFormat { get; set; }
    public string? ColumnGrpDesc { get; set; }
    public string? FlagActive { get; set; }
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

public class Mst_ColumnConfigGroup_UpdateReq
{
    public string? Model { get; set; }
    public string? ColumnConfigGrpCode { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
}
