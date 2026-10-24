using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MiniContract.Data;

namespace MiniContract.Controllers.Port;

// port 1:1 ServicesController.cs — ContractPartySend (gate: ServicesController.ContractPartySend)
// Nguồn (source_excerpt.txt hạng mục #256):
//   Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\ServicesController.cs:417-585
//   Gate gọi service: WA_Mst_ColumnConfig_Get (dòng 489), WA_Contract_Contract_Get (dòng 1123),
//     WA_Mst_Channel_Get (dòng 1266), WA_Mst_SubmissionForm_Get (dòng 1385), WA_Mst_ContractTypeDtl_Get (dòng 449).
//     Toàn bộ các lệnh trên đi qua TClientService.*.Instance.WA_* → dịch vụ API từ xa (URL động strAPI lấy từ MasterServer).
//   Gửi Email/SMS/Zalo: SendChannel -> EmailChannel/ZaloChannel/SMSChannel (gate helper ServicesController.cs:1206-1612)
//     gọi Mailgate / OS_idocNet_SMS_V10 / ZaloOA — MiniContract KHÔNG có các dịch vụ này.
//   API:  excerpt dòng 1617: "API: khong thay controller ServicesController.cs trong ...Biz.Web\Controllers" ⇒ KHÔNG có tầng API.
//   BIZ:  KHÔNG có — dịch vụ WA_* ở trên đều là ClientService (gọi HTTP sang hệ thống API động), không có hàm biz/DDL nội bộ.
//   Mã lỗi: excerpt dòng 1620-1622: 7 chỗ ném lỗi (throw new Exception) nhưng KHÔNG có mã TError.* (danh sách mã lỗi RỖNG).
// ⇒ Toàn bộ dữ liệu + hành vi nằm ở dịch vụ ngoài (iNOS SSO, MasterServer, ClientService API động, Mailgate/SMS/Zalo).
//   Theo đề bài P3 + luật B.7: DỪNG, KHÔNG tự chế bảng/kết quả/giá trị; endpoint trả GAP.NoBackend.
//   Xem .parity-run\conflicts\256_conflict.md (đã có, mô tả chi tiết luồng gọi + đề xuất BỎ).
[ApiController]
[Route("api/services")]
public class ServicesControllerPort : ControllerBase
{
    /// <summary>
    /// Tham số của gate (ServicesController.cs:417): `strJson` (chuỗi JSON) chứa TModelsUI.RQ_Contract_ContractUI
    /// có Lst_Contract_Contract (danh sách hợp đồng cần gửi), FlagSendPartyCreate, WAUserCode/WAUserPassword (dùng khi
    /// flagUserLogin="1" để đăng nhập SSO ngoài), NetworkID. Vì không port được phần gửi (dịch vụ ngoài),
    /// đích chỉ nhận đúng khung req để trả GAP.NoBackend — KHÔNG validate/thực thi nghiệp vụ.
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
        // Không có API/Biz nội bộ để port; không có bảng CSDL nội bộ nào để khai DDL ⇒ trả GAP.NoBackend.
        return PortResult.Fail("Services_ContractPartySend", "GAP.NoBackend",
            "WA_Contract_Contract_Get+WA_Mst_Channel_Get+WA_Mst_SubmissionForm_Get+WA_Mst_ContractTypeDtl_Get+SendChannel",
            "ServicesController.cs:489,1123,1266,1385,449,1206-1612 (dịch vụ ngoài)");
    }
}
