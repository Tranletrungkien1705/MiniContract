namespace MiniContract.Models;

// ── Multi-tenant ─────────────────────────────────────────────────────
/// <summary>Tổ chức/khách hàng thuê bao (multi-tenant). Mỗi Org dữ liệu hợp đồng riêng, cô lập.</summary>
public class Org
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Bảng dữ liệu thuộc về 1 Org — bị lọc theo tenant hiện tại + tự đóng dấu OrgId khi tạo.</summary>
public interface IOrgOwned { Guid OrgId { get; set; } }

// ── Enums ────────────────────────────────────────────────────────────
public enum ContractStatus
{
    Draft = 0,           // nháp
    Sent = 1,            // đã gửi các bên để ký
    PartiallySigned = 2, // một số bên đã ký
    Completed = 3,       // đủ chữ ký → hoàn tất
    Cancelled = 4        // đã hủy
}

public enum PartyRole { PartyA = 0, PartyB = 1, Witness = 2 }   // Bên A / Bên B / Người làm chứng

public enum SignMethod { DigitalCertificate = 0, Otp = 1 }     // Ký số CKS / Ký qua OTP

/// <summary>Loại thao tác ghi vào lịch sử (audit trail) của hợp đồng — port từ Contract_Contract_HistAction (QContract).</summary>
public enum HistoryAction
{
    Created = 0,     // tạo hợp đồng
    Sent = 1,        // gửi các bên ký
    Signed = 2,      // một bên ký (CKS/OTP)
    Completed = 3,   // đủ chữ ký → hoàn tất
    Cancelled = 4,   // hủy hợp đồng
    Remark = 5       // ghi chú/ghi chú xử lý
}

// ── Danh mục loại hợp đồng ───────────────────────────────────────────
public class ContractType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? BodyTemplate { get; set; }   // mẫu nội dung mặc định
}

// ── Hợp đồng ─────────────────────────────────────────────────────────
public class Contract : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int? TypeId { get; set; }
    public string Body { get; set; } = "";       // nội dung hợp đồng
    public decimal Value { get; set; }           // giá trị hợp đồng
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public string CreatedBy { get; set; } = "";
    public string? Note { get; set; }

    // ── Phụ lục hợp đồng (annex) ─────────────────────
    // Nguồn QContract: FlagContractAnnex (1 = phụ lục, 0 = hợp đồng) + ContractRefNo (số HĐ cha).
    public bool IsAnnex { get; set; }                       // true = đây là phụ lục của 1 hợp đồng gốc
    public int? ParentContractId { get; set; }              // FK tới hợp đồng gốc (null nếu là hợp đồng)
    public string? ParentContractCode { get; set; }         // số HĐ gốc (ContractRefNo) — lưu để tra cứu nhanh

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ContractType? Type { get; set; }
    public Contract? Parent { get; set; }
    public List<Contract> Annexes { get; set; } = [];       // các phụ lục của hợp đồng này
    public List<ContractParty> Parties { get; set; } = [];
    public List<ContractSignature> Signatures { get; set; } = [];
    public List<ContractHistory> History { get; set; } = [];

    // ── tính toán ────────────────────────────────────────────────────
    public bool IsOpen => Status is not (ContractStatus.Completed or ContractStatus.Cancelled);
    public int SignedCount => Parties.Count(p => p.HasSigned);
    public string Kind => IsAnnex ? "Phụ lục" : "Hợp đồng";
}

// ── Các bên tham gia ─────────────────────────────────────────────────
public class ContractParty : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public string Name { get; set; } = "";
    public string? TaxCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public PartyRole Role { get; set; } = PartyRole.PartyB;
    public int SignOrder { get; set; } = 1;
    public bool HasSigned { get; set; }
    public DateTime? SignedAt { get; set; }

    public Contract Contract { get; set; } = null!;
}

// ── Chữ ký (CKS / OTP) ───────────────────────────────────────────────
public class ContractSignature : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int PartyId { get; set; }
    public SignMethod Method { get; set; }
    public string SignerName { get; set; } = "";
    public string? CertSubject { get; set; }        // subject chứng thư (CKS)
    public string? SignatureValue { get; set; }     // giá trị chữ ký (base64) / bằng chứng OTP
    public DateTime SignedAt { get; set; } = DateTime.Now;
}

// ── Lịch sử thao tác (audit trail) ───────────────────────────────────
/// <summary>
/// Nhật ký mọi thao tác trên hợp đồng (tạo/gửi/ký/hoàn tất/hủy/ghi chú) — bất biến, chỉ ghi thêm.
/// Port từ nghiệp vụ Contract_Contract_HistAction của QContract: mỗi bản ghi lưu người thực hiện,
/// loại thao tác, mô tả và ghi chú để dựng "vòng đời" hợp đồng có thể kiểm toán.
/// </summary>
public class ContractHistory : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public HistoryAction Action { get; set; }
    public string Actor { get; set; } = "";        // người thực hiện (user/api/web)
    public string Description { get; set; } = "";  // mô tả thao tác
    public string? Remark { get; set; }            // ghi chú kèm theo
    public DateTime At { get; set; } = DateTime.Now;

    public Contract Contract { get; set; } = null!;
}
