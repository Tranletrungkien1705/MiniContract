using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_OrganizationController.cs — Mst_SignType
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220709.Contract_Contract\20220718.z11.insertbangMst_signType.sql:4-12
public class Mst_SignType
{
    public string SignType { get; set; } = "";
    public string NetworkID { get; set; } = "";
    public string SignTypeName { get; set; } = "";
    public string? Remark { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_SignTypeConfiguration : IEntityTypeConfiguration<Mst_SignType>
{
    public void Configure(EntityTypeBuilder<Mst_SignType> builder)
    {
        builder.ToTable("Mst_SignType");
        builder.HasKey(x => new { x.SignType, x.NetworkID });

        builder.Property(x => x.SignType).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.SignTypeName).HasMaxLength(200);
        builder.Property(x => x.Remark).HasMaxLength(400);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_SignTypeSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 46;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_SignType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_SignType
            {
                SignType = "REMOTE",
                NetworkID = "1001",
                SignTypeName = "Ký số từ xa (Remote Signing)",
                Remark = "Ký số qua dịch vụ Cloud CA / Remote Signing",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_SignType
            {
                SignType = "USBTOKEN",
                NetworkID = "1001",
                SignTypeName = "Ký số USB Token",
                Remark = "Ký số sử dụng USB Token phần cứng",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_SignType
            {
                SignType = "HSM",
                NetworkID = "1001",
                SignTypeName = "Ký số HSM",
                Remark = "Ký số qua thiết bị HSM tập trung",
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
