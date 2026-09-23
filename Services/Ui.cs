using MiniContract.Models;

namespace MiniContract.Services;

/// <summary>Helper hiển thị: nhãn + màu badge cho trạng thái/vai trò/phương thức ký.</summary>
public static class Ui
{
    public static (string text, string css) Status(ContractStatus s) => s switch
    {
        ContractStatus.Draft => ("Nháp", "secondary"),
        ContractStatus.Sent => ("Đã gửi ký", "info"),
        ContractStatus.PartiallySigned => ("Ký một phần", "warning"),
        ContractStatus.Completed => ("Hoàn tất", "success"),
        ContractStatus.Cancelled => ("Đã hủy", "dark"),
        ContractStatus.Finished => ("Đã kết thúc", "secondary"),
        _ => (s.ToString(), "secondary")
    };

    public static string Role(PartyRole r) => r switch
    {
        PartyRole.PartyA => "Bên A",
        PartyRole.PartyB => "Bên B",
        PartyRole.Witness => "Người làm chứng",
        _ => r.ToString()
    };

    public static string Method(SignMethod m) => m switch
    {
        SignMethod.DigitalCertificate => "Ký số (CKS)",
        SignMethod.Otp => "OTP",
        _ => m.ToString()
    };

    // Nhãn + màu cho trạng thái kiểm tra hợp đồng (checker).
    public static (string text, string css) CheckStatus(CheckerStatus s) => s switch
    {
        CheckerStatus.None => ("Không kiểm tra", "secondary"),
        CheckerStatus.Pending => ("Chờ kiểm tra", "warning"),
        CheckerStatus.OnProcess => ("Đã kiểm tra", "success"),
        _ => (s.ToString(), "secondary")
    };

    // Nhãn + màu cho loại lý do kết thúc hợp đồng (FinishType).
    public static (string text, string css) FinishType(Models.FinishType t) => t switch
    {
        Models.FinishType.Finished => ("Kết thúc", "success"),
        Models.FinishType.Stopped => ("Chấm dứt", "danger"),
        _ => (t.ToString(), "secondary")
    };

    // Nhãn + màu cho loại thao tác trong nhật ký (audit trail).
    public static (string text, string css, string icon) Action(HistoryAction a) => a switch
    {
        HistoryAction.Created => ("Tạo", "secondary", "bi-file-earmark-plus"),
        HistoryAction.Sent => ("Gửi ký", "info", "bi-send"),
        HistoryAction.Signed => ("Ký", "primary", "bi-pen"),
        HistoryAction.Completed => ("Hoàn tất", "success", "bi-check-circle"),
        HistoryAction.Cancelled => ("Hủy", "dark", "bi-x-circle"),
        HistoryAction.Remark => ("Ghi chú", "warning", "bi-chat-left-text"),
        _ => (a.ToString(), "secondary", "bi-dot")
    };
}
