using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ChannelController.cs — Mst_ChannelEmail
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:251-263
// và MasterData.cs:22181-22194
public class Mst_ChannelEmail
{
    public string OrgID { get; set; } = "";
    public string? SubFormCodeEmailContract { get; set; }
    public string? SubFormCodeEmailOTP { get; set; }
    public string? SubFormCodeEmailAccessKey { get; set; }
    public string? MailFrom { get; set; }
    public string? APIsSendMail { get; set; }
    public string? ApiKeySendMail { get; set; }
    public string? SolutionCodeSendMail { get; set; }
    public string? DisplayNameMailFrom { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ChannelEmailConfiguration : IEntityTypeConfiguration<Mst_ChannelEmail>
{
    public void Configure(EntityTypeBuilder<Mst_ChannelEmail> builder)
    {
        builder.ToTable("Mst_ChannelEmail");
        builder.HasKey(x => x.OrgID);

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeEmailContract).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeEmailOTP).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeEmailAccessKey).HasMaxLength(50);
        builder.Property(x => x.MailFrom).HasMaxLength(50);
        builder.Property(x => x.APIsSendMail).HasMaxLength(200);
        builder.Property(x => x.ApiKeySendMail).HasMaxLength(200);
        builder.Property(x => x.SolutionCodeSendMail).HasMaxLength(50);
        builder.Property(x => x.DisplayNameMailFrom).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ChannelEmailSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 34;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ChannelEmail>();
        if (await set.AnyAsync()) return;

        set.Add(new Mst_ChannelEmail
        {
            OrgID = "7200000001",
            SubFormCodeEmailContract = "SF_EMAIL_01",
            SubFormCodeEmailOTP = "SF_EMAIL_01",
            SubFormCodeEmailAccessKey = "SF_EMAIL_01",
            MailFrom = "support@qcontract.vn",
            FlagActive = "1",
            LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = "port"
        });
        await db.SaveChangesAsync();
    }
}
