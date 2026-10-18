using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_CustomerController.cs — Mst_GovIDType
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_GovIDType.cs:9-24
public class Mst_GovIDType
{
    public string GovIDType { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? GovIDTypeName { get; set; }
    public string? Remark { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_GovIDTypeConfiguration : IEntityTypeConfiguration<Mst_GovIDType>
{
    public void Configure(EntityTypeBuilder<Mst_GovIDType> builder)
    {
        builder.ToTable("Mst_GovIDType");
        builder.HasKey(x => x.GovIDType);

        builder.Property(x => x.GovIDType).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.GovIDTypeName).HasMaxLength(250);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_GovIDTypeSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 47;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_GovIDType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_GovIDType
            {
                GovIDType = "CCCD",
                GovIDTypeName = "Căn cước công dân",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_GovIDType
            {
                GovIDType = "CMND",
                GovIDTypeName = "Chứng minh nhân dân",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_GovIDType
            {
                GovIDType = "HC",
                GovIDTypeName = "Hộ chiếu",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
