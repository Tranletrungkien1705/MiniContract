using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ChannelController.cs — Mst_ChannelType
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_ChannelType.sql:1-7
public class Mst_ChannelType
{
    public string ChannelType { get; set; } = "";
    public string? ChannelTypeName { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ChannelTypeConfiguration : IEntityTypeConfiguration<Mst_ChannelType>
{
    public void Configure(EntityTypeBuilder<Mst_ChannelType> builder)
    {
        builder.ToTable("Mst_ChannelType");
        builder.HasKey(x => x.ChannelType);

        builder.Property(x => x.ChannelType).HasMaxLength(50);
        builder.Property(x => x.ChannelTypeName).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ChannelTypeSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 30;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ChannelType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_ChannelType
            {
                ChannelType = "EMAIL",
                ChannelTypeName = "email",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_ChannelType
            {
                ChannelType = "SMS",
                ChannelTypeName = "sms",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_ChannelType
            {
                ChannelType = "ZALO",
                ChannelTypeName = "zalo",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
