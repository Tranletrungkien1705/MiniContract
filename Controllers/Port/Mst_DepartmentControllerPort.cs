using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_DepartmentController.cs — Mst_Department
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_DepartmentController.cs:24-87 (Index), 91-141 (GET Create View), 142-190 (POST Create), 195-259 (Update View), 263-348 (POST Edit), 353-418 (Detail), 422-485 (Delete)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstDepartmentController.cs:20-89 (WA_Mst_Department_Get), 91-160 (WA_Mst_Department_Create), 162-231 (WA_Mst_Department_Update), 233-302 (WA_Mst_Department_Delete)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:35634-35698 (Mst_Department_CheckDB), 35700-35736 (Mst_Department_UpdBU), 35737-35980 (Get/GetX), 35981-36200 (WAS/Biz Create), 36201-36284 (WAS_Mst_Department_Update), 36285-36432 (Mst_Department_Update), 36433-36510 (WAS_Mst_Department_Delete), 36511-36640 (Mst_Department_Delete), 36795-36931 (Mst_Department_CreateX), 36932-37041 (Mst_Department_UpdateX), 37042-37097 (Mst_Department_DeleteX), 12001-12070 (Mst_NNT_CheckDB), 12121-12218 (WAS_Mst_NNT_Get)
[ApiController]
[Route("api/mst_department")]
public class Mst_DepartmentControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_DepartmentControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Danh sách / Tìm kiếm phân trang phòng ban.
    /// Gate: Mst_DepartmentController.cs:24-87 Index(string init = "init", int recordcount = 10, int page = 0)
    /// API:  MstDepartmentController.cs:20-89 WA_Mst_Department_Get
    /// BIZ:  Master.cs:35737-35835 (WAS_Mst_Department_Get), 35836-35980 (Mst_Department_Get), 36641-36794 (Mst_Department_GetX)
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Department_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số phân trang theo gate Mst_DepartmentController.cs:24:
            var recordcount = req?.RecordCount ?? req?.PageSize ?? 10;
            var page = req?.Page ?? req?.PageIndex ?? 0;

            // 2. Kiểm tra cờ init: nếu init == "search" (Gate: Mst_DepartmentController.cs:39, 75-78),
            // gate gán lại init = "init", không gọi backend và trả DataList rỗng, ItemCount = 0, PageCount = 0.
            if (string.Equals(req?.Init, "search", StringComparison.OrdinalIgnoreCase))
            {
                var emptyPage = new PageInfo<Mst_Department>(new List<Mst_Department>(), 0, 0, page, recordcount);
                return Ok(new
                {
                    Success = true,
                    Data = emptyPage
                });
            }

            // 3. Dựng truy vấn cơ sở từ bảng Mst_Department (Master.cs:36698)
            var query = _db.Set<Mst_Department>().AsNoTracking();

            // 4. Lọc theo các cột của Mst_Department (Master.cs:36749-36773 BuildWhere)
            var deptCode = req?.DepartmentCode?.Trim();
            if (!string.IsNullOrEmpty(deptCode))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(deptCode), PortQuery.Esc));
            }

            var deptName = req?.DepartmentName?.Trim();
            if (!string.IsNullOrEmpty(deptName))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentName ?? "").ToLower(), PortQuery.Contains(deptName), PortQuery.Esc));
            }

            var keyWord = req?.KeyWord?.Trim();
            if (!string.IsNullOrEmpty(keyWord))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)
                                      || EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc));
            }

            var flagActive = req?.FlagActive?.Trim();
            if (!string.IsNullOrEmpty(flagActive))
            {
                query = query.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            var mst = req?.MST?.Trim();
            if (!string.IsNullOrEmpty(mst))
            {
                query = query.Where(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(mst), PortQuery.Esc));
            }

            var networkId = req?.NetworkID?.Trim();
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var parentCode = req?.DepartmentCodeParent?.Trim();
            if (!string.IsNullOrEmpty(parentCode))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentCodeParent ?? "").ToLower(), PortQuery.Equal(parentCode), PortQuery.Esc));
            }

            var buCode = req?.DepartmentBUCode?.Trim();
            if (!string.IsNullOrEmpty(buCode))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentBUCode ?? "").ToLower(), PortQuery.Equal(buCode), PortQuery.Esc));
            }

            var deptLevel = req?.DepartmentLevel?.Trim();
            if (!string.IsNullOrEmpty(deptLevel))
            {
                query = query.Where(x => EF.Functions.Like((x.DepartmentLevel ?? "").ToLower(), PortQuery.Equal(deptLevel), PortQuery.Esc));
            }

            // 5. Đếm tổng số bản ghi (Master.cs:36705: select Count(0) MyCount from ...)
            var itemCount = await query.CountAsync();

            // 6. Sắp xếp: order by mdept.DepartmentCode asc (Master.cs:36701, 36740)
            var ordered = query.OrderBy(x => x.DepartmentCode);

            // 7. Xác định vị trí phân trang (Gate: Mst_DepartmentController.cs:56-57; BIZ: Master.cs:36673-36674, 36714-36715)
            long recordStart;
            long recordCount;
            if (req != null && req.Ft_RecordStart != null)
            {
                recordStart = req.Ft_RecordStart.Value;
                recordCount = req.Ft_RecordCount ?? recordcount;
            }
            else
            {
                recordStart = (long)page * recordcount;
                recordCount = recordcount;
            }

            // 8. Lấy dữ liệu trang (LESSON A.7, A.12 chống tràn int và xử lý chỉ số âm/kích thước 0)
            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_Department>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 9. Tính số trang (Gate: Mst_DepartmentController.cs:71: (itemCount % PageSizeConfig == 0) ? ... : ... + 1)
            const int pageSizeConfig = 10;
            var ps = recordcount > 0 ? recordcount : pageSizeConfig;
            var pageCount = list.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

            // 10. Trả về cấu trúc PageInfo bọc trong Success/Data (Data/PortQuery.cs:31, LESSON A.5, A.11)
            return Ok(new
            {
                Success = true,
                Data = new PageInfo<Mst_Department>(list, itemCount, pageCount, page, recordcount)
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Department_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Tạo mới phòng ban bộ phận.
    /// Gate: Mst_DepartmentController.cs:91-141 (GET View), 142-190 Create(string model)
    /// API:  MstDepartmentController.cs:91-160 WA_Mst_Department_Create
    /// BIZ:  Master.cs:35981-36061 (WAS_Mst_Department_Create) -> 36062-36200 (Mst_Department_Create) -> 36795-36931 (Mst_Department_CreateX)
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Department_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc thiếu dữ liệu (LESSON A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_Department_Create", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_DepartmentController.cs:144, 154)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Department_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.DepartmentCode ??= modelObj.DepartmentCode;
                    req.DepartmentCodeParent ??= modelObj.DepartmentCodeParent;
                    req.MST ??= modelObj.MST;
                    req.DepartmentName ??= modelObj.DepartmentName;
                    req.NetworkID ??= modelObj.NetworkID;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (req.DepartmentCode == null && req.DepartmentName == null && req.Model == null && req.MST == null && req.DepartmentCodeParent == null)
        {
            return PortResult.Fail("Mst_Department_Create", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:36827-36830
        var strDepartmentCode = StdParam(req.DepartmentCode);
        var strDepartmentCodeParent = StdParam(req.DepartmentCodeParent);
        var strMST = StdParam(req.MST);
        var strDepartmentName = req.DepartmentName != null ? string.Format("{0}", req.DepartmentName).Trim() : "";

        // 3. Guards theo Master.cs:36838-36894
        // Guard 1: DepartmentCode null hoặc rỗng (Master.cs:36838-36848)
        if (string.IsNullOrEmpty(strDepartmentCode))
        {
            return PortResult.Fail("Mst_Department_Create_InvalidDepartmentCode", "Check.strDepartmentCode", req.DepartmentCode);
        }

        // Mở Transaction (Master.cs:36101)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Guard 2: Kiểm tra trùng mã DepartmentCode (Master.cs:36850-36856, 35671-35681 Mst_Department_CheckDB với FlagExist="0")
            var deptExists = await _db.Set<Mst_Department>()
                .AnyAsync(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(strDepartmentCode), PortQuery.Esc));

            if (deptExists)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Department_CheckDB_DepartmentExist", "Check.DepartmentCode", strDepartmentCode);
            }

            // Guard 3: Kiểm tra MST trong Mst_NNT (Master.cs:36860-36867, 12028-12065 Mst_NNT_CheckDB với FlagExist="1", FlagActive="1")
            if (string.IsNullOrEmpty(strMST))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", req.MST);
            }

            var nnt = await _db.Set<Mst_NNT>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));

            if (nnt == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMST);
            }

            if (nnt.FlagActive != "1")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_FlagActiveNotMatched",
                    "Check.MST", strMST,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", nnt.FlagActive);
            }

            // Guard 4: Kiểm tra phòng ban cha nếu có DepartmentCodeParent (Master.cs:36871-36882, 35660-35697 Mst_Department_CheckDB với FlagExist="1", FlagActive="1")
            if (!string.IsNullOrEmpty(strDepartmentCodeParent))
            {
                var parentDept = await _db.Set<Mst_Department>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(strDepartmentCodeParent), PortQuery.Esc));

                if (parentDept == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Department_CheckDB_DepartmentNotFound", "Check.DepartmentCode", strDepartmentCodeParent);
                }

                if (parentDept.FlagActive != "1")
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_Department_CheckDB_FlagActiveNotMatched",
                        "Check.DepartmentCode", strDepartmentCodeParent,
                        "Check.strFlagActiveListToCheck", "1",
                        "DB.FlagActive", parentDept.FlagActive);
                }
            }

            // Guard 5: DepartmentName rỗng (Master.cs:36884-36894)
            if (string.IsNullOrEmpty(strDepartmentName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Department_Create_InvalidDepartmentName", "Check.strDepartmentName", strDepartmentName);
            }

            // 4. Lưu bản ghi mới vào Mst_Department (Master.cs:36898-36924)
            var dtimeSys = DateTime.UtcNow;
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";
            var networkId = req.NetworkID != null ? StdParam(req.NetworkID) : (_config?["Biz_NetworkID"] ?? "1001");

            var entity = new Mst_Department
            {
                DepartmentCode = strDepartmentCode,
                NetworkID = networkId,
                DepartmentCodeParent = strDepartmentCodeParent,
                DepartmentBUCode = "X",
                DepartmentBUPattern = "X",
                DepartmentLevel = "1",
                MST = strMST,
                DepartmentName = strDepartmentName,
                FlagActive = "1", // TConst.Flag.Active
                LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = userCode
            };

            _db.Set<Mst_Department>().Add(entity);
            await _db.SaveChangesAsync();

            // 5. Cập nhật cây phân cấp phòng ban Mst_Department_UpdBU() (Master.cs:36928, 35700-35736)
            await UpdBUAsync();

            await tx.CommitAsync();

            // 6. Trả kết quả thành công khớp gate MVC (Mst_DepartmentController.cs:179-182)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Tạo mới bộ phận phòng ban thành công!" },
                Title = "Tạo mới bộ phận phòng ban thành công!",
                RedirectUrl = "/Mst_Department",
                Data = entity
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Department_Create", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lấy thông tin phòng ban phục vụ màn hình cập nhật.
    /// Gate: Mst_DepartmentController.cs:195-259 Update(string departmentcode)
    /// API:  MstDepartmentController.cs:20-89 WA_Mst_Department_Get
    /// BIZ:  Master.cs:35737-35835 (WAS_Mst_Department_Get), 35836-35980 (Mst_Department_Get), 36641-36794 (Mst_Department_GetX)
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Department_UpdateReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào DepartmentCode (Gate: Mst_DepartmentController.cs:195, 203)
            var deptCode = StdParam(req?.DepartmentCode);
            if (string.IsNullOrEmpty(deptCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Department_UpdateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    deptCode = StdParam(modelObj?.DepartmentCode);
                }
                catch
                {
                    deptCode = StdParam(req.Model);
                }
            }

            // 2. Nếu DepartmentCode rỗng/null, gate trả về View(new Mst_Department()) (Mst_DepartmentController.cs:203, 258)
            if (string.IsNullOrEmpty(deptCode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Department_UpdateData?)null
                });
            }

            // 3. Truy vấn phòng ban theo DepartmentCode (Gate: Mst_DepartmentController.cs:205-233; BIZ: Master.cs:36701, 36740)
            var dept = await _db.Set<Mst_Department>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(deptCode), PortQuery.Esc))
                .OrderBy(x => x.DepartmentCode)
                .FirstOrDefaultAsync();

            // Nếu không tìm thấy, gate trả về View(new Mst_Department()) (Mst_DepartmentController.cs:233, 258)
            if (dept == null)
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Department_UpdateData?)null
                });
            }

            // 4. Lấy danh sách bộ phận phòng ban active (FlagActive = "1") (Gate: Mst_DepartmentController.cs:236-244: List_Mst_Department_Async)
            var listMst_Department = await _db.Set<Mst_Department>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .OrderBy(x => x.DepartmentCode)
                .ToListAsync();

            // 5. Lấy danh sách mã số thuế NNT active (FlagActive = "1") (Gate: Mst_DepartmentController.cs:246-254: List_Mst_NNT)
            var listMst_NNT = await _db.Set<Mst_NNT>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .OrderBy(x => x.MST)
                .ToListAsync();

            // 6. Trả về kết quả khớp gate MVC: Model là Mst_Department và ViewBag chứa ListMst_Department, ListMst_NNT (Mst_DepartmentController.cs:243, 253, 255)
            var data = new Mst_Department_UpdateData(dept, listMst_Department, listMst_NNT);
            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Department_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật thông tin phòng ban bộ phận.
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_DepartmentController.cs:263-348 Edit(string model)
    /// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstDepartmentController.cs:162-231 WA_Mst_Department_Update
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:36201-36284 (WAS_Mst_Department_Update) -> 36285-36432 (Mst_Department_Update) -> 36932-37041 (Mst_Department_UpdateX), 35634-35698 (Mst_Department_CheckDB), 12001-12070 (Mst_NNT_CheckDB)
    /// </summary>
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Department_EditReq? req)
    {
        // 1. Kiểm tra body rỗng hoàn toàn (Lesson A.15, A.21; Master.cs:36218, 36311)
        if (req == null)
        {
            return PortResult.Fail("Mst_Department_Update", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_DepartmentController.cs:263, 273)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_Department_EditReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.DepartmentCode ??= modelObj.DepartmentCode;
                    req.DepartmentCodeParent ??= modelObj.DepartmentCodeParent;
                    req.DepartmentBUCode ??= modelObj.DepartmentBUCode;
                    req.DepartmentBUPattern ??= modelObj.DepartmentBUPattern;
                    req.DepartmentLevel ??= modelObj.DepartmentLevel;
                    req.MST ??= modelObj.MST;
                    req.DepartmentName ??= modelObj.DepartmentName;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.Ft_Cols_Upd ??= modelObj.Ft_Cols_Upd;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        // Gate guard 1: Kiểm tra model/thông tin phòng ban trống (Mst_DepartmentController.cs:271, 343-346)
        if (string.IsNullOrWhiteSpace(req.DepartmentCode) &&
            string.IsNullOrWhiteSpace(req.DepartmentName) &&
            string.IsNullOrWhiteSpace(req.MST) &&
            string.IsNullOrWhiteSpace(req.Model))
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Bộ phận phòng ban trống!" },
                Title = "Bộ phận phòng ban trống!"
            });
        }

        var strDepartmentCode = StdParam(req.DepartmentCode);
        if (string.IsNullOrEmpty(strDepartmentCode))
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Bộ phận phòng ban trống!" },
                Title = "Bộ phận phòng ban trống!"
            });
        }

        // Gate guard 2: Tra cứu bản ghi phòng ban hiện có trong DB (Mst_DepartmentController.cs:279-303, 336-340; Master.cs:36701, 36740)
        var existing = await _db.Set<Mst_Department>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(strDepartmentCode), PortQuery.Esc));

        if (existing == null)
        {
            return Ok(new
            {
                Success = true,
                Messages = new[] { $"Mã bộ phận phòng ban '{req.DepartmentCode}' không có trong hệ thống!" },
                Title = $"Mã bộ phận phòng ban '{req.DepartmentCode}' không có trong hệ thống!"
            });
        }

        // Xác định các cột cập nhật theo Ft_Cols_Upd của gate (Mst_DepartmentController.cs:314-326; Master.cs:36979-36981)
        var strFt_Cols_Upd = req.Ft_Cols_Upd != null ? StdParam(req.Ft_Cols_Upd) : "";
        bool bUpd_MST = string.IsNullOrEmpty(strFt_Cols_Upd) || strFt_Cols_Upd.Contains("MST");
        bool bUpd_DepartmentName = string.IsNullOrEmpty(strFt_Cols_Upd) || strFt_Cols_Upd.Contains("DEPARTMENTNAME");
        bool bUpd_FlagActive = string.IsNullOrEmpty(strFt_Cols_Upd) || strFt_Cols_Upd.Contains("FLAGACTIVE");

        // Chuẩn hoá tham số đầu vào theo Master.cs:36973-36978
        var strMST = req.MST != null ? StdParam(req.MST) : StdParam(existing.MST);
        var strDepartmentName = req.DepartmentName != null ? string.Format("{0}", req.DepartmentName).Trim() : existing.DepartmentName;
        var strFlagActive = req.FlagActive != null ? StdFlag(req.FlagActive) : existing.FlagActive;

        // Mở transaction cập nhật (Master.cs:36330)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Biz guard 1: Kiểm tra MST trong Mst_NNT qua Mst_NNT_CheckDB (Master.cs:36997-37004, 12001-12070)
            if (string.IsNullOrEmpty(strMST))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", req.MST);
            }

            var nnt = await _db.Set<Mst_NNT>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));

            if (nnt == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_NNTNotFound", "Check.MST", strMST);
            }

            if (nnt.FlagActive != "1")
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_NNT_CheckDB_FlagActiveNotMatched",
                    "Check.MST", strMST,
                    "Check.strFlagActiveListToCheck", "1",
                    "DB.FlagActive", nnt.FlagActive);
            }

            // Biz guard 2: Kiểm tra DepartmentName không được rỗng khi cập nhật (Master.cs:37007-37018)
            if (bUpd_DepartmentName && string.IsNullOrEmpty(strDepartmentName))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Department_UpdateX_InvalidDepartmentName", "Check.strDepartmentName", strDepartmentName);
            }

            // Lưu các cột hiệu lực theo Master.cs:37024-37038
            if (bUpd_MST) existing.MST = strMST;
            if (bUpd_DepartmentName) existing.DepartmentName = strDepartmentName;
            if (bUpd_FlagActive) existing.FlagActive = strFlagActive;

            var dtimeSys = DateTime.UtcNow;
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

            existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
            existing.LogLUBy = userCode;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Trả kết quả thành công khớp gate MVC (Mst_DepartmentController.cs:332-334)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Sửa bộ phận phòng ban thành công!" },
                Title = "Sửa bộ phận phòng ban thành công!",
                RedirectUrl = "/Mst_Department",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_Department_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Lấy chi tiết thông tin phòng ban bộ phận.
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_DepartmentController.cs:353-418 Detail(string departmentcode)
    /// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstDepartmentController.cs:20-89 WA_Mst_Department_Get
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:35737-35835 (WAS_Mst_Department_Get) -> 35836-35980 (Mst_Department_Get) -> 36641-36794 (Mst_Department_GetX)
    /// </summary>
    [HttpPost("detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Department_DetailReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào DepartmentCode (Gate: Mst_DepartmentController.cs:353, 361)
            var deptCode = StdParam(req?.DepartmentCode);
            if (string.IsNullOrEmpty(deptCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Department_DetailReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    deptCode = StdParam(modelObj?.DepartmentCode);
                }
                catch
                {
                    deptCode = StdParam(req.Model);
                }
            }

            // 2. Nếu DepartmentCode rỗng/null, gate trả về View(new Mst_Department()) (Mst_DepartmentController.cs:361, 417)
            if (string.IsNullOrEmpty(deptCode))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Department_DetailData?)null
                });
            }

            // 3. Truy vấn phòng ban theo DepartmentCode (Gate: Mst_DepartmentController.cs:363-392; BIZ: Master.cs:36701, 36740)
            var dept = await _db.Set<Mst_Department>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(deptCode), PortQuery.Esc))
                .OrderBy(x => x.DepartmentCode)
                .FirstOrDefaultAsync();

            // Nếu không tìm thấy, gate trả về View(new Mst_Department()) (Mst_DepartmentController.cs:393, 417)
            if (dept == null)
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_Department_DetailData?)null
                });
            }

            // 4. Lấy danh sách bộ phận phòng ban active (FlagActive = "1") (Gate: Mst_DepartmentController.cs:396-402: List_Mst_Department_Async)
            var listMst_Department = await _db.Set<Mst_Department>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .OrderBy(x => x.DepartmentCode)
                .ToListAsync();

            // 5. Lấy danh sách mã số thuế NNT active (FlagActive = "1") (Gate: Mst_DepartmentController.cs:405-413: List_Mst_NNT)
            var listMst_NNT = await _db.Set<Mst_NNT>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .OrderBy(x => x.MST)
                .ToListAsync();

            // 6. Trả về kết quả khớp gate MVC: Model là Mst_Department và ViewBag chứa ListMst_Department, ListMst_NNT (Mst_DepartmentController.cs:402, 412, 414)
            var data = new Mst_Department_DetailData(dept, listMst_Department, listMst_NNT);
            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Department_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xóa bộ phận phòng ban.
    /// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_DepartmentController.cs:422-485 Delete(string departmentcode)
    /// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstDepartmentController.cs:233-302 WA_Mst_Department_Delete
    /// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:36433-36510 (WAS_Mst_Department_Delete) -> 36511-36640 (Mst_Department_Delete) -> 37042-37097 (Mst_Department_DeleteX), 35634-35698 (Mst_Department_CheckDB)
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Department_DeleteReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào DepartmentCode (Gate: Mst_DepartmentController.cs:422, 430)
            var deptCode = StdParam(req?.DepartmentCode);
            if (string.IsNullOrEmpty(deptCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Department_DeleteReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    deptCode = StdParam(modelObj?.DepartmentCode);
                }
                catch
                {
                    deptCode = StdParam(req.Model);
                }
            }

            // 2. Nếu DepartmentCode rỗng/null, gate trả về JsonResultEntry: Bộ phận phòng ban trống! (Mst_DepartmentController.cs:430, 480-482)
            if (string.IsNullOrEmpty(deptCode))
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Bộ phận phòng ban trống!" },
                    Title = "Bộ phận phòng ban trống!"
                });
            }

            // 3. Tra cứu bản ghi phòng ban hiện có trong DB (Gate: Mst_DepartmentController.cs:461-462; Master.cs:36701, 36740, 35648)
            var existing = await _db.Set<Mst_Department>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.DepartmentCode ?? "").ToLower(), PortQuery.Equal(deptCode), PortQuery.Esc));

            if (existing == null)
            {
                var rawCode = req?.DepartmentCode ?? deptCode;
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { $"Mã bộ phận phong ban'{rawCode}' không có trong hệ thống!" },
                    Title = $"Mã bộ phận phong ban'{rawCode}' không có trong hệ thống!"
                });
            }

            // 4. Mở transaction và xóa dữ liệu (Master.cs:36544, 36603, 37088-37094)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Set<Mst_Department>().Remove(existing);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // 5. Trả kết quả thành công khớp gate MVC (Mst_DepartmentController.cs:468-470)
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Xóa bộ phận phòng ban thành công!" },
                    Title = "Xóa bộ phận phòng ban thành công!",
                    RedirectUrl = "/Mst_Department",
                    Data = existing
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Department_Delete", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Department_Delete", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật cây phân cấp phòng ban Mst_Department_UpdBU (Master.cs:35700-35736).
    /// </summary>
    private async Task UpdBUAsync()
    {
        var allDepts = await _db.Set<Mst_Department>().ToListAsync();
        var dict = allDepts.ToDictionary(x => x.DepartmentCode, StringComparer.OrdinalIgnoreCase);

        // 1. Gốc 'HO' (Master.cs:35703-35715)
        if (dict.TryGetValue("HO", out var rootDept))
        {
            rootDept.DepartmentBUCode = "HO";
            rootDept.DepartmentBUPattern = "HO%";
            rootDept.DepartmentLevel = "1";
        }

        // 2. Vòng lặp độ sâu 6 cấp cho các phòng ban còn lại (Master.cs:35717-35733)
        for (int depth = 0; depth <= 6; depth++)
        {
            foreach (var dept in allDepts)
            {
                if (string.Equals(dept.DepartmentCode, "HO", StringComparison.OrdinalIgnoreCase))
                    continue;

                Mst_Department? parent = null;
                if (!string.IsNullOrEmpty(dept.DepartmentCodeParent))
                {
                    dict.TryGetValue(dept.DepartmentCodeParent, out parent);
                }

                if (parent != null && !string.IsNullOrEmpty(parent.DepartmentBUCode))
                {
                    dept.DepartmentBUCode = parent.DepartmentBUCode + "." + dept.DepartmentCode;
                    dept.DepartmentBUPattern = parent.DepartmentBUCode + "." + dept.DepartmentCode + "%";
                    int parentLevel = int.TryParse(parent.DepartmentLevel, out var pl) ? pl : 0;
                    dept.DepartmentLevel = (parentLevel + 1).ToString();
                }
                else
                {
                    dept.DepartmentBUCode = dept.DepartmentCode;
                    dept.DepartmentBUPattern = dept.DepartmentCode + "%";
                    dept.DepartmentLevel = "1";
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    private static string? StdParam(object? value)
    {
        if (value == null) return null;
        var s = value.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    private static string? StdStr(object? value)
    {
        if (value == null) return null;
        var s = value.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static string StdFlag(object? value)
    {
        return value?.ToString()?.Trim() == "1" ? "1" : "0";
    }
}

/// <summary>
/// DTO tham số cho action Create của Mst_DepartmentController.
/// </summary>
public class Mst_Department_CreateReq
{
    // Chuỗi JSON model từ gate MVC (Mst_DepartmentController.cs:144, 154)
    public string? Model { get; set; }

    // Các trường của Mst_Department theo Master.cs:36804-36807
    public string? DepartmentCode { get; set; }
    public string? DepartmentCodeParent { get; set; }
    public string? MST { get; set; }
    public string? DepartmentName { get; set; }
    public string? NetworkID { get; set; }
}

/// <summary>
/// DTO tham số cho action Index của Mst_DepartmentController.
/// </summary>
public class Mst_Department_IndexReq
{
    // Tham số tầng gate Mst_DepartmentController.cs:24
    public string? Init { get; set; } = "init";
    public int? RecordCount { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }

    // Tham số lọc và phân trang tầng BIZ Master.cs:36649-36655
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
    public string? Ft_WhereClause { get; set; }

    // Các cột lọc bảng Mst_Department (Master.cs:36753-36759)
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public string? MST { get; set; }
    public string? NetworkID { get; set; }
    public string? DepartmentCodeParent { get; set; }
    public string? DepartmentBUCode { get; set; }
    public string? DepartmentLevel { get; set; }
}

/// <summary>
/// DTO tham số cho action Update của Mst_DepartmentController.
/// </summary>
public class Mst_Department_UpdateReq
{
    // Tham số DepartmentCode từ gate Mst_DepartmentController.cs:195 Update(string departmentcode)
    public string? DepartmentCode { get; set; }

    // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
    public string? Model { get; set; }
}

/// <summary>
/// Dữ liệu trả về cho action Update của Mst_DepartmentController.
/// Bao gồm thông tin chi tiết phòng ban (model View), kèm danh sách phòng ban active (ViewBag.ListMst_Department)
/// và danh sách NNT active (ViewBag.ListMst_NNT) theo gate Mst_DepartmentController.cs:243, 253, 255.
/// </summary>
public class Mst_Department_UpdateData : Mst_Department
{
    public Mst_Department? Mst_Department { get; set; }
    public List<Mst_Department>? ListMst_Department { get; set; }
    public List<Mst_Department>? List_Mst_Department { get; set; }
    public List<Mst_NNT>? ListMst_NNT { get; set; }
    public List<Mst_NNT>? List_Mst_NNT { get; set; }

    public Mst_Department_UpdateData() { }

    public Mst_Department_UpdateData(Mst_Department src, List<Mst_Department>? listDept = null, List<Mst_NNT>? listNNT = null)
    {
        DepartmentCode = src.DepartmentCode;
        NetworkID = src.NetworkID;
        DepartmentCodeParent = src.DepartmentCodeParent;
        DepartmentBUCode = src.DepartmentBUCode;
        DepartmentBUPattern = src.DepartmentBUPattern;
        DepartmentLevel = src.DepartmentLevel;
        MST = src.MST;
        DepartmentName = src.DepartmentName;
        FlagActive = src.FlagActive;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;
        Mst_Department = src;
        ListMst_Department = listDept;
        List_Mst_Department = listDept;
        ListMst_NNT = listNNT;
        List_Mst_NNT = listNNT;
    }
}

/// <summary>
/// DTO tham số cho action Edit của Mst_DepartmentController.
/// </summary>
public class Mst_Department_EditReq
{
    // Chuỗi JSON model từ gate MVC (Mst_DepartmentController.cs:263, 273)
    public string? Model { get; set; }

    // Các trường của Mst_Department theo gate và BIZ Master.cs:36958-36964
    public string? DepartmentCode { get; set; }
    public string? DepartmentCodeParent { get; set; }
    public string? DepartmentBUCode { get; set; }
    public string? DepartmentBUPattern { get; set; }
    public string? DepartmentLevel { get; set; }
    public string? MST { get; set; }
    public string? DepartmentName { get; set; }
    public string? FlagActive { get; set; }
    public string? NetworkID { get; set; }
    public string? Ft_Cols_Upd { get; set; }
}

/// <summary>
/// DTO tham số cho action Detail của Mst_DepartmentController.
/// </summary>
public class Mst_Department_DetailReq
{
    // Tham số DepartmentCode từ gate Mst_DepartmentController.cs:353 Detail(string departmentcode)
    public string? DepartmentCode { get; set; }

    // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
    public string? Model { get; set; }
}

/// <summary>
/// Dữ liệu trả về cho action Detail của Mst_DepartmentController.
/// Bao gồm thông tin chi tiết phòng ban (model View), kèm danh sách phòng ban active (ViewBag.ListMst_Department)
/// và danh sách NNT active (ViewBag.ListMst_NNT) theo gate Mst_DepartmentController.cs:402, 412, 414.
/// </summary>
public class Mst_Department_DetailData : Mst_Department
{
    public Mst_Department? Mst_Department { get; set; }
    public List<Mst_Department>? ListMst_Department { get; set; }
    public List<Mst_Department>? List_Mst_Department { get; set; }
    public List<Mst_NNT>? ListMst_NNT { get; set; }
    public List<Mst_NNT>? List_Mst_NNT { get; set; }

    public Mst_Department_DetailData() { }

    public Mst_Department_DetailData(Mst_Department src, List<Mst_Department>? listDept = null, List<Mst_NNT>? listNNT = null)
    {
        DepartmentCode = src.DepartmentCode;
        NetworkID = src.NetworkID;
        DepartmentCodeParent = src.DepartmentCodeParent;
        DepartmentBUCode = src.DepartmentBUCode;
        DepartmentBUPattern = src.DepartmentBUPattern;
        DepartmentLevel = src.DepartmentLevel;
        MST = src.MST;
        DepartmentName = src.DepartmentName;
        FlagActive = src.FlagActive;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;
        Mst_Department = src;
        ListMst_Department = listDept;
        List_Mst_Department = listDept;
        ListMst_NNT = listNNT;
        List_Mst_NNT = listNNT;
    }
}

/// <summary>
/// DTO tham số cho action Delete của Mst_DepartmentController.
/// </summary>
public class Mst_Department_DeleteReq
{
    // Tham số DepartmentCode từ gate Mst_DepartmentController.cs:422 Delete(string departmentcode)
    public string? DepartmentCode { get; set; }

    // Hỗ trợ trường hợp client truyền chuỗi JSON hoặc mã thô trong Model
    public string? Model { get; set; }
}
