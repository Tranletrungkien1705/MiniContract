using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_OrganizationController.cs — Mst_Org
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:9-32
public class Mst_Org
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:11
    public string OrgID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:13
    public string? NetworkID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:15
    public string? OrgParent { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:17
    public string? OrgBUCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:19
    public string? OrgBUPattern { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:21
    public string? OrgLevel { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:23
    public string? Remark { get; set; }

    // MasterData.Org.cs:617
    public string? OrgIDSln { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:25
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:27
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:29
    public string? LogLUBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Org.cs:31
    public string? MST { get; set; }
}

public class Mst_OrgConfiguration : IEntityTypeConfiguration<Mst_Org>
{
    public void Configure(EntityTypeBuilder<Mst_Org> builder)
    {
        builder.ToTable("Mst_Org");
        builder.HasKey(x => x.OrgID);

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.OrgParent).HasMaxLength(50);
        builder.Property(x => x.OrgBUCode).HasMaxLength(50);
        builder.Property(x => x.OrgBUPattern).HasMaxLength(250);
        builder.Property(x => x.OrgLevel).HasMaxLength(50);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.Property(x => x.OrgIDSln).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.MST).HasMaxLength(50);
    }
}

public class Mst_OrgSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17). Chọn 22 trước Mst_NNT (25).
    public int Order => 22;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Org>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Org
            {
                OrgID = "0",
                OrgParent = null,
                NetworkID = "1001",
                OrgBUCode = "0",
                OrgBUPattern = "0%",
                OrgLevel = "1",
                Remark = "Tổ chức gốc",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_Org
            {
                OrgID = "7200000001",
                OrgParent = "0",
                NetworkID = "1001",
                OrgBUCode = "0.7200000001",
                OrgBUPattern = "0.7200000001%",
                OrgLevel = "2",
                Remark = "Hà Nội",
                FlagActive = "1",
                MST = "0100000001",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_Org
            {
                OrgID = "7200000002",
                OrgParent = "0",
                NetworkID = "1001",
                OrgBUCode = "0.7200000002",
                OrgBUPattern = "0.7200000002%",
                OrgLevel = "2",
                Remark = "Đà Nẵng",
                FlagActive = "1",
                MST = "0100000003",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
