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
    Cancelled = 4,       // đã hủy
    Finished = 5         // đã kết thúc/chấm dứt (theo lý do kết thúc)
}

/// <summary>
/// Loại lý do kết thúc hợp đồng — port từ TConst.ContractFinishType (QContract):
/// FINISHED = kết thúc (hoàn thành), STOPPED = chấm dứt (dừng trước hạn).
/// </summary>
public enum FinishType { Finished = 0, Stopped = 1 }

public enum PartyRole { PartyA = 0, PartyB = 1, Witness = 2 }   // Bên A / Bên B / Người làm chứng

public enum SignMethod { DigitalCertificate = 0, Otp = 1 }     // Ký số CKS / Ký qua OTP

/// <summary>Loại thao tác ghi vào lịch sử hợp đồng (theo FunctionActionType của QContract).</summary>
public enum HistoryActionType
{
    Create = 0,     // tạo hợp đồng
    Send = 1,       // gửi các bên ký
    Sign = 2,       // một bên ký (CKS/OTP)
    Cancel = 3,     // hủy hợp đồng
    Complete = 4,   // đủ chữ ký → hoàn tất
    Update = 5      // cập nhật nội dung/khác
}

/// <summary>Loại thao tác ghi vào lịch sử (audit trail) của hợp đồng — port từ Contract_Contract_HistAction (QContract).</summary>
public enum HistoryAction
{
    Created = 0,     // tạo hợp đồng
    Sent = 1,        // gửi các bên ký
    Signed = 2,      // một bên ký (CKS/OTP)
    Completed = 3,   // đủ chữ ký → hoàn tất
    Cancelled = 4,   // hủy hợp đồng
    Remark = 5,      // ghi chú/ghi chú xử lý
    SignLinkCreated = 6,  // tạo link ký công khai
    SignLinkRevoked = 7,  // thu hồi link ký công khai
    Approved = 8,    // phê duyệt hợp đồng (Contract_Contract_Approved)
    PartyCancelled = 9    // một bên hủy hợp đồng (Contract_ContractParty_Cancel)
}

/// <summary>Trạng thái hiệu lực của link ký công khai (tính từ thời điểm hết hạn + cờ thu hồi).</summary>
public enum SignLinkState { Active = 0, Expired = 1, Revoked = 2 }

/// <summary>
/// Kênh gửi hợp đồng cho các bên — port từ TConst.ChannelType / Client_Mst_ChannelType (QContract):
/// EMAIL = gửi email, SMS = tin nhắn, ZALO = Zalo ZNS.
/// </summary>
public enum ChannelType { Email = 0, Sms = 1, Zalo = 2 }

/// <summary>
/// Loại bản tin gửi — port từ TConst.Client_BulletinType (QContract):
/// CONTRACT = gửi hợp đồng để ký, OTP = gửi mã OTP xác thực.
/// </summary>
public enum BulletinType { Contract = 0, Otp = 1 }

/// <summary>
/// Loại ô ký trên hợp đồng — port từ TConst.ElementType (QContract):
/// ELECTRONIC = ký điện tử, SHORT = ký tắt, DIGITAL = ký số.
/// </summary>
public enum ElementType { Electronic = 0, Short = 1, Digital = 2 }

/// <summary>
/// Trạng thái kiểm tra (checker) của hợp đồng — port từ TConst.ContractStatus (QContract):
/// PENDING = chờ kiểm tra, ONPROCESS = đã kiểm tra xong (đủ người kiểm tra), NONE = không có người kiểm tra.
/// </summary>
public enum CheckerStatus { None = 0, Pending = 1, OnProcess = 2 }

/// <summary>
/// Kiểu ghép tiền tố/hậu tố khi sinh số hợp đồng — port từ TConst.Mst_Typefix (QContract):
/// PREFIX = tiền tố đứng trước số (VD: HD-0001), POSTFIX = hậu tố đứng sau số (VD: 0001/2026).
/// </summary>
public enum Typefix { Prefix = 0, Postfix = 1 }

/// <summary>
/// Trạng thái của một bên trong hợp đồng — port từ TConst.ContractPartyStatus (QContract):
/// ONPROCESS = đang xử lý, PENDING = chờ, APPROVED = đã duyệt, CANCELED = đã hủy,
/// CONFIRMED = đã xác nhận, FINISHED = đã kết thúc.
/// </summary>
public enum PartyStatus { OnProcess = 0, Pending = 1, Approved = 2, Cancelled = 3, Confirmed = 4, Finished = 5 }

// ── Danh mục loại hợp đồng ───────────────────────────────────────────
public class ContractType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? BodyTemplate { get; set; }   // mẫu nội dung mặc định

    public ContractNumberRule? NumberRule { get; set; }   // quy tắc đánh số của loại này (nếu có)
}

// ── Hợp đồng mẫu (Contract_TempContract) ─────────────────────────────
/// <summary>
/// Hợp đồng mẫu (template) — port từ Contract_TempContract (QContract).
/// Mẫu dùng để soạn nhanh hợp đồng mới: chọn mẫu → copy loại + nội dung mẫu.
/// Luật cốt lõi (Contract_TempContract_SaveX / _CheckDB):
///  - TContractCode (mã mẫu) bắt buộc;
///  - TContracName (tên mẫu) bắt buộc và KHÔNG trùng trong cùng Org;
///  - ContractType phải tồn tại và đang hiệu lực;
///  - Không xóa mẫu đang được hợp đồng sử dụng (Contract_Contract.TContractCode).
/// </summary>
public class ContractTemplate : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";            // TContractCode — mã mẫu
    public string Name { get; set; } = "";            // TContracName — tên mẫu
    public int? TypeId { get; set; }                   // ContractType — loại hợp đồng áp dụng
    public string Body { get; set; } = "";            // TContractBody — nội dung mẫu
    public string? Remark { get; set; }                // Remark
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // CreateDTimeUTC
    public string CreatedBy { get; set; } = "";        // CreateBy
    public ContractType? Type { get; set; }

    // ── tính toán ────────────────────
    public string TypeName => Type?.Name ?? "—";
}

// ── Nhóm hợp đồng mẫu (Contract_TempGroup) ───────────────────────────
/// <summary>
/// Nhóm hợp đồng mẫu — port từ Contract_TempGroup (QContract).
/// Một nhóm gom nhiều hợp đồng mẫu cùng chủ đề (VD: nhóm "Hợp đồng thương mại")
/// và mang các thuộc tính dùng chung (Contract_Attribute_Group) để điền sẵn khi soạn.
/// Luật cốt lõi (Contract_TempGroup_CreateX / _UpdateX / _DeleteX / _CheckDB):
///  - ContractTGroupCode (mã nhóm) bắt buộc và KHÔNG trùng khi tạo;
///  - khi sửa/xóa, mã nhóm phải tồn tại;
///  - mỗi thuộc tính phải có AttributeContractCode + AttributeValue (không rỗng);
///  - xóa nhóm thì xóa kèm toàn bộ thuộc tính của nhóm (delete all attributes).
/// </summary>
public class ContractTemplateGroup : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";            // ContractTGroupCode — mã nhóm
    public string Name { get; set; } = "";            // ContracTGroupName — tên nhóm
    public string? Body { get; set; }                  // ContractTGroupBody — nội dung chung của nhóm
    public string? ContractName { get; set; }          // ContractName — tên hợp đồng áp dụng
    public string? Remark { get; set; }                // Remark
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy
    public List<ContractAttributeGroup> Attributes { get; set; } = [];  // thuộc tính dùng chung của nhóm
    // ── tính toán ────────────────────
    public int AttributeCount => Attributes.Count;
}

// ── Thuộc tính nhóm hợp đồng mẫu (Contract_Attribute_Group) ──────────
/// <summary>
/// Thuộc tính (key-value) của một nhóm hợp đồng mẫu — port từ Contract_Attribute_Group (QContract).
/// Mỗi dòng gắn 1 nhóm (ContractTGroupCode) với 1 mã thuộc tính (AttributeContractCode)
/// và giá trị (AttributeValue). Luật cốt lõi: cả mã thuộc tính lẫn giá trị đều bắt buộc.
/// </summary>
public class ContractAttributeGroup : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int GroupId { get; set; }                   // FK tới nhóm hợp đồng mẫu
    public string AttributeCode { get; set; } = "";    // AttributeContractCode — mã thuộc tính
    public string AttributeValue { get; set; } = "";   // AttributeValue — giá trị thuộc tính
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy
    public ContractTemplateGroup Group { get; set; } = null!;
}

// ── Quy tắc đánh số hợp đồng theo loại (Mst_ContractTypeContractNo) ──
/// <summary>
/// Quy tắc sinh số hợp đồng cho một loại hợp đồng — port từ Mst_ContractTypeContractNo (QContract).
/// Mỗi loại có: kiểu ghép (TypefixCode: PREFIX/POSTFIX), chuỗi tiền/hậu tố (TypefixInput),
/// độ dài số (SeqNumberLength — zero-pad) và số bắt đầu (NumberStart).
/// Nguồn QContract: Seq_ContractNo_Get → Seq_ContractNo_GetX (ghép {TypefixInput}{số} hoặc {số}{TypefixInput})
/// + Contract_Contract_CountByContractType_Get (đếm hợp đồng theo loại để lấy số kế tiếp).
/// </summary>
public class ContractNumberRule : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int TypeId { get; set; }                 // ContractType — loại hợp đồng áp dụng
    public Typefix TypefixCode { get; set; } = Typefix.Prefix;  // TypefixCode
    public string TypefixInput { get; set; } = "";  // TypefixInput — tiền/hậu tố (VD: "HD-")
    public int SeqNumberLength { get; set; } = 4;   // SeqNumberLength — độ dài số (zero-pad)
    public int NumberStart { get; set; } = 1;       // NumberStart — số bắt đầu
    public bool Active { get; set; } = true;        // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ContractType? Type { get; set; }

    // ── tính toán ────────────────────────────────────────────────────
    public string TypefixLabel => TypefixCode == Typefix.Postfix ? "Hậu tố" : "Tiền tố";

    /// <summary>Sinh 1 số hợp đồng từ số thứ tự — port từ Seq_ContractNo_GetX + To10Mask (QContract).</summary>
    public string Build(long number)
    {
        var len = SeqNumberLength <= 0 ? 1 : SeqNumberLength;
        var padded = number.ToString().PadLeft(len, '0');
        return TypefixCode == Typefix.Postfix ? $"{padded}{TypefixInput}" : $"{TypefixInput}{padded}";
    }
}

// ── Danh mục lý do kết thúc hợp đồng (Mst_FinishedContractReason) ────
/// <summary>
/// Danh mục lý do kết thúc/chấm dứt hợp đồng — port từ Mst_FinishedContractReason (QContract).
/// Mỗi lý do có mã (ContractFinishReasonCode), tên (ContractFinishReasonName), loại
/// (FinishType: FINISHED/STOPPED), mô tả (FinishDescription) và cờ hiệu lực (FlagActive).
/// Nguồn QContract: Mst_FinishedContractReason_CreateX / _UpdateX / _CheckDB.
/// </summary>
public class FinishedContractReason : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // ContractFinishReasonCode
    public string Name { get; set; } = "";          // ContractFinishReasonName
    public FinishType Type { get; set; } = FinishType.Finished;  // FinishType
    public string? Description { get; set; }         // FinishDescription
    public bool Active { get; set; } = true;         // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ── tính toán ────────────────────────────────────────────────────
    public string TypeLabel => Type == FinishType.Stopped ? "Chấm dứt" : "Kết thúc";
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
    // ── Hợp đồng mẫu (Contract_TempContract) ─────────
    // Nguồn QContract: Contract_Contract.TContractCode — hợp đồng được soạn từ mẫu nào.
    public int? TemplateId { get; set; }                    // FK tới hợp đồng mẫu (null nếu soạn tay)
    public string? TemplateCode { get; set; }               // TContractCode — mã mẫu đã dùng

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ContractType? Type { get; set; }
    public Contract? Parent { get; set; }
    public ContractTemplate? Template { get; set; }         // hợp đồng mẫu đã dùng để soạn
    public List<Contract> Annexes { get; set; } = [];       // các phụ lục của hợp đồng này
    public List<ContractParty> Parties { get; set; } = [];
    public List<ContractSignature> Signatures { get; set; } = [];
    public List<ContractHistory> History { get; set; } = [];
    public List<ContractSignLink> SignLinks { get; set; } = [];
    public List<ContractElement> Elements { get; set; } = [];   // các ô ký trên bản thể hiện
    public List<ContractChecker> Checkers { get; set; } = [];   // người kiểm tra hợp đồng (theo thứ tự)
    public List<ContractUserInContract> UserAssignments { get; set; } = [];  // người dùng được phân quyền
    public List<ContractSendHist> SendHistory { get; set; } = [];   // lịch sử gửi cho các bên

    // ── Kiểm tra hợp đồng (checker) ──────────────────
    // Nguồn QContract: Contract_Checker + ContractStatus.PENDING/ONPROCESS.
    public CheckerStatus CheckerStatus { get; set; } = CheckerStatus.None;  // trạng thái kiểm tra

    // ── Kết thúc hợp đồng (lý do kết thúc) ───────────
    // Nguồn QContract: Contract_ContractParty_FinishX — khi kết thúc, hợp đồng ghi nhận
    // lý do (ContractFinishReasonCode/Name), mô tả (FinishDescription) và thời điểm (FinishedDTimeUTC).
    public string? FinishReasonCode { get; set; }    // ContractFinishReasonCode
    public string? FinishReasonName { get; set; }    // ContractFinishReasonName
    public string? FinishDescription { get; set; }   // FinishDescription
    public DateTime? FinishedAt { get; set; }        // FinishedDTimeUTC

    // ── Phê duyệt hợp đồng (approval) ────────────────
    // Nguồn QContract: Contract_Contract_ApprovedX — khi người kiểm tra phê duyệt,
    // hợp đồng ghi nhận thời điểm (ApprDTimeUTC) + người duyệt (ApprBy).
    public DateTime? ApprovedAt { get; set; }        // ApprDTimeUTC
    public string? ApprovedBy { get; set; }          // ApprBy

    // ── tính toán ────────────────────────────────────────────────────
    public bool IsOpen => Status is not (ContractStatus.Completed or ContractStatus.Cancelled or ContractStatus.Finished);
    public bool IsFinished => Status == ContractStatus.Finished;
    public bool IsApproved => ApprovedAt != null;
    public int SignedCount => Parties.Count(p => p.HasSigned);
    public string Kind => IsAnnex ? "Phụ lục" : "Hợp đồng";
    public int ElementSignedCount => Elements.Count(e => e.IsSigned);
    public int CheckedCount => Checkers.Count(c => c.HasChecked);
    public bool AllChecked => Checkers.Count > 0 && Checkers.All(c => c.HasChecked);
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

    // -- Huy hop dong boi mot ben (Contract_ContractParty_Cancel) --
    public PartyStatus Status { get; set; } = PartyStatus.OnProcess;  // ContractPartyStatus
    public DateTime? CancelledAt { get; set; }                       // CancelDTimeUTC
    public string? CancelledBy { get; set; }                         // CancelBy
    public string? Remark { get; set; }                              // Remark
    public Contract Contract { get; set; } = null!;

    // -- tinh toan --
    public bool IsCancelled => Status == PartyStatus.Cancelled;
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

// ── Link ký công khai (public signing link) ──────────────────────────
/// <summary>
/// Link ký công khai cho một bên của hợp đồng — port từ Contract_ContractSignLink (QContract).
/// Bên nhận link (qua Email/SMS) mở link để ký mà KHÔNG cần đăng nhập; link có thời hạn
/// (SignLinkEndDate) và có thể thu hồi. Nguồn QContract: WAS_Contract_ContractSignLink_Save /
/// _CheckLink (kiểm tra SignLinkEndDate >= now) / _GetSignLinkEndDate.
/// </summary>
public class ContractSignLink : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int PartyId { get; set; }                 // bên được phép ký qua link này
    public string Token { get; set; } = "";          // SignLink — chuỗi bí mật dùng trong URL
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; }            // SignLinkEndDate — hết hạn
    public bool Revoked { get; set; }                // FlagActive=0 → đã thu hồi
    public DateTime? UsedAt { get; set; }            // thời điểm link được dùng để ký

    public Contract Contract { get; set; } = null!;
    public ContractParty Party { get; set; } = null!;

    // ── tính toán ────────────────────────────────────────────────────
    public SignLinkState State => Revoked ? SignLinkState.Revoked
        : (DateTime.Now > EndDate ? SignLinkState.Expired : SignLinkState.Active);
    public bool IsUsable => State == SignLinkState.Active && UsedAt == null;
}

// ── Ô ký trên hợp đồng (Contract_ContractElement) ────────────────────
/// <summary>
/// Ô ký (signature field) đặt trên bản thể hiện hợp đồng — port từ Contract_ContractElement (QContract).
/// Mỗi ô thuộc 1 bên (PartyCode), có loại ký (ElementType: ELECTRONIC/SHORT/DIGITAL), tọa độ + kích thước
/// trên trang (PageIdx/ElementX/ElementY/ElementWidth/ElementHeight) và trạng thái đã ký hay chưa
/// (ElementSignStatus). Nguồn QContract: WAS_Contract_ContractElement_Update / _Calc.
/// </summary>
public class ContractElement : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int? PartyId { get; set; }               // bên sở hữu ô ký (PartyCode)
    public string ElementCode { get; set; } = "";   // mã tham số của ô
    public string ElementName { get; set; } = "";   // tên trường hiển thị
    public ElementType Type { get; set; } = ElementType.Electronic;

    // ── vị trí trên trang ────────────────────────────────────────────
    public int PageIdx { get; set; } = 1;           // số trang
    public double ElementX { get; set; }            // tọa độ X
    public double ElementY { get; set; }            // tọa độ Y
    public double ElementWidth { get; set; }        // chiều rộng ô ký
    public double ElementHeight { get; set; }       // chiều cao ô ký

    // ── trạng thái ký ────────────────────────────────────────────────
    public bool IsSigned { get; set; }              // ElementSignStatus: 0 chưa ký, 1 đã ký
    public string? SignerName { get; set; }         // ConfirmBy — người ký
    public DateTime? SignedAt { get; set; }         // ConfirmDTimeUTC — ngày ký
    public string? SignFrom { get; set; }           // ký từ: web / app / link token
    public string? ElementIP { get; set; }          // địa chỉ IP khi ký

    public Contract Contract { get; set; } = null!;
    public ContractParty? Party { get; set; }

    // ── tính toán ────────────────────────────────────────────────────
    public string TypeLabel => Type switch
    {
        ElementType.Electronic => "Ký điện tử",
        ElementType.Short => "Ký tắt",
        ElementType.Digital => "Ký số",
        _ => Type.ToString()
    };
}

// ── Người kiểm tra hợp đồng (Contract_Checker) ───────────────────────
/// <summary>
/// Người kiểm tra (checker) của hợp đồng — port từ Contract_Checker (QContract).
/// Mỗi hợp đồng có danh sách người kiểm tra theo thứ tự (Idx); khi FlagSeq=1 phải kiểm tra
/// tuần tự theo Idx. FlagChecker=1 là người kiểm tra, =0 là người ký. FlagCheck=1 là đã kiểm tra.
/// Nguồn QContract: WAS_Contract_Checker_Accept / Contract_Checker_CheckDB.
/// </summary>
public class ContractChecker : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public string UserCode { get; set; } = "";      // tên đăng nhập người kiểm tra
    public string UserName { get; set; } = "";      // tên hiển thị người kiểm tra
    public string? Position { get; set; }           // chức vụ người kiểm tra
    public int Idx { get; set; } = 1;               // thứ tự kiểm tra
    public bool IsChecker { get; set; } = true;     // FlagChecker: 1 = người kiểm tra, 0 = người ký
    public bool HasChecked { get; set; }            // FlagCheck: 1 = đã kiểm tra, 0 = chưa
    public bool Sequential { get; set; } = true;    // FlagSeq: 1 = kiểm tra theo thứ tự, 0 = không cần
    public string? Remark { get; set; }             // ghi chú khi kiểm tra
    public DateTime? CheckedAt { get; set; }        // CheckDTimeUTC — thời gian kiểm tra
    public DateTime? ApprovedAt { get; set; }       // ApprovedDTimeUTC — thời gian duyệt

    public Contract Contract { get; set; } = null!;

    // ── tính toán ────────────────────────────────────────────────────
    public string RoleLabel => IsChecker ? "Người kiểm tra" : "Người ký";
    public bool HasApproved => ApprovedAt != null;
}

// ── Phân quyền hợp đồng (Contract_UserInContract) ────────────────────
/// <summary>
/// Người dùng được phân quyền trên hợp đồng — port từ Contract_UserInContract (QContract).
/// Mỗi bản ghi gắn 1 người dùng (UserCode/UserName/EMail) vào 1 hợp đồng (ContractCode).
/// Nguồn QContract: WAS_Contract_UserInContract_Save → Contract_UserInContract_SaveX
/// (xoá toàn bộ phân quyền cũ của hợp đồng rồi ghi lại danh sách mới — full replace).
/// </summary>
public class ContractUserInContract : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public string UserCode { get; set; } = "";      // mã/tên đăng nhập người dùng
    public string UserName { get; set; } = "";      // tên hiển thị người dùng
    public string? Email { get; set; }              // EMail
    public DateTime AssignedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string AssignedBy { get; set; } = "";    // LogLUBy — người thực hiện phân quyền

    public Contract Contract { get; set; } = null!;
}

// ── Lịch sử gửi hợp đồng (Contract_SendHist) ─────────────────────────
/// <summary>
/// Lịch sử gửi hợp đồng cho các bên qua từng kênh — port từ Contract_SendHist (QContract).
/// Mỗi bản ghi lưu: hợp đồng, bên nhận (PartyCode), người nhận (UserName/UserToken),
/// thời điểm gửi (DTimeSend), kênh gửi (ChannelType: EMAIL/SMS/ZALO), loại bản tin
/// (BulletinType: CONTRACT/OTP) và thông tin nhận (InfoReceive: email/số điện thoại/Zalo).
/// Nguồn QContract: WAS_Contract_SendHist_Add → Contract_SendHist_SaveX (insert) +
/// WAS_Contract_SendHist_Get → Contract_SendHist_GetX (tra cứu theo ContractCode/BulletinType).
/// </summary>
public class ContractSendHist : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int? PartyId { get; set; }               // PartyCode — bên nhận
    public string PartyName { get; set; } = "";     // PartyName — tên bên nhận
    public string UserName { get; set; } = "";      // UserName — tên người nhận
    public string? UserToken { get; set; }          // UserToken — định danh người nhận
    public ChannelType Channel { get; set; } = ChannelType.Email;   // ChannelType
    public BulletinType Bulletin { get; set; } = BulletinType.Contract;  // BulletinType
    public string? InfoReceive { get; set; }        // InfoReceive — email/số ĐT/Zalo nhận
    public DateTime SentAt { get; set; } = DateTime.Now;  // DTimeSend
    public string? Remark { get; set; }             // Remark — ghi chú
    public string SentBy { get; set; } = "";        // LogLUBy — người thực hiện gửi
    public Contract Contract { get; set; } = null!;
    public ContractParty? Party { get; set; }

    // ── tính toán ────────────────────
    public string ChannelLabel => Channel switch
    {
        ChannelType.Email => "Email",
        ChannelType.Sms => "SMS",
        ChannelType.Zalo => "Zalo",
        _ => Channel.ToString()
    };
    public string BulletinLabel => Bulletin == BulletinType.Otp ? "OTP" : "Hợp đồng";
}

// ── Mã OTP xác thực ký hợp đồng (Contract_ContractVerifyOtp) ─────────
/// <summary>
/// Trạng thái hiệu lực của mã OTP (tính từ thời điểm hết hạn + cờ hiệu lực).
/// </summary>
public enum OtpState { Active = 0, Expired = 1, Inactive = 2 }

/// <summary>
/// Mã OTP xác thực khi ký hợp đồng — port từ Contract_ContractVerifyOtp (QContract).
/// Mỗi mã gắn với 1 hợp đồng (ContractCode) + 1 người ký (UserCodeSign), có thời hạn
/// (EndDate) và cờ hiệu lực (FlagActive). Luật cốt lõi (Contract_ContractVerifyOtp_SaveX):
/// khi sinh mã mới cho (hợp đồng, người ký) thì XOÁ toàn bộ mã cũ của cặp đó rồi ghi mã mới
/// (delete all + insert); khi xác thực, mã phải còn hiệu lực và chưa hết hạn (EndDate >= now).
/// Nguồn QContract: WAS_Contract_ContractVerifyOtp_Save → Contract_ContractVerifyOtp_SaveX
/// + Contract_ContractVerifyOtp_GetX (sinh mã ngẫu nhiên 6 ký tự hex, hạn 2 phút).
/// </summary>
public class ContractVerifyOtp : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }             // hợp đồng áp dụng
    public string ContractCode { get; set; } = "";  // ContractCode — số hợp đồng
    public string OtpCode { get; set; } = "";       // OtpCode — mã OTP (6 ký tự hex)
    public string UserCodeSign { get; set; } = "";  // UserCodeSign — người ký được xác thực
    public DateTime CreateDate { get; set; } = DateTime.Now;  // CreateDate
    public DateTime EndDate { get; set; }           // EndDate — hết hạn
    public bool Active { get; set; } = true;        // FlagActive
    public DateTime? UsedAt { get; set; }           // thời điểm mã được dùng để ký
    public string CreatedBy { get; set; } = "";     // LogLUBy — người sinh mã
    public Contract Contract { get; set; } = null!;

    // ── tính toán ────────────────────
    public OtpState State => !Active ? OtpState.Inactive
        : (DateTime.Now > EndDate ? OtpState.Expired : OtpState.Active);
    public bool IsUsable => State == OtpState.Active && UsedAt == null;
    public string StateLabel => State switch
    {
        OtpState.Active => "Còn hiệu lực",
        OtpState.Expired => "Hết hạn",
        OtpState.Inactive => "Đã vô hiệu",
        _ => State.ToString()
    };
}

// ── Chữ ký số của tổ chức (Mst_OrgCKS) ───────────────────────────────
/// <summary>
/// Trạng thái hiệu lực của chứng thư số (tính từ thời điểm hết hạn + cờ hiệu lực).
/// </summary>
public enum CertState { Active = 0, Expired = 1, NotYetValid = 2, Inactive = 3 }

/// <summary>
/// Chữ ký số (chứng thư số CA) của một tổ chức — port từ Mst_OrgCKS (QContract).
/// Mỗi bản ghi gắn 1 tổ chức (OrgID) với 1 chứng thư (CANumber) do một nhà cung cấp CA
/// (CAOrg) phát hành, có hiệu lực từ CAEffDTimeUTCStart đến CAEffDTimeUTCEnd.
/// Khóa nghiệp vụ là cặp (OrgID, CANumber) — mỗi tổ chức chỉ có 1 chứng thư cho 1 số CA.
/// Luật cốt lõi (Mst_OrgCKS_CheckDB / _CreateX / _UpdateX / _DeleteX):
///  - OrgID bắt buộc khi tạo;
///  - khi tạo, cặp (OrgID, CANumber) KHÔNG được trùng (FlagExistToCheck = No);
///  - khi sửa/xóa, cặp (OrgID, CANumber) phải tồn tại (FlagExistToCheck = Yes);
///  - sửa là cập nhật từng phần (CAOrg / CAEffDTimeUTCStart / CAEffDTimeUTCEnd / FlagActive).
/// </summary>
public class OrgCertificate : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CANumber { get; set; } = "";        // CANumber — số chứng thư số
    public string? CAOrg { get; set; }                 // CAOrg — nhà cung cấp CA (VD: VNPT-CA)
    public DateTime? EffectiveFrom { get; set; }       // CAEffDTimeUTCStart — hiệu lực từ
    public DateTime? EffectiveTo { get; set; }         // CAEffDTimeUTCEnd — hiệu lực đến
    public string? CtsPath { get; set; }               // CTSPath — đường dẫn file chứng thư (.pfx)
    public string? CtsPwd { get; set; }                // CTSPwd — mật khẩu file chứng thư
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // CreateDTimeUTC
    public string CreatedBy { get; set; } = "";        // CreateBy
    public DateTime? UpdatedAt { get; set; }           // UpdateDTimeUTC
    public string? UpdatedBy { get; set; }             // UpdateBy
    // ── tính toán ────────────────────
    public CertState State => !Active ? CertState.Inactive
        : (EffectiveTo.HasValue && DateTime.Now > EffectiveTo.Value ? CertState.Expired
        : (EffectiveFrom.HasValue && DateTime.Now < EffectiveFrom.Value ? CertState.NotYetValid
        : CertState.Active));
    public bool IsUsable => State == CertState.Active;
    public string StateLabel => State switch
    {
        CertState.Active => "Còn hiệu lực",
        CertState.Expired => "Hết hạn",
        CertState.NotYetValid => "Chưa hiệu lực",
        CertState.Inactive => "Đã vô hiệu",
        _ => State.ToString()
    };
    public string ValidityLabel => (EffectiveFrom, EffectiveTo) switch
    {
        (null, null) => "Không giới hạn",
        (not null, null) => $"Từ {EffectiveFrom:dd/MM/yyyy}",
        (null, not null) => $"Đến {EffectiveTo:dd/MM/yyyy}",
        _ => $"{EffectiveFrom:dd/MM/yyyy} → {EffectiveTo:dd/MM/yyyy}"
    };
}
