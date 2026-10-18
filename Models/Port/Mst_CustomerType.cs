using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_CustomerController.cs — Mst_CustomerType
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_CustomerType.cs:9-20
public class Mst_CustomerType
{
    // Customer.cs:3497
    public string CustomerType { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? CustomerTypeName { get; set; }
    public string? CustomerTypeDesc { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_CustomerTypeConfiguration : IEntityTypeConfiguration<Mst_CustomerType>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerType> builder)
    {
        builder.ToTable("Mst_CustomerType");
        builder.HasKey(x => x.CustomerType);

        builder.Property(x => x.CustomerType).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.CustomerTypeName).HasMaxLength(250);
        builder.Property(x => x.CustomerTypeDesc).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_CustomerTypeSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 41;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerType
            {
                CustomerType = "ENTERPRISE",
                NetworkID = "1001",
                CustomerTypeName = "Doanh nghiệp",
                CustomerTypeDesc = "Khách hàng doanh nghiệp",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerType
            {
                CustomerType = "PERSONAL",
                NetworkID = "1001",
                CustomerTypeName = "Cá nhân",
                CustomerTypeDesc = "Khách hàng cá nhân",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerType
            {
                CustomerType = "VIP",
                NetworkID = "1001",
                CustomerTypeName = "Khách hàng VIP",
                CustomerTypeDesc = "Khách hàng VIP ưu tiên",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_CustomerType
            {
                CustomerType = "INACTIVE",
                NetworkID = "1001",
                CustomerTypeName = "Ngừng hoạt động",
                CustomerTypeDesc = "Khách hàng ngừng hoạt động",
                FlagActive = "0",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
