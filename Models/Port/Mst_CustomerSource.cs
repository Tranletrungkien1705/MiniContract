using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_CustomerController.cs — Mst_CustomerSource
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_CustomerSource.cs:9-25
public class Mst_CustomerSource
{
    // Customer.cs:5653-5654
    public string OrgID { get; set; } = "";
    public string CustomerSourceCode { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? CustomerSourceCodeParent { get; set; }
    public string? CustomerSourceBUCode { get; set; }
    public string? CustomerSourceBUPattern { get; set; }
    public string? CustomerSourceLevel { get; set; }
    public string? CustomerSourceName { get; set; }
    public string? CustomerSourceDesc { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_CustomerSourceConfiguration : IEntityTypeConfiguration<Mst_CustomerSource>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerSource> builder)
    {
        builder.ToTable("Mst_CustomerSource");
        builder.HasKey(x => new { x.OrgID, x.CustomerSourceCode });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.CustomerSourceCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.CustomerSourceCodeParent).HasMaxLength(50);
        builder.Property(x => x.CustomerSourceBUCode).HasMaxLength(50);
        builder.Property(x => x.CustomerSourceBUPattern).HasMaxLength(250);
        builder.Property(x => x.CustomerSourceLevel).HasMaxLength(50);
        builder.Property(x => x.CustomerSourceName).HasMaxLength(250);
        builder.Property(x => x.CustomerSourceDesc).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_CustomerSourceSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 42;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerSource>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerSource
            {
                OrgID = "7200000001",
                CustomerSourceCode = "SRC01",
                NetworkID = "1001",
                CustomerSourceName = "Trực tiếp",
                CustomerSourceDesc = "Nguồn khách hàng trực tiếp",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerSource
            {
                OrgID = "7200000001",
                CustomerSourceCode = "SRC02",
                NetworkID = "1001",
                CustomerSourceName = "Giới thiệu",
                CustomerSourceDesc = "Nguồn khách hàng qua giới thiệu",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerSource
            {
                OrgID = "7200000001",
                CustomerSourceCode = "SRC03",
                NetworkID = "1001",
                CustomerSourceName = "Website",
                CustomerSourceDesc = "Nguồn khách hàng từ Website",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerSource
            {
                OrgID = "7200000002",
                CustomerSourceCode = "SRC01",
                NetworkID = "1002",
                CustomerSourceName = "Trực tiếp Org2",
                CustomerSourceDesc = "Nguồn khách hàng trực tiếp Org2",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
