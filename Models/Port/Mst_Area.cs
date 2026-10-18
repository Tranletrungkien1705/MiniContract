using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_AreaController.cs — Mst_Area
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:9-24
public class Mst_Area
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:11
    public string OrgID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:12
    public string AreaCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:13
    public string? NetworkID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:14
    public string? AreaCodeParent { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:15
    public string? AreaBUCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:16
    public string? AreaBUPattern { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:17
    public string? AreaLevel { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:18
    public string? AreaName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:19
    public string? AreaDesc { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:20
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:21
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Area.cs:22
    public string? LogLUBy { get; set; }
}

public class Mst_AreaConfiguration : IEntityTypeConfiguration<Mst_Area>
{
    public void Configure(EntityTypeBuilder<Mst_Area> builder)
    {
        builder.ToTable("Mst_Area");
        // Khoá chính kết hợp (OrgID, AreaCode) theo SQL nguồn Customer.cs:418-419
        builder.HasKey(x => new { x.OrgID, x.AreaCode });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.AreaCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.AreaCodeParent).HasMaxLength(50);
        builder.Property(x => x.AreaBUCode).HasMaxLength(50);
        builder.Property(x => x.AreaBUPattern).HasMaxLength(250);
        builder.Property(x => x.AreaLevel).HasMaxLength(50);
        builder.Property(x => x.AreaName).HasMaxLength(250);
        builder.Property(x => x.AreaDesc).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_AreaSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 20;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Area>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Area
            {
                OrgID = "7200000001",
                AreaCode = "MB",
                NetworkID = "1001",
                AreaName = "Miền Bắc",
                AreaDesc = "Khu vực Miền Bắc",
                AreaCodeParent = null,
                AreaLevel = "1",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_Area
            {
                OrgID = "7200000001",
                AreaCode = "MN",
                NetworkID = "1001",
                AreaName = "Miền Nam",
                AreaDesc = "Khu vực Miền Nam",
                AreaCodeParent = null,
                AreaLevel = "1",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_Area
            {
                OrgID = "7200000001",
                AreaCode = "MT",
                NetworkID = "1001",
                AreaName = "Miền Trung",
                AreaDesc = "Khu vực Miền Trung",
                AreaCodeParent = null,
                AreaLevel = "1",
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_Area
            {
                OrgID = "7200000002",
                AreaCode = "MB",
                NetworkID = "1002",
                AreaName = "Miền Bắc Org2",
                AreaDesc = "Khu vực Miền Bắc Org2",
                AreaCodeParent = null,
                AreaLevel = "1",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
