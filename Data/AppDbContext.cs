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

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minicontract");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();

        b.Entity<ContractType>().HasQueryFilter(x => x.OrgId == _orgId);
        b.Entity<Contract>(e =>
        {
            e.Property(x => x.Value).HasPrecision(18, 2);
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.IsOpen);
            e.Ignore(x => x.SignedCount);
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractParty>(e =>
        {
            e.HasOne(x => x.Contract).WithMany(x => x.Parties).HasForeignKey(x => x.ContractId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ContractSignature>().HasQueryFilter(x => x.OrgId == _orgId);
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
