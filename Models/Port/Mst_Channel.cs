using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ChannelController.cs — Mst_Channel
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:208-216
// và MasterData.cs:22154-22164
public class Mst_Channel
{
    public string OrgID { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? ChannelTypeContract { get; set; }
    public string? ChannelTypeOTP { get; set; }
    public string? ChannelTypeAccessKey { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ChannelConfiguration : IEntityTypeConfiguration<Mst_Channel>
{
    public void Configure(EntityTypeBuilder<Mst_Channel> builder)
    {
        builder.ToTable("Mst_Channel");
        builder.HasKey(x => x.OrgID);

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.ChannelTypeContract).HasMaxLength(50);
        builder.Property(x => x.ChannelTypeOTP).HasMaxLength(50);
        builder.Property(x => x.ChannelTypeAccessKey).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ChannelSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 33;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Channel>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Channel
            {
                OrgID = "7200000001",
                NetworkID = "1001",
                ChannelTypeContract = "EMAIL",
                ChannelTypeOTP = "SMS",
                ChannelTypeAccessKey = "EMAIL",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-01-01 10:00:00",
                LogLUBy = "port"
            },
            new Mst_Channel
            {
                OrgID = "7200000002",
                NetworkID = "1001",
                ChannelTypeContract = "SMS",
                ChannelTypeOTP = "EMAIL",
                ChannelTypeAccessKey = "SMS",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-01-02 10:00:00",
                LogLUBy = "port"
            },
            new Mst_Channel
            {
                OrgID = "7200000003",
                NetworkID = "1001",
                ChannelTypeContract = "ZALO",
                ChannelTypeOTP = "SMS",
                ChannelTypeAccessKey = "ZALO",
                FlagActive = "0",
                LogLUDTimeUTC = "2026-01-03 10:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
