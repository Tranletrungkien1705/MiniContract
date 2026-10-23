using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniContract.Data;
using MiniContract.Models.Port;

namespace MiniContract.Controllers.Port;

// port 1:1 DashboardController.cs — DashboardLicense
// Nguồn:
// Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\DashboardController.cs:146-209
// View: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Views\Dashboard\DashboardLicense.cshtml:1-100
// Service: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.ClientService\Services\ReportService.cs:27-31 (WA_Rpt_ContractForDashboard)
// API:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz.Web.DEV\Controllers\ReportController.cs:169-221 (WA_Rpt_ContractForDashboard_New20221101)
// BIZ:  D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Report.cs:1054-1119 (WAS_Rpt_ContractForDashboard_New20221101)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Report.cs:852-929 (Rpt_ContractForDashboard_New20221101)
//       D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Report.cs:544-728 (Rpt_ContractForDashboardX_New20221101)
[ApiController]
[Route("api/dashboard")]
public class DashboardControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public DashboardControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 action DashboardLicense từ gate DashboardController.cs:146-209.
    /// Chuỗi gọi:
    /// 1. Lấy thông tin phiên UserState (DashboardController.cs:148-158):
    ///    - MST: userState.MST -> ViewBag.MST
    ///    - Today: DateTime.Today -> ViewBag.Today
    ///    - WAUserCode: userState.SysUser.UserCode
    ///    - WAOrgID: userState.Mst_NNT.OrgID
    /// 2. Chuẩn bị khoảng thời gian thống kê (DashboardController.cs:160-171):
    ///    - convertdatetimeFrom = datetime + " 00:00:00"
    ///    - convertdatetimeTo = datetime + " 23:59:59"
    /// 3. Đóng gói RQ_Rpt_ContractForDashboard và gọi ReportService.Instance.WA_Rpt_ContractForDashboard (DashboardController.cs:176-197)
    ///    -> API WA_Rpt_ContractForDashboard_New20221101 (ReportController.cs:169-221)
    ///    -> BIZ WAS_Rpt_ContractForDashboard_New20221101 (Report.cs:1054-1119)
    ///    -> Rpt_ContractForDashboard_New20221101 (Report.cs:852-929)
    ///    -> Rpt_ContractForDashboardX_New20221101 (Report.cs:544-728):
    ///       a) #tbl_Contract_Contract_PENDING (Report.cs:613-625):
    ///          select distinct t.ContractCode from Contract_Contract t where t.ContractStatus in ('ONPROCESS')
    ///       b) #tbl_Contract_Contract_PendingForUser (Report.cs:631-646):
    ///          select distinct t.ContractCode from Contract_Contract t
    ///          inner join Contract_ContractUser ccu on t.ContractCode = ccu.ContractCode
    ///          left join Sys_User su on ccu.UserEmail = su.UserCode
    ///          where ccu.UserSignSatus in ('PENDING') and su.UserCode is not null and su.UserCode = '@strUserCode'
    ///       c) #tbl_Contract_Contract_PendingForOther (Report.cs:652-667):
    ///          select distinct t.ContractCode from Contract_Contract t
    ///          inner join Contract_ContractUser ccu on t.ContractCode = ccu.ContractCode
    ///          left join Sys_User su on ccu.UserEmail = su.UserCode
    ///          where ccu.UserSignSatus in ('PENDING') and (su.UserCode <> '@strUserCode' or su.UserCode is null)
    ///       d) #tbl_Summary (Report.cs:671-706):
    ///          - 'ContractCreatePending': count(t.ContractCode) từ #tbl_Contract_Contract_PENDING
    ///          - 'PendingForUser': count(t.ContractCode) từ #tbl_Contract_Contract_PendingForUser
    ///          - 'PendingForOther': count(t.ContractCode) từ #tbl_Contract_Contract_PendingForOther
    ///       Bảng Invoice_license đã bị comment out ở bản New20221101 (Report.cs:726) nên trả về danh sách rỗng.
    /// 4. Gán ViewBag.ListInvoice_License = listInvoice_license và trả về View(listRpt_ContractForDashboard) (DashboardController.cs:207-208).
    ///    Ở tầng API: trả về Success=true, Data=listRpt_ContractForDashboard, ListInvoice_License=listInvoice_license, Today=today, MST=mst.
    /// </summary>
    /// <summary>
    /// Port 1:1 action Index từ gate DashboardController.cs:15-79.
    /// Chuỗi gọi:
    /// 1. Lấy thông tin phiên UserState (DashboardController.cs:17-27):
    ///    - MST: userState.MST -> ViewBag.MST
    ///    - Today: DateTime.Today -> ViewBag.Today
    ///    - WAUserCode: userState.SysUser.UserCode
    ///    - WAOrgID: userState.Mst_NNT.OrgID
    /// 2. Chuẩn bị khoảng thời gian thống kê (DashboardController.cs:29-40):
    ///    - convertdatetimeFrom = datetime + " 00:00:00"
    ///    - convertdatetimeTo = datetime + " 23:59:59"
    /// 3. Đóng gói RQ_Rpt_ContractForDashboard và gọi ReportService.Instance.WA_Rpt_ContractForDashboard (DashboardController.cs:45-66)
    ///    -> API WA_Rpt_ContractForDashboard_New20221101 (ReportController.cs:169-221)
    ///    -> BIZ WAS_Rpt_ContractForDashboard_New20221101 (Report.cs:1054-1119)
    ///    -> Rpt_ContractForDashboard_New20221101 (Report.cs:852-929)
    ///    -> Rpt_ContractForDashboardX_New20221101 (Report.cs:544-728):
    ///       a) #tbl_Contract_Contract_PENDING (Report.cs:613-625):
    ///          select distinct t.ContractCode from Contract_Contract t where t.ContractStatus in ('ONPROCESS')
    ///       b) #tbl_Contract_Contract_PendingForUser (Report.cs:631-646):
    ///          select distinct t.ContractCode from Contract_Contract t
    ///          inner join Contract_ContractUser ccu on t.ContractCode = ccu.ContractCode
    ///          left join Sys_User su on ccu.UserEmail = su.UserCode
    ///          where ccu.UserSignSatus in ('PENDING') and su.UserCode is not null and su.UserCode = '@strUserCode'
    ///       c) #tbl_Contract_Contract_PendingForOther (Report.cs:652-667):
    ///          select distinct t.ContractCode from Contract_Contract t
    ///          inner join Contract_ContractUser ccu on t.ContractCode = ccu.ContractCode
    ///          left join Sys_User su on ccu.UserEmail = su.UserCode
    ///          where ccu.UserSignSatus in ('PENDING') and (su.UserCode <> '@strUserCode' or su.UserCode is null)
    ///       d) #tbl_Summary (Report.cs:671-706):
    ///          - 'ContractCreatePending': count(t.ContractCode) từ #tbl_Contract_Contract_PENDING
    ///          - 'PendingForUser': count(t.ContractCode) từ #tbl_Contract_Contract_PendingForUser
    ///          - 'PendingForOther': count(t.ContractCode) từ #tbl_Contract_Contract_PendingForOther
    ///       Bảng Invoice_license đã bị comment out ở bản New20221101 (Report.cs:726) nên trả về danh sách rỗng.
    /// 4. Gán ViewBag.ListInvoice_License = listInvoice_license và trả về View(listRpt_ContractForDashboard) (DashboardController.cs:77-78).
    ///    Ở tầng API: trả về Success=true, Data=listRpt_ContractForDashboard, ListInvoice_License=listInvoice_license, Today=today, MST=mst.
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Dashboard_IndexReq? req)
    {
        try
        {
            // 1. Xác định người dùng & MST từ request body hoặc HTTP headers
            var waUserCode = StdParam(req?.WAUserCode) ?? StdParam(req?.UserCode);
            if (string.IsNullOrEmpty(waUserCode) && Request?.Headers != null && Request.Headers.TryGetValue("WAUserCode", out var hUserCode))
            {
                waUserCode = StdParam(hUserCode.FirstOrDefault());
            }

            var mst = StdParam(req?.MST);
            if (string.IsNullOrEmpty(mst) && !string.IsNullOrEmpty(waUserCode))
            {
                var user = await _db.Set<Sys_User>().AsNoTracking()
                    .FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc));
                mst = user?.MST;
            }

            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // 2. Thống kê theo Report.cs:572-727 (Rpt_ContractForDashboardX_New20221101)
            // a) ContractCreatePending: Lấy tất cả hợp đồng đang xử lý (ContractStatus in ('ONPROCESS'))
            var contractCreatePendingCount = await _db.Set<Contract_Contract>().AsNoTracking()
                .Where(t => EF.Functions.Like((t.ContractStatus ?? "").ToLower(), PortQuery.Equal("ONPROCESS"), PortQuery.Esc))
                .Select(t => t.ContractCode)
                .Distinct()
                .CountAsync();

            // b) PendingForUser: Hợp đồng Chờ tôi ký (UserSignSatus in ('PENDING') và su.UserCode = waUserCode)
            var pendingForUserCount = 0;
            if (!string.IsNullOrEmpty(waUserCode))
            {
                var qUser = from t in _db.Set<Contract_Contract>().AsNoTracking()
                            join ccu in _db.Set<Contract_ContractUser>().AsNoTracking() on t.ContractCode equals ccu.ContractCode
                            join su in _db.Set<Sys_User>().AsNoTracking() on ccu.UserEmail equals su.UserCode
                            where EF.Functions.Like((ccu.UserSignSatus ?? "").ToLower(), PortQuery.Equal("PENDING"), PortQuery.Esc)
                               && EF.Functions.Like((su.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc)
                            select t.ContractCode;

                pendingForUserCount = await qUser.Distinct().CountAsync();
            }

            // c) PendingForOther: Hợp đồng Chờ người khác ký (UserSignSatus in ('PENDING') và (su.UserCode <> waUserCode hoặc su.UserCode is null))
            var qOther = from t in _db.Set<Contract_Contract>().AsNoTracking()
                         join ccu in _db.Set<Contract_ContractUser>().AsNoTracking() on t.ContractCode equals ccu.ContractCode
                         join su in _db.Set<Sys_User>().AsNoTracking() on ccu.UserEmail equals su.UserCode into suJoin
                         from su in suJoin.DefaultIfEmpty()
                         where EF.Functions.Like((ccu.UserSignSatus ?? "").ToLower(), PortQuery.Equal("PENDING"), PortQuery.Esc)
                            && (su == null || string.IsNullOrEmpty(waUserCode) || !EF.Functions.Like((su.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc))
                         select t.ContractCode;

            var pendingForOtherCount = await qOther.Distinct().CountAsync();

            // 3. Xây dựng danh sách kết quả theo Rpt_ContractForDashboard
            var listRpt_ContractForDashboard = new List<Rpt_ContractForDashboard>
            {
                new()
                {
                    ReportType = "ContractCreatePending",
                    TotalQty = contractCreatePendingCount
                },
                new()
                {
                    ReportType = "PendingForUser",
                    TotalQty = pendingForUserCount
                },
                new()
                {
                    ReportType = "PendingForOther",
                    TotalQty = pendingForOtherCount
                }
            };

            // Danh sách Invoice_license rỗng theo WAS_Rpt_ContractForDashboard_New20221101 (Report.cs:1054-1105)
            var listInvoice_license = new List<Invoice_license>();

            return Ok(new
            {
                Success = true,
                Data = listRpt_ContractForDashboard,
                ListInvoice_License = listInvoice_license,
                Today = today,
                MST = mst
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Dashboard_Index", "Exception", ex.Message);
        }
    }

    [HttpPost("dashboardlicense")]
    public async Task<IActionResult> DashboardLicense([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Dashboard_DashboardLicenseReq? req)
    {
        try
        {
            // 1. Xác định người dùng & MST từ request body hoặc HTTP headers
            var waUserCode = StdParam(req?.WAUserCode) ?? StdParam(req?.UserCode);
            if (string.IsNullOrEmpty(waUserCode) && Request?.Headers != null && Request.Headers.TryGetValue("WAUserCode", out var hUserCode))
            {
                waUserCode = StdParam(hUserCode.FirstOrDefault());
            }

            var mst = StdParam(req?.MST);
            if (string.IsNullOrEmpty(mst) && !string.IsNullOrEmpty(waUserCode))
            {
                var user = await _db.Set<Sys_User>().AsNoTracking()
                    .FirstOrDefaultAsync(u => EF.Functions.Like((u.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc));
                mst = user?.MST;
            }

            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // 2. Thống kê theo Report.cs:572-727 (Rpt_ContractForDashboardX_New20221101)
            // a) ContractCreatePending: Lấy tất cả hợp đồng đang xử lý (ContractStatus in ('ONPROCESS'))
            var contractCreatePendingCount = await _db.Set<Contract_Contract>().AsNoTracking()
                .Where(t => EF.Functions.Like((t.ContractStatus ?? "").ToLower(), PortQuery.Equal("ONPROCESS"), PortQuery.Esc))
                .Select(t => t.ContractCode)
                .Distinct()
                .CountAsync();

            // b) PendingForUser: Hợp đồng Chờ tôi ký (UserSignSatus in ('PENDING') và su.UserCode = waUserCode)
            var pendingForUserCount = 0;
            if (!string.IsNullOrEmpty(waUserCode))
            {
                var qUser = from t in _db.Set<Contract_Contract>().AsNoTracking()
                            join ccu in _db.Set<Contract_ContractUser>().AsNoTracking() on t.ContractCode equals ccu.ContractCode
                            join su in _db.Set<Sys_User>().AsNoTracking() on ccu.UserEmail equals su.UserCode
                            where EF.Functions.Like((ccu.UserSignSatus ?? "").ToLower(), PortQuery.Equal("PENDING"), PortQuery.Esc)
                               && EF.Functions.Like((su.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc)
                            select t.ContractCode;

                pendingForUserCount = await qUser.Distinct().CountAsync();
            }

            // c) PendingForOther: Hợp đồng Chờ người khác ký (UserSignSatus in ('PENDING') và (su.UserCode <> waUserCode hoặc su.UserCode is null))
            var qOther = from t in _db.Set<Contract_Contract>().AsNoTracking()
                         join ccu in _db.Set<Contract_ContractUser>().AsNoTracking() on t.ContractCode equals ccu.ContractCode
                         join su in _db.Set<Sys_User>().AsNoTracking() on ccu.UserEmail equals su.UserCode into suJoin
                         from su in suJoin.DefaultIfEmpty()
                         where EF.Functions.Like((ccu.UserSignSatus ?? "").ToLower(), PortQuery.Equal("PENDING"), PortQuery.Esc)
                            && (su == null || string.IsNullOrEmpty(waUserCode) || !EF.Functions.Like((su.UserCode ?? "").ToLower(), PortQuery.Equal(waUserCode), PortQuery.Esc))
                         select t.ContractCode;

            var pendingForOtherCount = await qOther.Distinct().CountAsync();

            // 3. Xây dựng danh sách kết quả theo Rpt_ContractForDashboard
            var listRpt_ContractForDashboard = new List<Rpt_ContractForDashboard>
            {
                new()
                {
                    ReportType = "ContractCreatePending",
                    TotalQty = contractCreatePendingCount
                },
                new()
                {
                    ReportType = "PendingForUser",
                    TotalQty = pendingForUserCount
                },
                new()
                {
                    ReportType = "PendingForOther",
                    TotalQty = pendingForOtherCount
                }
            };

            // Danh sách Invoice_license rỗng theo WAS_Rpt_ContractForDashboard_New20221101 (Report.cs:1054-1105)
            var listInvoice_license = new List<Invoice_license>();

            return Ok(new
            {
                Success = true,
                Data = listRpt_ContractForDashboard,
                ListInvoice_License = listInvoice_license,
                Today = today,
                MST = mst
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Dashboard_DashboardLicense", "Exception", ex.Message);
        }
    }

    #region Helper chuẩn hoá tham số theo quy ước nguồn (PORT_RULES §3)
    private static string? StdParam(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    }

    private static string? StdStr(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();
    }

    private static string StdFlag(string? s)
    {
        return s?.Trim() == "1" ? "1" : "0";
    }
    #endregion
}

#region Request & Response DTOs
public class Dashboard_IndexReq
{
    public string? NetworkID { get; set; }
    public string? ParentID { get; set; }
    public string? WAUserCode { get; set; }
    public string? UserCode { get; set; }
    public string? MST { get; set; }
}

public class Dashboard_DashboardLicenseReq
{
    public string? WAUserCode { get; set; }
    public string? UserCode { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? MST { get; set; }
    public string? ReportDTimeFrom { get; set; }
    public string? ReportDTimeTo { get; set; }
}

public class Rpt_ContractForDashboard
{
    public string? MST { get; set; } // Mã số thuế
    public string? ReportType { get; set; } // Loại báo cáo
    public double? TotalValExchange { get; set; } // Tổng giá trị quy đổi
    public double? TotalQtyContract { get; set; } // Tổng số Hợp đồng
    public double? TotalQty { get; set; } // Số lượng / Số license
    public double? TotalQtyUsed { get; set; } // Số license đã sử dụng
    public double? TotalQtyCancel { get; set; } // Số lượng đã hủy
    public double? QtyRemain { get; set; } // Số lượng còn lại
}

public class Invoice_license
{
    public string? MST { get; set; }
    public string? NetworkID { get; set; }
    public double? TotalQty { get; set; }
    public double? TotalQtyIssued { get; set; }
    public double? TotalQtyUsed { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public double? Qty { get; set; }
    public double? TotalQtyCancel { get; set; }
    public double? QtyRemain { get; set; }
}
#endregion
