using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_CustomerController.cs — Mst_CustomerGroupImages
// Nguồn: Gate Mst_CustomerController.cs:3277-3389, Excerpt không có DDL
public class Mst_CustomerGroupImages
{
    // Customer.cs:4473-4480, 4566-4567
    public string OrgID { get; set; } = "";
    public string CustomerGrpCode { get; set; } = "";
    public string? Idx { get; set; }
    public string? NetworkID { get; set; }
    public string? ImagePath { get; set; }
    public string? FlagPrimaryImage { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_CustomerGroupImagesConfiguration : IEntityTypeConfiguration<Mst_CustomerGroupImages>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerGroupImages> builder)
    {
        builder.ToTable("Mst_CustomerGroupImages");
        builder.HasKey(x => new { x.OrgID, x.CustomerGrpCode, x.Idx });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpCode).HasMaxLength(50);
        builder.Property(x => x.Idx).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.ImagePath).HasMaxLength(500);
        builder.Property(x => x.FlagPrimaryImage).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_CustomerGroupImagesSeeder : IPortSeeder
{
    public int Order => 18;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerGroupImages>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerGroupImages
            {
                OrgID = "7200000001",
                CustomerGrpCode = "GRP01",
                Idx = "0",
                NetworkID = "1001",
                ImagePath = "/images/grp01.jpg",
                FlagPrimaryImage = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
