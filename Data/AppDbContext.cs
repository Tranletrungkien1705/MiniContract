using Microsoft.EntityFrameworkCore;
using MiniContract.Models;

namespace MiniContract.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _orgId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options)
        => _orgId = tenant.OrgId;

    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<ContractType> ContractTypes => Set<ContractType>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractParty> Parties => Set<ContractParty>();
    public DbSet<ContractSignature> Signatures => Set<ContractSignature>();

    /// <summary>Lịch sử gửi HĐ/TL và OTP — port 1:1 bảng nguồn Contract_SendHist.</summary>
    public DbSet<ContractSendHist> SendHists => Set<ContractSendHist>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minicontract");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();

        b.Entity<ContractType>().HasQueryFilter(x => x.OrgId == _orgId);
        b.Entity<Contract>(e =>
        {
            e.Property(x => x.Value).HasPrecision(18, 2);
            // Tiền/tỉ giá port từ Contract_Contract: CurrencyRate, ValExchange
            e.Property(x => x.CurrencyRate).HasPrecision(18, 6);
            e.Property(x => x.ValExchange).HasPrecision(18, 2);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            // Guard nguồn "Số hợp đồng đã tồn tại!" (Modal.cshtml:1225-1270) — chặn tận DB, phạm vi trong 1 tổ chức
            e.HasIndex(x => new { x.OrgId, x.ContractNo }).IsUnique();
            e.Ignore(x => x.IsOpen);
            e.Ignore(x => x.SignedCount);
            e.Ignore(x => x.DocCaption);
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractParty>(e =>
        {
            // Bốn cột tiền của bên tham gia (Contract_ContractParty.cs:36-39)
            e.Property(x => x.ValContract).HasPrecision(18, 2);
            e.Property(x => x.ValPaymented).HasPrecision(18, 2);
            e.Property(x => x.ValRemain).HasPrecision(18, 2);
            e.Property(x => x.ValExchange).HasPrecision(18, 2);
            e.HasOne(x => x.Contract).WithMany(x => x.Parties).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSignature>().HasQueryFilter(x => x.OrgId == _orgId);
        b.Entity<ContractSendHist>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.SendHists).HasForeignKey(x => x.ContractId);
            e.Ignore(x => x.StatusName);
            e.Ignore(x => x.PartyRowId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }

    private void StampOrg()
    {
        foreach (var entry in ChangeTracker.Entries<IOrgOwned>())
            if (entry.State == EntityState.Added && entry.Entity.OrgId == Guid.Empty)
                entry.Entity.OrgId = _orgId;
    }
}
