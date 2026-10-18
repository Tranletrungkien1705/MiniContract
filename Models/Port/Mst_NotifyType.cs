using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Map_UserInNotifyTypeController.cs — Mst_NotifyType
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_NotifyType.cs:9-17
public class Mst_NotifyType
{
    public string NotifyType { get; set; } = "";
    public string? NotifyDesc { get; set; }
    public string? DefaultActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_NotifyTypeConfiguration : IEntityTypeConfiguration<Mst_NotifyType>
{
    public void Configure(EntityTypeBuilder<Mst_NotifyType> builder)
    {
        builder.ToTable("Mst_NotifyType");
        builder.HasKey(x => x.NotifyType);
        builder.Property(x => x.NotifyType).HasMaxLength(100);
        builder.Property(x => x.NotifyDesc).HasMaxLength(500);
        builder.Property(x => x.DefaultActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(100);
    }
}

public class Mst_NotifyTypeSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 65;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_NotifyType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_NotifyType
            {
                NotifyType = "NT01",
                NotifyDesc = "Thông báo ký hợp đồng",
                DefaultActive = "1",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            },
            new Mst_NotifyType
            {
                NotifyType = "NT02",
                NotifyDesc = "Thông báo hoàn thành hợp đồng",
                DefaultActive = "1",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            },
            new Mst_NotifyType
            {
                NotifyType = "NT03",
                NotifyDesc = "Thông báo huỷ hợp đồng",
                DefaultActive = "0",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
