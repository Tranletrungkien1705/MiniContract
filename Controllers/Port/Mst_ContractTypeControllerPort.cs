using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_ContractTypeController.cs — Mst_ContractType
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ContractTypeController.cs:26-90 (Index), 92-95 (Create GET), 97-146 (Create POST), 148-283 (Update), 287-365 (Edit POST), 367-408 (Detail), 411-460 (Delete), 594-742 (Import), 748-807 (Export), 811-833 (ExportTemplate)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstContractTypeController.cs:20-88 (Index, Update, Get, Export), 89-140 (Create), 160-228 (WA_Mst_ContractType_Update), 230-298 (WA_Mst_ContractType_Delete)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:43978-44048 (CheckDB), 44050-44141 (WAS Get/Update/Export), 44142-44297 (Biz Get/Update/Export), 44299-44548 (WAS/Biz Create), 44549-44633 (WAS Update), 44634-44798 (Biz Update), 44799-44877 (WAS Delete), 44878-45023 (Biz Delete), 45024-45180 (Search/Index/Update/Export GetX), 45181-45278 (CreateX), 45279-45372 (UpdateX), 45373-45483 (DeleteX)
[ApiController]
[Route("api/mst_contracttype")]
public class Mst_ContractTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;
    private readonly IWebHostEnvironment? _env;

    private static readonly Dictionary<string, string> ExportDicColumns = new()
    {
        { "ContractType", "Mã loại hợp đồng(*)" },
        { "ContractTypeName", "Tên loại hợp đồng(*)" },
        { "Description", "Mô tả" },
        { "FlagActive", "Trạng thái" }
    };

    private static readonly Dictionary<string, string> ExportTemplateDicColumns = new()
    {
        { "ContractType", "Mã loại hợp đồng(*)" },
        { "ContractTypeName", "Tên loại hợp đồng(*)" },
        { "Description", "Mô tả" }
    };

    public Mst_ContractTypeControllerPort(AppDbContext db, IConfiguration? config = null, IWebHostEnvironment? env = null)
    {
        _db = db;
        _config = config;
        _env = env;
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
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port"; // Master.cs:45267

        var (err, entity) = await CreateCoreAsync(networkId, contractType, orgId ?? "", contractTypeName, description, userCode);
        if (err != null)
        {
            return err;
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

    /// <summary>
    /// Xem chi tiết loại hợp đồng.
    /// Gate: Mst_ContractTypeController.cs:367-408 Detail(string contracttype)
    /// API:  MstContractTypeController.cs:20-88 WA_Mst_ContractType_Get
    /// BIZ:  Master.cs:44050-44141 WAS_Mst_ContractType_Get -> Mst_ContractType_Get:44142-44297 -> Mst_ContractType_GetX:45024-45180
    /// </summary>
    [HttpPost("detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ContractType_DetailReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào ContractType (Gate: Mst_ContractTypeController.cs:367, 378)
            var contractType = StdParam(req?.ContractType);
            if (string.IsNullOrEmpty(contractType) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_ContractType_DetailReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    contractType = StdParam(modelObj?.ContractType);
                }
                catch
                {
                    contractType = StdParam(req.Model);
                }
            }

            // 2. Nếu contracttype rỗng hoặc null, gate không gọi API và trả về View(new Mst_ContractType()) (Gate: Mst_ContractTypeController.cs:378, 407)
            if (string.IsNullOrEmpty(contractType))
            {
                return Ok(new
                {
                    Success = true,
                    Data = (Mst_ContractType?)null
                });
            }

            // 3. Dựng truy vấn cơ sở từ bảng Mst_ContractType (Master.cs:45078-45087, 45120-45127)
            var query = _db.Set<Mst_ContractType>().AsNoTracking();

            // Mệnh đề lọc: Mst_ContractType.ContractType = contracttype (Gate: Mst_ContractTypeController.cs:380)
            query = query.Where(x => EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contractType), PortQuery.Esc));

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

            // 4. Trả kết quả: nếu tìm thấy thì Data = item (Gate: dòng 404 JsonView("Detail", objRT_Mst_ContractType.Lst_Mst_ContractType[0])),
            //    nếu không tìm thấy thì Data = null (Gate: dòng 407 View(new Mst_ContractType())).
            return Ok(new
            {
                Success = true,
                Data = item
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_ContractType_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xóa loại hợp đồng.
    /// Gate: Mst_ContractTypeController.cs:411-460 Delete(string contracttype)
    /// API:  MstContractTypeController.cs:230-298 WA_Mst_ContractType_Delete
    /// BIZ:  Master.cs:44799-44877 WAS_Mst_ContractType_Delete -> Mst_ContractType_Delete:44878-45023 -> Mst_ContractType_DeleteX:45373-45483
    ///       Master.cs:43978-44048 Mst_ContractType_CheckDB
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ContractType_DeleteReq? req)
    {
        // 1. Kiểm tra body rỗng hoàn toàn (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_ContractType_Delete", "Check.strJson", null);
        }

        // Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_ContractTypeController.cs:411)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_ContractType_DeleteReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.ContractType ??= modelObj.ContractType;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.OrgID ??= modelObj.OrgID;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.ContractType) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_ContractType_Delete", "Check.strJson", null);
        }

        // 2. Chuẩn hoá tham số đầu vào theo Master.cs:45400-45401
        var networkId = req.NetworkID != null ? StdParam(req.NetworkID) : (_config?["Biz_NetworkID"] ?? "1001");
        if (string.IsNullOrEmpty(networkId))
        {
            return PortResult.Fail("Mst_ContractType_Delete", "Check.strNetworkID", req.NetworkID);
        }

        var contractType = StdParam(req.ContractType);
        if (string.IsNullOrEmpty(contractType))
        {
            return PortResult.Fail("Mst_ContractType_Delete", "Check.ContractType", req.ContractType);
        }

        // 3. Mở transaction (Master.cs:44912)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 4. Kiểm tra tồn tại trong DB qua Mst_ContractType_CheckDB (Master.cs:45406-45413, 43989-44018)
            var existing = await _db.Set<Mst_ContractType>()
                .FirstOrDefaultAsync(x =>
                    EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc) &&
                    EF.Functions.Like((x.ContractType ?? "").ToLower(), PortQuery.Equal(contractType), PortQuery.Esc));

            if (existing == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_ContractType_CheckDB_OrganNotFound",
                    "Check.NetworkID", networkId,
                    "Check.ContractType", contractType);
            }

            // 5. Kiểm tra ràng buộc sử dụng trong Contract_TempContract và Contract_ContractParty (Master.cs:45416-45467)
            bool hasTempContractTable = false;
            bool hasContractPartyTable = false;
            try
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
                using var cmdCheck = conn.CreateCommand();
                cmdCheck.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Contract_TempContract', 'Contract_ContractParty');";
                using var readerCheck = await cmdCheck.ExecuteReaderAsync();
                while (await readerCheck.ReadAsync())
                {
                    var tblName = readerCheck.GetString(0);
                    if (tblName.Equals("Contract_TempContract", StringComparison.OrdinalIgnoreCase)) hasTempContractTable = true;
                    if (tblName.Equals("Contract_ContractParty", StringComparison.OrdinalIgnoreCase)) hasContractPartyTable = true;
                }
            }
            catch
            {
                // Bỏ qua lỗi truy vấn sqlite_master nếu chạy trên môi trường khác
            }

            if (hasTempContractTable)
            {
                var conn = _db.Database.GetDbConnection();
                using var cmdTemp = conn.CreateCommand();
                cmdTemp.CommandText = "SELECT TContractCode, ContractType FROM Contract_TempContract WHERE ContractType = @ct;";
                var p = cmdTemp.CreateParameter();
                p.ParameterName = "@ct";
                p.Value = contractType;
                cmdTemp.Parameters.Add(p);
                using var readerTemp = await cmdTemp.ExecuteReaderAsync();
                var rowsTemp = new List<(string Code, string Type)>();
                while (await readerTemp.ReadAsync())
                {
                    rowsTemp.Add((readerTemp.GetString(0), readerTemp.GetString(1)));
                }
                if (rowsTemp.Count > 0)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_ContractType_Delete_ContractTypeUsed",
                        "Check.TContractCode", rowsTemp[0].Code,
                        "Check.ContractType", rowsTemp[0].Type,
                        "Check.TContractCount", rowsTemp.Count);
                }
            }

            if (hasContractPartyTable)
            {
                var conn = _db.Database.GetDbConnection();
                using var cmdParty = conn.CreateCommand();
                cmdParty.CommandText = "SELECT ContractCode, ContractType FROM Contract_ContractParty WHERE ContractType = @ct;";
                var p = cmdParty.CreateParameter();
                p.ParameterName = "@ct";
                p.Value = contractType;
                cmdParty.Parameters.Add(p);
                using var readerParty = await cmdParty.ExecuteReaderAsync();
                var rowsParty = new List<(string Code, string Type)>();
                while (await readerParty.ReadAsync())
                {
                    rowsParty.Add((readerParty.GetString(0), readerParty.GetString(1)));
                }
                if (rowsParty.Count > 0)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_ContractType_Delete_ContractTypeUsed",
                        "Check.TContractCode", rowsParty[0].Code,
                        "Check.ContractType", rowsParty[0].Type,
                        "Check.TContractCount", rowsParty.Count);
                }
            }

            // 6. Xóa bản ghi trong DB (Master.cs:45474-45480)
            _db.Set<Mst_ContractType>().Remove(existing);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // 7. Trả kết quả thành công khớp gate MVC (Mst_ContractTypeController.cs:449-452)
            return Ok(new
            {
                Success = true,
                Messages = new[] { "Xoá loại hợp đồng thành công!" },
                Title = "Xoá loại hợp đồng thành công!",
                RedirectUrl = "/Mst_ContractType",
                Data = existing
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_ContractType_Delete", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Nhập dữ liệu loại hợp đồng từ tệp Excel.
    /// Gate: Mst_ContractTypeController.cs:594-742 Import(HttpPostedFileWrapper file)
    /// API:  MstContractTypeController.cs:20-88 WA_Mst_ContractType_Get, 90-158 WA_Mst_ContractType_Create
    /// BIZ:  Master.cs:44050-44141 (WAS Get), 44142-44297 (Biz Get), 44299-44548 (WAS/Biz Create), 45024-45180 (GetX), 45181-45278 (CreateX)
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file == null && Request?.HasFormContentType == true && Request.Form.Files.Count > 0)
        {
            file = Request.Form.Files[0];
        }

        // 1. Kiểm tra file upload (Gate: Mst_ContractTypeController.cs:609-625)
        if (file == null || file.Length == 0)
        {
            return PortResult.Fail("Mst_ContractType_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            return PortResult.Fail("Mst_ContractType_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        // 2. Đọc bảng tính bằng PortExcel.ReadSheet (Gate: Mst_ContractTypeController.cs:631 ExcelImportNew.Query(filePath, "A2"))
        // Danh sách cột từ GetImportDicColums() (Gate:837-845):
        // 1. ContractType: Mã loại hợp đồng(*)
        // 2. ContractTypeName: Tên loại hợp đồng(*)
        // 3. Description: Mô tả
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (columnCount, rows) = PortExcel.ReadSheet(stream, new[] { "ContractType", "ContractTypeName", "Description" });
        }
        catch
        {
            return PortResult.Fail("Mst_ContractType_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        // 3. Kiểm tra bảng rỗng (Gate: Mst_ContractTypeController.cs:632, 729-734: table == null || table.Rows.Count == 0)
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Mst_ContractType_Import", "Message", "File Excel nhập không có dữ liệu!");
        }

        // 4. Kiểm tra số cột (Gate: Mst_ContractTypeController.cs:635-640: table.Columns.Count != GetImportDicColums().Count)
        if (columnCount != 3)
        {
            return PortResult.Fail("Mst_ContractType_Import", "Message", "File dữ liệu import không hợp lệ!");
        }

        // 5. Tải danh sách hiện tại từ DB để kiểm tra trùng (Gate: Mst_ContractTypeController.cs:643-667)
        var networkId = Request?.Headers["NetworkID"].FirstOrDefault() ?? _config?["Biz_NetworkID"] ?? "1001";
        var orgId = Request?.Headers["OrgID"].FirstOrDefault() ?? _config?["Biz_OrgID"] ?? "7200000001";
        var listCur = await _db.Set<Mst_ContractType>()
            .AsNoTracking()
            .Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc))
            .ToListAsync();

        // 6. Vòng lặp 1: Kiểm tra TẤT CẢ các dòng trước khi tạo (Gate: Mst_ContractTypeController.cs:669-683)
        foreach (var r in rows)
        {
            var code = r.GetValueOrDefault("ContractType");
            if (string.IsNullOrWhiteSpace(code))
            {
                return PortResult.Fail("Mst_ContractType_Import", "Message", "Mã loại hợp đồng không được trống!");
            }

            var name = r.GetValueOrDefault("ContractTypeName");
            if (string.IsNullOrWhiteSpace(name))
            {
                return PortResult.Fail("Mst_ContractType_Import", "Message", "Tên loại hợp đồng không được trống!");
            }
        }

        // 7. Vòng lặp 2: Lưu từng dòng tuần tự theo transaction độc lập (Gate: Mst_ContractTypeController.cs:704-722, hành vi import một phần)
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

        foreach (var r in rows)
        {
            var code = r.GetValueOrDefault("ContractType")?.Trim().ToUpperInvariant() ?? "";
            var name = r.GetValueOrDefault("ContractTypeName")?.Trim() ?? "";
            var desc = r.GetValueOrDefault("Description")?.Trim() ?? "";

            // Kiểm tra trùng mã trong danh sách hệ thống listCur (Gate: Mst_ContractTypeController.cs:706-712)
            if (listCur.Any(m => string.Equals((m.ContractType ?? "").Trim(), code, StringComparison.OrdinalIgnoreCase)))
            {
                var exitsData = $"Mã loại hợp đồng: '{code}' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!";
                return PortResult.Fail("Mst_ContractType_Import", "Message", exitsData);
            }

            // Gọi CreateCoreAsync độc lập cho từng dòng (Gate lines 720-721)
            var (err, entity) = await CreateCoreAsync(networkId, code, orgId, name, desc, userCode);
            if (err != null)
            {
                // Lỗi ở dòng k -> dừng ngay và trả lỗi của dòng đó (các dòng trước k đã commit thành công)
                return err;
            }

            // Cập nhật listCur trong bộ nhớ để các dòng tiếp theo trong cùng tệp nếu trùng cũng bị bắt
            listCur.Add(new Mst_ContractType
            {
                NetworkID = networkId,
                ContractType = code,
                ContractTypeName = name,
                OrgID = orgId
            });
        }

        // 8. Trả kết quả thành công khớp gate MVC (Gate: Mst_ContractTypeController.cs:724-726)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Đã nhập dữ liệu excel thành công" },
            Title = "Đã nhập dữ liệu excel thành công",
            RedirectUrl = "/Mst_ContractType"
        });
    }

    /// <summary>
    /// Xuất danh sách loại hợp đồng ra tệp Excel.
    /// Gate: Mst_ContractTypeController.cs:748-807 Export(int page = 0, int recordcount = 10)
    /// API:  MstContractTypeController.cs:20-88 WA_Mst_ContractType_Get
    /// BIZ:  Master.cs:44050-44141 (WAS Get), 44142-44297 (Biz Get), 45024-45180 (GetX)
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_ContractType_ExportReq? req)
    {
        try
        {
            // 1. Dựng truy vấn cơ sở từ bảng Mst_ContractType (Master.cs:45078-45087, 45120-45127)
            var query = _db.Set<Mst_ContractType>().AsNoTracking();

            // 2. Bộ lọc điều kiện theo các cột hỗ trợ (Master.cs:45139-45155)
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

            var description = req?.Description?.Trim();
            if (!string.IsNullOrEmpty(description))
            {
                query = query.Where(x => EF.Functions.Like((x.Description ?? "").ToLower(), PortQuery.Contains(description), PortQuery.Esc));
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

            // 3. Sắp xếp: order by mo.NetworkID asc, then by ContractType (Master.cs:45086, 45126)
            var ordered = query.OrderBy(x => x.NetworkID).ThenBy(x => x.ContractType);

            // 4. Phân trang nếu có truyền (Gate lines 773-774: Ft_RecordStart = "0", Ft_RecordCount = "123456000")
            long recordStart = req?.Ft_RecordStart ?? 0;
            long recordCount = req?.Ft_RecordCount ?? 123456000;
            if (req != null && req.RecordCount != null && req.Ft_RecordCount == null)
            {
                recordCount = req.RecordCount.Value;
                var page = req.Page ?? req.PageIndex ?? 0;
                recordStart = (long)page * recordCount;
            }

            var list = (recordCount <= 0 || recordStart < 0)
                ? new List<Mst_ContractType>()
                : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                               .Take((int)Math.Min(recordCount, int.MaxValue))
                               .ToListAsync();

            // 5. Kiểm tra danh sách rỗng (Gate lines 785, 796-797: Nonsense.MESS_CHECK_FILE_NULL)
            if (list == null || list.Count == 0)
            {
                return Ok(new
                {
                    Success = true,
                    Title = "File Excel nhập không có dữ liệu!",
                    CheckSuccess = "1"
                });
            }

            // 6. Xuất danh sách ra file Excel và trả URL (Gate lines 787-793: Nonsense.MESS_EXPORT_EXCEL_SUCCESS)
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_ContractType));
            PortExcel.ExportToExcelFromList(list, ExportDicColumns, filePath, nameof(Mst_ContractType));

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
            return PortResult.Fail("Mst_ContractType_Export", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xuất tệp mẫu Excel để nhập loại hợp đồng.
    /// Gate: Mst_ContractTypeController.cs:811-833 ExportTemplate()
    /// </summary>
    [HttpPost("exporttemplate")]
    public IActionResult ExportTemplate([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? req = null)
    {
        try
        {
            // 1. Tạo danh sách rỗng (Gate line 816: var listInv = new List<Mst_ContractType>())
            var listInv = new List<Mst_ContractType>();

            // 2. Lấy từ điển cột từ GetImportDicColums() gồm 3 cột (Gate lines 817, 837-845)
            var dicColNames = ExportTemplateDicColumns;

            // 3. Sinh đường dẫn file vật lý và URL ảo (Gate lines 820-821: GenExcelExportFilePath)
            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_ContractType));

            // 4. Xuất danh sách rỗng ra file Excel chứa dòng tiêu đề (Gate line 822: ExcelExport.ExportToExcelFromList)
            PortExcel.ExportToExcelFromList(listInv, dicColNames, filePath, nameof(Mst_ContractType));

            // 5. Trả kết quả thành công khớp gate MVC (Gate line 824: Nonsense.MESS_EXPORT_EXCEL_SUCCESS)
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
            // Gate lines 827-832: resultEntry.SetFailed().AddException(ex)
            return PortResult.Fail("Mst_ContractType_ExportTemplate", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hàm dùng chung thực hiện tạo mới 1 bản ghi Mst_ContractType trong 1 transaction riêng.
    /// Dùng cho cả action Create đơn và vòng lặp Import (hành vi import một phần).
    /// </summary>
    private async Task<(IActionResult? Error, Mst_ContractType? Entity)> CreateCoreAsync(
        string networkId, string contractType, string orgId, string contractTypeName, string description, string userCode)
    {
        var dtimeSys = DateTime.UtcNow;
        var entity = new Mst_ContractType
        {
            NetworkID = networkId,
            ContractType = contractType,
            OrgID = orgId,
            ContractTypeName = contractTypeName,
            Description = description,
            FlagActive = "1", // Master.cs:45265 TConst.Flag.Active
            LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = userCode
        };

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
                return (PortResult.Fail("Mst_ContractType_CheckDB_OrganExist",
                    "Check.NetworkID", networkId,
                    "Check.ContractType", contractType), null);
            }

            _db.Set<Mst_ContractType>().Add(entity);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return (null, entity);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (PortResult.Fail("Mst_ContractType_Create", "Exception", ex.Message), null);
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

/// <summary>
/// DTO tham số đầu vào cho action Detail.
/// Gate: Mst_ContractTypeController.cs:367 Detail(string contracttype)
/// </summary>
public class Mst_ContractType_DetailReq
{
    public string? ContractType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO tham số đầu vào cho action Delete.
/// Gate: Mst_ContractTypeController.cs:411 Delete(string contracttype)
/// BIZ:  Master.cs:45373-45483
/// </summary>
public class Mst_ContractType_DeleteReq
{
    public string? ContractType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// DTO tham số đầu vào cho action Export.
/// Gate: Mst_ContractTypeController.cs:748 Export(int page = 0, int recordcount = 10)
/// </summary>
public class Mst_ContractType_ExportReq
{
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }

    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? ContractType { get; set; }
    public string? ContractTypeName { get; set; }
    public string? Description { get; set; }
    public string? FlagActive { get; set; }
    public string? KeyWord { get; set; }
}
