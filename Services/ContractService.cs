using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models;

namespace MiniContract.Services;

public record ContractDash(int Total, int Draft, int AwaitingSign, int Completed, decimal TotalValue,
    List<(string Status, int Count)> ByStatus);

public interface IContractService
{
    Task<List<Contract>> ListAsync(ContractStatus? status, string? q);
    Task<Contract?> GetAsync(int id);
    Task<List<ContractType>> TypesAsync();
    Task<int> CreateAsync(Contract c, List<ContractParty> parties);
    Task SendAsync(int id);
    Task CancelAsync(int id);
    Task<(bool ok, string msg)> SignCksAsync(int contractId, int partyId);
    string OtpGenerate(int partyId);
    Task<(bool ok, string msg)> SignOtpAsync(int contractId, int partyId, string code);
    Task<ContractDash> DashboardAsync();

    // ── Port 1:1 màn Contract_Contract_Info.cshtml (Thông tin chung) ──
    Task<(bool ok, string msg)> SaveInfoAsync(int id, ContractInfoInput input);
    Task<(bool ok, string msg)> CheckContractNoAsync(int id, string? contractNo);

    // ── Port 1:1 màn BindData_Contract_Party_Info.cshtml (Thông tin các bên) ──
    Task<List<ContractParty>> PartyGridAsync(int contractId);
    Task<(bool ok, string msg)> SavePartyAsync(int contractId, int partyId, PartyInfoInput input);

    // ── Port 1:1 màn BindData_Contract_SendHist.cshtml (Lịch sử gửi) ──
    Task<List<ContractSendHist>> SendHistAsync(int contractId, BulletinKind bulletinType);
    Task<(bool ok, string msg)> ResendAsync(int contractId, BulletinKind bulletinType,
        int[] histIds, string?[] infoReceives, ChannelKind[] channelTypes);
}

/// <summary>
/// Dữ liệu nhập của màn "Thông tin chung" — mỗi trường là một ô <c>name="..."</c> trong
/// Views/Contract_Contract/Contract_Contract_Info.cshtml (số dòng ghi ở Models/Entities.cs).
/// </summary>
public record ContractInfoInput(
    string? ContractNo, string? ContractName, string? TContractCode, int? TypeId,
    DateTime? ContractDate, DateTime? EffDateStart, DateTime? EffDateEnd,
    string? ContractRefNo, bool FlagContractAnnex,
    string? ChannelTypeContract, string? ChannelTypeOTP, bool FlagCheckOTP,
    decimal ValContract, string? CurrencyCode, decimal CurrencyRate, decimal ValPaymented,
    bool FlagCheckOrderSign, bool FlagSysGenAccessKey, string? Remark);

/// <summary>Dữ liệu nhập một dòng lưới "Thông tin các bên" — BindData_Contract_Party_Info.cshtml:263-292.</summary>
public record PartyInfoInput(
    string? CustomerCode, string? CustomerCodeSys, string? CustomerName, string? TaxCode,
    string? CustomerEmail, string? CustomerPhoneNo, string? CustomerMobilePhone,
    string? Facebook, string? ZaloUserID, string? CustomerAddress, string? CustomerWebsite,
    string? BankAccountNo, string? BankName, string? BankCode,
    string? RepresentName, string? RepresentPosition, decimal ValContract);

public class ContractService(AppDbContext db, ISignatureService signer, OtpService otp) : IContractService
{
    public async Task<List<Contract>> ListAsync(ContractStatus? status, string? q)
    {
        var query = db.Contracts.Include(c => c.Type).Include(c => c.Parties).AsQueryable();
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        // BẨY ĐÃ VÁ: string.Contains bị EF dịch thành instr() trên SQLite ⇒ PHÂN BIỆT hoa/thường,
        // trong khi SQL nguồn dùng `like` (collation không phân biệt) ⇒ tìm "loc" không ra "PT-LOC".
        // Dùng EF.Functions.Like để giữ đúng hành vi nguồn. Ô tìm mở rộng thêm ContractNo (số HĐ người dùng nhập).
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = $"%{q.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.Title, kw)
                                  || EF.Functions.Like(c.Code, kw)
                                  || (c.ContractNo != null && EF.Functions.Like(c.ContractNo, kw)));
        }
        var list = await query.ToListAsync();
        return list.OrderByDescending(c => c.CreatedAt).ToList();
    }

    public Task<Contract?> GetAsync(int id) =>
        db.Contracts.Include(c => c.Type).Include(c => c.Parties).Include(c => c.Signatures)
          .FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<ContractType>> TypesAsync() => db.ContractTypes.OrderBy(t => t.Name).ToListAsync();

    public async Task<int> CreateAsync(Contract c, List<ContractParty> parties)
    {
        var count = await db.Contracts.CountAsync();
        c.Code = $"HD{DateTime.Now:yyMM}-{count + 1:D4}";
        c.Status = ContractStatus.Draft;
        int order = 1;
        foreach (var p in parties.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            p.SignOrder = order++;
            c.Parties.Add(p);
        }
        db.Contracts.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    public async Task SendAsync(int id)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (c.Status != ContractStatus.Draft) throw new InvalidOperationException("Chỉ gửi ký hợp đồng ở trạng thái Nháp.");
        if (c.Parties.Count == 0) throw new InvalidOperationException("Hợp đồng chưa có bên tham gia.");
        c.Status = ContractStatus.Sent;
        c.SentAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    public async Task CancelAsync(int id)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (c.Status == ContractStatus.Completed) throw new InvalidOperationException("Không hủy hợp đồng đã hoàn tất.");
        c.Status = ContractStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<(bool ok, string msg)> SignCksAsync(int contractId, int partyId)
    {
        var (c, p, err) = await LoadForSign(contractId, partyId);
        if (err != null) return (false, err);

        var (sigValue, certSubject) = signer.SignContract(c!.Id, c.Title, c.Body, p!.Name);
        db.Signatures.Add(new ContractSignature
        {
            ContractId = c.Id, PartyId = p.Id, Method = SignMethod.DigitalCertificate,
            SignerName = p.Name, CertSubject = certSubject, SignatureValue = sigValue
        });
        MarkSigned(c, p);
        await db.SaveChangesAsync();
        return (true, $"{p.Name} đã ký số (CKS) hợp đồng {c.Code}.");
    }

    public string OtpGenerate(int partyId) => otp.Generate(partyId);

    public async Task<(bool ok, string msg)> SignOtpAsync(int contractId, int partyId, string code)
    {
        var (c, p, err) = await LoadForSign(contractId, partyId);
        if (err != null) return (false, err);
        if (!otp.Verify(partyId, code)) return (false, "Mã OTP không đúng hoặc đã hết hạn.");

        db.Signatures.Add(new ContractSignature
        {
            ContractId = c!.Id, PartyId = p!.Id, Method = SignMethod.Otp,
            SignerName = p.Name, SignatureValue = $"OTP-VERIFIED-{DateTime.UtcNow:yyyyMMddHHmmss}"
        });
        MarkSigned(c, p);
        await db.SaveChangesAsync();
        return (true, $"{p.Name} đã ký qua OTP hợp đồng {c.Code}.");
    }

    public async Task<ContractDash> DashboardAsync()
    {
        var all = await db.Contracts.ToListAsync();
        var byStatus = all.GroupBy(c => c.Status).Select(g => (g.Key.ToString(), g.Count())).ToList();
        return new ContractDash(
            all.Count,
            all.Count(c => c.Status == ContractStatus.Draft),
            all.Count(c => c.Status is ContractStatus.Sent or ContractStatus.PartiallySigned),
            all.Count(c => c.Status == ContractStatus.Completed),
            all.Where(c => c.Status == ContractStatus.Completed).Sum(c => c.Value),
            byStatus);
    }

    // ══ Màn "Thông tin chung" — port 1:1 Contract_Contract_Info.cshtml (bảng Contract_Contract) ══

    /// <summary>
    /// Kiểm tra trùng số hợp đồng. Port 1:1 guard nguồn <c>CheckContractNoExists</c>
    /// (Views/Contract_Contract/Modal.cshtml:1225-1270): chỉ kiểm khi độ dài ≥ 2 (:1231),
    /// và LOẠI chính bản ghi đang sửa khỏi phép so (:1256 — <c>item.ContractCode != contractcode</c>).
    /// </summary>
    public async Task<(bool ok, string msg)> CheckContractNoAsync(int id, string? contractNo)
    {
        var no = (contractNo ?? "").Trim();
        if (no.Length < 2) return (true, "");   // nguồn Modal.cshtml:1231 — bỏ qua, không báo lỗi
        // So KHÔNG phân biệt hoa/thường để giống collation SQL Server của nguồn (SQLite mặc định phân biệt).
        // Dùng ToLower() chứ không dùng Like, vì số HĐ có thể chứa "_"/"%" — hai ký tự đại diện của Like.
        var noLower = no.ToLower();
        var trung = await db.Contracts.AnyAsync(c => c.ContractNo != null
                                                 && c.ContractNo.ToLower() == noLower && c.Id != id);
        return trung ? (false, "Số hợp đồng đã tồn tại!") : (true, "");
    }

    /// <summary>
    /// Lưu "Thông tin chung". Bộ guard port 1:1 từ màn nguồn:
    /// (1) ContractNo bắt buộc — <c>required tab-required</c> Contract_Contract_Info.cshtml:407;
    /// (2) ContractName bắt buộc — :414;
    /// (3) Kênh gửi HĐ/TL bắt buộc — :540; Kênh gửi OTP bắt buộc — :558;
    /// (4) HĐ/TL tham chiếu bắt buộc KHI là phụ lục — dấu * chỉ hiện khi FlagContractAnnex=1
    ///     (Contract_Contract_Info.cshtml:518 dùng biến displayNoneHDG, gán "" tại Update.cshtml:66);
    /// (5) Số HĐ không trùng — <see cref="CheckContractNoAsync"/>;
    /// (6) Chỉ sửa được khi HĐ ở trạng thái nháp: nguồn gán <c>disabled = "disabled-fix"</c> cho
    ///     mọi ô khi ContractStatus khác PENDING (Update.cshtml:154-173).
    /// Hai giá trị dẫn xuất tính đúng công thức nguồn:
    /// ValExchange = ValContract × CurrencyRate (Contract_ContractController.cs:16518; Biz/zTemp.cs:4853),
    /// ValRemain   = ValContract − ValPaymented (Contract_ContractController.cs:16516; Biz/zTemp.cs:4886).
    /// </summary>
    public async Task<(bool ok, string msg)> SaveInfoAsync(int id, ContractInfoInput input)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");

        // (6) khóa sửa sau khi đã gửi ký — tương đương disabled-fix của nguồn
        if (c.Status != ContractStatus.Draft)
            return (false, $"Chỉ sửa được {c.DocCaption} ở trạng thái Nháp (nguồn khóa ô nhập khi khác PENDING).");

        if (string.IsNullOrWhiteSpace(input.ContractNo)) return (false, "Số hợp đồng không được trống.");
        if (string.IsNullOrWhiteSpace(input.ContractName)) return (false, "Tên hợp đồng không được trống.");
        if (string.IsNullOrWhiteSpace(input.ChannelTypeContract)) return (false, "Kênh gửi HĐ/TL không được trống.");
        if (string.IsNullOrWhiteSpace(input.ChannelTypeOTP)) return (false, "Kênh gửi OTP không được trống.");
        if (input.FlagContractAnnex && string.IsNullOrWhiteSpace(input.ContractRefNo))
            return (false, "Là phụ lục thì HĐ/TL tham chiếu không được trống.");
        if (input.EffDateStart.HasValue && input.EffDateEnd.HasValue && input.EffDateEnd < input.EffDateStart)
            return (false, "Ngày hết hiệu lực phải sau ngày hiệu lực.");

        var (okNo, msgNo) = await CheckContractNoAsync(id, input.ContractNo);
        if (!okNo) return (false, msgNo);

        c.ContractNo = input.ContractNo.Trim();
        c.Title = input.ContractName.Trim();     // ContractName ở nguồn = Title ở đích
        c.TContractCode = input.TContractCode;
        c.TypeId = input.TypeId;
        c.ContractDate = input.ContractDate;
        c.EffDateStart = input.EffDateStart;
        c.EffDateEnd = input.EffDateEnd;
        c.ContractRefNo = input.ContractRefNo;
        c.FlagContractAnnex = input.FlagContractAnnex;
        c.ChannelTypeContract = input.ChannelTypeContract;
        c.ChannelTypeOTP = input.ChannelTypeOTP;
        c.FlagCheckOTP = input.FlagCheckOTP;
        c.FlagCheckOrderSign = input.FlagCheckOrderSign;
        c.FlagSysGenAccessKey = input.FlagSysGenAccessKey;
        c.CurrencyCode = input.CurrencyCode;
        c.CurrencyRate = input.CurrencyRate <= 0 ? 1 : input.CurrencyRate;  // nguồn mặc định 1 (:603)
        c.Value = input.ValContract;
        c.ValExchange = input.ValContract * c.CurrencyRate;
        c.Note = input.Remark;

        // Nguồn lưu 4 cột tiền trên BÊN TẠO (CustomerCodeSys = "OWN" — Contract_ContractController.cs:5431),
        // ở đích bên tạo là bên có vai Bên A.
        var own = c.Parties.OrderBy(p => p.SignOrder).FirstOrDefault(p => p.Role == PartyRole.PartyA);
        if (own != null)
        {
            own.ValContract = input.ValContract;
            own.ValPaymented = input.ValPaymented;
            own.ValRemain = input.ValContract - input.ValPaymented;
            own.ValExchange = input.ValContract * c.CurrencyRate;
            own.FlagOrgCreate = true;
            own.Remark = input.Remark;
        }

        await db.SaveChangesAsync();
        return (true, $"Đã lưu thông tin chung {c.DocCaption} {c.ContractNo}.");
    }

    // ══ Màn "Thông tin các bên" — port 1:1 BindData_Contract_Party_Info.cshtml (bảng Contract_ContractParty) ══

    /// <summary>
    /// Lưới các bên. Port 1:1 phép LOẠI bên tạo khỏi lưới: nguồn bỏ dòng có
    /// <c>PartyCode == "PARTY_A"</c> (BindData_Contract_Party_Info.cshtml:21,24).
    /// Ở đích bên tạo được nhận diện bằng vai <see cref="PartyRole.PartyA"/>.
    /// </summary>
    public async Task<List<ContractParty>> PartyGridAsync(int contractId)
    {
        var list = await db.Parties.Where(p => p.ContractId == contractId).ToListAsync();
        return list.Where(p => p.Role != PartyRole.PartyA && p.PartyCode != "PARTY_A")
                   .OrderBy(p => p.SignOrder).ToList();
    }

    /// <summary>
    /// Lưu một dòng lưới các bên. Guard port 1:1: nguồn đặt <c>tab-required</c> cho đúng 5 cột
    /// ContractNo / MST / CustomerName / UserNameSign / UserEmail (BindData_Contract_Party_Info.cshtml:243-246);
    /// ContractNo thuộc hợp đồng nên ở màn này chỉ còn 4 cột của bên tham gia.
    /// ValRemain/ValExchange tính lại theo công thức nguồn (Contract_ContractController.cs:16516,16518).
    /// </summary>
    public async Task<(bool ok, string msg)> SavePartyAsync(int contractId, int partyId, PartyInfoInput input)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        var p = await db.Parties.FirstOrDefaultAsync(x => x.Id == partyId && x.ContractId == contractId);
        if (p == null) return (false, "Không tìm thấy bên tham gia.");
        if (p.HasSigned) return (false, $"{p.Name} đã ký, không sửa được thông tin bên.");

        if (string.IsNullOrWhiteSpace(input.TaxCode)) return (false, "Mã số thuế (MST) không được trống.");
        if (string.IsNullOrWhiteSpace(input.CustomerName)) return (false, "Tên đối tác không được trống.");
        if (string.IsNullOrWhiteSpace(input.CustomerEmail)) return (false, "Email người ký không được trống.");

        p.CustomerCode = input.CustomerCode;
        p.CustomerCodeSys = input.CustomerCodeSys;
        p.CustomerName = input.CustomerName.Trim();
        p.Name = input.CustomerName.Trim();       // UserNameSign ở nguồn ↔ ContractParty.Name ở đích
        p.TaxCode = input.TaxCode.Trim();
        p.Email = input.CustomerEmail.Trim();     // UserEmail ở nguồn ↔ ContractParty.Email ở đích
        p.Phone = input.CustomerPhoneNo;
        p.CustomerMobilePhone = input.CustomerMobilePhone;
        p.Facebook = input.Facebook;
        p.ZaloUserID = input.ZaloUserID;
        p.CustomerAddress = input.CustomerAddress;
        p.CustomerWebsite = input.CustomerWebsite;
        p.BankAccountNo = input.BankAccountNo;
        p.BankName = input.BankName;
        p.BankCode = input.BankCode;
        p.RepresentName = input.RepresentName;
        p.RepresentPosition = input.RepresentPosition;
        p.ValContract = input.ValContract;
        p.ValRemain = input.ValContract - p.ValPaymented;
        p.ValExchange = input.ValContract * c.CurrencyRate;

        await db.SaveChangesAsync();
        return (true, $"Đã lưu thông tin bên {p.Name}.");
    }

    // ══ Màn "Lịch sử gửi / Gửi HĐ-TL" — port 1:1 BindData_Contract_SendHist.cshtml (bảng Contract_SendHist) ══

    /// <summary>
    /// Lưới lịch sử gửi theo loại bản tin. Port 1:1 bước "dựng thêm dòng chưa có" của nguồn
    /// (Contract_ContractController.cs:11250-11291): với mỗi (người nhận × kênh của hợp đồng) mà lịch sử
    /// CHƯA có dòng nào cùng UserToken + ChannelType thì thêm một dòng trống để người dùng gửi lần đầu;
    /// InfoReceive lấy Email nếu kênh EMAIL, còn lại lấy Phone, riêng ZALO lấy ZaloUserID (:11278-11283).
    /// Dòng dựng thêm KHÔNG ghi DB (nguồn cũng chỉ thêm vào list để render).
    /// </summary>
    public async Task<List<ContractSendHist>> SendHistAsync(int contractId, BulletinKind bulletinType)
    {
        var rows = await db.SendHists
            .Where(h => h.ContractId == contractId && h.BulletinType == bulletinType)
            .ToListAsync();

        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return rows;

        foreach (var kenh in KenhCuaHopDong(c, bulletinType))
            foreach (var p in c.Parties.OrderBy(x => x.SignOrder))
            {
                if (rows.Any(h => h.UserToken == (p.UserToken ?? p.Id.ToString()) && h.ChannelType == kenh)) continue;
                rows.Add(new ContractSendHist
                {
                    ContractId = contractId, PartyCode = p.PartyCode ?? p.Id.ToString(),
                    PartyName = p.CustomerName ?? p.Name, UserName = p.Name,
                    UserToken = p.UserToken ?? p.Id.ToString(),
                    ChannelType = kenh, BulletinType = bulletinType,
                    FlagActive = "", Remark = "", PartyRowId = p.Id,
                    InfoReceive = kenh switch
                    {
                        ChannelKind.Email => p.Email,
                        ChannelKind.Zalo => p.ZaloUserID ?? p.Phone,
                        _ => p.Phone
                    }
                });
            }

        return rows.OrderBy(h => h.UserName).ThenBy(h => h.ChannelType).ToList();
    }

    /// <summary>
    /// Gửi lại cho các dòng người dùng tick. Guard port 1:1:
    /// (1) "Thông tin nhận không được trống" — nguồn alert tại BindData_Contract_SendHist.cshtml:273 và :295;
    /// (2) dòng bản tin OTP KHÔNG cho gửi lại — nguồn ẩn ô tick của dòng OTP (:108) và đặt
    ///     <c>canSend = false</c> khi mở popup với bulletintype = OTP (Contract_ContractController.cs:11198-11200);
    /// (3) chỉ gửi khi hợp đồng đã ở trạng thái gửi ký (nguồn chỉ hiện nút "Gửi" khi canSend).
    /// Dòng chưa có trong DB (do lưới dựng thêm) được INSERT khi gửi.
    /// </summary>
    public async Task<(bool ok, string msg)> ResendAsync(int contractId, BulletinKind bulletinType,
        int[] histIds, string?[] infoReceives, ChannelKind[] channelTypes)
    {
        if (bulletinType == BulletinKind.Otp)
            return (false, "Lịch sử gửi OTP chỉ để xem, không gửi lại được.");

        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is not (ContractStatus.Sent or ContractStatus.PartiallySigned))
            return (false, "Chỉ gửi lại được khi hợp đồng đã gửi ký.");
        if (histIds.Length == 0) return (false, "Chưa chọn dòng nào để gửi.");
        if (infoReceives.Length != histIds.Length || channelTypes.Length != histIds.Length)
            return (false, "Dữ liệu gửi không khớp số dòng.");

        for (int i = 0; i < histIds.Length; i++)
            if (string.IsNullOrWhiteSpace(infoReceives[i]))
                return (false, "Thông tin nhận không được trống");

        int daGui = 0;
        for (int i = 0; i < histIds.Length; i++)
        {
            var h = histIds[i] > 0
                ? await db.SendHists.FirstOrDefaultAsync(x => x.Id == histIds[i] && x.ContractId == contractId)
                : null;
            if (h == null)
            {
                // Dòng do lưới dựng thêm (chưa có bản ghi) — PartyId truyền qua histIds ÂM (xem PartyRowId)
                var partyId = -histIds[i];
                var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
                if (p == null) continue;
                h = new ContractSendHist
                {
                    ContractId = contractId, PartyCode = p.PartyCode ?? p.Id.ToString(),
                    PartyName = p.CustomerName ?? p.Name, UserName = p.Name,
                    UserToken = p.UserToken ?? p.Id.ToString(),
                    ChannelType = channelTypes[i], BulletinType = bulletinType
                };
                db.SendHists.Add(h);
            }
            h.InfoReceive = infoReceives[i]!.Trim();
            h.DTimeSend = DateTime.Now;
            h.FlagActive = "1";                 // nguồn: "1" ⇒ Sucessful (BindData_Contract_SendHist.cshtml:92-95)
            h.Remark = $"Gửi lại lúc {DateTime.Now:dd/MM/yyyy HH:mm}";
            h.LogLUDTime = DateTime.Now;
            h.LogLUBy = "web";
            daGui++;
        }

        await db.SaveChangesAsync();
        return (true, $"Đã gửi {daGui} lượt HĐ/TL.");
    }

    /// <summary>Kênh gửi của hợp đồng: bản tin HĐ lấy cột ChannelTypeContract, bản tin OTP lấy ChannelTypeOTP
    /// (hai ô multi-select riêng ở nguồn — Contract_Contract_Info.cshtml:540 và :558). Rỗng ⇒ mặc định EMAIL.</summary>
    private static IEnumerable<ChannelKind> KenhCuaHopDong(Contract c, BulletinKind bulletinType)
    {
        var raw = bulletinType == BulletinKind.Otp ? c.ChannelTypeOTP : c.ChannelTypeContract;
        var found = (raw ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Enum.TryParse<ChannelKind>(s, true, out var k) ? (ChannelKind?)k : null)
            .Where(k => k != null).Select(k => k!.Value).Distinct().ToList();
        return found.Count > 0 ? found : [ChannelKind.Email];
    }

    // ── helpers ──────────────────────────────────────────────────────
    private async Task<(Contract? c, ContractParty? p, string? err)> LoadForSign(int contractId, int partyId)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (null, null, "Không tìm thấy hợp đồng.");
        if (c.Status is not (ContractStatus.Sent or ContractStatus.PartiallySigned))
            return (null, null, "Hợp đồng chưa được gửi ký hoặc đã kết thúc.");
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId);
        if (p == null) return (null, null, "Không tìm thấy bên tham gia.");
        if (p.HasSigned) return (null, null, $"{p.Name} đã ký rồi.");
        return (c, p, null);
    }

    private static void MarkSigned(Contract c, ContractParty p)
    {
        p.HasSigned = true;
        p.SignedAt = DateTime.Now;
        var allSigned = c.Parties.All(x => x.HasSigned);
        if (allSigned) { c.Status = ContractStatus.Completed; c.CompletedAt = DateTime.Now; }
        else c.Status = ContractStatus.PartiallySigned;
    }
}
