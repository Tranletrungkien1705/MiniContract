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
    public DbSet<ContractNumberRule> NumberRules => Set<ContractNumberRule>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractParty> Parties => Set<ContractParty>();
    public DbSet<ContractSignature> Signatures => Set<ContractSignature>();
    public DbSet<ContractHistory> Histories => Set<ContractHistory>();
    public DbSet<ContractSignLink> SignLinks => Set<ContractSignLink>();
    public DbSet<ContractElement> Elements => Set<ContractElement>();
    public DbSet<ContractChecker> Checkers => Set<ContractChecker>();
    public DbSet<ContractUserInContract> UserAssignments => Set<ContractUserInContract>();
    public DbSet<ContractSendHist> SendHistory => Set<ContractSendHist>();
    public DbSet<FinishedContractReason> FinishReasons => Set<FinishedContractReason>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minicontract");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();

        b.Entity<ContractType>().HasQueryFilter(x => x.OrgId == _orgId);
        b.Entity<ContractNumberRule>(e =>
        {
            e.Ignore(x => x.TypefixLabel);
            e.HasIndex(x => new { x.OrgId, x.TypeId }).IsUnique();
            e.HasOne(x => x.Type).WithOne(x => x.NumberRule).HasForeignKey<ContractNumberRule>(x => x.TypeId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Contract>(e =>
        {
            e.Property(x => x.Value).HasPrecision(18, 2);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.IsOpen);
            e.Ignore(x => x.SignedCount);
            e.Ignore(x => x.Kind);
            e.Ignore(x => x.ElementSignedCount);
            e.Ignore(x => x.CheckedCount);
            e.Ignore(x => x.AllChecked);
            e.Ignore(x => x.IsApproved);
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            e.HasOne(x => x.Parent).WithMany(x => x.Annexes).HasForeignKey(x => x.ParentContractId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractParty>(e =>
        {
            e.Ignore(x => x.IsCancelled);
            e.HasOne(x => x.Contract).WithMany(x => x.Parties).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSignature>().HasQueryFilter(x => x.OrgId == _orgId);
        b.Entity<ContractHistory>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.History).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSignLink>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Ignore(x => x.State);
            e.Ignore(x => x.IsUsable);
            e.HasOne(x => x.Contract).WithMany(x => x.SignLinks).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractElement>(e =>
        {
            e.Ignore(x => x.TypeLabel);
            e.HasOne(x => x.Contract).WithMany(x => x.Elements).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractChecker>(e =>
        {
            e.Ignore(x => x.RoleLabel);
            e.Ignore(x => x.HasApproved);
            e.HasOne(x => x.Contract).WithMany(x => x.Checkers).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractUserInContract>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.UserAssignments).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSendHist>(e =>
        {
            e.Ignore(x => x.ChannelLabel);
            e.Ignore(x => x.BulletinLabel);
            e.HasOne(x => x.Contract).WithMany(x => x.SendHistory).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<FinishedContractReason>(e =>
        {
            e.Ignore(x => x.TypeLabel);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
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
