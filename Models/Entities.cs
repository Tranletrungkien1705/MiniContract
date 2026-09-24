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
    PartyCancelled = 9,   // một bên hủy hợp đồng (Contract_ContractParty_Cancel)
    UpdateRemark = 10,    // cập nhật ghi chú của một bên (Contract_Contract_Party_UpdateRemark)
    PartySigned = 11,     // một bên ký hợp đồng (Contract_Contract_PartySign)
    PartyUpdAfterApproved = 12  // cập nhật hợp đồng sau phê duyệt (Contract_ContractParty_UpdAfterApproved)
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

/// <summary>
/// Trạng thái ký của một người ký trong hợp đồng — port từ TConst.UserSignSatus (QContract):
/// NONE = chưa đến lượt, PENDING = chờ ký, CONFIRMED = đã ký/xác nhận.
/// </summary>
public enum UserSignStatus { None = 0, Pending = 1, Confirmed = 2 }

/// <summary>
/// Loại tài liệu tham chiếu của file đính kèm hợp đồng — port từ Const.RefDocType (QContract):
/// CONTRACTCREATED = đính kèm khi tạo hợp đồng, CONTRACTTERMINATED = đính kèm khi kết thúc hợp đồng.
/// </summary>
public enum RefDocType { ContractCreated = 0, ContractTerminated = 1 }

// ── Danh mục loại hợp đồng (Mst_ContractType) ────────────────────────
/// <summary>
/// Danh mục loại hợp đồng — port từ Mst_ContractType (QContract).
/// Mỗi loại có mã (ContractType), tên (ContractTypeName), mô tả (Description) và cờ hiệu lực (FlagActive).
/// Luật cốt lõi (Mst_ContractType_CheckDB / _CreateX / _UpdateX / _DeleteX):
///  - ContractType bắt buộc và KHÔNG trùng khi tạo (FlagExistToCheck=No);
///  - khi sửa/xóa, loại phải tồn tại (FlagExistToCheck=Yes);
///  - KHÔNG xóa loại đang được dùng bởi hợp đồng mẫu (Contract_TempContract) hoặc bên hợp đồng
///    (Contract_ContractParty) — lỗi Mst_ContractType_Delete_ContractTypeUsed.
/// </summary>
public class ContractType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = "";            // ContractTypeName — tên loại hợp đồng
    public string? Code { get; set; }                  // ContractType — mã loại hợp đồng
    public string? Description { get; set; }           // Description — mô tả loại hợp đồng
    public string? BodyTemplate { get; set; }          // mẫu nội dung mặc định
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy

    public ContractNumberRule? NumberRule { get; set; }   // quy tắc đánh số của loại này (nếu có)

    // ── tính toán ────────────────────────────────────────────────────
    public int TemplateCount { get; set; }             // số hợp đồng mẫu đang dùng loại này
    public int ContractCount { get; set; }             // số hợp đồng đang dùng loại này
    public bool InUse => TemplateCount > 0 || ContractCount > 0;   // đang được sử dụng → không xóa được
    public string ActiveLabel => Active ? "Hiệu lực" : "Ngừng";
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

// ── Cấu hình loại hợp đồng (Mst_ContractTypeDtl) ─────────────────────
/// <summary>
/// Cấu hình chi tiết của một loại hợp đồng — port từ Mst_ContractTypeDtl (QContract).
/// Mỗi loại hợp đồng có 1 cấu hình: có tự sinh số hợp đồng hay không (FlagGenContractNo),
/// và các KÊNH được phép dùng để gửi hợp đồng (Email/SMS/Zalo) và để gửi OTP (Email/SMS/Zalo).
/// Luật cốt lõi (Mst_ContractTypeDtl_CheckDB / _SaveX):
///  - khóa nghiệp vụ là ContractType (mỗi loại chỉ có 1 cấu hình);
///  - khi tạo, cấu hình của loại KHÔNG được trùng (FlagExistToCheck = No);
///  - khi sửa/xóa, cấu hình của loại phải tồn tại (FlagExistToCheck = Yes);
///  - loại hợp đồng phải tồn tại và đang hiệu lực (Mst_ContractType_CheckDB).
/// </summary>
public class ContractTypeConfig : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int TypeId { get; set; }                 // ContractType — loại hợp đồng áp dụng
    public bool GenContractNo { get; set; } = true; // FlagGenContractNo — tự sinh số hợp đồng
    public bool EmailContract { get; set; } = true; // FlagEmailContract — gửi HĐ qua Email
    public bool SmsContract { get; set; }           // FlagSMSContract — gửi HĐ qua SMS
    public bool ZaloContract { get; set; }          // FlagZaloContract — gửi HĐ qua Zalo
    public bool EmailOtp { get; set; } = true;      // FlagEmailOTP — gửi OTP qua Email
    public bool SmsOtp { get; set; }                // FlagSMSOTP — gửi OTP qua SMS
    public bool ZaloOtp { get; set; }               // FlagZaloOTP — gửi OTP qua Zalo
    public bool Active { get; set; } = true;        // FlagActive
    public string? Remark { get; set; }             // Remark
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";     // LogLUBy
    public ContractType? Type { get; set; }

    // ── tính toán ────────────────────────────────────────────────────
    public string TypeName => Type?.Name ?? $"#{TypeId}";
    public List<ChannelType> ContractChannels => BuildChannels(EmailContract, SmsContract, ZaloContract);
    public List<ChannelType> OtpChannels => BuildChannels(EmailOtp, SmsOtp, ZaloOtp);
    public string ContractChannelsLabel => ChannelsLabel(ContractChannels);
    public string OtpChannelsLabel => ChannelsLabel(OtpChannels);

    private static List<ChannelType> BuildChannels(bool email, bool sms, bool zalo)
    {
        var list = new List<ChannelType>();
        if (email) list.Add(ChannelType.Email);
        if (sms) list.Add(ChannelType.Sms);
        if (zalo) list.Add(ChannelType.Zalo);
        return list;
    }

    private static string ChannelsLabel(List<ChannelType> channels) =>
        channels.Count == 0 ? "—" : string.Join(", ", channels.Select(c => c switch
        {
            ChannelType.Email => "Email",
            ChannelType.Sms => "SMS",
            ChannelType.Zalo => "Zalo",
            _ => c.ToString()
        }));
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
    public decimal CurrencyRate { get; set; } = 1;  // CurrencyRate — tỉ giá quy đổi (dùng khi cập nhật giá trị sau phê duyệt)
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

    // ── File hợp đồng (Contract_Contract_UpdateFilePath) ──
    // Nguồn QContract: Contract_Contract.ContractFileName/ContractFilePath/ContractFileVersion —
    // file bản thể hiện (PDF) đã ký của hợp đồng. Cập nhật qua WAS_Contract_Contract_UpdateFilePath.
    public string? FileName { get; set; }                   // ContractFileName — tên file (kèm phần mở rộng)
    public string? FilePath { get; set; }                   // ContractFilePath — đường dẫn file đã ký
    public string? FileVersion { get; set; }                // ContractFileVersion — phiên bản file
    public DateTime? FileUpdatedAt { get; set; }            // LogLUDTimeUTC — thời điểm cập nhật file
    public string? FileUpdatedBy { get; set; }              // LogLUBy — người cập nhật file

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
    public List<ContractSigner> Signers { get; set; } = [];   // người ký của hợp đồng (theo bên)
    public List<ContractSendHist> SendHistory { get; set; } = [];   // lịch sử gửi cho các bên
    public List<ContractDetail> Details { get; set; } = [];   // chi tiết hàng hóa/dịch vụ (Contract_ContractDtl)
    public List<ContractAttribute> Attributes { get; set; } = [];   // trường động cấp hợp đồng (Contract_Attribute_Contract)
    public List<ContractAttributeDtl> AttributeDetails { get; set; } = [];   // trường động cấp chi tiết (Contract_Attribute_ContractDtl)
    public List<ContractAttachment> Attachments { get; set; } = [];   // file đính kèm hợp đồng (Contract_ContractFiles)

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
    public bool HasFile => !string.IsNullOrWhiteSpace(FileName);   // đã có file bản thể hiện
    public int SignedCount => Parties.Count(p => p.HasSigned);
    public string Kind => IsAnnex ? "Phụ lục" : "Hợp đồng";
    public int ElementSignedCount => Elements.Count(e => e.IsSigned);
    public int CheckedCount => Checkers.Count(c => c.HasChecked);
    public bool AllChecked => Checkers.Count > 0 && Checkers.All(c => c.HasChecked);
    public int SignerConfirmedCount => Signers.Count(s => s.IsConfirmed);
    public bool AllSignersConfirmed => Signers.Count > 0 && Signers.All(s => s.IsConfirmed);
    public int PartyConfirmedCount => Parties.Count(p => p.IsConfirmed);   // số bên đã ký hợp đồng
    public bool AllPartiesConfirmed => Parties.Count > 0 && Parties.All(p => p.IsConfirmed);
    // ── chi tiết hợp đồng (Contract_ContractDtl) ─────────────────────
    public decimal DetailTotal => Details.Sum(d => d.ValContract);          // tổng thành tiền các dòng
    public decimal DetailTaxTotal => Details.Sum(d => d.ValTax);            // tổng tiền thuế
    public decimal DetailDiscountTotal => Details.Sum(d => d.ValDiscount);  // tổng tiền chiết khấu
    public decimal DetailGrandTotal => DetailTotal - DetailDiscountTotal + DetailTaxTotal;  // tổng thanh toán
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

    // -- Ky hop dong boi mot ben (Contract_Contract_PartySign) --
    // Nguon QContract: Contract_ContractParty.UserCodeSign/UserNameSign/InfoToken/
    // SignDateUTC/SignBy + UserToken (token nguoi ky) + ContractFileVersion (phien ban file da ky).
    public string? UserCodeSign { get; set; }                        // UserCodeSign — ma nguoi ky
    public string? UserNameSign { get; set; }                        // UserNameSign — ten nguoi ky
    public string? InfoToken { get; set; }                           // InfoToken — thong tin token khi ky
    public string? UserToken { get; set; }                           // UserToken — token xac thuc nguoi ky
    public DateTime? SignDateUTC { get; set; }                       // SignDateUTC — thoi diem ky
    public string? SignBy { get; set; }                              // SignBy — nguoi thuc hien ky
    public string? ContractFileVersion { get; set; }                 // ContractFileVersion — phien ban file da ky

    // -- Cap nhat hop dong sau phe duyet (Contract_ContractParty_UpdAfterApproved) --
    // Nguon QContract: Contract_ContractParty.ValContract/ValPaymented/ValRemain/ValExchange/
    // ContractType/ContractTypeName. Sau khi hop dong duoc phe duyet, ben cap nhat gia tri hop dong,
    // gia tri da thanh toan, loai hop dong va ghi chu; ValRemain = ValContract - ValPaymented.
    public decimal ValContract { get; set; }                         // ValContract — gia tri hop dong
    public decimal ValPaymented { get; set; }                        // ValPaymented — gia tri da thanh toan
    public decimal ValRemain { get; set; }                           // ValRemain — gia tri con lai
    public decimal ValExchange { get; set; }                         // ValExchange — gia tri quy doi (= ValContract * ti gia)
    public string? ContractType { get; set; }                        // ContractType — loai hop dong (ma)
    public string? ContractTypeName { get; set; }                    // ContractTypeName — ten loai hop dong
    public DateTime? ValueUpdatedAt { get; set; }                    // LogLUDTimeUTC — thoi diem cap nhat gia tri
    public string? ValueUpdatedBy { get; set; }                      // LogLUBy — nguoi cap nhat gia tri

    // -- Cap nhat thong tin ben tham gia (Contract_ContractParty_Update) --
    // Nguon QContract: Contract_ContractParty.CustomerAddress/CustomerWebsite/BankCode/BankName/
    // BankAccountNo/RepresentName/RepresentPosition. Cap nhat thong tin phap ly + lien he cua ben
    // (khoa nghiep vu la cap (ContractCode, MST)); ContractCode bat buoc.
    public string? Address { get; set; }                             // CustomerAddress — dia chi ben
    public string? Website { get; set; }                             // CustomerWebsite — website ben
    public string? BankCode { get; set; }                            // BankCode — ma ngan hang
    public string? BankName { get; set; }                            // BankName — ten ngan hang
    public string? BankAccountNo { get; set; }                       // BankAccountNo — so tai khoan
    public string? RepresentName { get; set; }                       // RepresentName — nguoi dai dien
    public string? RepresentPosition { get; set; }                   // RepresentPosition — chuc vu nguoi dai dien
    public DateTime? InfoUpdatedAt { get; set; }                     // LogLUDTimeUTC — thoi diem cap nhat thong tin
    public string? InfoUpdatedBy { get; set; }                       // LogLUBy — nguoi cap nhat thong tin

    // -- Ghi nhan gui email cho ben (Contract_ContractParty_UpdEmailSend) --
    // Nguon QContract: Contract_ContractParty.EmailSend/SendEmailDTimeUTC/SendEmailBy.
    // Khi gui thong bao (mail) cho ben, he thong ghi nhan email da dung + thoi diem + nguoi gui.
    public string? EmailSend { get; set; }                           // EmailSend — email nhan thong bao
    public DateTime? SendEmailDTimeUTC { get; set; }                 // SendEmailDTimeUTC — thoi diem gui mail
    public string? SendEmailBy { get; set; }                         // SendEmailBy — nguoi gui mail

    public Contract Contract { get; set; } = null!;

    // -- tinh toan --
    public bool IsCancelled => Status == PartyStatus.Cancelled;
    public bool IsConfirmed => Status == PartyStatus.Confirmed;      // da ky hop dong
    public bool HasValue => ValContract > 0;                         // da co gia tri hop dong
    public bool HasInfo => !string.IsNullOrWhiteSpace(Address) || !string.IsNullOrWhiteSpace(TaxCode)
        || !string.IsNullOrWhiteSpace(RepresentName);                // da co thong tin phap ly
    public bool EmailSent => SendEmailDTimeUTC != null;              // da gui mail thong bao
}

// ── Chi tiết hợp đồng (Contract_ContractDtl) ─────────────────────────
/// <summary>
/// Dòng chi tiết (hàng hóa/dịch vụ) của hợp đồng — port từ Contract_ContractDtl (QContract).
/// Mỗi dòng gắn 1 hợp đồng (ContractCode) với 1 mặt hàng (SpecCode/SpecName), đơn vị tính
/// (UnitCode/UnitName), đơn giá (UnitPrice), số lượng (Qty), thuế suất (VATRate/VATRateCode)
/// và chiết khấu (DiscountRate). Các giá trị tiền được tính theo công thức của QContract
/// (SignMulti.cshtml):
///  - ValContract (thành tiền) = Qty × UnitPrice;
///  - ValDiscount (tiền chiết khấu) = ValContract × DiscountRate / 100;
///  - ValTax (tiền thuế) = (ValContract − ValDiscount) × VATRate / 100.
/// Nguồn QContract: Contract_ContractDtl (insert trong Contract_Contract_SaveX) +
/// luồng tính toán ở Website Contract_Contract/SignMulti.cshtml.
/// </summary>
public class ContractDetail : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int Idx { get; set; } = 1;               // Idx — thứ tự dòng
    public string SpecCode { get; set; } = "";      // SpecCode — mã hàng hóa/dịch vụ
    public string SpecName { get; set; } = "";      // SpecName — tên hàng hóa/dịch vụ
    public string? VATRateCode { get; set; }         // VATRateCode — mã thuế suất
    public decimal VATRate { get; set; }             // VATRate — thuế suất (%)
    public string? UnitCode { get; set; }            // UnitCode — mã đơn vị tính
    public string? UnitName { get; set; }            // UnitName — tên đơn vị tính
    public decimal UnitPrice { get; set; }           // UnitPrice — đơn giá
    public decimal Qty { get; set; }                 // Qty — số lượng
    public decimal ValContract { get; set; }         // ValContract — thành tiền (Qty × UnitPrice)
    public decimal ValTax { get; set; }              // ValTax — tiền thuế
    public decimal DiscountRate { get; set; }        // DiscountRate — tỉ lệ chiết khấu (%)
    public decimal ValDiscount { get; set; }         // ValDiscount — tiền chiết khấu
    public string? Remark { get; set; }              // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";     // LogLUBy

    public Contract Contract { get; set; } = null!;

    // ── tính toán ────────────────────────────────────────────────────
    public decimal LineTotal => ValContract - ValDiscount + ValTax;   // tổng dòng (sau chiết khấu + thuế)
    public string UnitLabel => string.IsNullOrWhiteSpace(UnitName) ? (UnitCode ?? "—") : UnitName;
}

// ── Danh mục trường động (Mst_Attribute_Contract) ────────────────────
/// <summary>
/// Danh mục trường động (thuộc tính động) của hợp đồng — port từ Mst_Attribute_Contract (QContract).
/// Mỗi trường động có mã (AttributeContractCode), tên hiển thị (AttributeName) và giá trị mặc định
/// (DefaultValues). Luật cốt lõi (Mst_Attribute_Contract_CheckDB / _CreateX / _UpdateX / _DeleteX):
///  - AttributeContractCode bắt buộc khi tạo;
///  - khi tạo, mã trường KHÔNG được trùng (FlagExistToCheck = No);
///  - khi sửa/xóa, mã trường phải tồn tại (FlagExistToCheck = Yes).
/// </summary>
public class AttributeContract : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // AttributeContractCode — mã trường động
    public string Name { get; set; } = "";          // AttributeName — tên trường động
    public string? DefaultValues { get; set; }        // DefaultValues — giá trị mặc định
    public bool Active { get; set; } = true;          // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";      // LogLUBy
}

// ── Trường động của hợp đồng (Contract_Attribute_Contract) ───────────
/// <summary>
/// Giá trị trường động ở cấp HỢP ĐỒNG (header) — port từ Contract_Attribute_Contract (QContract).
/// Mỗi dòng gắn 1 hợp đồng (ContractCode) với 1 mã trường động (AttributeContractCode) và giá trị
/// (AttributeValue). Luật cốt lõi (Contract_Contract_SaveX):
///  - AttributeContractCode bắt buộc và phải tồn tại & đang hiệu lực trong Mst_Attribute_Contract;
///  - AttributeValue bắt buộc (không rỗng);
///  - khi lưu, GHI ĐÈ toàn bộ trường động cũ của hợp đồng (delete all + insert all).
/// </summary>
public class ContractAttribute : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public string AttributeContractCode { get; set; } = "";  // AttributeContractCode — mã trường động
    public string AttributeName { get; set; } = "";          // AttributeName — tên trường động
    public string AttributeValue { get; set; } = "";         // AttributeValue — giá trị
    public bool Active { get; set; } = true;                 // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";              // LogLUBy
    public Contract Contract { get; set; } = null!;
}

// ── Trường động chi tiết của hợp đồng (Contract_Attribute_ContractDtl) ──
/// <summary>
/// Giá trị trường động ở cấp CHI TIẾT (detail) — port từ Contract_Attribute_ContractDtl (QContract).
/// Giống Contract_Attribute_Contract nhưng gắn theo dòng chi tiết (Idx) của hợp đồng.
/// Luật cốt lõi: mã trường bắt buộc + phải tồn tại & đang hiệu lực; giá trị bắt buộc;
/// khi lưu GHI ĐÈ toàn bộ (delete all + insert all).
/// </summary>
public class ContractAttributeDtl : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int Idx { get; set; } = 1;                        // Idx — thứ tự dòng chi tiết
    public string AttributeContractCode { get; set; } = "";  // AttributeContractCode — mã trường động
    public string AttributeName { get; set; } = "";          // AttributeName — tên trường động
    public string AttributeValue { get; set; } = "";         // AttributeValue — giá trị
    public bool Active { get; set; } = true;                 // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";              // LogLUBy
    public Contract Contract { get; set; } = null!;
}

// ── File đính kèm hợp đồng (Contract_ContractFiles) ──────────────────
/// <summary>
/// File đính kèm của hợp đồng — port từ Contract_ContractFiles (QContract).
/// Mỗi hợp đồng có DANH SÁCH file đính kèm (khác với file bản thể hiện đã ký đơn lẻ ở
/// Contract.FileName/FilePath): tên file (ContractFileName), đường dẫn (ContractFilePath),
/// mô tả (ContractFileDesc), thứ tự (Idx), loại tài liệu tham chiếu (RefDocType:
/// CONTRACTCREATED = đính kèm khi tạo hợp đồng, CONTRACTTERMINATED = đính kèm khi kết thúc)
/// và cờ công khai (FlagPublic: 1 = công khai, 0 = nội bộ).
/// Nguồn QContract: Contract_ContractFiles (insert trong Contract_Contract_SaveX) +
/// luồng Website Contract_ContractController (model.FileList → Lst_Contract_ContractFiles).
/// </summary>
public class ContractAttachment : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int Idx { get; set; } = 1;               // Idx — thứ tự file đính kèm
    public string FileName { get; set; } = "";      // ContractFileName — tên file
    public string? FilePath { get; set; }            // ContractFilePath — đường dẫn file
    public string? Description { get; set; }         // ContractFileDesc — mô tả file
    public RefDocType RefDocType { get; set; } = RefDocType.ContractCreated;  // RefDocType — loại tài liệu tham chiếu
    public bool IsPublic { get; set; }               // FlagPublic: 1 = công khai, 0 = nội bộ
    public bool Active { get; set; } = true;         // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";     // LogLUBy

    public Contract Contract { get; set; } = null!;

    // ── tính toán ────────────────────────────────────────────────────
    public string RefDocTypeLabel => RefDocType == RefDocType.ContractTerminated ? "Khi kết thúc" : "Khi tạo hợp đồng";
    public string PublicLabel => IsPublic ? "Công khai" : "Nội bộ";
    public bool HasFile => !string.IsNullOrWhiteSpace(FilePath);
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

// ── Người ký của hợp đồng (Contract_ContractUser) ────────────────────
/// <summary>
/// Người ký của một bên trong hợp đồng — port từ Contract_ContractUser (QContract).
/// Mỗi bản ghi gắn 1 người ký (UserCodeSysSign) vào 1 bên (PartyCode) của 1 hợp đồng,
/// mang trạng thái ký (UserSignSatus: NONE/PENDING/CONFIRMED), cờ đã gửi yêu cầu ký
/// (FlagSendUser) + thời điểm/người gửi (SendDateUTC/SendBy) và thời điểm/người xác nhận
/// (ConfirmDTimeUTC/ConfirmBy). Khóa nghiệp vụ là bộ ba (ContractCode, PartyCode, UserCodeSysSign).
/// Nguồn QContract: Contract_ContractUser_CheckDB (kiểm tra tồn tại/FlagSendUser/UserSignSatus),
/// Contract_ContractUser_ConfirmX (xác nhận ký → CONFIRMED, hợp đồng chuyển ONPROCESS) và
/// Contract_ContractUser_UpdateFlagSendUserX (đánh dấu đã gửi → FlagSendUser=1).
/// </summary>
public class ContractSigner : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ContractId { get; set; }
    public int? PartyId { get; set; }               // PartyCode — bên mà người ký thuộc về
    public string PartyCode { get; set; } = "";     // PartyCode — mã bên (lưu để tra cứu nhanh)
    public string UserCodeSysSign { get; set; } = "";  // UserCodeSysSign — mã người ký (khóa nghiệp vụ)
    public int Idx { get; set; } = 1;               // Idx — thứ tự ký
    public string UserCodeSign { get; set; } = "";  // UserCodeSign — mã đăng nhập người ký
    public string UserNameSign { get; set; } = "";  // UserNameSign — tên người ký
    public string? UserEmail { get; set; }          // UserEmail
    public string? UserPhone { get; set; }          // UserPhone
    public string? UserZalo { get; set; }           // UserZalo
    public string? UserToken { get; set; }          // UserToken — token dùng cho link ký

    // ── trạng thái ký ────────────────────────────────────────────────
    public UserSignStatus SignStatus { get; set; } = UserSignStatus.Pending;  // UserSignSatus
    public DateTime? ConfirmDTimeUTC { get; set; }  // ConfirmDTimeUTC — thời điểm xác nhận ký
    public string? ConfirmBy { get; set; }          // ConfirmBy — người xác nhận

    // ── trạng thái gửi ───────────────────────────────────────────────
    public bool FlagSendUser { get; set; }          // FlagSendUser — đã gửi yêu cầu ký cho người này
    public DateTime? SendDateUTC { get; set; }      // SendDateUTC — thời điểm gửi
    public string? SendBy { get; set; }             // SendBy — người gửi

    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";     // LogLUBy

    public Contract Contract { get; set; } = null!;
    public ContractParty? Party { get; set; }

    // ── tính toán ────────────────────────────────────────────────────
    public bool IsConfirmed => SignStatus == UserSignStatus.Confirmed;
    public string SignStatusLabel => SignStatus switch
    {
        UserSignStatus.None => "Chưa đến lượt",
        UserSignStatus.Pending => "Chờ ký",
        UserSignStatus.Confirmed => "Đã ký",
        _ => SignStatus.ToString()
    };
}
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

// ── Cấu hình ký của tổ chức (Mst_OrgSignConfig) ──────────────────────
/// <summary>
/// Loại ký của tổ chức — port từ TConst.SignType (QContract):
/// REMOTE = ký từ xa (remote signing), SERVER = ký phía server, USBTOKEN = ký bằng USB Token.
/// </summary>
public enum SignType { Remote = 0, Server = 1, UsbToken = 2 }

/// <summary>
/// Cấu hình ký của một tổ chức — port từ Mst_OrgSignConfig (QContract).
/// Mỗi bản ghi gắn 1 tổ chức (OrgID) với 1 loại ký (SignType: REMOTE/SERVER/USBTOKEN) và
/// các tham số ký tương ứng: chứng thư (CANumber/CAOrg/CAEffDTimeUTCStart/CAEffDTimeUTCEnd),
/// ký server (ServerSignFilePath/ServerSignPassword), ký từ xa (SupplierCode/
/// RemoteSignAgreementUUID/RemoteSignPassCode) và hình thức xác thực (AuthenCode).
/// Luật cốt lõi (Mst_OrgSignConfig_CheckDB / _CreateX / _UpdateX / _DeleteX):
///  - khi tạo, AutoID KHÔNG được trùng (FlagExistToCheck = No);
///  - khi sửa/xóa, AutoID phải tồn tại (FlagExistToCheck = Yes);
///  - với mỗi cặp (SignType, OrgID) chỉ được có TỐI ĐA 1 bản ghi đang hiệu lực
///    (FlagActive=1) — nếu nhiều hơn 1 thì lỗi MoreThanOneActive.
/// </summary>
public class OrgSignConfig : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public SignType SignType { get; set; } = SignType.Remote;   // SignType — loại ký
    public string? NetworkID { get; set; }                       // NetworkID
    public string? OrgCode { get; set; }                         // OrgID — mã tổ chức (nghiệp vụ)
    public string? CANumber { get; set; }                        // CANumber — số chứng thư số
    public string? CAOrg { get; set; }                           // CAOrg — tổ chức cấp chứng thư
    public DateTime? EffectiveFrom { get; set; }                 // CAEffDTimeUTCStart — hiệu lực từ
    public DateTime? EffectiveTo { get; set; }                   // CAEffDTimeUTCEnd — hiệu lực đến
    public string? ServerSignFilePath { get; set; }              // ServerSignFilePath — ký server: đường dẫn file
    public string? ServerSignPassword { get; set; }              // ServerSignPassword — ký server: mật khẩu
    public string? SupplierCode { get; set; }                    // SupplierCode — nhà cung cấp ký từ xa
    public string? RemoteSignAgreementUUID { get; set; }         // RemoteSignAgreementUUID — mã hợp đồng ký từ xa
    public string? RemoteSignPassCode { get; set; }              // RemoteSignPassCode — mã ký từ xa
    public string? AuthenCode { get; set; }                      // AuthenCode — hình thức xác thực
    public bool Active { get; set; } = true;                     // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;      // CreateDTimeUTC
    public string CreatedBy { get; set; } = "";                 // CreateBy
    public DateTime? UpdatedAt { get; set; }                     // UpdateDTimeUTC
    public string? UpdatedBy { get; set; }                       // UpdateBy

    // ── tính toán ────────────────────────────────────────────────────
    public string SignTypeLabel => SignType switch
    {
        SignType.Remote => "Ký từ xa (REMOTE)",
        SignType.Server => "Ký server (SERVER)",
        SignType.UsbToken => "USB Token",
        _ => SignType.ToString()
    };
    public string ValidityLabel => (EffectiveFrom, EffectiveTo) switch
    {
        (null, null) => "Không giới hạn",
        (not null, null) => $"Từ {EffectiveFrom:dd/MM/yyyy}",
        (null, not null) => $"Đến {EffectiveTo:dd/MM/yyyy}",
        _ => $"{EffectiveFrom:dd/MM/yyyy} → {EffectiveTo:dd/MM/yyyy}"
    };
}

// ── Cấu hình kênh gửi của tổ chức (Mst_Channel) ──────────────────────
/// <summary>
/// Cấu hình kênh gửi của một tổ chức — port từ Mst_Channel (QContract).
/// Mỗi tổ chức có 1 cấu hình kênh: kênh gửi hợp đồng (ChannelTypeContract),
/// kênh gửi OTP (ChannelTypeOTP) và kênh gửi AccessKey (ChannelTypeAccessKey).
/// Luật cốt lõi (Mst_Channel_SaveX_New20240312 / Mst_Channel_CheckDB):
///  - khóa nghiệp vụ là OrgID (mỗi tổ chức chỉ có 1 cấu hình kênh);
///  - ChannelTypeContract và ChannelTypeOTP phải tồn tại và đang hiệu lực
///    (Mst_ChannelType_CheckDB, FlagExistToCheck=Yes, FlagActiveListToCheck=Active);
///  - ChannelTypeAccessKey luôn bị ép về EMAIL;
///  - lưu là upsert (chưa có thì tạo, đã có thì cập nhật); xóa thì bỏ cấu hình.
/// </summary>
public class ChannelConfig : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string? NetworkID { get; set; }                       // NetworkID
    public ChannelType ContractChannel { get; set; } = ChannelType.Email;  // ChannelTypeContract — kênh gửi HĐ
    public ChannelType OtpChannel { get; set; } = ChannelType.Email;       // ChannelTypeOTP — kênh gửi OTP
    public ChannelType AccessKeyChannel { get; set; } = ChannelType.Email; // ChannelTypeAccessKey — luôn EMAIL
    public bool Active { get; set; } = true;                     // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;      // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";                  // LogLUBy

    // ── cấu hình con theo từng kênh ──────────────────────────────────
    public ChannelEmailConfig? Email { get; set; }               // Mst_ChannelEmail
    public ChannelSmsConfig? Sms { get; set; }                   // Mst_ChannelSMS
    public ChannelZaloConfig? Zalo { get; set; }                 // Mst_ChannelZalo

    // ── tính toán ────────────────────────────────────────────────────
    public string ContractChannelLabel => ChannelLabel(ContractChannel);
    public string OtpChannelLabel => ChannelLabel(OtpChannel);
    public string AccessKeyChannelLabel => ChannelLabel(AccessKeyChannel);
    public bool HasEmail => Email != null;
    public bool HasSms => Sms != null;
    public bool HasZalo => Zalo != null;

    public static string ChannelLabel(ChannelType c) => c switch
    {
        ChannelType.Email => "Email",
        ChannelType.Sms => "SMS",
        ChannelType.Zalo => "Zalo",
        _ => c.ToString()
    };
}

// ── Cấu hình kênh Email (Mst_ChannelEmail) ───────────────────────────
/// <summary>
/// Cấu hình kênh Email của tổ chức — port từ Mst_ChannelEmail (QContract).
/// Gồm mẫu nội dung gửi (SubFormCodeEmailContract/OTP/AccessKey) và thông tin
/// máy chủ gửi mail (MailFrom/APIsSendMail/ApiKeySendMail/SolutionCodeSendMail/DisplayNameMailFrom).
/// </summary>
public class ChannelEmailConfig : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ChannelConfigId { get; set; }                     // FK tới cấu hình kênh
    public string? SubFormCodeEmailContract { get; set; }        // mẫu nội dung gửi ký HĐ
    public string? SubFormCodeEmailOtp { get; set; }             // mẫu nội dung gửi OTP
    public string? SubFormCodeEmailAccessKey { get; set; }       // mẫu nội dung gửi AccessKey
    public string? MailFrom { get; set; }                        // MailFrom — địa chỉ gửi
    public string? APIsSendMail { get; set; }                    // APIsSendMail — endpoint gửi mail
    public string? ApiKeySendMail { get; set; }                  // ApiKeySendMail
    public string? SolutionCodeSendMail { get; set; }            // SolutionCodeSendMail
    public string? DisplayNameMailFrom { get; set; }             // DisplayNameMailFrom — tên hiển thị
    public bool Active { get; set; } = true;                     // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;      // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";                  // LogLUBy
    public ChannelConfig ChannelConfig { get; set; } = null!;
}

// ── Cấu hình kênh SMS (Mst_ChannelSMS) ───────────────────────────────
/// <summary>
/// Cấu hình kênh SMS của tổ chức — port từ Mst_ChannelSMS (QContract).
/// Gồm mẫu nội dung gửi (SubFormCodeContractSMS/SMSOTP/AccessKey) và brandname (SMSBrandName).
/// </summary>
public class ChannelSmsConfig : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ChannelConfigId { get; set; }                     // FK tới cấu hình kênh
    public string? SubFormCodeContractSms { get; set; }          // mẫu nội dung gửi ký HĐ
    public string? SubFormCodeSmsOtp { get; set; }               // mẫu nội dung gửi OTP
    public string? SubFormCodeSmsAccessKey { get; set; }         // mẫu nội dung gửi AccessKey
    public string? SmsBrandName { get; set; }                    // SMSBrandName — brandname
    public bool Active { get; set; } = true;                     // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;      // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";                  // LogLUBy
    public ChannelConfig ChannelConfig { get; set; } = null!;
}

// ── Cấu hình kênh Zalo (Mst_ChannelZalo) ─────────────────────────────
/// <summary>
/// Cấu hình kênh Zalo của tổ chức — port từ Mst_ChannelZalo (QContract).
/// Gồm mẫu nội dung gửi (SubFormCodeContractZaloUserId/Phone/OTP/AccessKey) và
/// thông tin OA (AppID/ZaloOAID/RefreshToken/AccessToken/AppSecret/AccessCode).
/// </summary>
public class ChannelZaloConfig : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ChannelConfigId { get; set; }                     // FK tới cấu hình kênh
    public string? SubFormCodeContractZaloUserId { get; set; }   // mẫu nội dung gửi ký HĐ (ZaloUserId)
    public string? SubFormCodeContractPhone { get; set; }        // mẫu nội dung gửi ký HĐ (SĐT)
    public string? SubFormCodeOtp { get; set; }                  // mẫu nội dung gửi OTP
    public string? SubFormCodeAccessKeyZaloUserId { get; set; }  // mẫu nội dung gửi AccessKey (ZaloUserId)
    public string? SubFormCodeAccessKeyPhone { get; set; }       // mẫu nội dung gửi AccessKey (SĐT)
    public string? AppId { get; set; }                           // AppID
    public string? ZaloOaId { get; set; }                        // ZaloOAID
    public string? RefreshToken { get; set; }                    // RefreshToken
    public string? AccessToken { get; set; }                     // AccessToken
    public string? AppSecret { get; set; }                       // AppSecret
    public string? AccessCode { get; set; }                      // AccessCode
    public bool Active { get; set; } = true;                     // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;      // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";                  // LogLUBy
    public ChannelConfig ChannelConfig { get; set; } = null!;
}

// ── Mẫu nội dung gửi (Mst_SubmissionForm) ────────────────────────────
/// <summary>
/// Mẫu nội dung gửi (biểu mẫu) — port từ Mst_SubmissionForm (QContract).
/// Mỗi mẫu gắn 1 tổ chức (OrgID) với 1 kênh gửi (ChannelType: EMAIL/SMS/ZALO)
/// và 1 loại bản tin (BulletinType: CONTRACT/OTP), kèm mã ZNS (IDZNS) khi gửi qua Zalo.
/// Khóa nghiệp vụ là SubFormCode (mã mẫu) — dùng làm SubFormCodeEmailContract/OTP/AccessKey
/// trong cấu hình kênh (Mst_ChannelEmail/SMS/Zalo).
/// Luật cốt lõi (Mst_SubmissionForm_CheckDB / _SaveX):
///  - SubFormCode bắt buộc (nếu rỗng → lỗi InvalidSubFormCode);
///  - khi tạo, SubFormCode KHÔNG được trùng (SubFormCodeExisted);
///  - khi lưu, ChannelType phải tồn tại & đang hiệu lực (Mst_ChannelType_CheckDB);
///  - BulletinType phải tồn tại & đang hiệu lực (Mst_BulletinType_CheckDB);
///  - xóa mẫu thì xóa kèm nội dung (Message) + tham số ZNS của mẫu.
/// </summary>
public class SubmissionForm : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string SubFormCode { get; set; } = "";     // SubFormCode — mã mẫu gửi
    public string SubFormName { get; set; } = "";     // SubFormName — tên mẫu gửi
    public ChannelType ChannelType { get; set; } = ChannelType.Email;      // ChannelType — loại kênh
    public BulletinType BulletinType { get; set; } = BulletinType.Contract; // BulletinType — loại bản tin
    public string? IdZns { get; set; }                 // IDZNS — mã mẫu ZNS (khi gửi qua Zalo)
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy

    public List<SubmissionFormMessage> Messages { get; set; } = [];  // nội dung mẫu (tiêu đề + thân)
    public List<SubmissionFormZns> ZnsParams { get; set; } = [];     // tham số ZNS của mẫu

    // ── tính toán ────────────────────────────────────────────────────
    public string ChannelLabel => ChannelConfig.ChannelLabel(ChannelType);
    public string BulletinLabel => BulletinType == BulletinType.Otp ? "OTP" : "Hợp đồng";
    public int MessageCount => Messages.Count;
    public int ZnsParamCount => ZnsParams.Count;
}

// ── Nội dung mẫu gửi (Mst_SubmissionFormMessage) ─────────────────────
/// <summary>
/// Nội dung (tiêu đề + thân) của một mẫu gửi — port từ Mst_SubmissionFormMessage (QContract).
/// Mỗi dòng gắn 1 mẫu (SubFormCode) với tiêu đề (SubTitle) và nội dung (Message).
/// </summary>
public class SubmissionFormMessage : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int SubmissionFormId { get; set; }          // FK tới mẫu gửi
    public string SubFormCode { get; set; } = "";     // SubFormCode — mã mẫu (tra cứu nhanh)
    public string? SubTitle { get; set; }              // SubTitle — tiêu đề mẫu
    public string Message { get; set; } = "";         // Message — nội dung mẫu
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy
    public SubmissionForm SubmissionForm { get; set; } = null!;
}

// ── Tham số ZNS của mẫu gửi (Mst_SubmissionFormZNS) ──────────────────
/// <summary>
/// Tham số Zalo ZNS của một mẫu gửi — port từ Mst_SubmissionFormZNS (QContract).
/// Mỗi dòng gắn 1 mẫu (SubFormCode) với 1 tham số ZNS (ParamContractCodeZNS),
/// nguồn dữ liệu (SourceDataType), mã tham số hệ thống (ParamContractCode) và giá trị (ParamValue).
/// Luật cốt lõi: ParamContractCode phải tồn tại & đang hiệu lực (Mst_ParamContractSubmissForm_CheckDB).
/// </summary>
public class SubmissionFormZns : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int SubmissionFormId { get; set; }          // FK tới mẫu gửi
    public string SubFormCode { get; set; } = "";     // SubFormCode — mã mẫu (tra cứu nhanh)
    public string ParamContractCodeZns { get; set; } = "";  // ParamContractCodeZNS — tham số ZNS
    public string? SourceDataType { get; set; }        // SourceDataType — nguồn dữ liệu
    public string ParamContractCode { get; set; } = "";     // ParamContractCode — mã tham số hệ thống
    public string? ParamValue { get; set; }            // ParamValue — giá trị
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy
    public SubmissionForm SubmissionForm { get; set; } = null!;
}


// ── Danh mục loại thông báo (Mst_NotifyType) ─────────────────────────
/// <summary>
/// Danh mục loại thông báo của hệ thống — port từ Mst_NotifyType (QContract).
/// Mỗi loại thông báo có mã (NotifyType), mô tả (NotifyDesc) và cờ bật mặc định
/// (DefaultActive) cho người dùng mới. Khóa nghiệp vụ là NotifyType.
/// Luật cốt lõi (Mst_NotifyType_CheckDB / _CreateX / _UpdateX / _DeleteX):
///  - NotifyType bắt buộc khi tạo (nếu rỗng → lỗi InvalidNotifyType);
///  - khi tạo, NotifyType KHÔNG được trùng (FlagExistToCheck = No);
///  - khi sửa/xóa, NotifyType phải tồn tại (FlagExistToCheck = Yes);
///  - sửa là cập nhật từng phần (NotifyDesc / DefaultActive / FlagActive).
/// </summary>
public class NotifyType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string NotifyTypeCode { get; set; } = "";   // NotifyType — mã loại thông báo
    public string? NotifyDesc { get; set; }            // NotifyDesc — mô tả loại thông báo
    public bool DefaultActive { get; set; } = true;    // DefaultActive — bật mặc định cho người dùng mới
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy

    public List<UserNotifyType> UserMappings { get; set; } = [];  // Map_UserInNotifyType của loại này

    // ── tính toán ────────────────────────────────────────────────────
    public int SubscriberCount => UserMappings.Count(u => u.FlagNotify);   // số người đang bật nhận
}

// ── Bật/tắt thông báo theo người dùng (Map_UserInNotifyType) ─────────
/// <summary>
/// Bật/tắt nhận một loại thông báo cho một người dùng — port từ Map_UserInNotifyType (QContract).
/// Mỗi bản ghi gắn 1 người dùng (UserCode) với 1 loại thông báo (NotifyType) và cờ bật/tắt
/// (FlagNotify). Khóa nghiệp vụ là cặp (UserCode, NotifyType).
/// Luật cốt lõi (Map_UserInNotifyType_SaveX):
///  - lưu là GHI ĐÈ theo cặp (UserCode, NotifyType): xóa các bản ghi trùng cặp rồi insert lại
///    (delete matching + insert all);
///  - FlagNotify được chuẩn hóa về cờ Yes/No.
/// </summary>
public class UserNotifyType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string UserCode { get; set; } = "";         // UserCode — mã/tên đăng nhập người dùng
    public string NotifyTypeCode { get; set; } = "";   // NotifyType — mã loại thông báo
    public bool FlagNotify { get; set; } = true;       // FlagNotify — bật (true) / tắt (false)
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy

    // ── tính toán ────────────────────────────────────────────────────
    public string FlagLabel => FlagNotify ? "Bật" : "Tắt";
}

// ── Danh mục loại mẫu in (Mst_TempType) ──────────────────────────────
/// <summary>
/// Danh mục loại mẫu in (print template type) — port từ Mst_TempType (QContract).
/// Mỗi loại mẫu in có mã (TempType), tên (TempTypeName), mô tả (TempTypeDesc),
/// khổ/định dạng (TempSize) và đường dẫn ảnh xem trước (ImageFilePath).
/// Khóa nghiệp vụ là TempType.
/// Luật cốt lõi (Mst_TempType_CheckDB / _CreateX / _UpdateX / _DeleteX):
///  - TempType bắt buộc khi tạo (nếu rỗng → lỗi InvalidTempType);
///  - khi tạo, TempType KHÔNG được trùng (FlagExistToCheck = No);
///  - khi sửa/xóa, TempType phải tồn tại (FlagExistToCheck = Yes);
///  - TempTypeName, TempSize và ImageFilePath đều bắt buộc (không rỗng);
///  - sửa là cập nhật từng phần (TempTypeName / TempTypeDesc / TempSize / ImageFilePath / Remark / FlagActive).
/// </summary>
public class TempType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // TempType — mã loại mẫu in
    public string Name { get; set; } = "";             // TempTypeName — tên loại mẫu in
    public string? Description { get; set; }           // TempTypeDesc — mô tả
    public string Size { get; set; } = "";             // TempSize — khổ/định dạng mẫu
    public string ImageFilePath { get; set; } = "";    // ImageFilePath — đường dẫn ảnh xem trước
    public string? Remark { get; set; }                // Remark — ghi chú
    public bool Active { get; set; } = true;           // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy

    // ── tính toán ────────────────────────────────────────────────────
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageFilePath);   // đã có ảnh xem trước
}

// ── Tỷ giá ngoại tệ (Mst_CurrencyEx) ─────────────────────────────────
/// <summary>
/// Tỷ giá ngoại tệ của hệ thống — port từ Mst_CurrencyEx (QContract).
/// Mỗi bản ghi là 1 loại ngoại tệ (CurrencyCode) quy đổi về 1 đồng tiền gốc
/// (BaseCurrencyCode, thường là VND) với tỷ giá mua (BuyRate), tỷ giá bán (SellRate)
/// và tỷ giá quy đổi liên ngân hàng (InterEx).
/// Khóa nghiệp vụ là CurrencyCode.
/// Luật cốt lõi (Mst_CurrencyEx_CheckDB / _Create / _Update / _Delete):
///  - CurrencyCode bắt buộc khi tạo; khi tạo mã tiền KHÔNG được trùng (FlagExistToCheck = No);
///  - khi sửa/xóa, mã tiền phải tồn tại (FlagExistToCheck = Yes);
///  - CurrencyName bắt buộc (không rỗng);
///  - nếu có BaseCurrencyCode thì đồng tiền gốc phải tồn tại (Mst_CurrencyEx_CheckDB);
///  - sửa là cập nhật từng phần (CurrencyName / BuyRate / SellRate / InterEx / Remark).
/// </summary>
public class CurrencyExchange : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CurrencyCode { get; set; } = "";       // CurrencyCode — mã ngoại tệ (VD: USD)
    public string? NetworkID { get; set; }                // NetworkID
    public string CurrencyName { get; set; } = "";        // CurrencyName — tên ngoại tệ
    public string? BaseCurrencyCode { get; set; }         // BaseCurrencyCode — đồng tiền gốc (VD: VND)
    public decimal BuyRate { get; set; }                  // BuyRate — tỷ giá mua
    public decimal SellRate { get; set; }                 // SellRate — tỷ giá bán
    public decimal InterEx { get; set; }                  // InterEx — tỷ giá quy đổi liên ngân hàng
    public string? Remark { get; set; }                   // Remark — ghi chú
    public DateTime? UpdatedTime { get; set; }            // UpdatedTime — thời điểm cập nhật tỷ giá
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";           // LogLUBy

    // ── tính toán ────────────────────────────────────────────────────
    public string BaseLabel => string.IsNullOrWhiteSpace(BaseCurrencyCode) ? "—" : BaseCurrencyCode!;
    public string RateLabel => $"{BuyRate:N0} / {SellRate:N0}";   // mua / bán
    public bool HasRate => BuyRate > 0 || SellRate > 0;
}

// ── Tham số hệ thống (Mst_Param) ─────────────────────
/// <summary>
/// Tham số hệ thống (cấu hình dạng key-value) — port từ Mst_Param (QContract).
/// Mỗi bản ghi là 1 tham số (ParamCode) gắn với 1 mạng (NetworkID) và giá trị (ParamValue).
/// Khóa nghiệp vụ là ParamCode.
/// Luật cốt lõi (Mst_Param_CheckDB / _Create / _Update / _Delete):
///  - ParamCode bắt buộc khi tạo (nếu rỗng → lỗi Mst_Param_Create_InvalidParamCode);
///  - khi tạo, ParamCode KHÔNG được trùng (FlagExistToCheck = No → Mst_Param_CheckDB_ParamCodeExist);
///  - khi sửa/xóa, ParamCode phải tồn tại (FlagExistToCheck = Yes → Mst_Param_CheckDB_ParamCodeNotFound);
///  - sửa là cập nhật từng phần (chỉ ParamValue khi có trong danh sách cột cập nhật).
/// </summary>
public class SystemParam : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ParamCode { get; set; } = "";       // ParamCode — mã tham số hệ thống
    public string? NetworkID { get; set; }             // NetworkID — mạng áp dụng
    public string ParamValue { get; set; } = "";      // ParamValue — giá trị tham số
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy
    // ── tính toán ────────────────────
    public bool HasValue => !string.IsNullOrWhiteSpace(ParamValue);   // đã có giá trị
}

// ── Tham số riêng (Mst_ParamPrivate) ─────────────────
/// <summary>
/// Tham số riêng (cấu hình dạng key-value theo mạng) — port từ Mst_ParamPrivate (QContract).
/// Khác với Mst_Param (tham số hệ thống dùng chung), Mst_ParamPrivate là tham số RIÊNG của từng
/// mạng (NetworkID) — mỗi bản ghi là 1 tham số (ParamCode) gắn với 1 mạng và giá trị (ParamValue).
/// Khóa nghiệp vụ là ParamCode.
/// Luật cốt lõi (Mst_ParamPrivate_CheckDB / _Create / _Update / _Delete — MasterData.cs):
///  - ParamCode bắt buộc khi tạo (nếu rỗng → lỗi Mst_ParamPrivate_Create_InvalidParamCode);
///  - khi tạo, ParamCode KHÔNG được trùng (FlagExistToCheck = No → Mst_ParamPrivate_CheckDB_ParamPrivateExist);
///  - khi sửa/xóa, ParamCode phải tồn tại (FlagExistToCheck = Yes → Mst_ParamPrivate_CheckDB_ParamPrivateNotFound);
///  - sửa là cập nhật từng phần (chỉ ParamValue khi có trong danh sách cột cập nhật).
/// </summary>
public class PrivateParam : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ParamCode { get; set; } = "";       // ParamCode — mã tham số riêng
    public string? NetworkID { get; set; }             // NetworkID — mạng áp dụng
    public string ParamValue { get; set; } = "";      // ParamValue — giá trị tham số
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";        // LogLUBy
    // ── tính toán ────────────────────
    public bool HasValue => !string.IsNullOrWhiteSpace(ParamValue);   // đã có giá trị
}

// ── Danh mục quốc gia (Mst_Country) ──────────────────
/// <summary>
/// Danh mục quốc gia — port từ Mst_Country (QContract).
/// Mỗi quốc gia có mã (CountryCode), tên (CountryName) và cờ hiệu lực (FlagActive).
/// Luật cốt lõi (Mst_Country_CheckDB / _Create / _Update / _Delete — Master.cs):
///  - CountryCode bắt buộc khi tạo (rỗng → lỗi Mst_Country_Create_InvalidCountryCode);
///  - khi tạo, CountryCode KHÔNG được trùng (FlagExistToCheck = No → Mst_Country_CheckDB_CountryExist);
///  - khi sửa/xóa, CountryCode phải tồn tại (FlagExistToCheck = Yes → Mst_Country_CheckDB_CountryNotFound);
///  - CountryName bắt buộc (rỗng → Mst_Country_Create_InvalidCountryName / _Update_InvalidCountryName);
///  - sửa là cập nhật từng phần (CountryName/FlagActive khi có trong danh sách cột cập nhật).
/// </summary>
public class Country : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // CountryCode — mã quốc gia
    public string Name { get; set; } = "";          // CountryName — tên quốc gia
    public bool Active { get; set; } = true;         // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";      // LogLUBy

    // ── tính toán ────────────────────
    public int ProvinceCount { get; set; }           // số tỉnh/thành thuộc quốc gia
    public string ActiveLabel => Active ? "Hiệu lực" : "Ngừng";
}

// ── Danh mục tỉnh/thành (Mst_Province) ───────────────────────────────
/// <summary>
/// Danh mục tỉnh/thành phố — port từ Mst_Province (QContract).
/// Mỗi tỉnh có mã (ProvinceCode), tên (ProvinceName) và cờ hiệu lực (FlagActive).
/// Luật cốt lõi (Mst_Province_CheckDB / _Create / _Update / _Delete — Master.cs):
///  - ProvinceCode bắt buộc khi tạo (rỗng → Mst_Province_Create_InvalidProvinceCode);
///  - khi tạo, ProvinceCode KHÔNG được trùng (FlagExistToCheck = No → Mst_Province_CheckDB_ProvinceExist);
///  - khi sửa/xóa, ProvinceCode phải tồn tại (FlagExistToCheck = Yes → Mst_Province_CheckDB_ProvinceNotFound);
///  - ProvinceName bắt buộc (rỗng → Mst_Province_Create_InvalidProvinceName / _Update_InvalidProvinceName);
///  - sửa là cập nhật từng phần (ProvinceName/FlagActive khi có trong danh sách cột cập nhật).
/// </summary>
public class Province : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // ProvinceCode — mã tỉnh/thành
    public string Name { get; set; } = "";          // ProvinceName — tên tỉnh/thành
    public bool Active { get; set; } = true;         // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";      // LogLUBy

    // ── tính toán ────────────────────
    public int DistrictCount { get; set; }           // số quận/huyện thuộc tỉnh
    public string ActiveLabel => Active ? "Hiệu lực" : "Ngừng";
}

// ── Danh mục quận/huyện (Mst_District) ───────────────────────────────
/// <summary>
/// Danh mục quận/huyện — port từ Mst_District (QContract).
/// Mỗi quận/huyện có mã (DistrictCode), thuộc 1 tỉnh (ProvinceCode), tên (DistrictName) và cờ hiệu lực.
/// Luật cốt lõi (Mst_District_CheckDB / _Create / _Update / _Delete — Master.cs):
///  - DistrictCode bắt buộc khi tạo (rỗng → Mst_District_Create_InvalidDistrictCode);
///  - khi tạo, DistrictCode KHÔNG được trùng (FlagExistToCheck = No → Mst_District_CheckDB_DistrictExist);
///  - khi sửa/xóa, DistrictCode phải tồn tại (FlagExistToCheck = Yes → Mst_District_CheckDB_DistrictNotFound);
///  - DistrictName bắt buộc (rỗng → Mst_District_Create_InvalidDistrictName / _Update_InvalidDistrictName);
///  - ProvinceCode phải tồn tại & đang hiệu lực (Mst_Province_CheckDB).
/// </summary>
public class District : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // DistrictCode — mã quận/huyện
    public string ProvinceCode { get; set; } = "";  // ProvinceCode — tỉnh/thành chứa quận/huyện
    public string Name { get; set; } = "";          // DistrictName — tên quận/huyện
    public bool Active { get; set; } = true;         // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";      // LogLUBy

    // ── tính toán ────────────────────
    public int WardCount { get; set; }               // số phường/xã thuộc quận/huyện
    public string ActiveLabel => Active ? "Hiệu lực" : "Ngừng";
}

// ── Danh mục phường/xã (Mst_Ward) ────────────────────
/// <summary>
/// Danh mục phường/xã — port từ Mst_Ward (QContract).
/// Mỗi phường/xã có mã (WardCode), thuộc 1 quận/huyện (DistrictCode) + 1 tỉnh (ProvinceCode),
/// tên (WardName) và cờ hiệu lực (FlagActive).
/// Luật cốt lõi (Mst_Ward_CheckDB / _Create / _Update / _Delete — Master.cs):
///  - WardCode bắt buộc khi tạo (rỗng → lỗi);
///  - khi tạo, WardCode KHÔNG được trùng (FlagExistToCheck = No → Mst_Ward_CheckDB_WardExist);
///  - khi sửa/xóa, WardCode phải tồn tại (FlagExistToCheck = Yes → Mst_Ward_CheckDB_WardNotFound);
///  - WardName bắt buộc; DistrictCode/ProvinceCode phải tồn tại & đang hiệu lực.
/// </summary>
public class Ward : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // WardCode — mã phường/xã
    public string ProvinceCode { get; set; } = "";  // ProvinceCode — tỉnh/thành
    public string DistrictCode { get; set; } = "";  // DistrictCode — quận/huyện
    public string Name { get; set; } = "";          // WardName — tên phường/xã
    public bool Active { get; set; } = true;         // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // LogLUDTimeUTC
    public string CreatedBy { get; set; } = "";      // LogLUBy

    // ── tính toán ────────────────────
    public string ActiveLabel => Active ? "Hiệu lực" : "Ngừng";
}
