using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Map_UserInNotifyTypeController.cs — Map_UserInNotifyType
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Map_UserInNotifyType.cs:9-17
public class Map_UserInNotifyType
{
    public string UserCode { get; set; } = "";
    public string NotifyType { get; set; } = "";
    public string? FlagNotify { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Map_UserInNotifyTypeConfiguration : IEntityTypeConfiguration<Map_UserInNotifyType>
{
    public void Configure(EntityTypeBuilder<Map_UserInNotifyType> builder)
    {
        builder.ToTable("Map_UserInNotifyType");
        builder.HasKey(x => new { x.UserCode, x.NotifyType });
        builder.Property(x => x.UserCode).HasMaxLength(100);
        builder.Property(x => x.NotifyType).HasMaxLength(100);
        builder.Property(x => x.FlagNotify).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(100);
    }
}

public class Map_UserInNotifyTypeSeeder : IPortSeeder
{
    // Order Map_/Sys_ 100-199 (PortInfra.cs:16-17)
    public int Order => 110;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Map_UserInNotifyType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Map_UserInNotifyType
            {
                UserCode = "U01",
                NotifyType = "NT01",
                FlagNotify = "1",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            },
            new Map_UserInNotifyType
            {
                UserCode = "U01",
                NotifyType = "NT02",
                FlagNotify = "1",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            },
            new Map_UserInNotifyType
            {
                UserCode = "U02",
                NotifyType = "NT01",
                FlagNotify = "1",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            },
            new Map_UserInNotifyType
            {
                UserCode = "U03",
                NotifyType = "NT03",
                FlagNotify = "0",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
