using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace MiniContract.Services;

/// <summary>
/// Ký số phía server (CKS) cho hợp đồng bằng XML-DSig RSA-SHA256 (chứng thư tự ký demo — thay bằng USB/HSM thật ở production).
/// Trả về giá trị chữ ký (base64) + subject chứng thư để lưu bằng chứng ký.
/// </summary>
public interface ISignatureService
{
    (string signatureValue, string certSubject) SignContract(int contractId, string title, string body, string signerName);
    bool Verify(int contractId, string title, string body, string signerName, string signatureValueBase64);
}

public sealed class SignatureService : ISignatureService
{
    // Chứng thư demo tự ký (dùng chung, tạo 1 lần). Production: nạp từ USB token/HSM/CertificatePath.
    private static readonly Lazy<X509Certificate2> _cert = new(() =>
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=MiniContract Demo CA, O=idocNet Labs, C=VN", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(3));
    });

    public (string signatureValue, string certSubject) SignContract(int contractId, string title, string body, string signerName)
    {
        var doc = BuildDoc(contractId, title, body, signerName);
        var cert = _cert.Value;
        using var key = cert.GetRSAPrivateKey()!;

        var signedXml = new SignedXml(doc) { SigningKey = key };
        signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
        signedXml.AddReference(reference);
        signedXml.KeyInfo = new KeyInfo();
        signedXml.KeyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.ComputeSignature();

        var sigValue = Convert.ToBase64String(signedXml.Signature.SignatureValue);
        return (sigValue, cert.Subject);
    }

    public bool Verify(int contractId, string title, string body, string signerName, string signatureValueBase64)
    {
        try
        {
            var doc = BuildDoc(contractId, title, body, signerName);
            var cert = _cert.Value;
            using var key = cert.GetRSAPrivateKey()!;
            var signedXml = new SignedXml(doc) { SigningKey = key };
            signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            var reference = new Reference { Uri = "", DigestMethod = SignedXml.XmlDsigSHA256Url };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();
            var recomputed = Convert.ToBase64String(signedXml.Signature.SignatureValue);
            return recomputed == signatureValueBase64;
        }
        catch { return false; }
    }

    private static XmlDocument BuildDoc(int contractId, string title, string body, string signerName)
    {
        var xml = new StringBuilder();
        xml.Append("<Contract>");
        xml.Append($"<Id>{contractId}</Id>");
        xml.Append($"<Title>{Esc(title)}</Title>");
        xml.Append($"<Body>{Esc(body)}</Body>");
        xml.Append($"<Signer>{Esc(signerName)}</Signer>");
        xml.Append("</Contract>");
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.LoadXml(xml.ToString());
        return doc;
    }

    private static string Esc(string s) => System.Security.SecurityElement.Escape(s ?? "");
}

/// <summary>Sinh & xác thực OTP ký hợp đồng (demo: lưu in-memory theo party). Production: gửi SMS/Zalo/Email.</summary>
public sealed class OtpService
{
    private readonly Dictionary<int, (string code, DateTime exp)> _store = new();
    private readonly object _lock = new();

    public string Generate(int partyId)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        lock (_lock) _store[partyId] = (code, DateTime.UtcNow.AddMinutes(5));
        return code;   // demo: trả thẳng ra UI (thực tế gửi SMS/Zalo)
    }

    public bool Verify(int partyId, string code)
    {
        lock (_lock)
        {
            if (!_store.TryGetValue(partyId, out var v)) return false;
            if (DateTime.UtcNow > v.exp) { _store.Remove(partyId); return false; }
            var ok = v.code == code?.Trim();
            if (ok) _store.Remove(partyId);
            return ok;
        }
    }
}
