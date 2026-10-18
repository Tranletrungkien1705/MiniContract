using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_OrgCKSController.cs — Mst_OrgSignConfig
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_OrgCKSController.cs:22-65
// Service: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.ClientService\Services\Mst_OrgSignConfigService.cs:33-37
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\MstOrgSignConfigController.cs:90-158
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:49499-49900
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
