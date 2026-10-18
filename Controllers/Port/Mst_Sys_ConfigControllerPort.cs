using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 Mst_Sys_ConfigController.cs — Mst_Sys_Config
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_Sys_ConfigController.cs:21-90 (Index)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_Sys_ConfigController.cs:94-167 (Update)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_Sys_ConfigController.cs:170-217 (DeleteAttribute)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_Sys_ConfigController.cs:221-282 (UpdateAttribute)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_Sys_ConfigController.cs:286-341 (CreateAttribute)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstAttributeContractController.cs:20-88 (WA_Mst_Attribute_Contract_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstAttributeContractController.cs:90-158 (WA_Mst_Attribute_Contract_Create)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstAttributeContractController.cs:160-228 (WA_Mst_Attribute_Contract_Update)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstAttributeContractController.cs:230-298 (WA_Mst_Attribute_Contract_Delete)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstSysConfigController.cs:20-88 (WA_Mst_Sys_Config_Get)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\MstSysConfigController.cs:91-159 (WA_Mst_Sys_Config_Update)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7224-7312 (WAS_Mst_Sys_Config_Get) -> 7313-7439 (Mst_Sys_Config_Get) -> 7441-7597 (Mst_Sys_Config_GetX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7599-7678 (WAS_Mst_Sys_Config_Update) -> 7679-7810 (Mst_Sys_Config_Update) -> 7812-7904 (Mst_Sys_Config_UpdateX) -> 7158-7222 (Mst_Sys_Config_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7974-8070 (WAS_Mst_Attribute_Contract_Get) -> 8071-8227 (Mst_Attribute_Contract_Get) -> 8923-9052 (Mst_Attribute_Contract_GetX)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:8229-8308 (WAS_Mst_Attribute_Contract_Create) -> 8309-8458 (Mst_Attribute_Contract_Create) -> 9079-9168 (Mst_Attribute_Contract_CreateX) -> 7908-7972 (Mst_Attribute_Contract_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:8459-8541 (WAS_Mst_Attribute_Contract_Update) -> 8542-8700 (Mst_Attribute_Contract_Update) -> 9169-9253 (Mst_Attribute_Contract_UpdateX) -> 7908-7972 (Mst_Attribute_Contract_CheckDB)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:8701-8778 (WAS_Mst_Attribute_Contract_Delete) -> 8779-8922 (Mst_Attribute_Contract_Delete) -> 9254-9309 (Mst_Attribute_Contract_DeleteX) -> 7908-7972 (Mst_Attribute_Contract_CheckDB)
[ApiController]
[Route("api/mst_sys_config")]
public class Mst_Sys_ConfigControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Mst_Sys_ConfigControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action Index từ gate Mst_Sys_ConfigController.cs:21-90.
    /// Chuỗi gọi:
    /// 1. Mst_Attribute_ContractService.Instance.WA_Mst_Attribute_Contract_Get (gate dòng 53)
    ///    -> API WA_Mst_Attribute_Contract_Get -> WAS_Mst_Attribute_Contract_Get -> Mst_Attribute_Contract_Get -> Mst_Attribute_Contract_GetX (QContract.cs:8923-9052)
    ///    Lấy danh sách thuộc tính hợp đồng, gán vào ViewBag.ListMst_Attribute_Contract.
    /// 2. Mst_Sys_ConfigService.Instance.WA_Mst_Sys_Config_Get (gate dòng 83)
    ///    -> API WA_Mst_Sys_Config_Get -> WAS_Mst_Sys_Config_Get -> Mst_Sys_Config_Get -> Mst_Sys_Config_GetX (QContract.cs:7441-7597)
    ///    Lấy thông tin cấu hình hệ thống theo OrgID.
    /// 3. Nếu tìm thấy cấu hình (Lst_Mst_Sys_Config.Count > 0): trả về View(model), ngược lại trả về View() (gate dòng 85-89).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Sys_Config_IndexReq? req)
    {
        try
        {
            // 1. Chuẩn hoá tham số đầu vào (Lesson A.14, A.21, PORT_RULES §3)
            var orgId = StdParam(req?.OrgID);
            var networkId = StdParam(req?.NetworkID);

            // 2. Lấy thông tin thuộc tính hợp đồng Mst_Attribute_Contract (QContract.cs:8923-9052, gate dòng 31-59)
            // SQL nguồn: select distinct mo.AttributeContractCode from Mst_Attribute_Contract mo order by mo.AttributeContractCode asc
            var queryAttr = _db.Set<Mst_Attribute_Contract>().AsNoTracking();
            if (!string.IsNullOrEmpty(networkId))
            {
                queryAttr = queryAttr.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }
            var listMst_Attribute_Contract = await queryAttr
                .OrderBy(x => x.AttributeContractCode)
                .ToListAsync();

            // 3. Lấy thông tin cấu hình hệ thống Mst_Sys_Config (QContract.cs:7441-7597, gate dòng 61-89)
            // SQL nguồn: select distinct mo.NetworkID, mo.OrgID from Mst_Sys_Config mo where ... order by mo.NetworkID asc
            var queryConfig = _db.Set<Mst_Sys_Config>().AsNoTracking();
            if (!string.IsNullOrEmpty(orgId))
            {
                queryConfig = queryConfig.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(networkId))
            {
                queryConfig = queryConfig.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
            }

            // Sắp xếp theo NetworkID asc (QContract.cs:7503)
            queryConfig = queryConfig.OrderBy(x => x.NetworkID);

            var itemCount = await queryConfig.CountAsync();
            var listMst_Sys_Config = await queryConfig.ToListAsync();

            // 4. Trả về kết quả khớp gate MVC: Model là Mst_Sys_Config (hoặc Lst_Mst_Sys_Config) và ViewBag chứa ListMst_Attribute_Contract (Mst_Sys_ConfigController.cs:58, 85-89)
            var data = new Mst_Sys_Config_IndexData
            {
                Mst_Sys_Config = listMst_Sys_Config.FirstOrDefault(),
                Lst_Mst_Sys_Config = listMst_Sys_Config,
                ListMst_Attribute_Contract = listMst_Attribute_Contract,
                Lst_Mst_Attribute_Contract = listMst_Attribute_Contract,
                ItemCount = itemCount
            };

            return Ok(new
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Sys_Config_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action Update từ gate Mst_Sys_ConfigController.cs:94-167.
    /// Chuỗi gọi:
    /// 1. Gate deserialize model -> RQ_Mst_Sys_Config (gate dòng 103).
    ///    Nếu null hoặc rỗng -> trả về JsonResultEntry(Success=true, "Danh sách cấu hình trống!").
    /// 2. Gate gọi WA_Mst_Sys_Config_Get lấy cấu hình hiện có theo OrgID (gate dòng 130).
    ///    Nếu danh sách cấu hình rỗng -> trả về JsonResultEntry(Success=true, "Danh sách cấu hình trống!").
    /// 3. Gate gán các cờ FlagColmnConfig, FlagCurrency, FlagOTP và gọi WA_Mst_Sys_Config_Update (gate dòng 140-148).
    /// 4. API WA_Mst_Sys_Config_Update (MstSysConfigController.cs:91-159)
    ///    -> BIZ WAS_Mst_Sys_Config_Update (QContract.cs:7599-7678)
    ///    -> Mst_Sys_Config_Update (QContract.cs:7679-7810, mở transaction dòng 7720)
    ///    -> Mst_Sys_Config_UpdateX (QContract.cs:7812-7904):
    ///       - Mst_Sys_Config_CheckDB (QContract.cs:7158-7222): nếu OrgID không tồn tại trong DB -> ném Mst_Sys_Config_CheckDB_OrganNotFound (dòng 7190).
    ///       - Cập nhật các cột hiệu lực: FlagCurrency, FlagColmnConfig, FlagOTP, LogLUDTimeUTC, LogLUBy (dòng 7887-7895).
    ///       - Lưu DB và commit transaction.
    /// 5. Gate trả về JsonResultEntry(Success=true, "Sửa cấu hình thành công!", RedirectUrl="Index").
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Sys_Config_UpdateReq? req)
    {
        try
        {
            // 1. Hỗ trợ nhận model dạng chuỗi JSON thô như gate MVC (Mst_Sys_ConfigController.cs:94, 103)
            if (req != null && !string.IsNullOrWhiteSpace(req.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Sys_Config_UpdateReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        req.OrgID ??= modelObj.OrgID;
                        req.NetworkID ??= modelObj.NetworkID;
                        req.FlagCurrency ??= modelObj.FlagCurrency;
                        req.FlagColmnConfig ??= modelObj.FlagColmnConfig;
                        req.FlagOTP ??= modelObj.FlagOTP;
                        req.Ft_Cols_Upd ??= modelObj.Ft_Cols_Upd;
                        req.Mst_Sys_Config ??= modelObj.Mst_Sys_Config;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            // Gán thông tin từ Mst_Sys_Config lồng (nếu có)
            if (req?.Mst_Sys_Config != null)
            {
                req.OrgID ??= req.Mst_Sys_Config.OrgID;
                req.NetworkID ??= req.Mst_Sys_Config.NetworkID;
                req.FlagCurrency ??= req.Mst_Sys_Config.FlagCurrency;
                req.FlagColmnConfig ??= req.Mst_Sys_Config.FlagColmnConfig;
                req.FlagOTP ??= req.Mst_Sys_Config.FlagOTP;
            }

            // 2. Gate guard 1: Kiểm tra body/model rỗng (Gate: Mst_Sys_ConfigController.cs:105, 160-164)
            if (req == null || (string.IsNullOrWhiteSpace(req.Model) && req.Mst_Sys_Config == null && string.IsNullOrWhiteSpace(req.OrgID) && string.IsNullOrWhiteSpace(req.FlagCurrency) && string.IsNullOrWhiteSpace(req.FlagColmnConfig) && string.IsNullOrWhiteSpace(req.FlagOTP)))
            {
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Danh sách cấu hình trống!" },
                    Title = "Danh sách cấu hình trống!",
                    Data = (Mst_Sys_Config?)null
                });
            }

            // 3. Chuẩn hoá tham số đầu vào (QContract.cs:7850-7857)
            var orgId = StdParam(req.OrgID);
            var networkId = StdParam(req.NetworkID);

            // 4. Tìm kiếm bản ghi hiện có theo OrgID hoặc bản ghi đầu tiên trong hệ thống (Gate dòng 122, 130-131; BIZ QContract.cs:7168-7174)
            Mst_Sys_Config? existing = null;
            if (!string.IsNullOrEmpty(orgId))
            {
                existing = await _db.Set<Mst_Sys_Config>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgId), PortQuery.Esc));

                // Biz guard 1: Mst_Sys_Config_CheckDB kiểm tra OrgID không tồn tại (QContract.cs:7184-7194)
                if (existing == null)
                {
                    return PortResult.Fail("Mst_Sys_Config_CheckDB_OrganNotFound", "Check.OrgID", orgId);
                }
            }
            else
            {
                var query = _db.Set<Mst_Sys_Config>().AsQueryable();
                if (!string.IsNullOrEmpty(networkId))
                {
                    query = query.Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc));
                }
                existing = await query.OrderBy(x => x.NetworkID).FirstOrDefaultAsync();

                // Gate guard 2: Danh sách cấu hình trống (Gate: Mst_Sys_ConfigController.cs:131, 154-158)
                if (existing == null)
                {
                    return Ok(new
                    {
                        Success = true,
                        Messages = new[] { "Danh sách cấu hình trống!" },
                        Title = "Danh sách cấu hình trống!",
                        Data = (Mst_Sys_Config?)null
                    });
                }
            }

            // 5. Xác định các cột cập nhật theo Ft_Cols_Upd (Gate dòng 134-138; BIZ QContract.cs:7860-7864)
            var ftColsUpd = StdParam(req.Ft_Cols_Upd);
            bool updAll = string.IsNullOrEmpty(ftColsUpd);

            bool bUpd_FlagColmnConfig = updAll || ftColsUpd!.Contains("FLAGCOLMNCONFIG");
            bool bUpd_FlagCurrency = updAll || ftColsUpd!.Contains("FLAGCURRENCY");
            bool bUpd_FlagOTP = updAll || ftColsUpd!.Contains("FLAGOTP");
            bool bUpd_NetworkID = !updAll && ftColsUpd!.Contains("NETWORKID");

            // 6. Mở transaction và cập nhật dữ liệu (QContract.cs:7720, 7886-7901)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (bUpd_FlagColmnConfig)
                {
                    var val = req.FlagColmnConfig ?? req.Mst_Sys_Config?.FlagColmnConfig;
                    existing.FlagColmnConfig = StdFlag(val);
                }

                if (bUpd_FlagCurrency)
                {
                    var val = req.FlagCurrency ?? req.Mst_Sys_Config?.FlagCurrency;
                    existing.FlagCurrency = StdFlag(val);
                }

                if (bUpd_FlagOTP)
                {
                    var val = req.FlagOTP ?? req.Mst_Sys_Config?.FlagOTP;
                    existing.FlagOTP = StdFlag(val);
                }

                if (bUpd_NetworkID && !string.IsNullOrEmpty(networkId))
                {
                    existing.NetworkID = networkId;
                }

                var dtimeSys = DateTime.UtcNow;
                var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

                existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
                existing.LogLUBy = userCode;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // 7. Trả về kết quả thành công khớp gate MVC (Mst_Sys_ConfigController.cs:150-152)
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Sửa cấu hình thành công!" },
                    Title = "Sửa cấu hình thành công!",
                    RedirectUrl = "/Mst_Sys_Config",
                    Data = existing
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Sys_Config_Update", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Sys_Config_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action DeleteAttribute từ gate Mst_Sys_ConfigController.cs:170-217.
    /// Chuỗi gọi:
    /// 1. Gate nhận tham số attributecontractcode (gate dòng 170).
    /// 2. Gate tạo RQ_Mst_Attribute_Contract chứa Mst_Attribute_Contract(AttributeContractCode) và gọi
    ///    Mst_Attribute_ContractService.Instance.WA_Mst_Attribute_Contract_Delete (gate dòng 183-206).
    /// 3. API WA_Mst_Attribute_Contract_Delete (MstAttributeContractController.cs:230-298)
    ///    -> BIZ WAS_Mst_Attribute_Contract_Delete (QContract.cs:8701-8778)
    ///    -> Mst_Attribute_Contract_Delete (QContract.cs:8779-8922, mở transaction dòng 8812)
    ///    -> Mst_Attribute_Contract_DeleteX (QContract.cs:9254-9309):
    ///       - Chuẩn hoá strAttributeContractCode = StdParam(objAttributeContractCode) (dòng 9280).
    ///       - Mst_Attribute_Contract_CheckDB (QContract.cs:7908-7972): kiểm tra tồn tại bản ghi trong Mst_Attribute_Contract (dòng 7918-7928).
    ///         Nếu không tìm thấy bản ghi (dtDB.Rows.Count < 1) -> ném Mst_Attribute_Contract_CheckDB_OrganNotFound (dòng 7940).
    ///       - Xoá bản ghi: dtDB_Mst_Attribute_Contract.Rows[0].Delete() và lưu DB (dòng 9300-9306).
    ///       - Commit transaction (dòng 8885).
    /// 4. Gate trả về JsonResultEntry(Success=true, "Xoá thông tin thành công!") (gate dòng 207-209).
    /// </summary>
    [HttpPost("deleteattribute")]
    public async Task<IActionResult> DeleteAttribute([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Sys_Config_DeleteAttributeReq? req)
    {
        try
        {
            var rawCode = req?.AttributeContractCode;
            if (string.IsNullOrWhiteSpace(rawCode) && !string.IsNullOrWhiteSpace(req?.Model))
            {
                try
                {
                    var doc = JsonDocument.Parse(req.Model);
                    if (doc.RootElement.TryGetProperty("AttributeContractCode", out var p1) ||
                        doc.RootElement.TryGetProperty("attributecontractcode", out p1))
                    {
                        rawCode = p1.GetString();
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            var code = StdParam(rawCode);

            // Guard: Kiểm tra tồn tại trong DB (Mst_Attribute_Contract_CheckDB: QContract.cs:7935-7944)
            // Nếu mã null/rỗng hoặc không tìm thấy bản ghi trong DB -> ném Mst_Attribute_Contract_CheckDB_OrganNotFound
            if (string.IsNullOrEmpty(code))
            {
                return PortResult.Fail("Mst_Attribute_Contract_CheckDB_OrganNotFound", "Check.AttributeContractCode", code);
            }

            var existing = await _db.Set<Mst_Attribute_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.AttributeContractCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));

            if (existing == null)
            {
                return PortResult.Fail("Mst_Attribute_Contract_CheckDB_OrganNotFound", "Check.AttributeContractCode", code);
            }

            // Mở transaction và xoá bản ghi (QContract.cs:8812, 9300-9306)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Set<Mst_Attribute_Contract>().Remove(existing);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Xoá thông tin thành công!" },
                    Title = "Xoá thông tin thành công!"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Attribute_Contract_Delete", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Attribute_Contract_Delete", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action UpdateAttribute từ gate Mst_Sys_ConfigController.cs:221-282.
    /// Chuỗi gọi:
    /// 1. Gate nhận tham số model (chuỗi JSON đại diện cho Mst_Attribute_Contract) (gate dòng 221, 231).
    /// 2. Gate tạo RQ_Mst_Attribute_Contract và gán Ft_Cols_Upd gồm AttributeName, DefaultValues, FlagActive (gate dòng 235-256).
    /// 3. Gate gọi Mst_Attribute_ContractService.Instance.WA_Mst_Attribute_Contract_Update (gate dòng 270).
    /// 4. API WA_Mst_Attribute_Contract_Update (MstAttributeContractController.cs:160-228)
    ///    -> BIZ WAS_Mst_Attribute_Contract_Update (QContract.cs:8459-8541)
    ///    -> Mst_Attribute_Contract_Update (QContract.cs:8542-8700, mở transaction dòng 8585)
    ///    -> Mst_Attribute_Contract_UpdateX (QContract.cs:9169-9253):
    ///       - Chuẩn hoá strAttributeContractCode = StdParam(objAttributeContractCode) (dòng 9207).
    ///       - Mst_Attribute_Contract_CheckDB (QContract.cs:7908-7972): kiểm tra tồn tại bản ghi trong Mst_Attribute_Contract (dòng 7918-7928).
    ///         Nếu không tìm thấy bản ghi (dtDB.Rows.Count < 1) -> ném Mst_Attribute_Contract_CheckDB_OrganNotFound (dòng 7940).
    ///       - Cập nhật các cột hiệu lực: AttributeName, DefaultValues, FlagActive, LogLUDTimeUTC, LogLUBy (dòng 9235-9251).
    ///       - Lưu DB và commit transaction (dòng 8663).
    /// 5. Gate trả về JsonResultEntry(Success=true, "Cập nhật thông tin hợp đồng thành công!", RedirectUrl="Index") (gate dòng 271-274).
    /// </summary>
    [HttpPost("updateattribute")]
    public async Task<IActionResult> UpdateAttribute([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Sys_Config_UpdateAttributeReq? req)
    {
        try
        {
            // 1. Hỗ trợ nhận model dạng chuỗi JSON thô như gate MVC (Mst_Sys_ConfigController.cs:221, 231)
            if (req != null && !string.IsNullOrWhiteSpace(req.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Sys_Config_UpdateAttributeReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        req.AttributeContractCode ??= modelObj.AttributeContractCode;
                        req.NetworkID ??= modelObj.NetworkID;
                        req.AttributeName ??= modelObj.AttributeName;
                        req.DefaultValues ??= modelObj.DefaultValues;
                        req.FlagActive ??= modelObj.FlagActive;
                        req.Ft_Cols_Upd ??= modelObj.Ft_Cols_Upd;
                        req.Mst_Attribute_Contract ??= modelObj.Mst_Attribute_Contract;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            // Gán thông tin từ Mst_Attribute_Contract lồng (nếu có)
            if (req?.Mst_Attribute_Contract != null)
            {
                req.AttributeContractCode ??= req.Mst_Attribute_Contract.AttributeContractCode;
                req.NetworkID ??= req.Mst_Attribute_Contract.NetworkID;
                req.AttributeName ??= req.Mst_Attribute_Contract.AttributeName;
                req.DefaultValues ??= req.Mst_Attribute_Contract.DefaultValues;
                req.FlagActive ??= req.Mst_Attribute_Contract.FlagActive;
            }

            // 2. Chuẩn hoá tham số đầu vào (QContract.cs:9204-9211)
            var code = StdParam(req?.AttributeContractCode);

            // Guard: Kiểm tra tồn tại trong DB (Mst_Attribute_Contract_CheckDB: QContract.cs:7935-7944)
            // Nếu mã null/rỗng hoặc không tìm thấy bản ghi trong DB -> ném Mst_Attribute_Contract_CheckDB_OrganNotFound
            if (string.IsNullOrEmpty(code))
            {
                return PortResult.Fail("Mst_Attribute_Contract_CheckDB_OrganNotFound", "Check.AttributeContractCode", code);
            }

            var existing = await _db.Set<Mst_Attribute_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.AttributeContractCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));

            if (existing == null)
            {
                return PortResult.Fail("Mst_Attribute_Contract_CheckDB_OrganNotFound", "Check.AttributeContractCode", code);
            }

            // 3. Xác định các cột cập nhật theo Ft_Cols_Upd (Gate dòng 235-240; BIZ QContract.cs:9212-9216)
            var ftColsUpd = StdParam(req?.Ft_Cols_Upd);
            bool updAll = string.IsNullOrEmpty(ftColsUpd);

            bool bUpd_AttributeContractCode = !updAll && ftColsUpd!.Contains("ATTRIBUTECONTRACTCODE");
            bool bUpd_AttributeName = updAll || ftColsUpd!.Contains("ATTRIBUTENAME");
            bool bUpd_DefaultValues = updAll || ftColsUpd!.Contains("DEFAULTVALUES");
            bool bUpd_FlagActive = updAll || ftColsUpd!.Contains("FLAGACTIVE");

            // 4. Mở transaction và cập nhật dữ liệu (QContract.cs:8585, 9235-9251)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (bUpd_AttributeContractCode && !string.IsNullOrEmpty(code))
                {
                    existing.AttributeContractCode = code;
                }

                if (bUpd_AttributeName)
                {
                    existing.AttributeName = req?.AttributeName ?? req?.Mst_Attribute_Contract?.AttributeName ?? existing.AttributeName;
                }

                if (bUpd_DefaultValues)
                {
                    existing.DefaultValues = req?.DefaultValues ?? req?.Mst_Attribute_Contract?.DefaultValues ?? existing.DefaultValues;
                }

                if (bUpd_FlagActive)
                {
                    var rawFlag = req?.FlagActive ?? req?.Mst_Attribute_Contract?.FlagActive;
                    existing.FlagActive = StdFlag(rawFlag);
                }

                var dtimeSys = DateTime.UtcNow;
                var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

                existing.LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");
                existing.LogLUBy = userCode;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // 5. Trả về kết quả thành công khớp gate MVC (Mst_Sys_ConfigController.cs:271-274)
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Cập nhật thông tin hợp đồng thành công!" },
                    Title = "Cập nhật thông tin hợp đồng thành công!",
                    RedirectUrl = "/Mst_Sys_Config",
                    Data = existing
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Attribute_Contract_Update", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Attribute_Contract_Update", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Port 1:1 action CreateAttribute từ gate Mst_Sys_ConfigController.cs:286-341.
    /// Chuỗi gọi:
    /// 1. Gate nhận tham số model (chuỗi JSON đại diện cho Mst_Attribute_Contract) (gate dòng 286, 296).
    /// 2. Gate tạo RQ_Mst_Attribute_Contract chứa Mst_Attribute_Contract (AttributeContractCode, AttributeName, DefaultValues, FlagActive) (gate dòng 300-328).
    /// 3. Gate gọi Mst_Attribute_ContractService.Instance.WA_Mst_Attribute_Contract_Create (gate dòng 329).
    /// 4. API WA_Mst_Attribute_Contract_Create (MstAttributeContractController.cs:90-158)
    ///    -> BIZ WAS_Mst_Attribute_Contract_Create (QContract.cs:8229-8308)
    ///    -> Mst_Attribute_Contract_Create (QContract.cs:8309-8458, mở transaction dòng 8346)
    ///    -> Mst_Attribute_Contract_CreateX (QContract.cs:9079-9168):
    ///       - Chuẩn hoá strAttributeContractCode = StdParam(objAttributeContractCode) (dòng 9109).
    ///       - Guard 1: if (strAttributeContractCode == null || strAttributeContractCode.Length < 1)
    ///         -> ném Mst_Attribute_Contract_Create_InvalidAttributeContractCode (dòng 9120-9130).
    ///       - Guard 2: Mst_Attribute_Contract_CheckDB với FlagExist = "0" (QContract.cs:7908-7972, 9132-9138):
    ///         Nếu bản ghi đã tồn tại trong DB (dtDB.Rows.Count > 0)
    ///         -> ném Mst_Attribute_Contract_CheckDB_OrganExist (dòng 7951).
    ///       - Tạo mới bản ghi: AttributeContractCode, NetworkID, AttributeName, DefaultValues, FlagActive = "1", LogLUDTimeUTC, LogLUBy (dòng 9146-9158).
    ///       - Lưu DB (dòng 9161) và commit transaction (dòng 8421).
    /// 5. Gate trả về JsonResultEntry(Success=true, "Lưu thông tin hợp đồng thành công!", RedirectUrl="Index") (gate dòng 330-333).
    /// </summary>
    [HttpPost("createattribute")]
    public async Task<IActionResult> CreateAttribute([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Mst_Sys_Config_CreateAttributeReq? req)
    {
        try
        {
            // 1. Hỗ trợ nhận model dạng chuỗi JSON thô như gate MVC (Mst_Sys_ConfigController.cs:286, 296)
            if (req != null && !string.IsNullOrWhiteSpace(req.Model))
            {
                try
                {
                    var modelObj = JsonSerializer.Deserialize<Mst_Sys_Config_CreateAttributeReq>(req.Model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (modelObj != null)
                    {
                        req.AttributeContractCode ??= modelObj.AttributeContractCode;
                        req.NetworkID ??= modelObj.NetworkID;
                        req.AttributeName ??= modelObj.AttributeName;
                        req.DefaultValues ??= modelObj.DefaultValues;
                        req.FlagActive ??= modelObj.FlagActive;
                        req.Mst_Attribute_Contract ??= modelObj.Mst_Attribute_Contract;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse JSON thô
                }
            }

            // Gán thông tin từ Mst_Attribute_Contract lồng (nếu có)
            if (req?.Mst_Attribute_Contract != null)
            {
                req.AttributeContractCode ??= req.Mst_Attribute_Contract.AttributeContractCode;
                req.NetworkID ??= req.Mst_Attribute_Contract.NetworkID;
                req.AttributeName ??= req.Mst_Attribute_Contract.AttributeName;
                req.DefaultValues ??= req.Mst_Attribute_Contract.DefaultValues;
                req.FlagActive ??= req.Mst_Attribute_Contract.FlagActive;
            }

            // 2. Chuẩn hoá tham số đầu vào (QContract.cs:9109-9111)
            var code = StdParam(req?.AttributeContractCode);

            // Guard 1: Kiểm tra mã hợp lệ (QContract.cs:9120-9130)
            // if (strAttributeContractCode == null || strAttributeContractCode.Length < 1) -> ném InvalidAttributeContractCode
            if (string.IsNullOrEmpty(code))
            {
                return PortResult.Fail("Mst_Attribute_Contract_Create_InvalidAttributeContractCode", "Check.strAttributeContractCode", code);
            }

            // Guard 2: Kiểm tra bản ghi đã tồn tại chưa (Mst_Attribute_Contract_CheckDB: QContract.cs:7945-7955, 9132-9138)
            // if (strFlagExistToCheck == "0" && dtDB.Rows.Count > 0) -> ném OrganExist
            var existing = await _db.Set<Mst_Attribute_Contract>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.AttributeContractCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));

            if (existing != null)
            {
                return PortResult.Fail("Mst_Attribute_Contract_CheckDB_OrganExist", "Check.AttributeContractCode", code);
            }

            // 3. Mở transaction và lưu dữ liệu (QContract.cs:8346, 8421, 9146-9166)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var networkId = StdParam(req?.NetworkID) ?? _config?["Biz_NetworkID"] ?? "1001";
                var attrName = req?.AttributeName != null ? req.AttributeName.Trim() : "";
                var defaultValues = req?.DefaultValues != null ? req.DefaultValues.Trim() : "";
                var dtimeSys = DateTime.UtcNow;
                var userCode = Request?.Headers["WAUserCode"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userCode)) userCode = "port";

                var entity = new Mst_Attribute_Contract
                {
                    AttributeContractCode = code,
                    NetworkID = networkId,
                    AttributeName = attrName,
                    DefaultValues = defaultValues,
                    FlagActive = "1", // TConst.Flag.Active trong Mst_Attribute_Contract_CreateX dòng 9155
                    LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                    LogLUBy = userCode
                };

                await _db.Set<Mst_Attribute_Contract>().AddAsync(entity);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // 4. Trả về kết quả thành công khớp gate MVC (Mst_Sys_ConfigController.cs:330-333)
                return Ok(new
                {
                    Success = true,
                    Messages = new[] { "Lưu thông tin hợp đồng thành công!" },
                    Title = "Lưu thông tin hợp đồng thành công!",
                    RedirectUrl = "/Mst_Sys_Config",
                    Data = entity
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_Attribute_Contract_Create", "Exception", ex.Message);
            }
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_Attribute_Contract_Create", "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpperInvariant();
    private static string? StdStr(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string StdFlag(string? v) => (v?.Trim() == "1") ? "1" : "0";
}

/// <summary>
/// DTO yêu cầu cho action Index của Mst_Sys_ConfigController.
/// </summary>
public class Mst_Sys_Config_IndexReq
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public int? Page { get; set; }
    public int? PageIndex { get; set; }
    public int? Recordcount { get; set; }
    public int? PageSize { get; set; }
    public int? Ft_RecordStart { get; set; }
    public int? Ft_RecordCount { get; set; }
    public string? Ft_WhereClause { get; set; }
}

/// <summary>
/// DTO trả về cho action Index của Mst_Sys_ConfigController.
/// Bao gồm thông tin cấu hình hệ thống (Model View) và danh sách thuộc tính hợp đồng (ViewBag.ListMst_Attribute_Contract).
/// </summary>
public class Mst_Sys_Config_IndexData
{
    public Mst_Sys_Config? Mst_Sys_Config { get; set; }
    public List<Mst_Sys_Config> Lst_Mst_Sys_Config { get; set; } = new();
    public List<Mst_Attribute_Contract> ListMst_Attribute_Contract { get; set; } = new();
    public List<Mst_Attribute_Contract> Lst_Mst_Attribute_Contract { get; set; } = new();
    public int ItemCount { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action Update của Mst_Sys_ConfigController.
/// </summary>
public class Mst_Sys_Config_UpdateReq
{
    public string? Model { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagCurrency { get; set; }
    public string? FlagColmnConfig { get; set; }
    public string? FlagOTP { get; set; }
    public string? Ft_Cols_Upd { get; set; }
    public Mst_Sys_Config_UpdateItem? Mst_Sys_Config { get; set; }
}

public class Mst_Sys_Config_UpdateItem
{
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagCurrency { get; set; }
    public string? FlagColmnConfig { get; set; }
    public string? FlagOTP { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action DeleteAttribute của Mst_Sys_ConfigController.
/// </summary>
public class Mst_Sys_Config_DeleteAttributeReq
{
    public string? Model { get; set; }
    public string? AttributeContractCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action UpdateAttribute của Mst_Sys_ConfigController.
/// </summary>
public class Mst_Sys_Config_UpdateAttributeReq
{
    public string? Model { get; set; }
    public string? AttributeContractCode { get; set; }
    public string? NetworkID { get; set; }
    public string? AttributeName { get; set; }
    public string? DefaultValues { get; set; }
    public string? FlagActive { get; set; }
    public string? Ft_Cols_Upd { get; set; }
    public Mst_Attribute_Contract_UpdateItem? Mst_Attribute_Contract { get; set; }
}

public class Mst_Attribute_Contract_UpdateItem
{
    public string? AttributeContractCode { get; set; }
    public string? NetworkID { get; set; }
    public string? AttributeName { get; set; }
    public string? DefaultValues { get; set; }
    public string? FlagActive { get; set; }
}

/// <summary>
/// DTO yêu cầu cho action CreateAttribute của Mst_Sys_ConfigController.
/// </summary>
public class Mst_Sys_Config_CreateAttributeReq
{
    public string? Model { get; set; }
    public string? AttributeContractCode { get; set; }
    public string? NetworkID { get; set; }
    public string? AttributeName { get; set; }
    public string? DefaultValues { get; set; }
    public string? FlagActive { get; set; }
    public Mst_Attribute_Contract_CreateItem? Mst_Attribute_Contract { get; set; }
}

public class Mst_Attribute_Contract_CreateItem
{
    public string? AttributeContractCode { get; set; }
    public string? NetworkID { get; set; }
    public string? AttributeName { get; set; }
    public string? DefaultValues { get; set; }
    public string? FlagActive { get; set; }
}
