using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ChannelController.cs — Mst_ChannelSMS
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:276-284
// và MasterData.cs:22213-22223
public class Mst_ChannelSMS
{
    public string OrgID { get; set; } = "";
    public string? SubFormCodeContractSMS { get; set; }
    public string? SubFormCodeSMSOTP { get; set; }
    public string? SubFormCodeSMSAccessKey { get; set; }
    public string? SMSBrandName { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ChannelSMSConfiguration : IEntityTypeConfiguration<Mst_ChannelSMS>
{
    public void Configure(EntityTypeBuilder<Mst_ChannelSMS> builder)
    {
        builder.ToTable("Mst_ChannelSMS");
        builder.HasKey(x => x.OrgID);

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeContractSMS).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeSMSOTP).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeSMSAccessKey).HasMaxLength(50);
        builder.Property(x => x.SMSBrandName).HasMaxLength(100);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ChannelSMSSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 35;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ChannelSMS>();
        if (await set.AnyAsync()) return;

        set.Add(new Mst_ChannelSMS
        {
            OrgID = "7200000001",
            SubFormCodeContractSMS = "SF_SMS_01",
            SubFormCodeSMSOTP = "SF_SMS_01",
            SMSBrandName = "QContract",
            FlagActive = "1",
            LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = "port"
        });
        await db.SaveChangesAsync();
    }
}
