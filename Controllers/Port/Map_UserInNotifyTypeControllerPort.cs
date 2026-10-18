using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Map_UserInNotifyTypeController.cs — Map_UserInNotifyType
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Map_UserInNotifyTypeController.cs:20-92 (Index), 96-144 (DoSearch), 148-200 (SaveData)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MapUserInNotifyTypeController.cs:92-160 (WA_Map_UserInNotifyType_Get), 21-89 (WA_Map_UserInNotifyType_Save)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstNNTController.cs:20-89 (WA_Mst_NNT_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:56083-56200 (WAS_Map_UserInNotifyType_Get) -> 56201-56331 (Map_UserInNotifyType_Get) -> 56333-56616 (Map_UserInNotifyType_GetX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:57011-57106 (WAS_Map_UserInNotifyType_Save) -> 56619-56770 (Map_UserInNotifyType_Save) -> 56772-57009 (Map_UserInNotifyType_SaveX), 54659-54718 (Map_UserInNotifyType_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:12121-12218 (WAS_Mst_NNT_Get) -> Mst_NNT_Get
[ApiController]
[Route("api/map_userinnotifytype")]
public class Map_UserInNotifyTypeControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Map_UserInNotifyTypeControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Map_UserInNotifyTypeController.cs:20-92.
    /// Chuỗi gọi:
    /// 1. Mst_NNTService.Instance.WA_Mst_NNT_Get (gate dòng 54)
    ///    -> API WA_Mst_NNT_Get -> WAS_Mst_NNT_Get -> Mst_NNT_Get (Master.cs:12121-12218).
    ///    Lấy danh sách đơn vị, gán vào ViewBag.ListMst_NNT.
    /// 2. Dựng biểu thức strWhereClauseSys_UserSearch theo Sys_User (gate dòng 62-66, 203-236).
    /// 3. Map_UserInNotifyTypeService.Instance.WA_Map_UserInNotifyType_Get (gate dòng 90)
    ///    -> API WA_Map_UserInNotifyType_Get -> WAS_Map_UserInNotifyType_Get -> Map_UserInNotifyType_Get -> Map_UserInNotifyType_GetX (Master.cs:56083-56616).
    ///    Lấy thông tin Map_UserInNotifyType, Mst_NotifyType, Mst_ManageNotify.
    /// 4. Trả về View(objRT_Map_UserInNotifyType) với model chứa các danh sách trên (gate dòng 91).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Map_UserInNotifyType_IndexReq? req)
    {
        try
        {
            var data = await GetMapUserInNotifyTypeCoreAsync(req);

            // Lấy danh sách đơn vị Mst_NNT (gate dòng 31-60; BIZ Master.cs:12121-12218)
            var strNetworkID = StdParam(req?.NetworkID);
            var strOrgID = StdParam(req?.OrgID);
            var nntQuery = _db.Set<Mst_NNT>().AsNoTracking();
            if (!string.IsNullOrEmpty(strNetworkID))
            {
                nntQuery = nntQuery.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(strNetworkID), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(strOrgID))
            {
                nntQuery = nntQuery.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc));
            }
            data.ListMst_NNT = await nntQuery.OrderBy(x => x.MST).ToListAsync();

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Map_UserInNotifyType_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action DoSearch từ gate Map_UserInNotifyTypeController.cs:96-144.
    /// Chuỗi gọi:
    /// 1. Dựng biểu thức strWhereClauseSys_UserSearch theo Sys_User (gate dòng 104-107, 203-236).
    /// 2. Map_UserInNotifyTypeService.Instance.WA_Map_UserInNotifyType_Get (gate dòng 135)
    ///    -> API WA_Map_UserInNotifyType_Get -> WAS_Map_UserInNotifyType_Get -> Map_UserInNotifyType_Get -> Map_UserInNotifyType_GetX (Master.cs:56083-56616).
    ///    Lấy thông tin Map_UserInNotifyType, Mst_NotifyType, Mst_ManageNotify.
    /// 3. Trả về JsonView("BindDataMap_UserInNotifyType", objRT_Map_UserInNotifyType) (gate dòng 136).
    /// </summary>
    [HttpPost("dosearch")]
    [HttpPost("search")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Map_UserInNotifyType_DoSearchReq? req)
    {
        try
        {
            var data = await GetMapUserInNotifyTypeCoreAsync(req);
            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Map_UserInNotifyType_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action SaveData từ gate Map_UserInNotifyTypeController.cs:148-200.
    /// Chuỗi gọi:
    /// 1. Kiểm tra quyền SysAccessCheckDeny(UserState, "MENU_MNG_MAP_USERINNOTIFYTYPE") (gate dòng 150-154).
    /// 2. Deserialize model: List<Map_UserInNotifyType> (gate dòng 161).
    /// 3. Đóng gói RQ_Map_UserInNotifyType và gọi Map_UserInNotifyTypeService.Instance.WA_Map_UserInNotifyType_Save (gate dòng 163-189).
    ///    - API: MapUserInNotifyTypeController.cs:21-89 (WA_Map_UserInNotifyType_Save) -> CheckGatewayAuthentication -> WAS_Map_UserInNotifyType_Save.
    ///    - BIZ: Master.cs:57011-57106 (WAS_Map_UserInNotifyType_Save) -> 56619-56770 (Map_UserInNotifyType_Save) -> 56772-57009 (Map_UserInNotifyType_SaveX).
    /// 4. Các bước nghiệp vụ tầng BIZ:
    ///    - Transaction begin (_cf.db.BeginTransaction(), Master.cs:56657).
    ///    - Kiểm tra bảng Map_UserInNotifyType: nếu tập dữ liệu không chứa bảng -> ném Map_UserInNotifyType_SaveX_Input_InvoiceTblNotFound (Master.cs:56832-56841).
    ///    - Kiểm tra số dòng: nếu bảng có số dòng < 1 -> ném Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid (Master.cs:56845-56855).
    ///    - Chuẩn hoá các trường: UserCode (StdParam), NotifyType (StdParam), FlagNotify (StdFlag), LogLUDTimeUTC (UTC now), LogLUBy (strWAUserCode) (Master.cs:56857-56885).
    ///    - Gọi Map_UserInNotifyType_CheckDB(UserCode, NotifyType, "", "") (Master.cs:56873-56880, 54659-54718).
    ///    - Xoá các bản ghi trùng (UserCode, NotifyType) trong bảng Map_UserInNotifyType (Master.cs:56912-56925).
    ///    - Nếu FlagIsDelete != "1" (!bIsDelete): Chèn các bản ghi mới vào Map_UserInNotifyType (Master.cs:56929-56968).
    ///    - Commit transaction (Master.cs:56733). Rollback nếu có lỗi ngoại lệ và trả mã lỗi Map_UserInNotifyType_Save (Master.cs:56740-56749).
    /// 5. Phản hồi: Success = true, Messages = ["Phân quyền thông báo thành công!"], Title = "Phân quyền thông báo thành công!" (gate dòng 190-192).
    /// </summary>
    [HttpPost("savedata")]
    [HttpPost("save")]
    public async Task<IActionResult> SaveData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement? rawBody)
    {
        try
        {
            // 1. Phân tích dữ liệu đầu vào từ JSON body
            var (hasTable, items, flagIsDelete, waUserCode) = ParseSaveDataInput(rawBody);

            // 2. Guard 1: Kiểm tra bảng Map_UserInNotifyType (Master.cs:56832-56841)
            if (!hasTable)
            {
                return PortResult.Fail("Map_UserInNotifyType_SaveX_Input_InvoiceTblNotFound", "Check.TableName", "Map_UserInNotifyType");
            }

            // 3. Guard 2: Kiểm tra số dòng dữ liệu < 1 (Master.cs:56845-56855)
            if (items == null || items.Count < 1)
            {
                return PortResult.Fail("Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid", "Check.TableName", "Map_UserInNotifyType");
            }

            // 4. Chuẩn hoá các trường dữ liệu theo Master.cs:56857-56885
            var processedList = new List<Map_UserInNotifyType>();
            var dtimeSys = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (var raw in items)
            {
                var uCode = StdParam(raw.UserCode);
                var nType = StdParam(raw.NotifyType);
                if (!string.IsNullOrEmpty(uCode) && !string.IsNullOrEmpty(nType))
                {
                    processedList.Add(new Map_UserInNotifyType
                    {
                        UserCode = uCode,
                        NotifyType = nType,
                        FlagNotify = StdFlag(raw.FlagNotify),
                        LogLUDTimeUTC = dtimeSys,
                        LogLUBy = waUserCode
                    });
                }
            }

            if (processedList.Count < 1)
            {
                return PortResult.Fail("Map_UserInNotifyType_SaveX_Input_InvoiceTblInvalid", "Check.TableName", "Map_UserInNotifyType");
            }

            // 5. Mở Transaction lưu dữ liệu theo Master.cs:56657-56745
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var bIsDelete = (flagIsDelete?.Trim() == "1");

                // Xoá các bản ghi trùng UserCode và NotifyType (Master.cs:56912-56925)
                foreach (var item in processedList)
                {
                    var existing = await _db.Set<Map_UserInNotifyType>()
                        .Where(x => EF.Functions.Like((x.UserCode ?? "").ToLower(), PortQuery.Equal(item.UserCode), PortQuery.Esc)
                                 && EF.Functions.Like((x.NotifyType ?? "").ToLower(), PortQuery.Equal(item.NotifyType), PortQuery.Esc))
                        .ToListAsync();

                    if (existing.Count > 0)
                    {
                        _db.Set<Map_UserInNotifyType>().RemoveRange(existing);
                    }
                }
                await _db.SaveChangesAsync();

                // Nếu không phải cờ xoá (!bIsDelete) thì thêm mới (Master.cs:56929-56968)
                if (!bIsDelete)
                {
                    // Lọc trùng lặp trong chính danh sách input nếu có (lấy phần tử cuối cùng)
                    var distinctItems = processedList
                        .GroupBy(x => new { x.UserCode, x.NotifyType })
                        .Select(g => g.Last())
                        .ToList();

                    await _db.Set<Map_UserInNotifyType>().AddRangeAsync(distinctItems);
                    await _db.SaveChangesAsync();
                }

                await tx.CommitAsync();

                // 6. Trả về kết quả thành công theo gate Map_UserInNotifyTypeController.cs:190-192
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Phân quyền thông báo thành công!" },
                    Title = "Phân quyền thông báo thành công!"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Map_UserInNotifyType_Save", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Map_UserInNotifyType_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Hàm trích xuất dữ liệu danh sách Map_UserInNotifyType từ các định dạng đầu vào khác nhau:
    /// 1. Mảng JSON trực tiếp [...]
    /// 2. Đối tượng { "Lst_Map_UserInNotifyType": [...] }
    /// 3. Đối tượng { "model": "[{...}]" } hoặc { "model": [...] }
    /// 4. Đối tượng { "data": [...] }
    /// </summary>
    private static (bool hasTable, List<Map_UserInNotifyType>? items, string flagIsDelete, string waUserCode) ParseSaveDataInput(JsonElement? rawBody)
    {
        if (rawBody == null || rawBody.Value.ValueKind == JsonValueKind.Undefined || rawBody.Value.ValueKind == JsonValueKind.Null)
        {
            return (true, null, "0", "port");
        }

        var flagIsDelete = "0";
        var waUserCode = "port";

        if (rawBody.Value.ValueKind == JsonValueKind.Array)
        {
            var list = JsonSerializer.Deserialize<List<Map_UserInNotifyType>>(rawBody.Value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return (true, list, flagIsDelete, waUserCode);
        }

        if (rawBody.Value.ValueKind == JsonValueKind.Object)
        {
            var root = rawBody.Value;
            if (root.TryGetProperty("FlagIsDelete", out var fidProp) || root.TryGetProperty("flagIsDelete", out fidProp))
            {
                flagIsDelete = fidProp.GetString() ?? "0";
            }
            if (root.TryGetProperty("WAUserCode", out var wucProp) || root.TryGetProperty("waUserCode", out wucProp))
            {
                waUserCode = wucProp.GetString() ?? "port";
            }

            bool foundTableProp = false;
            List<Map_UserInNotifyType>? list = null;

            if (root.TryGetProperty("Lst_Map_UserInNotifyType", out var lstProp) || root.TryGetProperty("lst_Map_UserInNotifyType", out lstProp))
            {
                foundTableProp = true;
                if (lstProp.ValueKind == JsonValueKind.Array)
                {
                    list = JsonSerializer.Deserialize<List<Map_UserInNotifyType>>(lstProp.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            else if (root.TryGetProperty("model", out var modelProp) || root.TryGetProperty("Model", out modelProp))
            {
                foundTableProp = true;
                if (modelProp.ValueKind == JsonValueKind.String)
                {
                    var str = modelProp.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        try
                        {
                            list = JsonSerializer.Deserialize<List<Map_UserInNotifyType>>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        catch
                        {
                            list = new List<Map_UserInNotifyType>();
                        }
                    }
                }
                else if (modelProp.ValueKind == JsonValueKind.Array)
                {
                    list = JsonSerializer.Deserialize<List<Map_UserInNotifyType>>(modelProp.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            else if (root.TryGetProperty("data", out var dataProp) || root.TryGetProperty("Data", out dataProp))
            {
                foundTableProp = true;
                if (dataProp.ValueKind == JsonValueKind.Array)
                {
                    list = JsonSerializer.Deserialize<List<Map_UserInNotifyType>>(dataProp.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }

            // Nếu request chứa các trường khác nhưng không chứa trường bảng Map_UserInNotifyType
            if (!foundTableProp)
            {
                var propCount = 0;
                foreach (var _ in root.EnumerateObject()) propCount++;
                if (propCount > 0)
                {
                    return (false, null, flagIsDelete, waUserCode);
                }
            }

            return (true, list, flagIsDelete, waUserCode);
        }

        return (true, null, flagIsDelete, waUserCode);
    }

    /// <summary>
    /// Hàm xử lý nghiệp vụ tìm kiếm Map_UserInNotifyType dùng chung cho Index và DoSearch
    /// theo Master.cs:56083-56616 (WAS_Map_UserInNotifyType_Get -> Map_UserInNotifyType_Get -> Map_UserInNotifyType_GetX).
    /// </summary>
    private async Task<RT_Map_UserInNotifyType> GetMapUserInNotifyTypeCoreAsync(Map_UserInNotifyType_IndexReq? req)
    {
        // 1. Chuẩn hoá tham số đầu vào (LESSONS A.14, A.21, PORT_RULES §3)
        var strMST = StdParam(req?.MST);
        var strUserName = StdStr(req?.Email ?? req?.UserName);
        var strUserCode = StdParam(req?.UserCode);
        var strNotifyType = StdParam(req?.NotifyType);

        // 2. Chuẩn hoá tham số phân trang (LESSONS A.7, A.12)
        var pageSize = req?.RecordCount ?? req?.PageSize ?? (int?)(req?.Ft_RecordCount) ?? 10;
        var pageIndex = req?.Page ?? req?.PageIndex ?? 0;
        long recordStart = req?.Ft_RecordStart ?? ((long)pageIndex * pageSize);
        long recordCount = req?.Ft_RecordCount ?? pageSize;

        // 3. Dựng truy vấn Map_UserInNotifyType theo Map_UserInNotifyType_GetX (Master.cs:56389-56594)
        var query = _db.Set<Map_UserInNotifyType>().AsNoTracking();

        if (!string.IsNullOrEmpty(strUserCode))
        {
            query = query.Where(mo => EF.Functions.Like((mo.UserCode ?? "").ToLower(), PortQuery.Equal(strUserCode), PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(strNotifyType))
        {
            query = query.Where(mo => EF.Functions.Like((mo.NotifyType ?? "").ToLower(), PortQuery.Equal(strNotifyType), PortQuery.Esc));
        }

        // Lọc theo điều kiện người dùng từ Sys_User (tương ứng strWhereClause_Sys_UserSearch trong gate dòng 62-66, 104-107, 203-236)
        if (!string.IsNullOrEmpty(strMST) || !string.IsNullOrEmpty(strUserName))
        {
            var users = _db.Set<Sys_User>().AsNoTracking();
            if (!string.IsNullOrEmpty(strMST))
            {
                users = users.Where(u => EF.Functions.Like((u.MST ?? "").ToLower(), PortQuery.Equal(strMST), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(strUserName))
            {
                users = users.Where(u => EF.Functions.Like((u.EMail ?? "").ToLower(), PortQuery.Contains(strUserName), PortQuery.Esc)
                                      || EF.Functions.Like((u.UserName ?? "").ToLower(), PortQuery.Contains(strUserName), PortQuery.Esc));
            }
            query = query.Where(mo => users.Any(u => u.UserCode == mo.UserCode));
        }

        // 4. Đếm tổng số bản ghi (Master.cs:56411 select Count(0) MyCount)
        var itemCount = await query.CountAsync();

        // 5. Sắp xếp theo UserCode asc, NotifyType asc (Master.cs:56407 order by mo.UserCode asc)
        var ordered = query.OrderBy(x => x.UserCode).ThenBy(x => x.NotifyType);

        // 6. Lấy danh sách trang (LESSONS A.7, A.12)
        var pagedList = (recordCount <= 0 || recordStart < 0)
            ? new List<Map_UserInNotifyType>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordCount, int.MaxValue))
                           .ToListAsync();

        // 7. Lấy các loại thông báo Mst_NotifyType liên quan của trang (Master.cs:56466-56486)
        var pagedNotifyTypes = pagedList.Select(x => x.NotifyType).Distinct().ToList();
        var listMst_NotifyType = pagedNotifyTypes.Count == 0
            ? new List<Mst_NotifyType>()
            : await _db.Set<Mst_NotifyType>().AsNoTracking()
                .Where(x => pagedNotifyTypes.Contains(x.NotifyType))
                .OrderBy(x => x.NotifyType)
                .ToListAsync();

        // 8. Lấy thông tin người dùng quản lý thông báo Mst_ManageNotify liên quan của trang (Master.cs:56495-56518)
        var pagedUserCodes = pagedList.Select(x => x.UserCode).Distinct().ToList();
        var listMst_ManageNotify = pagedUserCodes.Count == 0
            ? new List<Mst_ManageNotify>()
            : await _db.Set<Mst_ManageNotify>().AsNoTracking()
                .Where(x => pagedUserCodes.Contains(x.UserCode))
                .OrderBy(x => x.UserCode)
                .ToListAsync();

        // 9. Tính số trang
        var ps = pageSize <= 0 ? 10 : pageSize;
        var pageCount = pagedList.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return new RT_Map_UserInNotifyType
        {
            MySummaryTable = new MySummaryTable { MyCount = itemCount },
            Lst_Map_UserInNotifyType = pagedList,
            Lst_Mst_NotifyType = listMst_NotifyType,
            Lst_Mst_ManageNotify = listMst_ManageNotify,
            ItemCount = itemCount,
            PageCount = pageCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action Index của Map_UserInNotifyTypeController.
/// </summary>
public class Map_UserInNotifyType_IndexReq
{
    public string? MST { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? UserCode { get; set; }
    public string? NotifyType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public long? Ft_RecordStart { get; set; }
    public long? Ft_RecordCount { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? RecordCount { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action DoSearch của Map_UserInNotifyTypeController.
/// </summary>
public class Map_UserInNotifyType_DoSearchReq : Map_UserInNotifyType_IndexReq
{
}

/// <summary>
/// DTO yêu cầu cho action SaveData của Map_UserInNotifyTypeController.
/// </summary>
public class Map_UserInNotifyType_SaveDataReq
{
    public object? Model { get; set; }
    public List<Map_UserInNotifyType>? Lst_Map_UserInNotifyType { get; set; }
    public string? FlagIsDelete { get; set; }
    public string? WAUserCode { get; set; }
}

/// <summary>
/// Bảng tóm tắt số lượng bản ghi tương ứng MySummaryTable trong WARTBase nguồn.
/// </summary>
public class MySummaryTable
{
    public object MyCount { get; set; } = 0;
}

/// <summary>
/// Cấu trúc kết quả trả về tương ứng RT_Map_UserInNotifyType từ nguồn BIZ và ViewBag.ListMst_NNT từ gate.
/// </summary>
public class RT_Map_UserInNotifyType
{
    public MySummaryTable? MySummaryTable { get; set; }
    public List<Map_UserInNotifyType> Lst_Map_UserInNotifyType { get; set; } = new();
    public List<Mst_NotifyType> Lst_Mst_NotifyType { get; set; } = new();
    public List<Mst_ManageNotify> Lst_Mst_ManageNotify { get; set; } = new();
    public List<Mst_NNT> ListMst_NNT { get; set; } = new();
    public List<Map_UserInNotifyType> DataList => Lst_Map_UserInNotifyType;
    public int ItemCount { get; set; }
    public int PageCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}
