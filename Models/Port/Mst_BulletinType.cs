using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_SubmissionFormController.cs — Mst_BulletinType
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:138-150
public class Mst_BulletinType
{
    public string BulletinType { get; set; } = "";
    public string? BulletinTypeName { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_BulletinTypeConfiguration : IEntityTypeConfiguration<Mst_BulletinType>
{
    public void Configure(EntityTypeBuilder<Mst_BulletinType> builder)
    {
        builder.ToTable("Mst_BulletinType");
        builder.HasKey(x => x.BulletinType);

        builder.Property(x => x.BulletinType).HasMaxLength(50);
        builder.Property(x => x.BulletinTypeName).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_BulletinTypeSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 29;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_BulletinType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_BulletinType
            {
                BulletinType = "CONTRACT_NOTIFY",
                BulletinTypeName = "Thông báo hợp đồng",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_BulletinType
            {
                BulletinType = "OTP",
                BulletinTypeName = "Mã xác thực OTP",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_BulletinType
            {
                BulletinType = "ZNS_NOTIFY",
                BulletinTypeName = "Thông báo ZNS",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_BulletinType
            {
                BulletinType = "INACTIVE_BULLETIN",
                BulletinTypeName = "Bản tin tạm dừng",
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
