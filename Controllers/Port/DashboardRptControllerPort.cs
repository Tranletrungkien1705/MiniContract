using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MiniContract.Data;

namespace MiniContract.Controllers.Port;

// port 1:1 DashboardRptController.cs — Rpt_ContractForDashboard
// Nguồn (source_excerpt.txt của hạng mục #90):
//   Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\DashboardRptController.cs:16-93
//   Gate gọi service: ReportService.Instance.WA_Rpt_ContractForDashboard (DashboardRptController.cs:74)
//   API:  KHÔNG có — excerpt dòng 134: "khong thay controller DashboardRptController.cs trong ...Biz.Web\Controllers"
//   BIZ:  KHÔNG có — excerpt dòng 127-133: không thấy model Rpt_ContractForDashboard / Invoice_license, không có DDL
//   Mã lỗi: excerpt dòng 137-138: 0 chỗ ném lỗi, danh sách mã lỗi RỖNG.
// ⇒ Toàn bộ phép tính báo cáo nằm ở dịch vụ ngoài (ReportService.Instance.WA_Rpt_ContractForDashboard).
//   Theo đề bài P3 + luật B.7: DỪNG, KHÔNG tự chế bảng/kết quả/giá trị; endpoint trả GAP.NoBackend.
//   Xem .parity-run\conflicts\90_conflict.md và NO_BACKEND.flag.
[ApiController]
public class DashboardRptControllerPort : ControllerBase
{
    /// <summary>
    /// Tham số của gate (DashboardRptController.cs:16): `networkid` (chuỗi, không chuẩn hoá ở gate).
    /// Gate còn đọc phiên `UserReportState` (dòng 18-19) — đích không có phiên đăng nhập báo cáo (GAP).
    /// </summary>
    public class DashboardRpt_DashboardSummaryReq
    {
        public string? NetworkID { get; set; }
    }

    // Nguồn DashboardRptController.cs:16-93: POST /api/dashboardrpt/dashboardsummary
    [HttpPost("api/dashboardrpt/dashboardsummary")]
    public IActionResult DashboardSummary(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DashboardRpt_DashboardSummaryReq? req)
    {
        _ = req;
        // Dòng 74: ReportService.Instance.WA_Rpt_ContractForDashboard — tầng API/biz KHÔNG có trong nguồn được trích.
        return PortResult.Fail("DashboardRpt_DashboardSummary", "GAP.NoBackend",
            "WA_Rpt_ContractForDashboard", "ReportService.Instance.WA_Rpt_ContractForDashboard (DashboardRptController.cs:74)");
    }
}
