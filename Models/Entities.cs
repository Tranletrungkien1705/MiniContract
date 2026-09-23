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
    SignLinkRevoked = 7   // thu hồi link ký công khai
}

/// <summary>Trạng thái hiệu lực của link ký công khai (tính từ thời điểm hết hạn + cờ thu hồi).</summary>
public enum SignLinkState { Active = 0, Expired = 1, Revoked = 2 }

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

// ── Danh mục loại hợp đồng ───────────────────────────────────────────
public class ContractType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? BodyTemplate { get; set; }   // mẫu nội dung mặc định
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

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ContractType? Type { get; set; }
    public Contract? Parent { get; set; }
    public List<Contract> Annexes { get; set; } = [];       // các phụ lục của hợp đồng này
    public List<ContractParty> Parties { get; set; } = [];
    public List<ContractSignature> Signatures { get; set; } = [];
    public List<ContractHistory> History { get; set; } = [];
    public List<ContractSignLink> SignLinks { get; set; } = [];
    public List<ContractElement> Elements { get; set; } = [];   // các ô ký trên bản thể hiện
    public List<ContractChecker> Checkers { get; set; } = [];   // người kiểm tra hợp đồng (theo thứ tự)
    public List<ContractUserInContract> UserAssignments { get; set; } = [];  // người dùng được phân quyền

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

    // ── tính toán ────────────────────────────────────────────────────
    public bool IsOpen => Status is not (ContractStatus.Completed or ContractStatus.Cancelled or ContractStatus.Finished);
    public bool IsFinished => Status == ContractStatus.Finished;
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

    public Contract Contract { get; set; } = null!;

    // ── tính toán ────────────────────────────────────────────────────
    public string RoleLabel => IsChecker ? "Người kiểm tra" : "Người ký";
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
