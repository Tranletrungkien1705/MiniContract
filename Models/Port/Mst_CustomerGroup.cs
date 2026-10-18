using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_CustomerController.cs — Mst_CustomerGroup
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_CustomerGroup.cs:9-27
public class Mst_CustomerGroup
{
    // Customer.cs:4530-4531
    public string OrgID { get; set; } = "";
    public string CustomerGrpCode { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? CustomerGrpCodeParent { get; set; }
    public string? CustomerGrpBUCode { get; set; }
    public string? CustomerGrpBUPattern { get; set; }
    public string? CustomerGrpLevel { get; set; }
    public string? CustomerGrpName { get; set; }
    public string? CustomerGrpDesc { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? CodeGuid { get; set; }
}

public class Mst_CustomerGroupConfiguration : IEntityTypeConfiguration<Mst_CustomerGroup>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerGroup> builder)
    {
        builder.ToTable("Mst_CustomerGroup");
        builder.HasKey(x => new { x.OrgID, x.CustomerGrpCode });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpCodeParent).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpBUCode).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpBUPattern).HasMaxLength(250);
        builder.Property(x => x.CustomerGrpLevel).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpName).HasMaxLength(250);
        builder.Property(x => x.CustomerGrpDesc).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.CodeGuid).HasMaxLength(50);
    }
}

public class Mst_CustomerGroupSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 43;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerGroup>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerGroup
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP01",
                NetworkID = "1001",
                CustomerGrpName = "Nhóm Chiến Lược",
                CustomerGrpDesc = "Nhóm khách hàng chiến lược",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerGroup
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP02",
                NetworkID = "1001",
                CustomerGrpName = "Nhóm Tiềm Năng",
                CustomerGrpDesc = "Nhóm khách hàng tiềm năng",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerGroup
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP03",
                NetworkID = "1001",
                CustomerGrpName = "Nhóm Khách Lẻ",
                CustomerGrpDesc = "Nhóm khách hàng mua lẻ",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerGroup
            {
                OrgID = "7200000002",
                CustomerGrpCode = "GRP01",
                NetworkID = "1002",
                CustomerGrpName = "Nhóm Chiến Lược Org2",
                CustomerGrpDesc = "Nhóm chiến lược Org2",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
