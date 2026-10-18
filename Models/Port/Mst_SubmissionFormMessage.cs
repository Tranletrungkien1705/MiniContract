using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_SubmissionFormController.cs — Mst_SubmissionFormMessage
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:114-126
public class Mst_SubmissionFormMessage
{
    public string SubFormCode { get; set; } = "";
    public string? Message { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_SubmissionFormMessageConfiguration : IEntityTypeConfiguration<Mst_SubmissionFormMessage>
{
    public void Configure(EntityTypeBuilder<Mst_SubmissionFormMessage> builder)
    {
        builder.ToTable("Mst_SubmissionFormMessage");
        builder.HasKey(x => x.SubFormCode);

        builder.Property(x => x.SubFormCode).HasMaxLength(50);
        builder.Property(x => x.Message); // nvarchar(max)
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_SubmissionFormMessageSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 38;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_SubmissionFormMessage>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_SubmissionFormMessage
            {
                SubFormCode = "SF_EMAIL_01",
                Message = "Kính gửi quý khách, hợp đồng của quý khách đã được tạo thành công.",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-01 10:00:00",
                LogLUBy = "port"
            },
            new Mst_SubmissionFormMessage
            {
                SubFormCode = "SF_SMS_01",
                Message = "Ma OTP cua quy khach la: {OTP}. Vui long khong chia se ma nay.",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-02 10:00:00",
                LogLUBy = "port"
            },
            new Mst_SubmissionFormMessage
            {
                SubFormCode = "SF_SMS_INACTIVE",
                Message = "Tin nhan tam dung.",
                FlagActive = "0",
                LogLUDTimeUTC = "2024-01-04 10:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
