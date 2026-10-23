using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Sys_GroupController.cs — Sys_Group (action SaveObjectInGroup, hạng mục #274)
// Nguồn:
// Gate:      D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Sys_GroupController.cs:731-826 (SaveObjectInGroup)
// Helper gate: BaseController.cs:3780-3795 (GetSysObjectSetting), 3843-3894 (ResolveObjects)
// API:       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\SysAccessController.cs:94-165 (WA_Sys_Access_Save)
// BIZ:       System.cs:1251-1337 (WAS_Sys_Access_Save) -> 1008-1249 (Sys_Access_Save)
//            System.cs:1341-1400 (Sys_Group_CheckDB), System.cs:14497-14560 (Sys_Object_CheckDB)
[ApiController]
[Route("api/sys_group")]
public class Sys_Group_SaveObjectInGroupPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;
    private readonly IWebHostEnvironment? _env;

    public Sys_Group_SaveObjectInGroupPort(AppDbContext db, IConfiguration? config = null, IWebHostEnvironment? env = null)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    /// <summary>
    /// Port 1:1 action SaveObjectInGroup từ gate Sys_GroupController.cs:731-826.
    /// Xem chi tiết chuỗi gọi + GAP ở phần comment đầu file và trong thân hàm.
    /// </summary>
    [HttpPost("saveobjectingroup")]
    public async Task<IActionResult> SaveObjectInGroup([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Sys_Group_SaveObjectInGroupReq? req)
    {
        // Gate: Sys_GroupController.cs:742, 814-816
        if (req == null)
        {
            return Ok(new { Success = true, Title = "Chưa chọn Menu - Button gán vào nhóm người dùng!" });
        }

        try
        {
            // Gate: dòng 762-767
            var title = "Gán module vào nhóm người dùng thành công!";
            var lstAccess = req.Lst_Sys_Access;
            if (lstAccess == null || lstAccess.Count == 0)
            {
                lstAccess = new List<Sys_Access>();
                title = "Xóa module khỏi nhóm thành công!";
            }
            else
            {
                // Gate: dòng 770-808
                var listObjectCode = new List<string>();
                var listFunctions = new List<string>();
                foreach (var item in lstAccess)
                {
                    if ("MENU".Equals(item.so_ObjectType)) listObjectCode.Add(item.ObjectCode);
                    else if ("BUTTON".Equals(item.so_ObjectType)) listFunctions.Add(item.ObjectCode);
                }

                var functions = ResolveObjects(listObjectCode);
                if (functions != null && functions.Count > 0)
                {
                    if (listFunctions.Count > 0) functions.AddRange(listFunctions);
                    functions = functions.Distinct().ToList();

                    // Gate dòng 801: GroupCode = Sys_Group.GroupCode.Trim()
                    var groupCodeFromReq = (req.Sys_Group?.GroupCode ?? "").Trim();

                    var rebuilt = new List<Sys_Access>(functions.Count);
                    foreach (var it in functions)
                    {
                        rebuilt.Add(new Sys_Access
                        {
                            ObjectCode = it.Trim(),
                            GroupCode = groupCodeFromReq
                        });
                    }
                    lstAccess = rebuilt;
                }
                // else: giữ nguyên lstAccess của client (LỖI NGUỒN GIỮ NGUYÊN — gate Sys_GroupController.cs:787-808)
            }

            var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();

            // ---- Sys_Access_Save (System.cs:1008-1249) — transaction
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Sys_Group_CheckDB(objGroupCode, "1", "") (System.cs:1098-1104, 1341-1400)
                var groupCodeInput = req.Sys_Group?.GroupCode;
                var groupCodeStd = StdParam(groupCodeInput);
                if (string.IsNullOrEmpty(groupCodeStd))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Sys_Group_CheckDB_GroupCodeNotFound", "Check.GroupCodeNotFound", groupCodeInput);
                }
                var group = await _db.Set<Sys_Group>().AsNoTracking()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(groupCodeStd), PortQuery.Esc));
                if (group == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Sys_Group_CheckDB_GroupCodeNotFound", "Check.GroupCodeNotFound", groupCodeStd);
                }

                // 2. Loop từng ObjectCode: Sys_Object_CheckDB(ObjectCode, "1", "1") (System.cs:1146-1161, 14497-14560)
                foreach (var row in lstAccess)
                {
                    var objCodeStd = StdParam(row.ObjectCode);
                    if (string.IsNullOrEmpty(objCodeStd))
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Sys_Object_CheckDB_ObjectCodeNotFound", "Check.ObjectCodeNotFound", row.ObjectCode);
                    }
                    var so = await _db.Set<Sys_Object>().AsNoTracking()
                        .FirstOrDefaultAsync(x => EF.Functions.Like((x.ObjectCode ?? "").ToLower(), PortQuery.Equal(objCodeStd), PortQuery.Esc));
                    if (so == null)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Sys_Object_CheckDB_ObjectCodeNotFound", "Check.ObjectCodeNotFound", objCodeStd);
                    }
                    // System.cs:14550 — "1".Contains(FlagActive): ""->true, "1"->true, "0"->false
                    if (!"1".Contains(so.FlagActive ?? ""))
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Sys_Object_CheckDB_FlagActiveNotMatched",
                            "Check.ObjectCode", objCodeStd,
                            "Check.FlagActiveListToCheck", "1",
                            "DB.FlagActive", so.FlagActive);
                    }
                }

                // 3. DELETE where GroupCode = group.GroupCode (System.cs:1178-1184)
                var existing = await _db.Set<Sys_Access>()
                    .Where(x => EF.Functions.Like((x.GroupCode ?? "").ToLower(), PortQuery.Equal(group.GroupCode), PortQuery.Esc))
                    .ToListAsync();
                if (existing.Count > 0) _db.Set<Sys_Access>().RemoveRange(existing);

                // 4. INSERT (GroupCode, ObjectCode, LogLUDTimeUTC, LogLUBy) (System.cs:1186-1203)
                //    GAP: NetworkID không có cột ở port — không ghi.
                var logLudUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var logLuBy = string.IsNullOrEmpty(userCode) ? "port" : userCode;
                foreach (var row in lstAccess)
                {
                    var objCodeStd = StdParam(row.ObjectCode) ?? "";
                    _db.Set<Sys_Access>().Add(new Sys_Access
                    {
                        GroupCode = group.GroupCode,
                        ObjectCode = objCodeStd,
                        LogLUDTimeUTC = logLudUtc,
                        LogLUBy = logLuBy
                    });
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            return Ok(new { Success = true, Title = title });
        }
        catch (Exception ex)
        {
            // Mã lỗi mặc định của biz nguồn (System.cs:1268 TError.ErridQContract.WAS_Sys_Access_Save)
            return PortResult.Fail("WAS_Sys_Access_Save", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 helper ResolveObjects từ BaseController.cs:3843-3894.
    /// </summary>
    private List<string> ResolveObjects(List<string> objectCodes)
    {
        var list = new List<string>();
        var setting = GetSysObjectSetting();
        var dic = new Dictionary<string, bool>();
        if (setting != null && setting.List != null)
        {
            foreach (var code in objectCodes)
            {
                if (!dic.ContainsKey(code))
                {
                    dic.Add(code, true);
                    list.Add(code);
                }
                foreach (var scode in setting.List)
                {
                    if (scode.ObjectCode != null
                        && string.Equals(scode.ObjectCode, code, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (scode.FunctionCodes != null)
                        {
                            foreach (var f in scode.FunctionCodes)
                            {
                                if (!string.IsNullOrEmpty(f))
                                {
                                    if (!dic.ContainsKey(f))
                                    {
                                        dic.Add(f, true);
                                        var arrCode = f.Split(',');
                                        for (int i = 0; i < arrCode.Length; i++)
                                        {
                                            list.Add(arrCode[i]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // BaseController.cs:3891 — không có setting thì trả nguyên danh sách
            list = objectCodes;
        }
        return list;
    }

    /// <summary>
    /// Port 1:1 GetSysObjectSetting từ BaseController.cs:3780-3795:
    /// đọc App_Data/SysObjectSetting.config từ ContentRootPath; thiếu file -> null.
    /// </summary>
    private SysObjectSetting? GetSysObjectSetting()
    {
        if (_env == null) return null;
        var filePath = System.IO.Path.Combine(_env.ContentRootPath, "App_Data", "SysObjectSetting.config");
        if (!System.IO.File.Exists(filePath)) return null;
        try
        {
            using var fs = System.IO.File.OpenRead(filePath);
            var ser = new System.Runtime.Serialization.DataContractSerializer(typeof(SysObjectSetting));
            return (SysObjectSetting?)ser.ReadObject(fs);
        }
        catch
        {
            return null;
        }
    }

    // Chuẩn hoá input — copy nguyên văn từ Sys_GroupControllerPort.cs (StdParam).
    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
}

/// <summary>
/// Body action SaveObjectInGroup (gate Sys_GroupController.cs:742 — Newtonsoft deserialize RQ_Sys_Access).
/// Gate nhận string model, port nhận nguyên object (A.21).
/// </summary>
public class Sys_Group_SaveObjectInGroupReq
{
    public Sys_Group? Sys_Group { get; set; }
    public List<Sys_Access>? Lst_Sys_Access { get; set; }
}
