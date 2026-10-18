using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_OrgCKSController.cs — Mst_OrgSignConfig
// Nguồn:
// Gate Create: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_OrgCKSController.cs:22-65
// Gate Edit:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_OrgCKSController.cs:148-257
// Gate Delete: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_OrgCKSController.cs:259-318
// Service: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.ClientService\Services\Mst_OrgSignConfigService.cs:27-43
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstOrgSignConfigController.cs:90-278
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:49063-49127, 49499-49900, 50180-50350, 50352-50596
[ApiController]
[Route("api/mst_orgcks")]
public class Mst_OrgCKSControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_OrgCKSControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Tạo mới cấu hình chữ ký số cho tổ chức.
    /// Gate: Mst_OrgCKSController.cs:22-65 Create(string model)
    /// BIZ: Master.cs:49499-49900 WAS_Mst_OrgSignConfig_Create -> Mst_OrgSignConfig_Create -> Mst_OrgSignConfig_CreateX
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_OrgCKS_CreateReq? req)
    {
        // 1. Kiểm tra body rỗng (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_OrgSignConfig_Create", "Check.strJson", null);
        }

        // 2. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_OrgCKSController.cs:22, 31)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_OrgCKS_CreateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.SignType ??= modelObj.SignType;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.OrgID ??= modelObj.OrgID;
                    req.CANumber ??= modelObj.CANumber;
                    req.CAOrg ??= modelObj.CAOrg;
                    req.CAEffDTimeUTCStart ??= modelObj.CAEffDTimeUTCStart;
                    req.CAEffDTimeUTCEnd ??= modelObj.CAEffDTimeUTCEnd;
                    req.CTSPath ??= modelObj.CTSPath;
                    req.CTSPwd ??= modelObj.CTSPwd;
                    req.ServerSignFilePath ??= modelObj.ServerSignFilePath;
                    req.ServerSignPassword ??= modelObj.ServerSignPassword;
                    req.SupplierCode ??= modelObj.SupplierCode;
                    req.RemoteSignAgreementUUID ??= modelObj.RemoteSignAgreementUUID;
                    req.RemoteSignPassCode ??= modelObj.RemoteSignPassCode;
                    req.AuthenCode ??= modelObj.AuthenCode;
                    req.FlagActive ??= modelObj.FlagActive;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (string.IsNullOrWhiteSpace(req.SignType) && string.IsNullOrWhiteSpace(req.OrgID) && string.IsNullOrWhiteSpace(req.CANumber) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_OrgSignConfig_Create", "Check.strJson", null);
        }

        // 3. Chuẩn hoá tham số đầu vào theo Master.cs:49803-49816
        var dtimeSys = DateTime.UtcNow;
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

        var entity = new Mst_OrgSignConfig
        {
            SignType = StdParam(req.SignType),
            NetworkID = StdParam(req.NetworkID) ?? _config["Biz_NetworkID"] ?? "1001",
            OrgID = StdParam(req.OrgID),
            CANumber = StdParam(req.CANumber),
            CAOrg = StdParam(req.CAOrg),
            CAEffDTimeUTCStart = StdParam(req.CAEffDTimeUTCStart),
            CAEffDTimeUTCEnd = StdParam(req.CAEffDTimeUTCEnd),
            CTSPath = null,
            CTSPwd = null,
            ServerSignFilePath = StdStr(req.ServerSignFilePath),
            ServerSignPassword = StdStr(req.ServerSignPassword),
            SupplierCode = StdParam(req.SupplierCode),
            RemoteSignAgreementUUID = StdStr(req.RemoteSignAgreementUUID),
            RemoteSignPassCode = StdStr(req.RemoteSignPassCode),
            AuthenCode = StdParam(req.AuthenCode),
            UpdateDTimeUTC = null,
            UpdateBy = null,
            CreateDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
            CreateBy = userCode,
            FlagActive = StdParam(req.FlagActive),
            LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = userCode
        };

        // 4. Lưu dữ liệu và kiểm tra ràng buộc duy nhất trong transaction (Master.cs:49648-49720, 49857-49898)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.Set<Mst_OrgSignConfig>().Add(entity);
            await _db.SaveChangesAsync();

            // Kiểm tra không quá 1 bản ghi active cho REMOTE hoặc SERVER (Master.cs:49866-49898)
            var activeConflict = await _db.Set<Mst_OrgSignConfig>()
                .Where(x => x.FlagActive == "1" && (x.SignType == "REMOTE" || x.SignType == "SERVER"))
                .GroupBy(x => new { x.SignType, x.OrgID })
                .Select(g => new { g.Key.SignType, g.Key.OrgID, QtyRowActive = g.Count() })
                .Where(g => g.QtyRowActive > 1)
                .FirstOrDefaultAsync();

            if (activeConflict != null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_OrgSignConfig_CheckDB_MoreThanOneActive",
                    "Check.SignType", activeConflict.SignType,
                    "Check.OrgID", activeConflict.OrgID);
            }

            await tx.CommitAsync();
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }

        // 5. Trả kết quả thành công khớp gate (Mst_OrgCKSController.cs:54-57)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Tạo mới chữ ký số thành công!" },
            Title = "Tạo mới chữ ký số thành công!",
            RedirectUrl = "/Mst_OrgCKS",
            Data = entity
        });
    }

    /// <summary>
    /// Cập nhật cấu hình chữ ký số cho tổ chức.
    /// Gate: Mst_OrgCKSController.cs:148-257 Edit(string model)
    /// BIZ: Master.cs:49063-49127 Mst_OrgSignConfig_CheckDB
    ///      Master.cs:50180-50350 WAS_Mst_OrgSignConfig_Update -> Mst_OrgSignConfig_Update -> Mst_OrgSignConfig_UpdateX
    /// </summary>
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_OrgCKS_EditReq? req)
    {
        // 1. Kiểm tra body rỗng (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_OrgSignConfig_Update", "Check.strJson", null);
        }

        // 2. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_OrgCKSController.cs:148, 155-157)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_OrgCKS_EditReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.AutoID ??= modelObj.AutoID;
                    req.SignType ??= modelObj.SignType;
                    req.NetworkID ??= modelObj.NetworkID;
                    req.OrgID ??= modelObj.OrgID;
                    req.CANumber ??= modelObj.CANumber;
                    req.CAOrg ??= modelObj.CAOrg;
                    req.CAEffDTimeUTCStart ??= modelObj.CAEffDTimeUTCStart;
                    req.CAEffDTimeUTCEnd ??= modelObj.CAEffDTimeUTCEnd;
                    req.CTSPath ??= modelObj.CTSPath;
                    req.CTSPwd ??= modelObj.CTSPwd;
                    req.ServerSignFilePath ??= modelObj.ServerSignFilePath;
                    req.ServerSignPassword ??= modelObj.ServerSignPassword;
                    req.SupplierCode ??= modelObj.SupplierCode;
                    req.RemoteSignAgreementUUID ??= modelObj.RemoteSignAgreementUUID;
                    req.RemoteSignPassCode ??= modelObj.RemoteSignPassCode;
                    req.AuthenCode ??= modelObj.AuthenCode;
                    req.FlagActive ??= modelObj.FlagActive;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (req.AutoID == null && string.IsNullOrWhiteSpace(req.SignType) && string.IsNullOrWhiteSpace(req.OrgID) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_OrgSignConfig_Update", "Check.strJson", null);
        }

        if (req.AutoID == null || req.AutoID <= 0)
        {
            return PortResult.Fail("Mst_OrgSignConfig_CheckDB_OrganNotFound", "Check.AutoID", req.AutoID);
        }

        // 3. Tra cứu bản ghi hiện có theo AutoID và OrgID (Gate: Mst_OrgCKSController.cs:160-164, 184; BIZ: Master.cs:49071-49085, 50275)
        var query = _db.Set<Mst_OrgSignConfig>().Where(x => x.AutoID == req.AutoID.Value);
        var orgId = StdParam(req.OrgID);
        if (!string.IsNullOrEmpty(orgId))
        {
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
        }

        var existing = await query.FirstOrDefaultAsync();
        if (existing == null)
        {
            return PortResult.Fail("Mst_OrgSignConfig_CheckDB_OrganNotFound", "Check.AutoID", req.AutoID);
        }

        // 4. Chuẩn hoá tham số đầu vào và cập nhật các cột theo từng SignType (Gate: Mst_OrgCKSController.cs:187-230, BIZ: Master.cs:50242-50305)
        var dtimeSys = DateTime.UtcNow;
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

        var signType = StdParam(req.SignType) ?? existing.SignType ?? "";
        existing.SignType = signType;

        if (string.Equals(signType, "USBTOKEN", StringComparison.OrdinalIgnoreCase))
        {
            if (req.CAOrg != null) existing.CAOrg = StdParam(req.CAOrg);
            if (req.CAEffDTimeUTCStart != null) existing.CAEffDTimeUTCStart = StdParam(req.CAEffDTimeUTCStart);
            if (req.CAEffDTimeUTCEnd != null) existing.CAEffDTimeUTCEnd = StdParam(req.CAEffDTimeUTCEnd);
            if (req.FlagActive != null) existing.FlagActive = StdParam(req.FlagActive);
        }
        else if (string.Equals(signType, "SERVER", StringComparison.OrdinalIgnoreCase))
        {
            if (req.ServerSignFilePath != null) existing.ServerSignFilePath = StdStr(req.ServerSignFilePath);
            if (req.ServerSignPassword != null) existing.ServerSignPassword = StdStr(req.ServerSignPassword);
            if (req.FlagActive != null) existing.FlagActive = StdParam(req.FlagActive);
        }
        else // REMOTE hoặc mặc định
        {
            if (req.SupplierCode != null) existing.SupplierCode = StdParam(req.SupplierCode);
            if (req.RemoteSignAgreementUUID != null) existing.RemoteSignAgreementUUID = StdStr(req.RemoteSignAgreementUUID);
            if (req.RemoteSignPassCode != null) existing.RemoteSignPassCode = StdStr(req.RemoteSignPassCode);
            if (req.AuthenCode != null) existing.AuthenCode = StdParam(req.AuthenCode);
            if (req.FlagActive != null) existing.FlagActive = StdParam(req.FlagActive);
        }

        // Cột audit (Master.cs:50302-50305)
        existing.UpdateDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
        existing.UpdateBy = userCode;
        existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
        existing.LogLUBy = userCode;

        // 5. Lưu dữ liệu và kiểm tra ràng buộc duy nhất trong transaction (Master.cs:50076, 50316-50350)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            await _db.SaveChangesAsync();

            // Kiểm tra không quá 1 bản ghi active cho REMOTE hoặc SERVER (Master.cs:50317-50348)
            var activeConflict = await _db.Set<Mst_OrgSignConfig>()
                .Where(x => x.FlagActive == "1" && (x.SignType == "REMOTE" || x.SignType == "SERVER"))
                .GroupBy(x => new { x.SignType, x.OrgID })
                .Select(g => new { g.Key.SignType, g.Key.OrgID, QtyRowActive = g.Count() })
                .Where(g => g.QtyRowActive > 1)
                .FirstOrDefaultAsync();

            if (activeConflict != null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_OrgSignConfig_CheckDB_MoreThanOneActive",
                    "Check.SignType", activeConflict.SignType,
                    "Check.OrgID", activeConflict.OrgID);
            }

            await tx.CommitAsync();
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }

        // 6. Trả kết quả thành công khớp gate (Mst_OrgCKSController.cs:241-243)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Sửa chữ ký số thành công!" },
            Title = "Sửa chữ ký số thành công!",
            RedirectUrl = "/Mst_OrgCKS",
            Data = existing
        });
    }

    /// <summary>
    /// Xóa cấu hình chữ ký số cho tổ chức.
    /// Gate: Mst_OrgCKSController.cs:259-318 Delete(string autoID)
    /// API:  MstOrgSignConfigController.cs:229-278 WA_Mst_OrgSignConfig_Delete
    /// BIZ:  Master.cs:50352-50596 WAS_Mst_OrgSignConfig_Delete -> Mst_OrgSignConfig_Delete -> Mst_OrgSignConfig_DeleteX
    ///       Master.cs:49063-49127 Mst_OrgSignConfig_CheckDB
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_OrgCKS_DeleteReq? req)
    {
        // 1. Kiểm tra body rỗng (lesson A.15, A.21)
        if (req == null)
        {
            return PortResult.Fail("Mst_OrgSignConfig_Delete", "Check.strJson", null);
        }

        // 2. Hỗ trợ trường hợp client truyền chuỗi JSON 'model' như gate MVC (Mst_OrgCKSController.cs:261)
        if (!string.IsNullOrWhiteSpace(req.Model))
        {
            try
            {
                var modelObj = JsonSerializer.Deserialize<Mst_OrgCKS_DeleteReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (modelObj != null)
                {
                    req.AutoID ??= modelObj.AutoID;
                    req.OrgID ??= modelObj.OrgID;
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON thô
            }
        }

        if (req.AutoID == null && string.IsNullOrWhiteSpace(req.OrgID) && string.IsNullOrWhiteSpace(req.Model))
        {
            return PortResult.Fail("Mst_OrgSignConfig_Delete", "Check.strJson", null);
        }

        if (req.AutoID == null || req.AutoID <= 0)
        {
            return PortResult.Fail("Mst_OrgSignConfig_CheckDB_OrganNotFound", "Check.AutoID", req.AutoID);
        }

        // 3. Tra cứu bản ghi hiện có theo AutoID và OrgID (Gate: Mst_OrgCKSController.cs:270-295; BIZ: Master.cs:49071-49085, 50572)
        var query = _db.Set<Mst_OrgSignConfig>().Where(x => x.AutoID == req.AutoID.Value);
        var orgId = StdParam(req.OrgID);
        if (!string.IsNullOrEmpty(orgId))
        {
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
        }

        var existing = await query.FirstOrDefaultAsync();
        if (existing == null)
        {
            return PortResult.Fail("Mst_OrgSignConfig_CheckDB_OrganNotFound", "Check.AutoID", req.AutoID);
        }

        // 4. Xóa dữ liệu trong transaction (Master.cs:50455-50525, 50583-50594)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.Set<Mst_OrgSignConfig>().Remove(existing);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }

        // 5. Trả kết quả thành công khớp gate (Mst_OrgCKSController.cs:301-303)
        return Ok(new
        {
            Success = true,
            Messages = new[] { "Xóa chữ ký số thành công!" },
            Title = "Xóa chữ ký số thành công!",
            RedirectUrl = "/Mst_OrgCKS",
            Data = existing
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

public class Mst_OrgCKS_CreateReq
{
    public string? Model { get; set; }
    public string? SignType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? CANumber { get; set; }
    public string? CAOrg { get; set; }
    public string? CAEffDTimeUTCStart { get; set; }
    public string? CAEffDTimeUTCEnd { get; set; }
    public string? CTSPath { get; set; }
    public string? CTSPwd { get; set; }
    public string? ServerSignFilePath { get; set; }
    public string? ServerSignPassword { get; set; }
    public string? SupplierCode { get; set; }
    public string? RemoteSignAgreementUUID { get; set; }
    public string? RemoteSignPassCode { get; set; }
    public string? AuthenCode { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_OrgCKS_EditReq
{
    public string? Model { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? AutoID { get; set; }

    public string? SignType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? CANumber { get; set; }
    public string? CAOrg { get; set; }
    public string? CAEffDTimeUTCStart { get; set; }
    public string? CAEffDTimeUTCEnd { get; set; }
    public string? CTSPath { get; set; }
    public string? CTSPwd { get; set; }
    public string? ServerSignFilePath { get; set; }
    public string? ServerSignPassword { get; set; }
    public string? SupplierCode { get; set; }
    public string? RemoteSignAgreementUUID { get; set; }
    public string? RemoteSignPassCode { get; set; }
    public string? AuthenCode { get; set; }
    public string? FlagActive { get; set; }
}

public class Mst_OrgCKS_DeleteReq
{
    public string? Model { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? AutoID { get; set; }

    public string? OrgID { get; set; }
}
