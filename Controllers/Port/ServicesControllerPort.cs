using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MiniContract.Data;

namespace MiniContract.Controllers.Port;

// port 1:1 ServicesController.cs — ContractPartySend (#256) + ContractPartySend_SSO (#257) + ContractSend (#258)
//   Gate #256: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ServicesController.cs:417-585
//   Gate #257: cùng file :587-756
//   Gate #258: cùng file :759-926
//   Trích nguồn #258: .parity-run\logs\258_ServicesController_ContractSend\source_excerpt.txt
//   Tầng gate #258 chỉ gọi:
//     - CheckGatewayAuthentication (BaseController.cs:864-872) — so header Gw*, không phải nghiệp vụ
//     - InitSSO / CheckAuthorize / AccountService / OrgService (BaseController.cs:808-907) — iNOS SSO (dịch vụ ngoài)
//     - API_Get (BaseController.cs:959-982) → MasterServerService.Instance.WA_MstSv_Sys_User_Login (dịch vụ ngoài)
//     - WA_Mst_ColumnConfig_Get (ServicesController.cs:3746-3793) → TClientService...WA_Mst_ColumnConfig_Get (dịch vụ ngoài)
//     - ContractData_Get (ServicesController.cs:1078-1154) → WA_Contract_Contract_Get (dịch vụ ngoài)
//     - RT_Contract_Contract_Return_CheckUserToSend (ServicesController.cs:3141-3196) — lọc theo FlagSendAll/FlagSendUser trên dữ liệu API trả về
//     - SendChannel (ServicesController.cs:1206-1612) → WA_Mst_Channel_Get / WA_Mst_SubmissionForm_Get /
//       WA_Mst_ContractTypeDtl_Get / EmailChannel / ZaloChannel / SMSChannel (Mailgate / OS_idocNet_SMS_V10 / ZaloOA — dịch vụ ngoài)
//   API:  excerpt dòng 1616: "API: khong thay controller ServicesController.cs trong ...Biz.Web.Controllers"
//         ⇒ KHÔNG có tầng API (Biz.Web) cho ContractSend.
//   BIZ:  KHÔNG có — mọi WA_* là ClientService gọi HTTP sang API động, không có hàm biz/DDL nội bộ.
//   Mã lỗi: excerpt dòng 1620-1622: 7 chỗ throw new Exception, danh sách mã TError.* RỖNG (không có mã lỗi nghiệp vụ).
// ⇒ Toàn bộ dữ liệu + hành vi nằm ở dịch vụ ngoài (iNOS SSO, MasterServer, ClientService API động, Mailgate/SMS/ZaloOA).
//   Theo đề bài P3 + luật B.7: DỪNG, KHÔNG tự chế bảng/kết quả/giá trị; endpoint trả GAP.NoBackend.
//   Xem .parity-run\conflicts\258_conflict.md.
[ApiController]
[Route("api/services")]
public class ServicesControllerPort : ControllerBase
{
    // =====================================================================================
    // #256 — ServicesController.ContractPartySend (ServicesController.cs:417-585)
    // =====================================================================================

    /// <summary>
    /// Tham số của gate #256 (ServicesController.cs:417): `strJson` chứa TModelsUI.RQ_Contract_ContractUI
    /// có Lst_Contract_Contract, FlagSendPartyCreate, WAUserCode/WAUserPassword, NetworkID.
    /// Không port được phần gửi (dịch vụ ngoài) ⇒ đích chỉ nhận đúng khung req để trả GAP.NoBackend.
    /// </summary>
    public class Services_ContractPartySendReq
    {
        public string? FlagSendPartyCreate { get; set; }
        public string? NetworkID { get; set; }
        public List<Services_ContractPartySendContract>? Lst_Contract_Contract { get; set; }
    }

    public class Services_ContractPartySendContract
    {
        public string? ContractCode { get; set; }
    }

    // Nguồn ServicesController.cs:417-585 — POST /api/services/contractpartysend
    [HttpPost("contractpartysend")]
    public IActionResult ContractPartySend(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Services_ContractPartySendReq? req)
    {
        _ = req;
        // Dòng 489/1123/1266/1385/449: TClientService.*.Instance.WA_*_Get — API từ xa (KHÔNG có trong Biz.Web nguồn).
        // Dòng 1206-1612: SendChannel -> Email/Zalo/SMS — MiniContract không có hạ tầng Mailgate/SMS/ZaloOA.
        return PortResult.Fail("Services_ContractPartySend", "GAP.NoBackend",
            "WA_Contract_Contract_Get+WA_Mst_Channel_Get+WA_Mst_SubmissionForm_Get+WA_Mst_ContractTypeDtl_Get+SendChannel",
            "ServicesController.cs:489,1123,1266,1385,449,1206-1612 (dịch vụ ngoài)");
    }

    // =====================================================================================
    // #257 — ServicesController.ContractPartySend_SSO (ServicesController.cs:587-756)
    // =====================================================================================

    /// <summary>
    /// Tham số của gate #257 (ServicesController.cs:587): `strJson` chứa TModelsUI.RQ_Contract_ContractUI
    /// với AccessToken (dùng cho SSO khi flagUserLogin="1"), NetworkID, FlagSendPartyCreate, Lst_Contract_Contract.
    /// Gate đọc thêm header FlagUserLogin / NetworkID / OrgID / Tid / GwUserCode / GwPassword (CheckGatewayAuthentication).
    /// Không port được phần SSO + gửi (dịch vụ ngoài) ⇒ đích chỉ nhận đúng khung req để trả GAP.NoBackend.
    /// </summary>
    public class Services_ContractPartySend_SSOReq
    {
        public string? AccessToken { get; set; }
        public string? NetworkID { get; set; }
        public string? FlagSendPartyCreate { get; set; }
        public List<Services_ContractPartySendContract>? Lst_Contract_Contract { get; set; }
    }

    // Nguồn ServicesController.cs:587-756 — POST /api/services/contractpartysend_sso
    [HttpPost("contractpartysend_sso")]
    public IActionResult ContractPartySend_SSO(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Services_ContractPartySend_SSOReq? req)
    {
        _ = req;
        // Dòng 598-602: CheckGatewayAuthentication + header GwUserCode/GwPassword/FlagUserLogin/NetworkID/OrgID/Tid.
        //   CheckGatewayAuthentication ném "GatewayAuthenticateFailed" khi Gw* không khớp (BaseController.cs:864-872) —
        //   nhưng ở đích KHÔNG có hệ thống GW MasterServer ⇒ không port được, chỉ ghi nhận.
        // Dòng 605-738: strJson rỗng → throw "strJson is null"; flagUserLogin="1" → SSO AccountService/OrgService
        //   (dịch vụ ngoài); khác → CheckAuthorize (cũng gọi iNOS SSO). Sau đó:
        //   API_Get (dòng 655-659) → MasterServerService.WA_MstSv_Sys_User_Login
        //   WA_Mst_ColumnConfig_Get (dòng 660) → TClientService.Mst_ColumnConfigService.WA_Mst_ColumnConfig_Get
        //   ContractData_Get (dòng 671-678) → TClientService.Contract_ContractService.WA_Contract_Contract_Get
        //   SendChannel (dòng 717) → WA_Mst_Channel_Get / WA_Mst_SubmissionForm_Get / WA_Mst_ContractTypeDtl_Get
        //     + EmailChannel/ZaloChannel/SMSChannel (Mailgate/OS_idocNet_SMS_V10/ZaloOA)
        // Toàn bộ là dịch vụ ngoài; không có tầng Biz.Web, không có hàm biz local, không có mã TError ⇒ GAP.NoBackend.
        return PortResult.Fail("Services_ContractPartySend_SSO", "GAP.NoBackend",
            "CheckGatewayAuthentication+CheckAuthorize+API_Get+WA_Mst_ColumnConfig_Get+WA_Contract_Contract_Get+WA_Mst_Channel_Get+WA_Mst_SubmissionForm_Get+WA_Mst_ContractTypeDtl_Get+SendChannel",
            "ServicesController.cs:598-602,653,655-659,660,671-678,717,1206-1612 (dịch vụ ngoài)");
    }

    // =====================================================================================
    // #258 — ServicesController.ContractSend (ServicesController.cs:759-926)
    // =====================================================================================

    /// <summary>
    /// Tham số của gate #258 (ServicesController.cs:759): `strJson` chứa TModelsUI.RQ_Contract_ContractUI với
    /// Lst_Contract_Contract (mỗi item ContractCode), FlagSendAll ("1" gửi tất cả người ký, khác "1"/null chỉ gửi
    /// người ký có FlagSendUser="0" — ServicesController.cs:3163-3174), NetworkID, WAUserCode, WAUserPassword
    /// (chỉ dùng khi header FlagUserLogin="1" — ServicesController.cs:781-785).
    /// Gate đọc thêm header FlagUserLogin / NetworkID / OrgID / Tid / GwUserCode / GwPassword (CheckGatewayAuthentication).
    /// Không port được phần SSO + tra cứu hợp đồng + gửi kênh (dịch vụ ngoài) ⇒ đích chỉ nhận đúng khung req để trả GAP.NoBackend.
    /// </summary>
    public class Services_ContractSendReq
    {
        public string? FlagSendAll { get; set; }
        public string? NetworkID { get; set; }
        public string? WAUserCode { get; set; }
        public string? WAUserPassword { get; set; }
        public List<Services_ContractPartySendContract>? Lst_Contract_Contract { get; set; }
    }

    // Nguồn ServicesController.cs:759-926 — POST /api/services/contractsend
    [HttpPost("contractsend")]
    public IActionResult ContractSend(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Services_ContractSendReq? req)
    {
        _ = req;
        // Dòng 776-912 — chuỗi nghiệp vụ yêu cầu dịch vụ ngoài để lấy dữ liệu:
        //   - 781-820  flagUserLogin="1": SSO AccountService.RequestToken + GetCurrentUser + OrgService.GetMyOrgUserList
        //              (dịch vụ ngoài iNOS) — sai/không token → throw "Login fail"
        //   - 823        flagUserLogin≠"1": CheckAuthorize (BaseController.cs:874-907, cũng gọi iNOS SSO) →
        //              throw "invalid_user - user not found" khi orgUser==null
        //   - 825-831   API_Get → MasterServerService.Instance.WA_MstSv_Sys_User_Login (dịch vụ ngoài)
        //   - 830-831   WA_Mst_ColumnConfig_Get → TClientService.Mst_ColumnConfigService.WA_Mst_ColumnConfig_Get (dịch vụ ngoài)
        //   - 833-838   gom ContractCode đầu vào; 907 → throw "Lst_Contract_Contract_Input is null" khi rỗng
        //   - 841-848   ContractData_Get → TClientService.Contract_ContractService.WA_Contract_Contract_Get (dịch vụ ngoài)
        //               rỗng ⇒ throw "ContractCode not found" (899-902); còn throw "ContractCode not found" (893-897)
        //   - 856-857   RT_Contract_Contract_Return_CheckUserToSend (ServicesController.cs:3141-3196) lọc theo FlagSendAll/
        //               FlagSendUser — chỉ trên dữ liệu API trả về, không tự sinh được
        //   - 873-889   mỗi hợp đồng gọi SendChannel (ServicesController.cs:1206-1612):
        //                 WA_Mst_Channel_Get + WA_Mst_SubmissionForm_Get + WA_Mst_ContractTypeDtl_Get (dịch vụ ngoài)
        //                 + EmailChannel / ZaloChannel / SMSChannel (Mailgate / OS_idocNet_SMS_V10 / ZaloOA — dịch vụ ngoài)
        //   - 912       strJson rỗng → throw "strJson is null"
        // Không có mã TError.* (mọi chỗ throw new Exception → gate SetFailed().AddException).
        // Không có tầng Biz.Web, không có hàm biz local, không có DDL nội bộ ⇒ GAP.NoBackend (P3 + luật B.7).
        return PortResult.Fail("Services_ContractSend", "GAP.NoBackend",
            "CheckGatewayAuthentication+CheckAuthorize/SSO+API_Get+WA_Mst_ColumnConfig_Get+WA_Contract_Contract_Get+WA_Mst_Channel_Get+WA_Mst_SubmissionForm_Get+WA_Mst_ContractTypeDtl_Get+SendChannel",
            "ServicesController.cs:769,781-820,823,825-831,830-831,841-848,856-857,873-889,907,912,1206-1612,3141-3196 (dịch vụ ngoài)");
    }
}
