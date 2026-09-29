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

/// <summary>
/// Trạng thái THỰC HIỆN hợp đồng — khác hẳn <see cref="ContractStatus"/> (trạng thái ký).
/// Port 1:1 hằng nguồn <c>EffStatus</c>: PROCESSING / FINISHED.
/// Nguồn: idn.Skycic.QContract.Constants/Const.Main.BE.cs:1028-1033.
/// </summary>
public enum EffStatus
{
    Processing = 0, // PROCESSING — đang thực hiện
    Finished = 1    // FINISHED   — đã kết thúc
}

/// <summary>Kênh gửi HĐ/OTP — port 1:1 hằng <c>Client_Mst_ChannelType</c> (Const.Main.cs:320-325).</summary>
public enum ChannelKind { Email = 0, Sms = 1, Zalo = 2 }

/// <summary>Loại bản tin gửi — port 1:1 hằng <c>Client_BulletinType</c> (Const.Main.cs:327-334).</summary>
public enum BulletinKind { Contract = 0, Otp = 1 }

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

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ── Port 1:1 màn "Thông tin chung" (Contract_Contract_Info.cshtml — bảng Contract_Contract) ──
    // Định nghĩa cột nguồn: idn.Skycic.QContract.Common/Models/QContract/Contract_Contract/Contract_Contract.cs:11-72
    // Ô nhập tương ứng: Views/Contract_Contract/Contract_Contract_Info.cshtml (số dòng ghi ở từng cột)

    /// <summary>Số hợp đồng do người dùng nhập — BẮT BUỘC, không trùng. KHÁC <see cref="Code"/> (mã tra cứu do hệ thống sinh).
    /// Nguồn: Contract_Contract.cs:15 · ô nhập Contract_Contract_Info.cshtml:407.</summary>
    public string? ContractNo { get; set; }

    /// <summary>Số hợp đồng CHA (HĐ/TL tham chiếu). Bắt buộc khi là phụ lục.
    /// Nguồn: Contract_Contract.cs:16 · ô nhập Contract_Contract_Info.cshtml:525.</summary>
    public string? ContractRefNo { get; set; }

    /// <summary>Mã mẫu hợp đồng đã chọn. Nguồn: Contract_Contract.cs:17 · Contract_Contract_Info.cshtml:381.</summary>
    public string? TContractCode { get; set; }

    /// <summary>Tên mẫu hợp đồng (biz trả ra, chỉ hiển thị). Nguồn: Contract_Contract.cs:18 (chú ý nguồn viết thiếu chữ "t": TContracName).</summary>
    public string? TContracName { get; set; }

    /// <summary>Ngày hợp đồng. Nguồn: Contract_Contract.cs:20 (ContractDateUTC) · ô nhập Contract_Contract_Info.cshtml:468.</summary>
    public DateTime? ContractDate { get; set; }

    /// <summary>Ngày bắt đầu hiệu lực. Nguồn: Contract_Contract.cs:21 · ô nhập Contract_Contract_Info.cshtml:483.</summary>
    public DateTime? EffDateStart { get; set; }

    /// <summary>Ngày hết hiệu lực. Nguồn: Contract_Contract.cs:22 · ô nhập Contract_Contract_Info.cshtml:492.</summary>
    public DateTime? EffDateEnd { get; set; }

    /// <summary>Mã loại tiền. Nguồn: Contract_Contract.cs:24 · ô chọn Contract_Contract_Info.cshtml:581.</summary>
    public string? CurrencyCode { get; set; }

    /// <summary>Tên loại tiền. Nguồn: Contract_Contract.cs:25.</summary>
    public string? CurrencyName { get; set; }

    /// <summary>Tỉ giá quy đổi. Mặc định 1 đúng như nguồn <c>objContract_Contract.CurrencyRate ?? 1</c>.
    /// Nguồn: Contract_Contract.cs:26 · ô nhập Contract_Contract_Info.cshtml:603.</summary>
    public decimal CurrencyRate { get; set; } = 1;

    /// <summary>Giá trị quy đổi = <see cref="Value"/> × <see cref="CurrencyRate"/> (ô nguồn readonly, biz tính).
    /// Công thức nguồn: Contract_ContractController.cs:16518 và Biz/zTemp.cs:4853 · ô hiển thị Contract_Contract_Info.cshtml:611.</summary>
    public decimal ValExchange { get; set; }

    /// <summary>Trạng thái thực hiện. Nguồn: Contract_Contract.cs:29 · ô hiển thị Contract_Contract_Info.cshtml:680.</summary>
    public EffStatus EffStatus { get; set; } = EffStatus.Processing;

    /// <summary>Cờ phụ lục: true = phụ lục (nguồn "1"), false = hợp đồng (nguồn "0").
    /// Nguồn: Contract_Contract.cs:34 · Update.cshtml:59-67 (đổi nhãn "hợp đồng"→"phụ lục" và bật dấu * cho HĐ tham chiếu).</summary>
    public bool FlagContractAnnex { get; set; }

    /// <summary>Cờ "Quy định thứ tự ký". Nguồn: Contract_Contract.cs:69 · ô tick Contract_Contract_Info.cshtml:787.</summary>
    public bool FlagCheckOrderSign { get; set; }

    /// <summary>Cờ "Hệ thống sinh Mã truy cập (AccessKey) khi gửi HĐ/TL". Nguồn: Contract_Contract.cs:71 · ô tick Contract_Contract_Info.cshtml:793.</summary>
    public bool FlagSysGenAccessKey { get; set; }

    /// <summary>Cờ bắt buộc ký kèm OTP. Nguồn: Contract_Contract.cs:67 (nguồn hard-code "0" tại Update.cshtml:199 — xem mục 5 báo cáo).</summary>
    public bool FlagCheckOTP { get; set; }

    /// <summary>Kênh gửi HĐ/TL (nguồn multi-select "contractchannel", lưu chuỗi phân cách dấu phẩy).
    /// Nguồn: Contract_Contract_Info.cshtml:540 (bảng chi tiết Contract_ContractChannel).</summary>
    public string? ChannelTypeContract { get; set; }

    /// <summary>Kênh gửi OTP. Nguồn: Contract_Contract.cs:68 · ô chọn Contract_Contract_Info.cshtml:558.</summary>
    public string? ChannelTypeOTP { get; set; }

    /// <summary>Tên file HĐ/TL đính kèm. Nguồn: Contract_Contract.cs:31 · Contract_Contract_Info.cshtml:706.</summary>
    public string? ContractFileName { get; set; }

    /// <summary>Đường dẫn file HĐ/TL. Nguồn: Contract_Contract.cs:32 · Contract_Contract_Info.cshtml:706.</summary>
    public string? ContractFilePath { get; set; }

    /// <summary>Version file HĐ/TL. Nguồn: Contract_Contract.cs:33 · Contract_Contract_Info.cshtml (hidden, name="ContractFileVersion").</summary>
    public string? ContractFileVersion { get; set; }

    /// <summary>Tên đơn vị tạo hợp đồng. Nguồn: Contract_Contract.cs:58.</summary>
    public string? NNTFullName { get; set; }

    /// <summary>Tên liên hệ. Nguồn: Contract_Contract.cs:59.</summary>
    public string? ContactName { get; set; }

    /// <summary>Tên người tạo. Nguồn: Contract_Contract.cs:60.</summary>
    public string? UserNameCreate { get; set; }

    /// <summary>Chức vụ người tạo. Nguồn: Contract_Contract.cs:61.</summary>
    public string? PositionCreate { get; set; }

    /// <summary>SĐT người tạo. Nguồn: Contract_Contract.cs:62.</summary>
    public string? PhoneNoCreate { get; set; }

    /// <summary>Email người tạo. Nguồn: Contract_Contract.cs:63.</summary>
    public string? EMailCreate { get; set; }

    /// <summary>Mã lý do kết thúc HĐ. Nguồn: Contract_Contract.cs:39.</summary>
    public string? ContractFinishReasonCode { get; set; }

    /// <summary>Tên lý do kết thúc HĐ. Nguồn: Contract_Contract.cs:40.</summary>
    public string? ContractFinishReasonName { get; set; }

    /// <summary>Diễn giải kết thúc. Nguồn: Contract_Contract.cs:41.</summary>
    public string? FinishDescription { get; set; }

    /// <summary>Thời gian kết thúc HĐ. Nguồn: Contract_Contract.cs:45 (FinishedDTimeUTC) · Update.cshtml:48-52.</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Đường dẫn file giải thích lý do kết thúc HĐ. Nguồn: Contract_Contract.cs:42 · Update.cshtml:53-55
    /// (nguồn cắt tên file bằng LastIndexOf("/")).</summary>
    public string? ContractFinishReasonFilePath { get; set; }

    /// <summary>Version file kết thúc. Nguồn: Contract_Contract.cs:43 · Update.cshtml:47.</summary>
    public string? ContractFinishReasonFilePathVersion { get; set; }

    /// <summary>Cờ bản ghi còn hiệu lực (soft-delete của nguồn). Nguồn: Contract_Contract.cs:52.</summary>
    public bool FlagActive { get; set; } = true;

    /// <summary>Cờ "hợp đồng có ô ký (element)" — nguồn bật khi Lst_Contract_ContractElement có phần tử
    /// (Update.cshtml:200-204). Bảng chi tiết Contract_ContractElement CHƯA port (xem mục 5 báo cáo).
    /// Nguồn cột: Contract_Contract.cs:66.</summary>
    public bool FlagElement { get; set; }

    public ContractType? Type { get; set; }
    public List<ContractParty> Parties { get; set; } = [];
    public List<ContractSignature> Signatures { get; set; } = [];
    public List<ContractSendHist> SendHists { get; set; } = [];

    // ── tính toán ────────────────────────────────────────────────────
    public bool IsOpen => Status is not (ContractStatus.Completed or ContractStatus.Cancelled);
    public int SignedCount => Parties.Count(p => p.HasSigned);

    /// <summary>Nhãn loại chứng từ theo cờ phụ lục — nguồn đổi caption "hợp đồng"/"phụ lục" tại Update.cshtml:37,65.</summary>
    public string DocCaption => FlagContractAnnex ? "phụ lục" : "hợp đồng";
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

    // ── Port 1:1 màn "Thông tin các bên" (BindData_Contract_Party_Info.cshtml — bảng Contract_ContractParty) ──
    // Định nghĩa cột nguồn: Common/Models/QContract/Contract_Contract/Contract_ContractParty.cs (số dòng ở từng cột)
    // Tập cột thực tế của lưới: BindData_Contract_Party_Info.cshtml:263-292 (objCommon của renderContractParty_20221106)

    /// <summary>Mã bên tham gia hợp đồng. Bên tạo mang mã cố định "PARTY_A" và bị LOẠI khỏi lưới (nguồn: BindData_Contract_Party_Info.cshtml:21).
    /// Nguồn cột: Contract_ContractParty.cs:12.</summary>
    public string? PartyCode { get; set; }

    /// <summary>Mã đối tác trong hệ thống (nguồn dùng giá trị "OWN" cho chính đơn vị tạo — xem Contract_ContractController.cs:5431).
    /// Nguồn cột: Contract_ContractParty.cs:13.</summary>
    public string? CustomerCodeSys { get; set; }

    /// <summary>Mã đối tác. Nguồn cột: Contract_ContractParty.cs:14.</summary>
    public string? CustomerCode { get; set; }

    /// <summary>Tên đối tác — BẮT BUỘC trong lưới (tab-required tại BindData_Contract_Party_Info.cshtml:244).
    /// Nguồn cột: Contract_ContractParty.cs:21.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Địa chỉ đối tác. Nguồn cột: Contract_ContractParty.cs:22.</summary>
    public string? CustomerAddress { get; set; }

    /// <summary>Website đối tác. Nguồn cột: Contract_ContractParty.cs:25.</summary>
    public string? CustomerWebsite { get; set; }

    /// <summary>Số di động đối tác — cột này CHỈ có ở lưới (BindData_Contract_Party_Info.cshtml:279), KHÔNG có trong Contract_ContractParty.cs.</summary>
    public string? CustomerMobilePhone { get; set; }

    /// <summary>Facebook đối tác — cột chỉ có ở lưới (BindData_Contract_Party_Info.cshtml:280).</summary>
    public string? Facebook { get; set; }

    /// <summary>ZaloUserID — đích gửi khi kênh = ZALO. Nguồn cột: Contract_ContractParty.cs:78 · lưới :281.</summary>
    public string? ZaloUserID { get; set; }

    /// <summary>Mã ngân hàng. Nguồn cột: Contract_ContractParty.cs:26.</summary>
    public string? BankCode { get; set; }

    /// <summary>Tên ngân hàng. Nguồn cột: Contract_ContractParty.cs:27.</summary>
    public string? BankName { get; set; }

    /// <summary>Số tài khoản ngân hàng. Nguồn cột: Contract_ContractParty.cs:28.</summary>
    public string? BankAccountNo { get; set; }

    /// <summary>Người đại diện. Nguồn cột: Contract_ContractParty.cs:29.</summary>
    public string? RepresentName { get; set; }

    /// <summary>Chức vụ người đại diện. Nguồn cột: Contract_ContractParty.cs:30.</summary>
    public string? RepresentPosition { get; set; }

    /// <summary>Giá trị hợp đồng của bên này. Nguồn cột: Contract_ContractParty.cs:36 · ô nhập Contract_Contract_Info.cshtml:578.</summary>
    public decimal ValContract { get; set; }

    /// <summary>Giá trị đã thanh toán. Nguồn cột: Contract_ContractParty.cs:37 · ô nhập Contract_Contract_Info.cshtml:621.</summary>
    public decimal ValPaymented { get; set; }

    /// <summary>Giá trị còn lại = <see cref="ValContract"/> − <see cref="ValPaymented"/> (ô nguồn readonly).
    /// Công thức nguồn: Contract_ContractController.cs:16516 và Biz/zTemp.cs:4886 · ô hiển thị Contract_Contract_Info.cshtml:630.</summary>
    public decimal ValRemain { get; set; }

    /// <summary>Giá trị quy đổi của bên này = <see cref="ValContract"/> × tỉ giá HĐ.
    /// Công thức nguồn: Contract_ContractController.cs:16518 · ô hiển thị Contract_Contract_Info.cshtml:611.</summary>
    public decimal ValExchange { get; set; }

    /// <summary>Trạng thái của bên tham gia (nguồn ContractPartyStatus: ONPROCESS/PENDING/APPROVED/CANCELED/CONFIRMED/FINISHED
    /// — Const.Main.BE.cs:1034-1043). Giữ dạng chuỗi vì taxonomy nguồn khác enum ký của đích.
    /// Nguồn cột: Contract_ContractParty.cs:58.</summary>
    public string? ContractPartyStatus { get; set; }

    /// <summary>Cờ "bên này là đơn vị tạo HĐ" (1 = HĐ đi, 0 = HĐ đến). Nguồn cột: Contract_ContractParty.cs:57.</summary>
    public bool FlagOrgCreate { get; set; }

    /// <summary>UserToken của bên — khóa dedupe khi dựng lưới lịch sử gửi (Contract_ContractController.cs:11262).
    /// Nguồn cột: Contract_ContractParty.cs:59.</summary>
    public string? UserToken { get; set; }

    /// <summary>Mô tả. Nguồn cột: Contract_ContractParty.cs:50.</summary>
    public string? Description { get; set; }

    /// <summary>Ghi chú — nguồn giới hạn 400 ký tự (Contract_Contract_Info.cshtml:506).
    /// Nguồn cột: Contract_ContractParty.cs:51.</summary>
    public string? Remark { get; set; }

    public Contract Contract { get; set; } = null!;
}

// ── Lịch sử gửi HĐ/TL và OTP ─────────────────────────────────────────
/// <summary>
/// Port 1:1 bảng <c>Contract_SendHist</c> — lưới "Lịch sử gửi / Gửi HĐ-TL".
/// Nguồn cột: Common/Models/QContract/Contract_Contract/Contract_SendHist.cs:11-28.
/// Nguồn lưới: Views/Contract_Contract/BindData_Contract_SendHist.cshtml:45-173.
/// </summary>
public class ContractSendHist : IOrgOwned
{
    /// <summary>Khóa tự tăng — nguồn AutoID (Contract_SendHist.cs:11).</summary>
    public int Id { get; set; }

    public Guid OrgId { get; set; }

    /// <summary>Khóa về hợp đồng ở đích. Nguồn dùng chuỗi ContractCode (Contract_SendHist.cs:12).</summary>
    public int ContractId { get; set; }

    /// <summary>Mã bên nhận. Nguồn: Contract_SendHist.cs:13 · hidden BindData_Contract_SendHist.cshtml:111.</summary>
    public string? PartyCode { get; set; }

    /// <summary>UserToken người nhận — khóa dedupe dòng (Contract_ContractController.cs:11262).
    /// Nguồn: Contract_SendHist.cs:14 · hidden BindData_Contract_SendHist.cshtml:113.</summary>
    public string? UserToken { get; set; }

    /// <summary>Họ tên người nhận — cột "Họ tên". Nguồn: Contract_SendHist.cs:15 · BindData_Contract_SendHist.cshtml:58,121.</summary>
    public string UserName { get; set; } = "";

    /// <summary>Tên bên đối tác. Nguồn: Contract_SendHist.cs:20 · hidden BindData_Contract_SendHist.cshtml:112.</summary>
    public string? PartyName { get; set; }

    /// <summary>Thời gian gửi — cột "Thời gian gửi". Nguồn lưu UTC rồi đổi theo UtcOffset khi hiển thị
    /// (BindData_Contract_SendHist.cshtml:79-82). Nguồn cột: Contract_SendHist.cs:16.</summary>
    public DateTime? DTimeSend { get; set; }

    /// <summary>Kênh gửi — cột "Kênh". Nguồn: Contract_SendHist.cs:17 · BindData_Contract_SendHist.cshtml:60,139.</summary>
    public ChannelKind ChannelType { get; set; } = ChannelKind.Email;

    /// <summary>Loại bản tin — cột "Loại". Nguồn: Contract_SendHist.cs:18 · BindData_Contract_SendHist.cshtml:59,125.
    /// Nhãn nguồn: SIGNHD = "Gửi HĐ/TL", OTP = "OTP" (:33-40).</summary>
    public BulletinKind BulletinType { get; set; } = BulletinKind.Contract;

    /// <summary>Thông tin nhận (email / sđt / ZaloUserID) — KHÔNG ĐƯỢC TRỐNG khi gửi
    /// (guard nguồn BindData_Contract_SendHist.cshtml:273, :295). Nguồn cột: Contract_SendHist.cs:19.</summary>
    public string? InfoReceive { get; set; }

    /// <summary>Kết quả gửi — cột "Trạng thái". Nguồn: rỗng ⇒ chưa gửi, "1" ⇒ Sucessful, còn lại ⇒ Failed
    /// (BindData_Contract_SendHist.cshtml:86-99). Nguồn cột: Contract_SendHist.cs:21.</summary>
    public string? FlagActive { get; set; }

    /// <summary>Ghi chú — cột "Ghi chú". Nguồn: Contract_SendHist.cs:22 · BindData_Contract_SendHist.cshtml:166.</summary>
    public string? Remark { get; set; }

    /// <summary>Thời điểm ghi log. Nguồn: Contract_SendHist.cs:23 (LogLUDTimeUTC).</summary>
    public DateTime LogLUDTime { get; set; } = DateTime.Now;

    /// <summary>Người ghi log. Nguồn: Contract_SendHist.cs:24 (LogLUBy).</summary>
    public string? LogLUBy { get; set; }

    public Contract Contract { get; set; } = null!;

    /// <summary>
    /// KHÔNG lưu DB. Với dòng do lưới "dựng thêm" (chưa từng gửi — nguồn Contract_ContractController.cs:11267
    /// cũng chỉ thêm vào list để render, không INSERT), trường này giữ Id của bên tham gia để khi bấm Gửi
    /// còn biết dòng đó thuộc bên nào.
    /// </summary>
    public int PartyRowId { get; set; }

    /// <summary>Nhãn trạng thái theo đúng 3 nhánh nguồn — kể cả chính tả "Sucessful" của nguồn
    /// (BindData_Contract_SendHist.cshtml:94).</summary>
    public string StatusName =>
        string.IsNullOrEmpty(FlagActive) ? "" : FlagActive == "1" ? "Sucessful" : "Failed";
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
