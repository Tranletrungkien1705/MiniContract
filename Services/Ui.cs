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
}
