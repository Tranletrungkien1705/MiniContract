using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ProductGroupController.cs / Mst_ProductController.cs — Mst_ProductGroup
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:9-26
public class Mst_ProductGroup
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:11
    public string OrgID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:12
    public string ProductGrpCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:13
    public string? NetworkID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:14
    public string? ProductGrpCodeParent { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:15
    public string? ProductGrpBUCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:16
    public string? ProductGrpBUPattern { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:17
    public string? ProductGrpName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:18
    public string? ProductGrpDesc { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:19
    public string? BrandCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:20
    public string? ProductGrpLevel { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:21
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:22
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:23
    public string? LogLUBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_ProductGroup.cs:25
    public string? CodeGuid { get; set; }
}

public class Mst_ProductGroupConfiguration : IEntityTypeConfiguration<Mst_ProductGroup>
{
    public void Configure(EntityTypeBuilder<Mst_ProductGroup> builder)
    {
        builder.ToTable("Mst_ProductGroup");
        // Khoá chính kết hợp (OrgID, ProductGrpCode) theo DDL và SQL nguồn Product.cs:1271, 1318
        builder.HasKey(x => new { x.OrgID, x.ProductGrpCode });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.ProductGrpCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.ProductGrpCodeParent).HasMaxLength(50);
        builder.Property(x => x.ProductGrpBUCode).HasMaxLength(50);
        builder.Property(x => x.ProductGrpBUPattern).HasMaxLength(250);
        builder.Property(x => x.ProductGrpName).HasMaxLength(250);
        builder.Property(x => x.ProductGrpDesc).HasMaxLength(500);
        builder.Property(x => x.BrandCode).HasMaxLength(50);
        builder.Property(x => x.ProductGrpLevel).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.CodeGuid).HasMaxLength(50);
    }
}

public class Mst_ProductGroupSeeder : IPortSeeder
{
    // Order danh mục 10-99 (chọn 56 chưa dùng trong Models/Port)
    public int Order => 56;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ProductGroup>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_ProductGroup
            {
                OrgID = "1001",
                ProductGrpCode = "GRP01",
                NetworkID = "1001",
                ProductGrpName = "Nhóm hàng hoá 01",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            new Mst_ProductGroup
            {
                OrgID = "1001",
                ProductGrpCode = "GRP02",
                NetworkID = "1001",
                ProductGrpName = "Nhóm dịch vụ 02",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            new Mst_ProductGroup
            {
                OrgID = "9999",
                ProductGrpCode = "GRP99",
                NetworkID = "1001",
                ProductGrpName = "Nhóm Org khác",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            }
        );
        await db.SaveChangesAsync();
    }
}
