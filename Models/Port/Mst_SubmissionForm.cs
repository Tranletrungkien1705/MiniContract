using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ChannelController.cs — Mst_SubmissionForm
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:24-34
public class Mst_SubmissionForm
{
    public string SubFormCode { get; set; } = "";
    public string? SubFormName { get; set; }
    public string? OrgID { get; set; }
    public string? ChannelType { get; set; }
    public string? BulletinType { get; set; }
    public string? IDZNS { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_SubmissionFormConfiguration : IEntityTypeConfiguration<Mst_SubmissionForm>
{
    public void Configure(EntityTypeBuilder<Mst_SubmissionForm> builder)
    {
        builder.ToTable("Mst_SubmissionForm");
        builder.HasKey(x => x.SubFormCode);

        builder.Property(x => x.SubFormCode).HasMaxLength(50);
        builder.Property(x => x.SubFormName).HasMaxLength(50);
        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.ChannelType).HasMaxLength(50);
        builder.Property(x => x.BulletinType).HasMaxLength(50);
        builder.Property(x => x.IDZNS).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_SubmissionFormSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 31;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_SubmissionForm>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_SubmissionForm
            {
                SubFormCode = "SF_EMAIL_01",
                SubFormName = "Mẫu gửi Email 01",
                OrgID = "7200000001",
                ChannelType = "EMAIL",
                BulletinType = "CONTRACT_NOTIFY",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-01 10:00:00",
                LogLUBy = "port"
            },
            new Mst_SubmissionForm
            {
                SubFormCode = "SF_SMS_01",
                SubFormName = "Mẫu gửi SMS 01",
                OrgID = "7200000001",
                ChannelType = "SMS",
                BulletinType = "OTP",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-02 10:00:00",
                LogLUBy = "port"
            },
            new Mst_SubmissionForm
            {
                SubFormCode = "SF_ZALO_01",
                SubFormName = "Mẫu gửi Zalo 01",
                OrgID = "7200000001",
                ChannelType = "ZALO",
                BulletinType = "ZNS_NOTIFY",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-03 10:00:00",
                LogLUBy = "port"
            },
            new Mst_SubmissionForm
            {
                SubFormCode = "SF_SMS_INACTIVE",
                SubFormName = "Mẫu gửi SMS tạm dừng",
                OrgID = "7200000001",
                ChannelType = "SMS",
                BulletinType = "OTP",
                FlagActive = "0",
                LogLUDTimeUTC = "2024-01-04 10:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
