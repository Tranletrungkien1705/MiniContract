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
