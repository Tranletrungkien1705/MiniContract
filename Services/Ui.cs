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

    /// <summary>Nhãn trạng thái thực hiện — port 1:1 nhãn nguồn Contract_Contract_Info.cshtml:672-679.</summary>
    public static string Eff(EffStatus s) => s switch
    {
        EffStatus.Processing => "Đang thực hiện",
        EffStatus.Finished => "Đã kết thúc",
        _ => s.ToString()
    };

    /// <summary>Nhãn kênh gửi — port 1:1 tên nguồn EMAIL/SMS/ZALO (BindData_Contract_SendHist.cshtml:11-28).</summary>
    public static string Channel(ChannelKind k) => k switch
    {
        ChannelKind.Email => "EMAIL",
        ChannelKind.Sms => "SMS",
        ChannelKind.Zalo => "ZALO",
        _ => k.ToString()
    };

    /// <summary>Nhãn loại bản tin — port 1:1 nhãn nguồn "Gửi HĐ/TL" và "OTP" (BindData_Contract_SendHist.cshtml:29-41).</summary>
    public static string Bulletin(BulletinKind k) => k switch
    {
        BulletinKind.Contract => "Gửi HĐ/TL",
        BulletinKind.Otp => "OTP",
        _ => k.ToString()
    };
}
