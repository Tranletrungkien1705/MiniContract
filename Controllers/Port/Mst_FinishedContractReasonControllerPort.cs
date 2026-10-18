using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_FinishedContractReasonController.cs — Mst_FinishedContractReason
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_FinishedContractReasonController.cs:21-85 (Index), 87-90 (Create GET), 92-140 (Create POST), 142-193 (Update), 197-249 (Edit), 251-295 (Detail), 298-346 (Delete), 351-499 (Import), 505-564 (Export), 568-590 (ExportTemplate)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstFinishedContractReasonController.cs:20-88 (Index, Update, Export), 89-140 (Create), 160-228 (Edit/Update), 230-298 (Delete)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:45487-45558 (CheckDB), 45559-45655 (WAS Get/Update/Export), 45656-45812 (Biz Get/Update/Export), 45814-46046 (WAS/Biz Create), 46048-46303 (WAS/Biz Update), 46304-46382 (WAS Delete), 46383-46528 (Biz Delete), 46529-46685 (Search/Index/Update/Export GetX), 46686-46781 (CreateX), 46782-46889 (UpdateX), 46890-46947 (DeleteX)
[ApiController]
[Route("api/mst_finishedcontractreason")]
public class Mst_FinishedContractReasonControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;
    private readonly IWebHostEnvironment? _env;

    private static readonly Dictionary<string, string> ExportDicColumns = new()
    {
        { "ContractFinishReasonCode", "Mã lý do(*)" },
        { "ContractFinishReasonName", "Nội dung(*)" },
        { "FinishDescription", "Mô tả" },
        { "FlagActive", "Trạng thái" }
    };

    private static readonly Dictionary<string, string> ExportTemplateDicColumns = new()
    {
        { "ContractFinishReasonCode", "Mã lý do(*)" },
        { "ContractFinishReasonName", "Nội dung(*)" },
        { "FinishDescription", "Mô tả" }
    };

    public Mst_FinishedContractReasonControllerPort(AppDbContext db, IConfiguration? config = null, IWebHostEnvironment? env = null)
    {
        _db = db;
        _config = config;
        _env = env;
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
        var finishDesc = req.FinishDescription?.Trim() ?? ""; // Master.cs:46722 string.Format("{0}", objFinishDescription)
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port"; // Master.cs:46770

        var (err, entity) = await CreateCoreAsync(networkId, reasonCode, reasonName, finishDesc, userCode);
        if (err != null) return err;

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

    /// <summary>
    /// Lấy thông tin nguyên nhân chấm dứt hợp đồng để nạp vào form cập nhật (Update).
    /// Gate: Mst_FinishedContractReasonController.cs:142-193 Update(string reasoncode)
    /// API:  MstFinishedContractReasonController.cs:20-88 WA_Mst_FinishedContractReason_Get
    /// BIZ:  Master.cs:45559-45655 WAS_Mst_FinishedContractReason_Get -> Mst_FinishedContractReason_Get:45656-45812 -> Mst_FinishedContractReason_GetX:46529-46685
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_UpdateReq? req)
    {
        // 1. Dựng truy vấn cơ sở từ bảng Mst_FinishedContractReason (Master.cs:46581-46592, 46624-46633)
        var query = _db.Set<Mst_FinishedContractReason>().AsNoTracking();

        // 2. Bộ lọc điều kiện theo ContractFinishReasonCode (Gate: Mst_FinishedContractReasonController.cs:153-156)
        var reasonCode = StdParam(req?.ReasonCode ?? req?.ContractFinishReasonCode);
        if (!string.IsNullOrEmpty(reasonCode))
        {
            query = query.Where(x => EF.Functions.Like((x.ContractFinishReasonCode ?? "").ToLower(), PortQuery.Equal(reasonCode), PortQuery.Esc));
        }

        // Lọc theo NetworkID nếu có truyền (Gate: Mst_FinishedContractReasonController.cs:169 lấy từ UserState.Mst_NNT.NetworkID)
        var networkId = req?.NetworkID != null ? StdParam(req.NetworkID) : null;
        if (!string.IsNullOrEmpty(networkId))
        {
            query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
        }

        // 3. Sắp xếp: order by mo.NetworkID asc, then by ContractFinishReasonCode (Master.cs:46591, 46631)
        var item = await query.OrderBy(x => x.NetworkID)
                              .ThenBy(x => x.ContractFinishReasonCode)
                              .FirstOrDefaultAsync();

        // 4. Trả kết quả: Nếu tìm thấy thì trả về Data = item (Gate: dòng 181-184 JsonView("Update", Lst_Mst_FinishedContractReason[0])),
        //    nếu không tìm thấy thì Data = null (Gate: dòng 192 JsonView("Update") với model = null).
        return Ok(new
        {
            Success = true,
            Data = item
        });
    }

    /// <summary>
    /// Lấy chi tiết nguyên nhân chấm dứt hợp đồng.
    /// Gate: Mst_FinishedContractReasonController.cs:251-295 Detail(string reasoncode)
    /// API:  MstFinishedContractReasonController.cs:20-88 WA_Mst_FinishedContractReason_Get
    /// BIZ:  Master.cs:45559-45655 WAS_Mst_FinishedContractReason_Get -> Mst_FinishedContractReason_Get:45656-45812 -> Mst_FinishedContractReason_GetX:46529-46685
    /// </summary>
    [HttpPost("detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_DetailReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào reasoncode (Gate: Mst_FinishedContractReasonController.cs:251, 262)
            var reasonCode = StdParam(req?.ReasonCode ?? req?.ContractFinishReasonCode);

            // 2. Nếu reasoncode rỗng hoặc null, gate không gọi API và trả về View(new Mst_FinishedContractReason()) (Gate dòng 262, 294)
            if (string.IsNullOrEmpty(reasonCode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_FinishedContractReason?)null
                });
            }

            // 3. Dựng truy vấn cơ sở từ bảng Mst_FinishedContractReason (Master.cs:46581-46592, 46624-46633)
            var query = _db.Set<Mst_FinishedContractReason>().AsNoTracking();

            // Mệnh đề lọc: Mst_FinishedContractReason.ContractFinishReasonCode = reasoncode (Gate: Mst_FinishedContractReasonController.cs:266)
            query = query.Where(x => EF.Functions.Like((x.ContractFinishReasonCode ?? "").ToLower(), PortQuery.Equal(reasonCode), PortQuery.Esc));

            // Lọc theo NetworkID nếu có truyền (Gate: Mst_FinishedContractReasonController.cs:277 lấy từ UserState.Mst_NNT.NetworkID)
            var networkId = req?.NetworkID != null ? StdParam(req.NetworkID) : null;
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // Sắp xếp: order by mo.NetworkID asc, then by ContractFinishReasonCode (Master.cs:46591, 46631)
            var item = await query.OrderBy(x => x.NetworkID)
                                  .ThenBy(x => x.ContractFinishReasonCode)
                                  .FirstOrDefaultAsync();

            // 4. Trả kết quả: Nếu tìm thấy thì trả về Data = item (Gate: dòng 289-291 JsonView("Detail", Lst_Mst_FinishedContractReason[0])),
            //    nếu không tìm thấy thì Data = null (Gate: dòng 294 View(new Mst_FinishedContractReason())).
            return Ok(new
            {
                Success = true,
                Data = item
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật nguyên nhân chấm dứt hợp đồng.
    /// Gate: Mst_FinishedContractReasonController.cs:197-249 Edit(string model)
    /// API:  MstFinishedContractReasonController.cs:160-228 WA_Mst_FinishedContractReason_Update
    /// BIZ:  Master.cs:45487-45558 (CheckDB), 46048-46134 (WAS), 46135-46303 (Update), 46782-46889 (UpdateX)
    /// </summary>
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_EditReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc null (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Update", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_FinishedContractReasonController.cs:197, 209)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_FinishedContractReason_EditReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.NetworkID ??= modelObj.NetworkID;
                    req.ContractFinishReasonCode ??= modelObj.ContractFinishReasonCode ?? modelObj.ReasonCode;
                    req.ContractFinishReasonName ??= modelObj.ContractFinishReasonName;
                    req.FinishDescription ??= modelObj.FinishDescription;
                    req.FinishType ??= modelObj.FinishType;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.Ft_Cols_Upd ??= modelObj.Ft_Cols_Upd;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContractFinishReasonCode) &&
            string.IsNullOrWhiteSpace(req.ReasonCode) &&
            string.IsNullOrWhiteSpace(req.ContractFinishReasonName) &&
            string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_FinishedContractReason_Update", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:46822-46830
        var networkId = req.NetworkID != null ? StdParam(req.NetworkID) : (Request?.Headers["NetworkID"].FirstOrDefault() ?? _config?["Biz_NetworkID"] ?? "1001");
        networkId ??= "1001";
        var reasonCode = StdParam(req.ContractFinishReasonCode ?? req.ReasonCode);
        if (string.IsNullOrEmpty(reasonCode))
        {
            return PortResult.Fail("Mst_FinishedContractReason_CheckDB_OrganNotFound",
                "Check.NetworkID", networkId,
                "Check.ContractFinishReasonCode", req.ContractFinishReasonCode ?? req.ReasonCode);
        }

        // 3. Mở transaction và kiểm tra tồn tại bản ghi trong DB (Master.cs:46186, 46841-46848, 45516-45527)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.Set<Mst_FinishedContractReason>()
                .FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc) &&
                    EF.Functions.Like((x.ContractFinishReasonCode ?? "").ToLower(), PortQuery.Equal(reasonCode), PortQuery.Esc));

            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_FinishedContractReason_CheckDB_OrganNotFound",
                    "Check.NetworkID", networkId,
                    "Check.ContractFinishReasonCode", reasonCode);
            }

            // 4. Kiểm tra hợp lệ FinishType: bắt buộc là FINISHED hoặc STOPPED (Master.cs:46850-46864)
            var finishType = req.FinishType != null ? StdParam(req.FinishType) : existing.FinishType;
            if (!string.Equals(finishType, "FINISHED", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(finishType, "STOPPED", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_FinishedContractReason_Invalid_FinishType",
                    "Check.NetworkID", networkId,
                    "Check.ContractFinishReasonCode", reasonCode);
            }

            // 5. Cập nhật các cột chỉ định theo Ft_Cols_Upd (Master.cs:46832-46835, 46874-46886)
            var colsUpd = StdParam(req.Ft_Cols_Upd);
            bool bUpdName = string.IsNullOrEmpty(colsUpd) || colsUpd.Contains("MST_FINISHEDCONTRACTREASON.CONTRACTFINISHREASONNAME");
            bool bUpdType = string.IsNullOrEmpty(colsUpd) || colsUpd.Contains("MST_FINISHEDCONTRACTREASON.FINISHTYPE");
            bool bUpdDesc = string.IsNullOrEmpty(colsUpd) || colsUpd.Contains("MST_FINISHEDCONTRACTREASON.FINISHDESCRIPTION");
            bool bUpdFlag = string.IsNullOrEmpty(colsUpd) || colsUpd.Contains("MST_FINISHEDCONTRACTREASON.FLAGACTIVE");

            if (bUpdName && req.ContractFinishReasonName != null)
            {
                existing.ContractFinishReasonName = req.ContractFinishReasonName.Trim();
            }

            if (bUpdType)
            {
                existing.FinishType = finishType;
            }

            if (bUpdDesc && req.FinishDescription != null)
            {
                existing.FinishDescription = req.FinishDescription.Trim();
            }

            if (bUpdFlag && req.FlagActive != null)
            {
                existing.FlagActive = req.FlagActive.Trim() == "1" ? "1" : "0";
            }

            var dtimeSys = DateTime.UtcNow;
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

            existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
            existing.LogLUBy = userCode;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // 6. Trả kết quả thành công khớp gate MVC (Mst_FinishedContractReasonController.cs:238-241)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Cập nhật nguyên nhân chấm dứt hợp đồng thành công!" },
                Title = "Cập nhật nguyên nhân chấm dứt hợp đồng thành công!",
                RedirectUrl = "/Mst_FinishedContractReason",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_FinishedContractReason_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xóa nguyên nhân chấm dứt hợp đồng.
    /// Gate: Mst_FinishedContractReasonController.cs:298-346 Delete(string reasonCode)
    /// API:  MstFinishedContractReasonController.cs:230-298 WA_Mst_FinishedContractReason_Delete
    /// BIZ:  Master.cs:46304-46382 WAS_Mst_FinishedContractReason_Delete -> Mst_FinishedContractReason_Delete:46383-46528 -> Mst_FinishedContractReason_DeleteX:46890-46947
    ///       Master.cs:45487-45558 Mst_FinishedContractReason_CheckDB
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_DeleteReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc null (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Delete", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_FinishedContractReasonController.cs:298)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_FinishedContractReason_DeleteReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.NetworkID ??= modelObj.NetworkID;
                    req.ContractFinishReasonCode ??= modelObj.ContractFinishReasonCode ?? modelObj.ReasonCode;
                    req.ReasonCode ??= modelObj.ReasonCode;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContractFinishReasonCode) &&
            string.IsNullOrWhiteSpace(req.ReasonCode) &&
            string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_FinishedContractReason_Delete", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:46917-46918
        var networkId = req.NetworkID != null ? StdParam(req.NetworkID) : (Request?.Headers["NetworkID"].FirstOrDefault() ?? _config?["Biz_NetworkID"] ?? "1001");
        networkId ??= "1001";
        var reasonCode = StdParam(req.ContractFinishReasonCode ?? req.ReasonCode);
        if (string.IsNullOrEmpty(reasonCode))
        {
            return PortResult.Fail("Mst_FinishedContractReason_CheckDB_OrganNotFound",
                "Check.NetworkID", networkId,
                "Check.ContractFinishReasonCode", req.ContractFinishReasonCode ?? req.ReasonCode);
        }

        // 3. Mở transaction và kiểm tra tồn tại bản ghi trong DB (Master.cs:46417, 46923-46930, 45516-45527)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.Set<Mst_FinishedContractReason>()
                .FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc) &&
                    EF.Functions.Like((x.ContractFinishReasonCode ?? "").ToLower(), PortQuery.Equal(reasonCode), PortQuery.Esc));

            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_FinishedContractReason_CheckDB_OrganNotFound",
                    "Check.NetworkID", networkId,
                    "Check.ContractFinishReasonCode", reasonCode);
            }

            // 4. Xóa bản ghi khỏi DB (Master.cs:46938, 46941-46944)
            _db.Set<Mst_FinishedContractReason>().Remove(existing);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // 5. Trả kết quả thành công khớp gate MVC (Mst_FinishedContractReasonController.cs:335-338)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Xoá nguyên nhân chấm dứt thành công!" },
                Title = "Xoá nguyên nhân chấm dứt thành công!",
                RedirectUrl = "/Mst_FinishedContractReason",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_FinishedContractReason_Delete", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Nhập dữ liệu nguyên nhân chấm dứt hợp đồng từ tệp Excel.
    /// Gate: Mst_FinishedContractReasonController.cs:351-499 Import(HttpPostedFileWrapper file)
    /// API:  MstFinishedContractReasonController.cs:20-88 WA_Mst_FinishedContractReason_Get, 89-140 WA_Mst_FinishedContractReason_Create
    /// BIZ:  Master.cs:45559-45655 (WAS Get), 45656-45812 (Biz Get), 45814-46046 (WAS/Biz Create), 46529-46685 (GetX), 46686-46781 (CreateX)
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        // 1. Kiểm tra file upload (Gate lines 366-382)
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (Gate line 388 ExcelImportNew.Query(filePath, "A2"))
        // Danh sách cột từ GetImportDicColums() (Gate:594-602):
        // 1. ContractFinishReasonCode: Mã lý do(*)
        // 2. ContractFinishReasonName: Nội dung(*)
        // 3. FinishDescription: Mô tả
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "ContractFinishReasonCode", "ContractFinishReasonName", "FinishDescription" });
        }
        catch
        {
            return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        // 3. Kiểm tra bảng rỗng (Gate lines 389, 485-490: table == null || table.Rows.Count == 0)
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", "File Excel nhập không có dữ liệu!");
        }

        // 4. Kiểm tra số cột (Gate lines 392-397: table.Columns.Count != GetImportDicColums().Count)
        if (columnCount != 3)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        // 5. Tải danh sách hiện tại từ DB để kiểm tra trùng (Gate lines 400-424)
        var networkId = _config?["Biz_NetworkID"] ?? "1001";
        var listCur = await _db.Set<Mst_FinishedContractReason>()
            .AsNoTracking()
            .Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc))
            .ToListAsync();

        // 6. Vòng lặp 1: Kiểm tra TẤT CẢ các dòng trước khi tạo (Gate lines 426-440)
        foreach (var r in rows)
        {
            var code = r.GetValueOrDefault("ContractFinishReasonCode");
            if (string.IsNullOrWhiteSpace(code))
            {
                return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", "Mã lý do không được trống!");
            }

            var name = r.GetValueOrDefault("ContractFinishReasonName");
            if (string.IsNullOrWhiteSpace(name))
            {
                return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", "Nội dung không được trống!");
            }
        }

        // 7. Vòng lặp 2: Lưu từng dòng tuần tự theo transaction độc lập (Gate lines 461-478, hành vi import một phần)
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

        foreach (var r in rows)
        {
            var code = r.GetValueOrDefault("ContractFinishReasonCode")?.Trim().ToUpperInvariant() ?? "";
            var name = r.GetValueOrDefault("ContractFinishReasonName")?.Trim() ?? "";
            var desc = r.GetValueOrDefault("FinishDescription")?.Trim() ?? "";

            // Kiểm tra trùng mã trong danh sách hệ thống listCur (Gate lines 463-469)
            if (listCur.Any(m => string.Equals((m.ContractFinishReasonCode ?? "").Trim(), code, StringComparison.OrdinalIgnoreCase)))
            {
                var exitsData = $"Mã lý do: '{code}' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!";
                return PortResult.Fail("Mst_FinishedContractReason_Import", "Message", exitsData);
            }

            // Gọi CreateCoreAsync độc lập cho từng dòng (Gate lines 471-477)
            var (err, entity) = await CreateCoreAsync(networkId, code, name, desc, userCode);
            if (err != null)
            {
                // Lỗi ở dòng k -> dừng ngay và trả lỗi của dòng đó (các dòng trước k đã commit thành công)
                return err;
            }

            // Cập nhật listCur trong bộ nhớ để các dòng tiếp theo trong cùng tệp nếu trùng cũng bị bắt
            listCur.Add(new Mst_FinishedContractReason
            {
                NetworkID = networkId,
                ContractFinishReasonCode = code,
                ContractFinishReasonName = name
            });
        }

        // 8. Trả kết quả thành công khớp gate MVC (Gate lines 480-482)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Đã nhập dữ liệu excel thành công" },
            Title = "Đã nhập dữ liệu excel thành công"
        });
    }

    /// <summary>
    /// Xuất danh sách nguyên nhân chấm dứt hợp đồng ra tệp Excel.
    /// Gate: Mst_FinishedContractReasonController.cs:505-564 Export(int page = 0, int recordcount = 10)
    /// API:  MstFinishedContractReasonController.cs:20-88 WA_Mst_FinishedContractReason_Get
    /// BIZ:  Master.cs:45559-45655 (WAS Get), 45656-45812 (Biz Get), 46529-46685 (GetX)
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_FinishedContractReason_ExportReq? req)
    {
        try
        {
            // 1. Dựng truy vấn cơ sở từ bảng Mst_FinishedContractReason (Master.cs:46581-46592)
            var query = _db.Set<Mst_FinishedContractReason>().AsNoTracking();

            // 2. Bộ lọc điều kiện theo các cột hỗ trợ (Master.cs:46640-46664)
            var networkId = req?.NetworkID?.Trim();
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var reasonCode = req?.ContractFinishReasonCode?.Trim() ?? req?.ReasonCode?.Trim();
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

            // 3. Sắp xếp: order by mo.NetworkID asc, then by ContractFinishReasonCode (Master.cs:46591, 46631)
            var ordered = query.OrderBy(x => x.NetworkID).ThenBy(x => x.ContractFinishReasonCode);

            // 4. Phân trang nếu có truyền (Gate lines 530-531: Ft_RecordStart = "0", Ft_RecordCount = "123456000")
            long recordStart = req?.Ft_RecordStart ?? 0;
            long recordCount = req?.Ft_RecordCount ?? 123456000;
            if (req != null && req.RecordCount != null && req.Ft_RecordCount == null)
            {
                recordCount = req.RecordCount.Value;
                var page = req.Page ?? req.PageIndex ?? 0;
                recordStart = (long)page * recordCount;
            }

            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_FinishedContractReason>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 5. Kiểm tra danh sách rỗng (Gate lines 542-555: Nonsense.MESS_CHECK_FILE_NULL)
            if (list == null || list.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Title = "File Excel nhập không có dữ liệu!",
                    CheckSuccess = "1"
                });
            }

            // 6. Xuất danh sách ra file Excel và trả URL (Gate lines 544-551: Nonsense.MESS_EXPORT_EXCEL_SUCCESS)
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_FinishedContractReason));
            PortExcel.ExportToExcelFromList(list, ExportDicColumns, filePath, nameof(Mst_FinishedContractReason));

            return Ok(new
            {
                Success = true,
                Title = "Đã xuất excel thành công",
                CheckSuccess = "1",
                strUrl = virtualPath
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_FinishedContractReason_Export", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xuất tệp mẫu Excel để nhập nguyên nhân chấm dứt hợp đồng.
    /// Gate: Mst_FinishedContractReasonController.cs:568-590 ExportTemplate()
    /// </summary>
    [HttpPost("exporttemplate")]
    public IActionResult ExportTemplate([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? req = null)
    {
        try
        {
            // 1. Tạo danh sách rỗng (Gate line 573: var listInv = new List<Mst_FinishedContractReason>())
            var listInv = new List<Mst_FinishedContractReason>();

            // 2. Lấy từ điển cột từ GetImportDicColums() gồm 3 cột (Gate lines 574, 594-602)
            var dicColNames = ExportTemplateDicColumns;

            // 3. Sinh đường dẫn file vật lý và URL ảo (Gate lines 577-578: GenExcelExportFilePath)
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_FinishedContractReason));

            // 4. Xuất danh sách rỗng ra file Excel chứa dòng tiêu đề (Gate line 579: ExcelExport.ExportToExcelFromList)
            PortExcel.ExportToExcelFromList(listInv, dicColNames, filePath, nameof(Mst_FinishedContractReason));

            // 5. Trả kết quả thành công khớp gate MVC (Gate line 581: Nonsense.MESS_EXPORT_EXCEL_SUCCESS)
            return Ok(new
            {
                Success = true,
                Title = "Đã xuất excel thành công",
                CheckSuccess = "1",
                strUrl = virtualPath
            });
        }
        catch (Exception ex)
        {
            // Gate lines 584-589: resultEntry.SetFailed().AddException(ex)
            return PortResult.Fail("Mst_FinishedContractReason_ExportTemplate", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hàm dùng chung thực hiện tạo mới 1 bản ghi Mst_FinishedContractReason trong 1 transaction riêng.
    /// Dùng cho cả action Create đơn và vòng lặp Import (hành vi import một phần).
    /// </summary>
    private async Task<(IActionResult? Error, Mst_FinishedContractReason? Entity)> CreateCoreAsync(
        string networkId, string reasonCode, string reasonName, string finishDesc, string userCode)
    {
        var dtimeSys = DateTime.UtcNow;
        var entity = new Mst_FinishedContractReason
        {
            NetworkID = networkId,
            ContractFinishReasonCode = reasonCode,
            ContractFinishReasonName = reasonName,
            FinishType = "FINISHED",
            FinishDescription = finishDesc,
            FlagActive = "1",
            LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = userCode
        };

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
                return (PortResult.Fail("Mst_FinishedContractReason_CheckDB_OrganExist",
                    "Check.NetworkID", networkId,
                    "Check.ContractFinishReasonCode", reasonCode), null);
            }

            _db.Set<Mst_FinishedContractReason>().Add(entity);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (null, entity);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (PortResult.Fail("Mst_FinishedContractReason_Create", "Exception", ex.Message), null);
        }
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

/// <summary>
/// DTO tham số đầu vào cho action Update.
/// Gate: Mst_FinishedContractReasonController.cs:142 Update(string reasoncode)
/// </summary>
public class Mst_FinishedContractReason_UpdateReq
{
    public string? ReasonCode { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// DTO tham số đầu vào cho action Detail.
/// Gate: Mst_FinishedContractReasonController.cs:251 Detail(string reasoncode)
/// </summary>
public class Mst_FinishedContractReason_DetailReq
{
    public string? ReasonCode { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// DTO tham số đầu vào cho action Edit.
/// Gate: Mst_FinishedContractReasonController.cs:197 Edit(string model)
/// </summary>
public class Mst_FinishedContractReason_EditReq
{
    public string? Model { get; set; }
    public string? NetworkID { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? ReasonCode { get; set; }
    public string? ContractFinishReasonName { get; set; }
    public string? FinishType { get; set; }
    public string? FinishDescription { get; set; }
    public string? FlagActive { get; set; }
    public string? Ft_Cols_Upd { get; set; }
}

/// <summary>
/// DTO tham số đầu vào cho action Delete.
/// Gate: Mst_FinishedContractReasonController.cs:298 Delete(string reasonCode)
/// </summary>
public class Mst_FinishedContractReason_DeleteReq
{
    public string? ReasonCode { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? NetworkID { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO tham số đầu vào cho action Export.
/// Gate: Mst_FinishedContractReasonController.cs:505 Export(int page = 0, int recordcount = 10)
/// </summary>
public class Mst_FinishedContractReason_ExportReq
{
    public int? Page { get; set; }
    public int? RecordCount { get; set; }
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? ContractFinishReasonCode { get; set; }
    public string? ReasonCode { get; set; }
    public string? ContractFinishReasonName { get; set; }
    public string? KeyWord { get; set; }
    public string? FinishType { get; set; }
    public string? FlagActive { get; set; }
    public string? Ft_WhereClause { get; set; }
}

