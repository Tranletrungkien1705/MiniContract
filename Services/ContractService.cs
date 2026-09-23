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
