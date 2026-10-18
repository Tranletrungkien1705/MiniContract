using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ContractTypeController.cs — Mst_ContractType
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ContractTypeController.cs:26-90 (Index), 92-95 (Create GET), 97-146 (Create POST), 148-283 (Update), 287-365 (Edit POST)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstContractTypeController.cs:20-88 (Index, Update), 89-140 (Create), 160-228 (WA_Mst_ContractType_Update)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:43978-44048 (CheckDB), 44050-44141 (WAS Get/Update), 44142-44297 (Biz Get/Update), 44299-44548 (WAS/Biz Create), 44549-44633 (WAS Update), 44634-44798 (Biz Update), 45024-45180 (Search/Index/Update GetX), 45181-45278 (CreateX), 45279-45372 (UpdateX)
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

    /// <summary>
    /// Lấy thông tin loại hợp đồng để nạp vào giao diện cập nhật (Update view).
    /// Gate: Mst_ContractTypeController.cs:148-283 Update(string contracttype)
    /// API:  MstContractTypeController.cs:20-88 WA_Mst_ContractType_Get
    /// BIZ:  Master.cs:44050-44141 WAS_Mst_ContractType_Get -> Mst_ContractType_Get:44142-44297 -> Mst_ContractType_GetX:45024-45180
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ContractType_UpdateReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào ContractType (Gate: Mst_ContractTypeController.cs:148, 159)
            var contractType = StdParam(req?.ContractType);
            if (string.IsNullOrEmpty(contractType) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_ContractType_UpdateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    contractType = StdParam(modelObj?.ContractType);
                }
                catch
                {
                    contractType = StdParam(req.Model);
                }
            }

            // 2. Lấy danh sách mẫu gửi active (Gate: Mst_ContractTypeController.cs:165-193: Mst_SubmissionForm.FlagActive = '1')
            var listSubmissionForm = await _db.Set<Mst_SubmissionForm>()
                .AsNoTracking()
                .Where(x => x.FlagActive == "1")
                .OrderBy(x => x.SubFormCode)
                .ToListAsync();

            // 3. Dựng truy vấn cơ sở từ bảng Mst_ContractType (Master.cs:45078-45087, 45120-45127)
            var query = _db.Set<Mst_ContractType>().AsNoTracking();

            if (!string.IsNullOrEmpty(contractType))
            {
                query = query.Where(x => EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contractType), PortQuery.Esc));
            }

            var networkId = StdParam(req?.NetworkID);
            if (!string.IsNullOrEmpty(networkId))
            {
                query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            var orgId = StdParam(req?.OrgID);
            if (!string.IsNullOrEmpty(orgId))
            {
                query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }

            // Sắp xếp: order by mo.NetworkID asc, then by ContractType (Master.cs:45086, 45126)
            var item = await query.OrderBy(x => x.NetworkID)
                                  .ThenBy(x => x.ContractType)
                                  .FirstOrDefaultAsync();

            // 4. Trả kết quả: nếu tìm thấy thì Data = Mst_ContractType_UpdateData (kèm List_Mst_SubmissionForm theo gate :193, 274),
            //    nếu không tìm thấy thì Data = null (Gate: dòng 282 JsonView("Update") với model = null).
            if (item == null)
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_ContractType_UpdateData?)null
                });
            }

            var resultData = new Mst_ContractType_UpdateData(item, listSubmissionForm);
            return Ok(new
            {
                Success = true,
                Data = resultData
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_ContractType_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật loại hợp đồng.
    /// Gate: Mst_ContractTypeController.cs:287-365 Edit(string model, string modelDtl)
    /// API:  MstContractTypeController.cs:160-228 WA_Mst_ContractType_Update
    /// BIZ:  Master.cs:44549-44633 WAS_Mst_ContractType_Update -> Mst_ContractType_Update:44634-44798 -> Mst_ContractType_UpdateX:45279-45372, Mst_ContractType_CheckDB:43978-44048
    /// </summary>
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ContractType_EditReq? req)
    {
        // 1. Kiểm tra body rỗng hoặc thiếu thông tin (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_ContractType_Update", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ContractTypeController.cs:287, 299)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_ContractType_EditReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.NetworkID ??= modelObj.NetworkID;
                    req.ContractType ??= modelObj.ContractType;
                    req.OrgID ??= modelObj.OrgID;
                    req.ContractTypeName ??= modelObj.ContractTypeName;
                    req.Description ??= modelObj.Description;
                    req.FlagActive ??= modelObj.FlagActive;
                    req.Ft_Cols_Upd ??= modelObj.Ft_Cols_Upd;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContractType) && string.IsNullOrWhiteSpace(req.ContractTypeName) && string.IsNullOrWhiteSpace(req.Description) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_ContractType_Update", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:45319-45327
        var networkId = req.NetworkID != null ? StdParam(req.NetworkID) : (_config?["Biz_NetworkID"] ?? "1001");
        if (string.IsNullOrEmpty(networkId))
        {
            return PortResult.Fail("Mst_ContractType_Update", "Check.strNetworkID", req.NetworkID);
        }

        var contractType = StdParam(req.ContractType);
        if (string.IsNullOrEmpty(contractType))
        {
            return PortResult.Fail("Mst_ContractType_Update", "Check.ContractType", req.ContractType);
        }

        // 3. Kiểm tra sự tồn tại trong DB qua Mst_ContractType_CheckDB (Master.cs:45340-45347, 43978-44048)
        var existing = await _db.Set<Mst_ContractType>()
            .FirstOrDefaultAsync(x =>
                EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc) &&
                EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contractType), PortQuery.Esc));

        if (existing == null)
        {
            return PortResult.Fail("Mst_ContractType_CheckDB_OrganNotFound",
                "Check.NetworkID", networkId,
                "Check.ContractType", contractType);
        }

        // 4. Mở transaction và cập nhật dữ liệu (Master.cs:44681, 45353-45371)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dtimeSys = DateTime.UtcNow;
            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

            // Cập nhật các cột theo gate :303-307 và Master.cs:45332-45334, 45358-45363
            var ftColsUpd = req.Ft_Cols_Upd?.ToUpperInvariant();
            bool updAll = string.IsNullOrEmpty(ftColsUpd);

            if (updAll || ftColsUpd!.Contains("CONTRACTTYPENAME"))
            {
                if (req.ContractTypeName != null)
                {
                    existing.ContractTypeName = req.ContractTypeName.Trim();
                }
            }

            if (updAll || ftColsUpd!.Contains("DESCRIPTION"))
            {
                if (req.Description != null)
                {
                    existing.Description = req.Description.Trim();
                }
            }

            if (updAll || ftColsUpd!.Contains("FLAGACTIVE"))
            {
                if (req.FlagActive != null)
                {
                    existing.FlagActive = StdFlag(req.FlagActive);
                }
                else if (!string.IsNullOrEmpty(ftColsUpd) && ftColsUpd.Contains("FLAGACTIVE"))
                {
                    existing.FlagActive = StdFlag(null);
                }
            }

            existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
            existing.LogLUBy = userCode;

            // GAP: Mst_ContractTypeDtlService.Instance.WA_Mst_ContractTypeDtl_Save(objRQ_Mst_ContractTypeDtl, addressAPIs)
            // Backend API cho chi tiết loại hợp đồng không có trong Biz.Web (chỉ có DEV/zTemp). Bỏ qua theo Luật §1, P3.

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // 5. Trả kết quả thành công khớp gate MVC (Mst_ContractTypeController.cs:354-357)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Cập nhật loại hợp đồng thành công!" },
                Title = "Cập nhật loại hợp đồng thành công!",
                RedirectUrl = "/Mst_ContractType",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ContractType_Update", "Exception", ex.Message);
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

    private static string StdFlag(object? value)
    {
        var s = value?.ToString()?.Trim();
        return s == "1" ? "1" : "0";
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

/// <summary>
/// DTO tham số đầu vào cho action Update.
/// Gate: Mst_ContractTypeController.cs:148 Update(string contracttype)
/// </summary>
public class Mst_ContractType_UpdateReq
{
    public string? ContractType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// Dữ liệu trả về cho action Update, bao gồm thực thể Mst_ContractType và danh sách mẫu gửi active (ViewBag.List_Mst_SubmissionForm).
/// Gate: Mst_ContractTypeController.cs:193, 274
/// </summary>
public class Mst_ContractType_UpdateData : Mst_ContractType
{
    public Mst_ContractType? Mst_ContractType { get; set; }
    public List<Mst_SubmissionForm>? List_Mst_SubmissionForm { get; set; }

    public Mst_ContractType_UpdateData() { }

    public Mst_ContractType_UpdateData(Mst_ContractType src, List<Mst_SubmissionForm>? submissionForms = null)
    {
        NetworkID = src.NetworkID;
        ContractType = src.ContractType;
        OrgID = src.OrgID;
        ContractTypeName = src.ContractTypeName;
        Description = src.Description;
        FlagActive = src.FlagActive;
        LogLUDTimeUTC = src.LogLUDTimeUTC;
        LogLUBy = src.LogLUBy;
        Mst_ContractType = src;
        List_Mst_SubmissionForm = submissionForms;
    }
}

/// <summary>
/// DTO tham số đầu vào cho action Edit.
/// Gate: Mst_ContractTypeController.cs:287 Edit(string model, string modelDtl)
/// BIZ:  Master.cs:45279-45372
/// </summary>
public class Mst_ContractType_EditReq
{
    public string? Model { get; set; }
    public string? ModelDtl { get; set; }
    public string? NetworkID { get; set; }
    public string? ContractType { get; set; }
    public string? OrgID { get; set; }
    public string? ContractTypeName { get; set; }
    public string? Description { get; set; }
    public string? FlagActive { get; set; }
    public string? Ft_Cols_Upd { get; set; }
}
