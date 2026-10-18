using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ColumnConfigGroupController.cs — Mst_ColumnConfigGroup
// Nguồn model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:9-20
// Nguồn SQL:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:32021-32047, 52960-53012
public class Mst_ColumnConfigGroup
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:12
    public string OrgID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:11
    public string ColumnConfigGrpCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:13
    public string? NetworkID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:14
    public string? ColumnGrpName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:15
    public string? ColumnGrpFormat { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:16
    public string? ColumnGrpDesc { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:17
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:18
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_ColumnConfigGroup.cs:19
    public string? LogLUBy { get; set; }
}

public class Mst_ColumnConfigGroupConfiguration : IEntityTypeConfiguration<Mst_ColumnConfigGroup>
{
    public void Configure(EntityTypeBuilder<Mst_ColumnConfigGroup> builder)
    {
        builder.ToTable("Mst_ColumnConfigGroup");
        // Khoá chính kết hợp (OrgID, ColumnConfigGrpCode) theo SQL nguồn Master.cs:52022-52023, 52964-52965
        builder.HasKey(x => new { x.OrgID, x.ColumnConfigGrpCode });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.ColumnConfigGrpCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.ColumnGrpName).HasMaxLength(250);
        builder.Property(x => x.ColumnGrpFormat).HasMaxLength(50);
        builder.Property(x => x.ColumnGrpDesc).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ColumnConfigGroupSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 10;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ColumnConfigGroup>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_ColumnConfigGroup
            {
                OrgID = "7200000001",
                ColumnConfigGrpCode = "GRP01",
                NetworkID = "1001",
                ColumnGrpName = "Nhóm cấu hình tiền tệ",
                ColumnGrpFormat = "###,###,###",
                ColumnGrpDesc = "Định dạng hiển thị số tiền",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_ColumnConfigGroup
            {
                OrgID = "7200000001",
                ColumnConfigGrpCode = "GRP02",
                NetworkID = "1001",
                ColumnGrpName = "Nhóm cấu hình số lượng",
                ColumnGrpFormat = "###,###.##",
                ColumnGrpDesc = "Định dạng hiển thị số lượng",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_ColumnConfigGroup
            {
                OrgID = "7200000001",
                ColumnConfigGrpCode = "GRP03",
                NetworkID = "1001",
                ColumnGrpName = "Nhóm cấu hình tỷ lệ phần trăm",
                ColumnGrpFormat = "##0.00%",
                ColumnGrpDesc = "Định dạng hiển thị phần trăm chiết khấu",
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_ColumnConfigGroup
            {
                OrgID = "7200000002",
                ColumnConfigGrpCode = "GRP01",
                NetworkID = "1002",
                ColumnGrpName = "Nhóm cấu hình Org 2",
                ColumnGrpFormat = "###,###",
                ColumnGrpDesc = "Định dạng nhóm Org 2",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
