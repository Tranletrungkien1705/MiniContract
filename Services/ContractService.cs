using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models;

namespace MiniContract.Services;

public record ContractDash(int Total, int Draft, int AwaitingSign, int Completed, decimal TotalValue,
    List<(string Status, int Count)> ByStatus);

/// <summary>Thống kê ô ký theo loại — port từ Contract_ContractElementSum (QContract).</summary>
public record ElementStats(int Total, int Signed, int Electronic, int Short, int Digital);

/// <summary>Thống kê người kiểm tra — port từ Contract_Checker (QContract).</summary>
public record CheckerStats(int Total, int Checked, int Pending, CheckerStatus Status);

public interface IContractService
{
    Task<List<Contract>> ListAsync(ContractStatus? status, string? q);
    Task<Contract?> GetAsync(int id);
    Task<List<ContractType>> TypesAsync();
    Task<int> CreateAsync(Contract c, List<ContractParty> parties);
    Task<int> CreateAnnexAsync(int parentId, Contract annex, List<ContractParty> parties);
    Task<List<Contract>> AnnexesAsync(int parentId);
    Task SendAsync(int id);
    Task CancelAsync(int id);
    Task<(bool ok, string msg)> SignCksAsync(int contractId, int partyId);
    string OtpGenerate(int partyId);
    Task<(bool ok, string msg)> SignOtpAsync(int contractId, int partyId, string code);
    Task<List<ContractHistory>> HistoryAsync(int contractId);
    Task AddRemarkAsync(int contractId, string actor, string remark);
    Task<ContractDash> DashboardAsync();

    // ── Link ký công khai (Contract_ContractSignLink) ────────────────
    Task<ContractSignLink> CreateSignLinkAsync(int contractId, int partyId, int validHours, string actor);
    Task<List<ContractSignLink>> SignLinksAsync(int contractId);
    Task RevokeSignLinkAsync(int linkId, string actor);
    Task<ContractSignLink?> ResolveSignLinkAsync(string token);
    Task<(bool ok, string msg)> SignViaLinkAsync(string token, string? signerName);

    // ── Ô ký trên hợp đồng (Contract_ContractElement) ────────────────
    Task<List<ContractElement>> ElementsAsync(int contractId);
    Task<ContractElement> AddElementAsync(int contractId, ContractElement el);
    Task<(bool ok, string msg)> SignElementAsync(int elementId, string signerName, string signFrom, string? ip);
    Task<ElementStats> ElementStatsAsync(int contractId);

    // ── Người kiểm tra hợp đồng (Contract_Checker) ───────────────────
    Task<List<ContractChecker>> CheckersAsync(int contractId);
    Task<ContractChecker> AddCheckerAsync(int contractId, ContractChecker checker);
    Task<(bool ok, string msg)> AcceptCheckAsync(int contractId, int checkerId, string? remark);
    Task<CheckerStats> CheckerStatsAsync(int contractId);

    // ── Lý do kết thúc hợp đồng (Mst_FinishedContractReason) ─────────
    Task<List<FinishedContractReason>> FinishReasonsAsync(bool activeOnly = false);
    Task<FinishedContractReason> AddFinishReasonAsync(FinishedContractReason reason);
    Task<(bool ok, string msg)> FinishContractAsync(int contractId, int reasonId, string? description, string actor);

    // ── Phân quyền hợp đồng (Contract_UserInContract) ────────────────
    Task<List<ContractUserInContract>> UserAssignmentsAsync(int contractId);
    Task<(bool ok, string msg)> SaveUserAssignmentsAsync(int contractId, List<ContractUserInContract> users, string actor);

    // ── Lịch sử gửi hợp đồng (Contract_SendHist) ─────────────────────
    Task<List<ContractSendHist>> SendHistoryAsync(int contractId, BulletinType? bulletin = null);
    Task<ContractSendHist> AddSendHistAsync(int contractId, int? partyId, ChannelType channel, BulletinType bulletin, string? infoReceive, string? remark, string actor);
    Task<(bool ok, string msg)> ResendAsync(int contractId, List<int> sendHistIds, string actor);
}

public class ContractService(AppDbContext db, ISignatureService signer, OtpService otp) : IContractService
{
    public async Task<List<Contract>> ListAsync(ContractStatus? status, string? q)
    {
        var query = db.Contracts.Include(c => c.Type).Include(c => c.Parties).AsQueryable();
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Title.Contains(q) || c.Code.Contains(q));
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
        await LogAsync(c.Id, HistoryAction.Created, c.CreatedBy, $"Tạo {c.Kind.ToLower()} {c.Code}");
        return c.Id;
    }

    // Tạo phụ lục cho 1 hợp đồng gốc. Nguồn QContract: FlagContractAnnex=1 + ContractRefNo = số HĐ cha.
    public async Task<int> CreateAnnexAsync(int parentId, Contract annex, List<ContractParty> parties)
    {
        var parent = await db.Contracts.FirstOrDefaultAsync(x => x.Id == parentId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng gốc.");
        if (parent.IsAnnex) throw new InvalidOperationException("Không thể tạo phụ lục của một phụ lục.");
        if (parent.Status == ContractStatus.Cancelled) throw new InvalidOperationException("Hợp đồng gốc đã hủy, không tạo phụ lục.");

        var count = await db.Contracts.CountAsync();
        annex.Code = $"PL{DateTime.Now:yyMM}-{count + 1:D4}";
        annex.Status = ContractStatus.Draft;
        annex.IsAnnex = true;
        annex.ParentContractId = parent.Id;
        annex.ParentContractCode = parent.Code;
        int order = 1;
        foreach (var p in parties.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            p.SignOrder = order++;
            annex.Parties.Add(p);
        }
        db.Contracts.Add(annex);
        await db.SaveChangesAsync();
        await LogAsync(annex.Id, HistoryAction.Created, annex.CreatedBy, $"Tạo phụ lục {annex.Code} cho hợp đồng {parent.Code}");
        await LogAsync(parent.Id, HistoryAction.Remark, annex.CreatedBy, $"Phát sinh phụ lục {annex.Code}");
        return annex.Id;
    }

    public Task<List<Contract>> AnnexesAsync(int parentId) =>
        db.Contracts.Include(c => c.Parties).Where(c => c.ParentContractId == parentId)
          .OrderByDescending(c => c.CreatedAt).ToListAsync();

    public async Task SendAsync(int id)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (c.Status != ContractStatus.Draft) throw new InvalidOperationException("Chỉ gửi ký hợp đồng ở trạng thái Nháp.");
        if (c.Parties.Count == 0) throw new InvalidOperationException("Hợp đồng chưa có bên tham gia.");
        c.Status = ContractStatus.Sent;
        c.SentAt = DateTime.Now;
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Sent, c.CreatedBy, $"Gửi {c.Kind.ToLower()} {c.Code} cho {c.Parties.Count} bên ký");
    }

    public async Task CancelAsync(int id)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (c.Status == ContractStatus.Completed) throw new InvalidOperationException("Không hủy hợp đồng đã hoàn tất.");
        c.Status = ContractStatus.Cancelled;
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Cancelled, c.CreatedBy, $"Hủy {c.Kind.ToLower()} {c.Code}");
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
        await LogAsync(c.Id, HistoryAction.Signed, p.Name, $"{p.Name} ký số (CKS) — {Ui.Role(p.Role)}");
        if (c.Status == ContractStatus.Completed)
            await LogAsync(c.Id, HistoryAction.Completed, "system", $"Đủ chữ ký các bên — {c.Code} hoàn tất");
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
        await LogAsync(c.Id, HistoryAction.Signed, p.Name, $"{p.Name} ký qua OTP — {Ui.Role(p.Role)}");
        if (c.Status == ContractStatus.Completed)
            await LogAsync(c.Id, HistoryAction.Completed, "system", $"Đủ chữ ký các bên — {c.Code} hoàn tất");
        return (true, $"{p.Name} đã ký qua OTP hợp đồng {c.Code}.");
    }

    public Task<List<ContractHistory>> HistoryAsync(int contractId) =>
        db.Histories.Where(h => h.ContractId == contractId).OrderByDescending(h => h.At).ToListAsync();

    public async Task AddRemarkAsync(int contractId, string actor, string remark)
    {
        if (string.IsNullOrWhiteSpace(remark)) return;
        await LogAsync(contractId, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor, remark.Trim());
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

    // ── Link ký công khai (Contract_ContractSignLink) ────────────────
    // Tạo link ký cho 1 bên: sinh token bí mật + thời hạn (SignLinkEndDate).
    // Nguồn QContract: WAS_Contract_ContractSignLink_Save (SignLink + SignLinkEndDate).
    public async Task<ContractSignLink> CreateSignLinkAsync(int contractId, int partyId, int validHours, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is not (ContractStatus.Sent or ContractStatus.PartiallySigned))
            throw new InvalidOperationException("Chỉ tạo link ký khi hợp đồng đã gửi và đang chờ ký.");
        var p = c.Parties.FirstOrDefault(x => x.Id == partyId)
            ?? throw new KeyNotFoundException("Không tìm thấy bên tham gia.");
        if (p.HasSigned) throw new InvalidOperationException($"{p.Name} đã ký rồi.");
        if (validHours <= 0) validHours = 72;

        var link = new ContractSignLink
        {
            ContractId = c.Id, PartyId = p.Id,
            Token = "sl_" + Guid.NewGuid().ToString("N"),
            EndDate = DateTime.Now.AddHours(validHours)
        };
        db.SignLinks.Add(link);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.SignLinkCreated, actor,
            $"Tạo link ký công khai cho {p.Name} — hết hạn {link.EndDate:dd/MM/yyyy HH:mm}");
        return link;
    }

    public Task<List<ContractSignLink>> SignLinksAsync(int contractId) =>
        db.SignLinks.Include(x => x.Party).Where(x => x.ContractId == contractId)
          .OrderByDescending(x => x.CreatedAt).ToListAsync();

    public async Task RevokeSignLinkAsync(int linkId, string actor)
    {
        var link = await db.SignLinks.Include(x => x.Party).FirstOrDefaultAsync(x => x.Id == linkId)
            ?? throw new KeyNotFoundException("Không tìm thấy link ký.");
        if (link.Revoked) return;
        link.Revoked = true;
        await db.SaveChangesAsync();
        await LogAsync(link.ContractId, HistoryAction.SignLinkRevoked, actor,
            $"Thu hồi link ký của {link.Party?.Name}");
    }

    // Kiểm tra link còn hiệu lực (tồn tại + chưa hết hạn + chưa thu hồi).
    // Nguồn QContract: Contract_ContractSignLink_CheckLink (SignLinkEndDate >= now).
    public Task<ContractSignLink?> ResolveSignLinkAsync(string token) =>
        db.SignLinks.Include(x => x.Contract).Include(x => x.Party)
          .FirstOrDefaultAsync(x => x.Token == token);

    // Ký qua link công khai (không cần đăng nhập) — dùng chữ ký CKS phía server.
    public async Task<(bool ok, string msg)> SignViaLinkAsync(string token, string? signerName)
    {
        var link = await ResolveSignLinkAsync(token);
        if (link == null) return (false, "Link ký không tồn tại.");
        if (link.Revoked) return (false, "Link ký đã bị thu hồi.");
        if (DateTime.Now > link.EndDate) return (false, "Link ký đã hết hạn.");
        if (link.UsedAt != null) return (false, "Link ký đã được sử dụng.");

        var (c, p, err) = await LoadForSign(link.ContractId, link.PartyId);
        if (err != null) return (false, err);

        var name = string.IsNullOrWhiteSpace(signerName) ? p!.Name : signerName.Trim();
        var (sigValue, certSubject) = signer.SignContract(c!.Id, c.Title, c.Body, name);
        db.Signatures.Add(new ContractSignature
        {
            ContractId = c.Id, PartyId = p!.Id, Method = SignMethod.DigitalCertificate,
            SignerName = name, CertSubject = certSubject, SignatureValue = sigValue
        });
        link.UsedAt = DateTime.Now;
        MarkSigned(c, p);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Signed, name, $"{name} ký qua link công khai — {Ui.Role(p.Role)}");
        if (c.Status == ContractStatus.Completed)
            await LogAsync(c.Id, HistoryAction.Completed, "system", $"Đủ chữ ký các bên — {c.Code} hoàn tất");
        return (true, $"{name} đã ký hợp đồng {c.Code} qua link công khai.");
    }

    // ── Ô ký trên hợp đồng (Contract_ContractElement) ────────────────
    // Nguồn QContract: WAS_Contract_ContractElement_Update (thêm/sửa ô ký) +
    // WAS_Contract_ContractElement_Calc (thống kê theo ElementType).
    public Task<List<ContractElement>> ElementsAsync(int contractId) =>
        db.Elements.Include(x => x.Party).Where(x => x.ContractId == contractId)
          .OrderBy(x => x.PageIdx).ThenBy(x => x.ElementY).ThenBy(x => x.ElementX).ToListAsync();

    public async Task<ContractElement> AddElementAsync(int contractId, ContractElement el)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            throw new InvalidOperationException("Hợp đồng đã kết thúc, không thêm ô ký.");
        if (el.PartyId.HasValue && c.Parties.All(p => p.Id != el.PartyId.Value))
            throw new InvalidOperationException("Bên sở hữu ô ký không thuộc hợp đồng này.");
        if (string.IsNullOrWhiteSpace(el.ElementCode)) el.ElementCode = "EL" + Guid.NewGuid().ToString("N")[..6];
        if (el.ElementWidth <= 0) el.ElementWidth = 120;
        if (el.ElementHeight <= 0) el.ElementHeight = 40;
        el.ContractId = c.Id;
        el.IsSigned = false;
        db.Elements.Add(el);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, "web",
            $"Thêm ô ký '{el.ElementName}' ({el.TypeLabel}) cho {(el.PartyId.HasValue ? c.Parties.First(p => p.Id == el.PartyId).Name : "—")}");
        return el;
    }

    // Đánh dấu 1 ô ký đã được ký — port từ ElementSignStatus/ConfirmBy/ConfirmDTimeUTC (QContract).
    public async Task<(bool ok, string msg)> SignElementAsync(int elementId, string signerName, string signFrom, string? ip)
    {
        var el = await db.Elements.Include(x => x.Contract).Include(x => x.Party)
            .FirstOrDefaultAsync(x => x.Id == elementId);
        if (el == null) return (false, "Không tìm thấy ô ký.");
        if (el.IsSigned) return (false, "Ô ký này đã được ký rồi.");
        if (el.Contract.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            return (false, "Hợp đồng đã kết thúc, không thể ký ô ký.");

        el.IsSigned = true;
        el.SignerName = string.IsNullOrWhiteSpace(signerName) ? (el.Party?.Name ?? "—") : signerName.Trim();
        el.SignedAt = DateTime.Now;
        el.SignFrom = string.IsNullOrWhiteSpace(signFrom) ? "web" : signFrom;
        el.ElementIP = ip;
        await db.SaveChangesAsync();
        await LogAsync(el.ContractId, HistoryAction.Signed, el.SignerName,
            $"Ký ô '{el.ElementName}' ({el.TypeLabel}) — từ {el.SignFrom}");
        return (true, $"Đã ký ô '{el.ElementName}'.");
    }

    // Thống kê ô ký theo loại — port từ Contract_ContractElementSum (QContract).
    public async Task<ElementStats> ElementStatsAsync(int contractId)
    {
        var els = await db.Elements.Where(x => x.ContractId == contractId).ToListAsync();
        return new ElementStats(
            els.Count,
            els.Count(e => e.IsSigned),
            els.Count(e => e.Type == ElementType.Electronic),
            els.Count(e => e.Type == ElementType.Short),
            els.Count(e => e.Type == ElementType.Digital));
    }

    // ── Người kiểm tra hợp đồng (Contract_Checker) ───────────────────
    // Nguồn QContract: WAS_Contract_Checker_Accept (kiểm tra tuần tự theo Idx) +
    // Contract_Checker_CheckDB (kiểm tra tồn tại/FlagChecker/FlagActive).
    public Task<List<ContractChecker>> CheckersAsync(int contractId) =>
        db.Checkers.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Idx).ThenBy(x => x.Id).ToListAsync();

    public async Task<ContractChecker> AddCheckerAsync(int contractId, ContractChecker checker)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            throw new InvalidOperationException("Hợp đồng đã kết thúc, không thêm người kiểm tra.");
        if (string.IsNullOrWhiteSpace(checker.UserName))
            throw new InvalidOperationException("Cần tên người kiểm tra.");
        if (string.IsNullOrWhiteSpace(checker.UserCode))
            checker.UserCode = checker.UserName.Trim().ToLower().Replace(" ", ".");
        if (checker.Idx <= 0) checker.Idx = c.Checkers.Count + 1;
        checker.ContractId = c.Id;
        checker.HasChecked = false;
        db.Checkers.Add(checker);
        await db.SaveChangesAsync();

        // Có người kiểm tra → hợp đồng chuyển sang trạng thái chờ kiểm tra (PENDING).
        if (c.CheckerStatus == CheckerStatus.None)
        {
            c.CheckerStatus = CheckerStatus.Pending;
            await db.SaveChangesAsync();
        }
        await LogAsync(c.Id, HistoryAction.Remark, "web",
            $"Thêm người kiểm tra '{checker.UserName}' (thứ tự {checker.Idx}{(checker.Sequential ? ", tuần tự" : "")})");
        return checker;
    }

    // Người kiểm tra xác nhận đã kiểm tra — port từ Contract_Checker_AcceptX (QContract).
    // Kiểm tra tuần tự: nếu FlagSeq=1 thì phải kiểm tra theo thứ tự Idx (không được nhảy cóc).
    public async Task<(bool ok, string msg)> AcceptCheckAsync(int contractId, int checkerId, string? remark)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Completed or ContractStatus.Cancelled)
            return (false, "Hợp đồng đã kết thúc, không thể kiểm tra.");
        var ck = c.Checkers.FirstOrDefault(x => x.Id == checkerId);
        if (ck == null) return (false, "Không tìm thấy người kiểm tra.");
        if (!ck.IsChecker) return (false, $"{ck.UserName} không phải người kiểm tra.");
        if (ck.HasChecked) return (false, $"{ck.UserName} đã kiểm tra rồi.");

        // Kiểm tra tuần tự: các người kiểm tra trước (Idx nhỏ hơn) phải đã kiểm tra xong.
        if (ck.Sequential)
        {
            var earlier = c.Checkers.Where(x => x.IsChecker && x.Idx < ck.Idx && !x.HasChecked).ToList();
            if (earlier.Count > 0)
                return (false, $"Phải kiểm tra theo thứ tự — còn {earlier.Count} người kiểm tra trước chưa xử lý.");
        }

        ck.HasChecked = true;
        ck.CheckedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(remark)) ck.Remark = remark.Trim();

        // Đủ người kiểm tra → hợp đồng chuyển sang ONPROCESS (đã kiểm tra xong).
        var allChecked = c.Checkers.Where(x => x.IsChecker).All(x => x.HasChecked);
        if (allChecked) c.CheckerStatus = CheckerStatus.OnProcess;
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Remark, ck.UserName,
            $"{ck.UserName} kiểm tra hợp đồng{(string.IsNullOrWhiteSpace(remark) ? "" : " — " + remark.Trim())}");
        if (allChecked)
            await LogAsync(c.Id, HistoryAction.Remark, "system", $"Đủ người kiểm tra — {c.Code} chuyển sang xử lý");
        return (true, allChecked
            ? $"{ck.UserName} đã kiểm tra. Đủ người kiểm tra — hợp đồng chuyển sang xử lý."
            : $"{ck.UserName} đã kiểm tra hợp đồng.");
    }

    // Thống kê người kiểm tra — port từ Contract_Checker (QContract).
    public async Task<CheckerStats> CheckerStatsAsync(int contractId)
    {
        var c = await db.Contracts.Include(x => x.Checkers).FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return new CheckerStats(0, 0, 0, CheckerStatus.None);
        var checkers = c.Checkers.Where(x => x.IsChecker).ToList();
        return new CheckerStats(
            checkers.Count,
            checkers.Count(x => x.HasChecked),
            checkers.Count(x => !x.HasChecked),
            c.CheckerStatus);
    }

    // ── Lý do kết thúc hợp đồng (Mst_FinishedContractReason) ─────────
    // Nguồn QContract: Mst_FinishedContractReason_CreateX / _UpdateX / _CheckDB.
    public async Task<List<FinishedContractReason>> FinishReasonsAsync(bool activeOnly = false)
    {
        var q = db.FinishReasons.AsQueryable();
        if (activeOnly) q = q.Where(x => x.Active);
        return await q.OrderBy(x => x.Type).ThenBy(x => x.Name).ToListAsync();
    }

    public async Task<FinishedContractReason> AddFinishReasonAsync(FinishedContractReason reason)
    {
        if (string.IsNullOrWhiteSpace(reason.Name))
            throw new InvalidOperationException("Cần tên lý do kết thúc.");
        if (string.IsNullOrWhiteSpace(reason.Code))
            reason.Code = "LR" + Guid.NewGuid().ToString("N")[..6].ToUpper();
        if (await db.FinishReasons.AnyAsync(x => x.Code == reason.Code))
            throw new InvalidOperationException($"Mã lý do '{reason.Code}' đã tồn tại.");
        db.FinishReasons.Add(reason);
        await db.SaveChangesAsync();
        return reason;
    }

    // Kết thúc/chấm dứt hợp đồng theo 1 lý do — port từ Contract_ContractParty_FinishX (QContract).
    // Ghi nhận lý do + mô tả + thời điểm, chuyển trạng thái hợp đồng sang FINISHED.
    public async Task<(bool ok, string msg)> FinishContractAsync(int contractId, int reasonId, string? description, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc.");
        var reason = await db.FinishReasons.FirstOrDefaultAsync(x => x.Id == reasonId);
        if (reason == null) return (false, "Không tìm thấy lý do kết thúc.");
        if (!reason.Active) return (false, $"Lý do '{reason.Name}' đã ngừng hiệu lực.");

        c.Status = ContractStatus.Finished;
        c.FinishReasonCode = reason.Code;
        c.FinishReasonName = reason.Name;
        c.FinishDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        c.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        var desc = $"{c.Kind} {c.Code} kết thúc — lý do: {reason.Name} ({reason.TypeLabel})"
            + (c.FinishDescription != null ? $" — {c.FinishDescription}" : "");
        await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor, desc);
        return (true, $"Đã kết thúc {c.Kind.ToLower()} {c.Code} — {reason.Name}.");
    }

    // ── Phân quyền hợp đồng (Contract_UserInContract) ────────────────
    // Nguồn QContract: WAS_Contract_UserInContract_Save → Contract_UserInContract_SaveX.
    // Danh sách người dùng được phân quyền trên hợp đồng (theo thứ tự thêm).
    public Task<List<ContractUserInContract>> UserAssignmentsAsync(int contractId) =>
        db.UserAssignments.Where(x => x.ContractId == contractId)
          .OrderBy(x => x.Id).ToListAsync();

    // Lưu phân quyền hợp đồng — GHI ĐÈ TOÀN BỘ (full replace): xoá hết phân quyền cũ của
    // hợp đồng rồi ghi lại danh sách mới, đúng như Contract_UserInContract_SaveX (delete all + insert all).
    public async Task<(bool ok, string msg)> SaveUserAssignmentsAsync(int contractId, List<ContractUserInContract> users, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không phân quyền.");

        // Chuẩn hoá + loại trùng theo UserCode (giữ bản ghi đầu tiên).
        var clean = new List<ContractUserInContract>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in users ?? [])
        {
            var code = (u.UserCode ?? "").Trim();
            var name = (u.UserName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name)) continue;
            if (string.IsNullOrWhiteSpace(code)) code = name.ToLower().Replace(" ", ".");
            if (!seen.Add(code)) continue;
            clean.Add(new ContractUserInContract
            {
                ContractId = c.Id, UserCode = code, UserName = string.IsNullOrWhiteSpace(name) ? code : name,
                Email = string.IsNullOrWhiteSpace(u.Email) ? null : u.Email.Trim(),
                AssignedBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor
            });
        }

        // Xoá toàn bộ phân quyền cũ của hợp đồng (delete all).
        var old = await db.UserAssignments.Where(x => x.ContractId == c.Id).ToListAsync();
        db.UserAssignments.RemoveRange(old);
        // Ghi lại danh sách mới (insert all).
        db.UserAssignments.AddRange(clean);
        await db.SaveChangesAsync();

        await LogAsync(c.Id, HistoryAction.Remark, string.IsNullOrWhiteSpace(actor) ? "web" : actor,
            $"Phân quyền hợp đồng cho {clean.Count} người dùng" + (clean.Count > 0 ? ": " + string.Join(", ", clean.Select(x => x.UserName)) : ""));
        return (true, $"Đã cập nhật phân quyền hợp đồng — {clean.Count} người dùng.");
    }

    // ── Lịch sử gửi hợp đồng (Contract_SendHist) ─────────────────────
    // Nguồn QContract: WAS_Contract_SendHist_Get → Contract_SendHist_GetX (tra cứu theo
    // ContractCode + BulletinType, sắp theo thời gian gửi tăng dần).
    public Task<List<ContractSendHist>> SendHistoryAsync(int contractId, BulletinType? bulletin = null)
    {
        var q = db.SendHistory.Include(x => x.Party).Where(x => x.ContractId == contractId);
        if (bulletin.HasValue) q = q.Where(x => x.Bulletin == bulletin.Value);
        return q.OrderBy(x => x.SentAt).ThenBy(x => x.Id).ToListAsync();
    }

    // Ghi 1 bản ghi lịch sử gửi — port từ WAS_Contract_SendHist_Add → Contract_SendHist_SaveX.
    // Thông tin nhận (InfoReceive) mặc định lấy từ bên nhận theo kênh nếu không truyền vào.
    public async Task<ContractSendHist> AddSendHistAsync(int contractId, int? partyId, ChannelType channel,
        BulletinType bulletin, string? infoReceive, string? remark, string actor)
    {
        var c = await db.Contracts.Include(x => x.Parties).FirstOrDefaultAsync(x => x.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        var p = partyId.HasValue ? c.Parties.FirstOrDefault(x => x.Id == partyId.Value) : null;
        if (partyId.HasValue && p == null)
            throw new InvalidOperationException("Bên nhận không thuộc hợp đồng này.");

        var info = string.IsNullOrWhiteSpace(infoReceive)
            ? (channel == ChannelType.Email ? p?.Email : p?.Phone)
            : infoReceive.Trim();

        var h = new ContractSendHist
        {
            ContractId = c.Id, PartyId = p?.Id, PartyName = p?.Name ?? "",
            UserName = p?.Name ?? "", UserToken = p != null ? $"ut_{p.Id}" : null,
            Channel = channel, Bulletin = bulletin, InfoReceive = info,
            Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim(),
            SentBy = string.IsNullOrWhiteSpace(actor) ? "web" : actor
        };
        db.SendHistory.Add(h);
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, h.SentBy,
            $"Gửi {h.BulletinLabel} cho {h.PartyName} qua {h.ChannelLabel}" + (info != null ? $" → {info}" : ""));
        return h;
    }

    // Gửi lại (resend) các bản ghi đã chọn — port từ ContractReSendHist → ResendChannel (QContract).
    // Mỗi lần gửi lại tạo 1 bản ghi lịch sử mới (giữ nguyên bản ghi cũ) để có vết gửi đầy đủ.
    public async Task<(bool ok, string msg)> ResendAsync(int contractId, List<int> sendHistIds, string actor)
    {
        var c = await db.Contracts.FirstOrDefaultAsync(x => x.Id == contractId);
        if (c == null) return (false, "Không tìm thấy hợp đồng.");
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
            return (false, "Hợp đồng đã hủy hoặc đã kết thúc, không gửi lại.");
        if (sendHistIds == null || sendHistIds.Count == 0)
            return (false, "Chưa chọn bản ghi để gửi lại.");

        var src = await db.SendHistory.Where(x => x.ContractId == contractId && sendHistIds.Contains(x.Id)).ToListAsync();
        if (src.Count == 0) return (false, "Không tìm thấy bản ghi gửi để gửi lại.");

        var who = string.IsNullOrWhiteSpace(actor) ? "web" : actor;
        foreach (var s in src)
            db.SendHistory.Add(new ContractSendHist
            {
                ContractId = c.Id, PartyId = s.PartyId, PartyName = s.PartyName,
                UserName = s.UserName, UserToken = s.UserToken, Channel = s.Channel,
                Bulletin = s.Bulletin, InfoReceive = s.InfoReceive,
                Remark = "Gửi lại", SentBy = who
            });
        await db.SaveChangesAsync();
        await LogAsync(c.Id, HistoryAction.Remark, who, $"Gửi lại {src.Count} bản tin cho các bên");
        return (true, $"Đã gửi lại {src.Count} bản tin.");
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

    // Ghi 1 dòng nhật ký thao tác (audit trail) — port từ Contract_Contract_HistAction (QContract).
    private async Task LogAsync(int contractId, HistoryAction action, string actor, string description)
    {
        db.Histories.Add(new ContractHistory
        {
            ContractId = contractId, Action = action,
            Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            Description = description
        });
        await db.SaveChangesAsync();
    }
}
