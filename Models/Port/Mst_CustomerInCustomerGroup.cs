using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_CustomerController.cs — Mst_CustomerInCustomerGroup
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_CustomerInCustomerGroup.cs:9-29
public class Mst_CustomerInCustomerGroup
{
    // Customer.cs:4574-4575
    public string OrgID { get; set; } = "";
    public string CustomerGrpCode { get; set; } = "";
    public string CustomerCodeSys { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? CodeGuid { get; set; }
}

public class Mst_CustomerInCustomerGroupConfiguration : IEntityTypeConfiguration<Mst_CustomerInCustomerGroup>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerInCustomerGroup> builder)
    {
        builder.ToTable("Mst_CustomerInCustomerGroup");
        builder.HasKey(x => new { x.OrgID, x.CustomerGrpCode, x.CustomerCodeSys });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpCode).HasMaxLength(50);
        builder.Property(x => x.CustomerCodeSys).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.CodeGuid).HasMaxLength(50);
    }
}

public class Mst_CustomerInCustomerGroupSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 44;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerInCustomerGroup>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerInCustomerGroup
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP01",
                CustomerCodeSys = "CUS001",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerInCustomerGroup
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP02",
                CustomerCodeSys = "CUS001",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerInCustomerGroup
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP02",
                CustomerCodeSys = "CUS002",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerInCustomerGroup
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP03",
                CustomerCodeSys = "CUS003",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerInCustomerGroup
            {
                OrgID = "7200000002",
                CustomerGrpCode = "GRP01",
                CustomerCodeSys = "CUS005",
                NetworkID = "1002",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
