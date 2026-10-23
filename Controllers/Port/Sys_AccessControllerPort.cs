using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Sys_AccessController.cs — Sys_Access
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:23-105 (Index)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:110-167 (GetDetailData)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:467-482 (strWhereClause_SysObject)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_AccessController.cs:484-499 (strWhereClause_SysAccess)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysObjectController.cs:20-91 (WA_Sys_Object_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysAccessController.cs:20-91 (WA_Sys_Access_Get)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:14990-15089 (WAS_Sys_Object_Get) -> 14765-14900 (Sys_Object_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:704-801 (WAS_Sys_Access_Get) -> 445-560 / 221-443 (Sys_Access_Get) -> 44-107 (Sys_Access_CheckDeny)
[ApiController]
[Route("api/sys_access")]
public class Sys_AccessControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;
    private readonly IWebHostEnvironment? _env;

    public Sys_AccessControllerPort(AppDbContext db, IConfiguration? config = null, IWebHostEnvironment? env = null)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Sys_AccessController.cs:23-105.
    /// Chuỗi gọi:
    /// 1. Sys_ObjectService.Instance.WA_Sys_Object_Get (gate dòng 60)
    ///    -> API WA_Sys_Object_Get (SysObjectController.cs:20-91)
    ///    -> BIZ WAS_Sys_Object_Get (System.cs:14990-15089)
    ///    -> Sys_Object_Get (System.cs:14765-14900)
    ///    Lọc Sys_Object theo ObjectType = "MENU" (hoặc req.ObjectType nếu truyền) và FlagActive = "1" (hoặc req.FlagActive nếu truyền), sắp xếp theo ObjectCode asc.
    /// 2. Nếu Lst_Sys_Object != null && Lst_Sys_Object.Count > 0:
    ///    Lấy mã menu đầu tiên: objectCode = Lst_Sys_Object[0].ObjectCode.Trim() (gate dòng 63).
    ///    Hoặc nếu req?.ObjectCode được chỉ định thì dùng req.ObjectCode.
    ///    Sys_AccessService.Instance.WA_Sys_Access_Get (gate dòng 96)
    ///    -> API WA_Sys_Access_Get (SysAccessController.cs:20-91)
    ///    -> BIZ WAS_Sys_Access_Get (System.cs:704-801)
    ///    -> Sys_Access_Get (System.cs:445-560 / 221-443)
    ///    Lọc Sys_Access theo ObjectCode = objectCode, inner join Sys_Access left join Sys_Object on sa.ObjectCode = so.ObjectCode.
    ///    Sắp xếp theo GroupCode asc, ObjectCode asc.
    /// 3. Gate gán: ViewBag.ListSysAccess = listSysAccess; return View(listSysObject); (gate dòng 103-104).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Access_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (Lesson A.14, A.21, PORT_RULES §3)
            var objectType = StdParam(req?.ObjectType) ?? "MENU";
            var flagActive = req?.FlagActive != null ? StdFlag(req.FlagActive) : "1";
            var specifiedObjectCode = StdParam(req?.ObjectCode);
            var groupCode = StdParam(req?.GroupCode);

            // 2. Lấy danh sách Sys_Object (gate dòng 33-64, System.cs:14841-14894)
            // SQL nguồn: select distinct so.ObjectCode into #tbl_Sys_Object_Filter_Draft from Sys_Object so where (1=1) and ... order by so.ObjectCode asc
            var queryObj = _db.Set<Sys_Object>().AsNoTracking();

            if (!string.IsNullOrEmpty(objectType))
            {
                queryObj = queryObj.Where(x => EF.Functions.Like((x.ObjectType ?? "").ToLower(), PortQuery.Equal(objectType), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(flagActive))
            {
                queryObj = queryObj.Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc));
            }

            var listSysObject = await queryObj
                .OrderBy(x => x.ObjectCode)
                .ToListAsync();

            var listSysAccess = new List<Sys_Access>();
            var objectCode = "";

            // 3. Nếu danh sách Sys_Object có phần tử -> Lấy danh sách nhóm quyền Sys_Access (gate dòng 61-102)
            // SQL nguồn: select distinct sa.GroupCode, sa.ObjectCode into #tbl_Sys_Access_Filter_Draft from Sys_Access sa where ...
            // inner join Sys_Access sa on ... left join Sys_Object so on sa.ObjectCode = so.ObjectCode (System.cs:332-349)
            if (listSysObject.Count > 0)
            {
                objectCode = !string.IsNullOrEmpty(specifiedObjectCode)
                    ? specifiedObjectCode
                    : (listSysObject[0].ObjectCode ?? "").Trim();

                listSysAccess = await GetSysAccessListAsync(objectCode, groupCode);
            }

            // 4. Trả về kết quả khớp gate MVC: Model là listSysObject, ViewBag là listSysAccess (gate dòng 103-104)
            var data = new Sys_Access_IndexData
            {
                ListSysObject = listSysObject,
                Lst_Sys_Object = listSysObject,
                ListSysAccess = listSysAccess,
                Lst_Sys_Access = listSysAccess,
                ObjectCode = objectCode
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Access_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action GetDetailData từ gate Sys_AccessController.cs:110-167.
    /// Chuỗi gọi:
    /// 1. Khởi tạo sysAccess = new Sys_Access { GroupCode = "", ObjectCode = objectcode } (gate dòng 123-127).
    ///    Tạo điều kiện strWhereClause_SysAccess (gate dòng 128, 484-499).
    /// 2. Sys_AccessService.Instance.WA_Sys_Access_Get (gate dòng 152)
    ///    -> API WA_Sys_Access_Get (SysAccessController.cs:20-91)
    ///    -> BIZ WAS_Sys_Access_Get (System.cs:704-801)
    ///    -> Sys_Access_Get (System.cs:445-560 / 221-443)
    ///    Lọc Sys_Access theo ObjectCode = objectcode (nếu có), inner join Sys_Access left join Sys_Object on sa.ObjectCode = so.ObjectCode.
    ///    Sắp xếp theo GroupCode asc, ObjectCode asc.
    /// 3. Trả về JsonView("ShowDetailData", listSysAccess) (gate dòng 159).
    /// </summary>
    [HttpPost("getdetaildata")]
    public async Task<IActionResult> GetDetailData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Access_GetDetailDataReq? req)
    {
        try
        {
            var objectCode = StdParam(req?.ObjectCode);
            var groupCode = StdParam(req?.GroupCode);

            var listSysAccess = await GetSysAccessListAsync(objectCode, groupCode);

            return Ok(new
            {
                Success = true,
                Data = listSysAccess
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_Access_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action GetFunction từ gate Sys_AccessController.cs:172-263.
    /// Chuỗi gọi:
    /// 1. Sys_ObjectService.Instance.WA_Sys_Object_Get (gate dòng 46)
    ///    -> API WA_Sys_Object_Get (SysObjectController.cs:20-91)
    ///    -> BIZ WAS_Sys_Object_Get (System.cs:14990-15089)
    ///    -> Sys_Object_Get (System.cs:14765-14900)
    ///    Lọc Sys_Object theo ObjectType = "FUNC" (gate dòng 187) và FlagActive = "1", sắp xếp theo ObjectCode asc.
    /// 2. GetSysObjectSetting() (gate dòng 220) -> đọc file ~/App_Data/SysObjectSetting.config
    ///    Tra danh sách mã hàm được gán cho objectCode (gate dòng 225-250).
    /// 3. Trả về listSysObject + danh sách mã hàm (gate dòng 255).
    /// </summary>
    [HttpPost("getfunction")]
    public async Task<IActionResult> GetFunction([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_AccessController_GetFunctionReq? req)
    {
        try
        {
            var objectCode = StdParam(req?.ObjectCode) ?? "";

            // 1. Lấy danh sách Sys_Object với ObjectType = "FUNC" (gate dòng 33-64, System.cs:14841-14894)
            // Bước này từ WA_Sys_Object_Get; phiên bản GET của biz
            var flagActive = "1";
            var listSysObject = await _db.Set<Sys_Object>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.ObjectType ?? "").ToLower(), PortQuery.Equal("FUNC"), PortQuery.Esc))
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc))
                .OrderBy(x => x.ObjectCode)
                .ToListAsync();

            // 2. Đọc cấu hình SysObjectSetting.config -> tra danh sách mã hàm của objectCode (gate dòng 220-250)
            var listStrObjectCode = new List<string>();
            var setting = GetSysObjectSettingFromFile();
            if (setting != null && setting.List != null)
            {
                foreach (var o in setting.List)
                {
                    if (o.ObjectCode.Equals(objectCode, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (o.FunctionCodes != null && o.FunctionCodes.Count > 0)
                        {
                            foreach (var it in o.FunctionCodes)
                            {
                                listStrObjectCode.Add(it.Trim());
                            }
                        }
                        break;
                    }
                }
            }

            // 3. Trả về kết quả (gate dòng 255-256)
            var data = new Sys_AccessController_GetFunctionData
            {
                ListSysObject = listSysObject,
                SelectedList = listStrObjectCode,
                ObjectCode = objectCode
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Sys_AccessController_GetFunction", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action SaveFuncObjectInMenu từ gate Sys_AccessController.cs:267-464.
    /// Chuỗi gọi:
    /// 1. Gate nhận model (JSON) chứa MenuCode (mã menu) và Lst_FuncCode (danh sách mã hàm).
    ///    Nếu Lst_FuncCode null/rỗng -> xóa hàm khỏi menu.
    /// 2. UpdateSysObjectFunctions(MenuCode, Lst_FuncCode) -> lưu vào file ~/App_Data/SysObjectSetting.config
    ///    (gate BaseController.cs:3809-3842).
    /// 3. Lấy danh sách Sys_Access có ObjectCode = MenuCode (gate dòng 340-371, Sys_Access_Get).
    /// 4. Với mỗi GroupCode, lấy danh sách Sys_Access của GroupCode (gate dòng 379-389).
    ///    Lọc rows với so_ObjectType = "MENU" (từ left join Sys_Object).
    /// 5. Resolve thêm function từ file SysObjectSetting.config (gate dòng 321-394, ResolveObjects).
    ///    Kết hợp các hàm từ menu + các hàm hiện tại của nhóm để tính danh sách hàm mới.
    /// 6. Lưu danh sách Sys_Access mới (delete + insert) thông qua Sys_Access_Save (System.cs:803-1006).
    /// 7. Trả về Success/Error.
    /// </summary>
    [HttpPost("save_func_object_in_menu")]
    public async Task<IActionResult> SaveFuncObjectInMenu([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SaveFuncObjectInMenuReq? req)
    {
        var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Chuẩn hoá tham số (PORT_RULES §3, Lesson A.21)
            if (req == null || string.IsNullOrWhiteSpace(req.MenuCode))
            {
                return PortResult.Fail("Sys_Access_SaveFuncObjectInMenu", "Check.MenuCode", null);
            }

            var menuCode = StdParam(req.MenuCode) ?? "";
            var lstFuncCode = req.Lst_FuncCode ?? new List<string>();

            // 2. Lưu cấu hình hàm vào file SysObjectSetting.config (gate dòng 294)
            var funcCodesTrimmed = lstFuncCode.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            UpdateSysObjectSettingInFile(menuCode, funcCodesTrimmed);

            // 3. Lấy danh sách Sys_Access có ObjectCode = menuCode (gate dòng 301-331)
            var listSysAccessByMenu = await _db.Set<Sys_Access>()
                .AsNoTracking()
                .Where(x => EF.Functions.Like((x.ObjectCode ?? "").ToLower(), PortQuery.Equal(menuCode), PortQuery.Esc))
                .ToListAsync();

            if (listSysAccessByMenu.Count > 0)
            {
                // 4. Với mỗi GroupCode tìm được, lấy danh sách quyền và resolve function (gate dòng 336-415)
                var distinctGroups = listSysAccessByMenu.Select(x => x.GroupCode).Distinct().ToList();

                foreach (var groupCode in distinctGroups)
                {
                    // 4a. Lấy danh sách Sys_Access của GroupCode kèm thông tin Sys_Object (gate dòng 345-350)
                    var listSysAccessByGroup = await _db.Set<Sys_Access>()
                        .AsNoTracking()
                        .Join(
                            _db.Set<Sys_Object>().AsNoTracking(),
                            sa => sa.ObjectCode,
                            so => so.ObjectCode,
                            (sa, so) => new { sa, so }
                        )
                        .Where(x => EF.Functions.Like((x.sa.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc))
                        .ToListAsync();

                    // 4b. Lọc lấy các menu (so_ObjectType = "MENU") (gate dòng 351)
                    var menuRows = listSysAccessByGroup
                        .Where(x => x.so != null && (x.so.ObjectType ?? "").Equals("MENU", StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.sa.ObjectCode)
                        .Distinct()
                        .ToList();

                    if (menuRows.Count > 0)
                    {
                        // 4c. Resolve function từ file (gate dòng 321-394)
                        var resolvedFunctions = ResolveObjectsForMenu(menuRows);

                        // 4d. Lấy danh sách button hiện tại của nhóm (gate dòng 384-392)
                        var buttonRows = listSysAccessByGroup
                            .Where(x => x.so != null && (x.so.ObjectType ?? "").Equals("BUTTON", StringComparison.OrdinalIgnoreCase))
                            .Select(x => x.sa.ObjectCode)
                            .ToList();

                        // Kết hợp function + button, loại trùng (gate dòng 390-393)
                        var combinedFunctions = new List<string>(resolvedFunctions);
                        if (buttonRows.Count > 0)
                        {
                            combinedFunctions.AddRange(buttonRows);
                        }
                        combinedFunctions = combinedFunctions.Distinct().ToList();

                        // 4e. Chuẩn bị danh sách Sys_Access mới (gate dòng 395-407)
                        var newAccessList = new List<Sys_Access>();
                        foreach (var funcCode in combinedFunctions)
                        {
                            if (!string.IsNullOrWhiteSpace(funcCode))
                            {
                                newAccessList.Add(new Sys_Access
                                {
                                    GroupCode = groupCode.Trim(),
                                    ObjectCode = funcCode.Trim(),
                                    LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                    LogLUBy = "port"
                                });
                            }
                        }

                        // 4f. Lưu vào DB (xóa quyền cũ của GroupCode, insert lại danh sách mới)
                        // Tương ứng Sys_Access_Save (System.cs:932-963)
                        var oldAccess = _db.Set<Sys_Access>()
                            .Where(x => x.GroupCode == groupCode)
                            .ToList();

                        if (oldAccess.Count > 0)
                        {
                            _db.Set<Sys_Access>().RemoveRange(oldAccess);
                        }

                        // Thêm những quyền mới (từ ResolveObjects + button)
                        if (newAccessList.Count > 0)
                        {
                            await _db.Set<Sys_Access>().AddRangeAsync(newAccessList);
                        }

                        await _db.SaveChangesAsync();
                    }
                }
            }

            await tx.CommitAsync();

            var title = lstFuncCode == null || lstFuncCode.Count == 0
                ? "Xóa FUNC khỏi Menu thành công!"
                : "Gán FUNC vào Menu thành công!";

            return Ok(new
            {
                Success = true,
                Title = title
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Sys_Access_SaveFuncObjectInMenu", "Exception", ex.Message);
        }
        finally
        {
            await tx.DisposeAsync();
        }
    }

    /// <summary>
    /// Hàm dùng chung truy vấn phân quyền Sys_Access kết hợp thông tin Sys_Object
    /// theo System.cs:445-560 / 221-443 (Sys_Access_Get).
    /// </summary>
    private async Task<List<Sys_Access>> GetSysAccessListAsync(string? objectCode, string? groupCode = null)
    {
        var queryAccess = from sa in _db.Set<Sys_Access>().AsNoTracking()
                          join so in _db.Set<Sys_Object>().AsNoTracking()
                              on sa.ObjectCode equals so.ObjectCode into g
                          from so in g.DefaultIfEmpty()
                          select new { sa, so };

        if (!string.IsNullOrEmpty(objectCode))
        {
            queryAccess = queryAccess.Where(x => EF.Functions.Like((x.sa.ObjectCode ?? "").ToLower(), PortQuery.Equal(objectCode), PortQuery.Esc));
        }

        if (!string.IsNullOrEmpty(groupCode))
        {
            queryAccess = queryAccess.Where(x => EF.Functions.Like((x.sa.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCode), PortQuery.Esc));
        }

        var accessRows = await queryAccess
            .OrderBy(x => x.sa.GroupCode)
            .ThenBy(x => x.sa.ObjectCode)
            .ToListAsync();

        return accessRows.Select(x => new Sys_Access
        {
            GroupCode = x.sa.GroupCode,
            ObjectCode = x.sa.ObjectCode,
            LogLUDTimeUTC = x.sa.LogLUDTimeUTC,
            LogLUBy = x.sa.LogLUBy,
            so_ObjectCode = x.so != null ? x.so.ObjectCode : null,
            so_ObjectName = x.so != null ? x.so.ObjectName : null,
            so_ServiceCode = x.so != null ? x.so.ServiceCode : null,
            so_ObjectType = x.so != null ? x.so.ObjectType : null,
            so_FlagExecModal = x.so != null ? x.so.FlagExecModal : null,
            so_FlagActive = x.so != null ? x.so.FlagActive : null,
        }).ToList();
    }

    /// <summary>
    /// Đọc cấu hình từ file SysObjectSetting.config (gate BaseController.cs:3780-3795).
    /// Nếu file không tồn tại, trả về null (giống nguồn).
    /// </summary>
    private SysObjectSetting? GetSysObjectSettingFromFile()
    {
        try
        {
            var filePath = System.IO.Path.Combine(
                _env?.ContentRootPath ?? AppContext.BaseDirectory,
                "App_Data",
                "SysObjectSetting.config"
            );

            if (!System.IO.File.Exists(filePath))
                return null;

            // Đọc file JSON (hoặc XML tuỳ theo định dạng nguồn)
            // Cho bây giờ, giả sử file JSON theo mẫu tham chiếu
            var json = System.IO.File.ReadAllText(filePath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var setting = new SysObjectSetting { List = new List<SysObjectSettingItem>() };
            if (root.TryGetProperty("List", out var listProp) && listProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in listProp.EnumerateArray())
                {
                    var settingItem = new SysObjectSettingItem
                    {
                        ObjectCode = item.TryGetProperty("ObjectCode", out var oc) ? oc.GetString() ?? "" : "",
                        FunctionCodes = new List<string>()
                    };

                    if (item.TryGetProperty("FunctionCodes", out var fcProp))
                    {
                        if (fcProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var fc in fcProp.EnumerateArray())
                            {
                                if (fc.ValueKind == System.Text.Json.JsonValueKind.String)
                                    settingItem.FunctionCodes.Add(fc.GetString() ?? "");
                            }
                        }
                    }

                    setting.List.Add(settingItem);
                }
            }

            return setting;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Lưu danh sách hàm cho 1 đối tượng vào file SysObjectSetting.config (gate BaseController.cs:3809-3842).
    /// Nếu đối tượng đã có → cập nhật danh sách hàm; nếu chưa → thêm mới.
    /// </summary>
    private void UpdateSysObjectSettingInFile(string objectCode, List<string> functionCodes)
    {
        try
        {
            var filePath = System.IO.Path.Combine(
                _env?.ContentRootPath ?? AppContext.BaseDirectory,
                "App_Data",
                "SysObjectSetting.config"
            );

            var setting = new SysObjectSetting { List = new List<SysObjectSettingItem>() };

            // Đọc cấu hình hiện tại (nếu file tồn tại)
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("List", out var listProp) && listProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in listProp.EnumerateArray())
                        {
                            var settingItem = new SysObjectSettingItem
                            {
                                ObjectCode = item.TryGetProperty("ObjectCode", out var oc) ? oc.GetString() ?? "" : "",
                                FunctionCodes = new List<string>()
                            };

                            if (item.TryGetProperty("FunctionCodes", out var fcProp))
                            {
                                if (fcProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var fc in fcProp.EnumerateArray())
                                    {
                                        if (fc.ValueKind == System.Text.Json.JsonValueKind.String)
                                            settingItem.FunctionCodes.Add(fc.GetString() ?? "");
                                    }
                                }
                            }

                            setting.List.Add(settingItem);
                        }
                    }
                }
                catch
                {
                    // Nếu parse lỗi, dùng cấu hình trắng
                }
            }

            // Tìm và cập nhật hoặc thêm mới objectCode
            var idx = setting.List.FindIndex(x =>
                x.ObjectCode.Equals(objectCode, StringComparison.InvariantCultureIgnoreCase));

            if (idx >= 0)
            {
                setting.List[idx].FunctionCodes = functionCodes;
            }
            else
            {
                setting.List.Add(new SysObjectSettingItem
                {
                    ObjectCode = objectCode.ToUpper().Trim(),
                    FunctionCodes = functionCodes
                });
            }

            // Tạo thư mục nếu chưa có
            var dirPath = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dirPath) && !System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }

            // Lưu vào file (JSON format)
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonOutput = System.Text.Json.JsonSerializer.Serialize(setting, options);
            System.IO.File.WriteAllText(filePath, jsonOutput);
        }
        catch (Exception ex)
        {
            // Log hoặc báo lỗi, nhưng không dừng quá trình (giống nguồn)
            System.Diagnostics.Debug.WriteLine($"UpdateSysObjectSettingInFile error: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolve danh sách hàm từ file cấu hình cho một danh sách menu.
    /// Từ gate BaseController.cs:3843-3894 (ResolveObjects).
    /// Tương tự: với mỗi menu trong listObjectCodes, tra file để lấy danh sách hàm liên kết,
    /// cộng thêm các button hiện tại của nhóm, loại bỏ trùng lặp.
    /// </summary>
    private List<string> ResolveObjectsForMenu(List<string> objectCodes)
    {
        var result = new List<string>();
        var dic = new Dictionary<string, bool>();

        // Thêm chính các menu trước (gate dòng 277-280)
        foreach (var code in objectCodes)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var trimmed = code.Trim();
                if (!dic.ContainsKey(trimmed))
                {
                    dic.Add(trimmed, true);
                    result.Add(trimmed);
                }
            }
        }

        // Tra file cấu hình để tìm thêm các hàm/button liên kết với menu (gate dòng 283-307)
        var setting = GetSysObjectSettingFromFile();
        if (setting != null && setting.List != null)
        {
            foreach (var code in objectCodes)
            {
                if (string.IsNullOrWhiteSpace(code)) continue;

                foreach (var scode in setting.List)
                {
                    if (scode.ObjectCode.Equals(code, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Tìm được menu này trong cấu hình -> lấy danh sách hàm
                        if (scode.FunctionCodes != null)
                        {
                            foreach (var f in scode.FunctionCodes)
                            {
                                if (!string.IsNullOrWhiteSpace(f))
                                {
                                    var trimmedF = f.Trim();
                                    if (!dic.ContainsKey(trimmedF))
                                    {
                                        dic.Add(trimmedF, true);

                                        // Split nếu có dấu ',' (gate dòng 272-281)
                                        var arrCode = trimmedF.Split(',');
                                        foreach (var strCode in arrCode)
                                        {
                                            if (!string.IsNullOrWhiteSpace(strCode))
                                            {
                                                result.Add(strCode.Trim());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }
        else
        {
            // Nếu file không có hoặc lỗi, trả lại danh sách đầu vào (gate dòng 313)
            result = objectCodes.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();
        }

        return result;
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action SaveFuncObjectInMenu (port gate dòng 277, 267).
/// </summary>
public class SaveFuncObjectInMenuReq
{
    public string? MenuCode { get; set; }
    public List<string>? Lst_FuncCode { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action Index của Sys_AccessController.
/// </summary>
public class Sys_Access_IndexReq
{
    public string? FlagActive { get; set; }
    public string? ObjectType { get; set; }
    public string? ObjectCode { get; set; }
    public string? GroupCode { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action GetDetailData của Sys_AccessController.
/// Nhận objectcode từ client (Sys_AccessController.cs:110).
/// </summary>
public class Sys_Access_GetDetailDataReq
{
    public string? ObjectCode { get; set; }
    public string? GroupCode { get; set; }
}

/// <summary>
/// DTO trả về cho action Index của Sys_AccessController.
/// Gồm danh sách menu Sys_Object (Model View) và danh sách phân quyền Sys_Access (ViewBag.ListSysAccess).
/// </summary>
public class Sys_Access_IndexData
{
    public List<Sys_Object> ListSysObject { get; set; } = new();
    public List<Sys_Object> Lst_Sys_Object { get; set; } = new();
    public List<Sys_Access> ListSysAccess { get; set; } = new();
    public List<Sys_Access> Lst_Sys_Access { get; set; } = new();
    public string ObjectCode { get; set; } = "";
}

/// <summary>
/// DTO yêu cầu cho action GetFunction của Sys_AccessController.
/// Nhận ObjectCode từ client (Sys_AccessController.cs:172).
/// </summary>
public class Sys_AccessController_GetFunctionReq
{
    public string? ObjectCode { get; set; }
}

/// <summary>
/// DTO trả về cho action GetFunction của Sys_AccessController.
/// Gồm danh sách hàm (FUNC) và danh sách mã hàm được gán cho ObjectCode.
/// </summary>
public class Sys_AccessController_GetFunctionData
{
    public List<Sys_Object> ListSysObject { get; set; } = new();
    public List<string> SelectedList { get; set; } = new();
    public string ObjectCode { get; set; } = "";
}

/// <summary>
/// Cấu trúc đọc từ file SysObjectSetting.config (port BaseController.cs:3780-3795).
/// </summary>
public class SysObjectSetting
{
    public List<SysObjectSettingItem> List { get; set; } = new();
}

/// <summary>
/// Mục trong cấu hình SysObjectSetting.
/// </summary>
public class SysObjectSettingItem
{
    public string ObjectCode { get; set; } = "";
    public List<string> FunctionCodes { get; set; } = new();
}
