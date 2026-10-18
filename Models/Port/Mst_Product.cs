using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ProductController.cs — Mst_Product
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:11-81
public class Mst_Product
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:13
    public string OrgID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:14
    public string ProductCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:15
    public string? NetworkID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:16
    public string? ProductLevelSys { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:17
    public string? ProductCodeUser { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:18
    public string? BrandCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:19
    public string? ProductType { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:20
    public string? ProductGrpCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:21
    public string? ProductName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:22
    public string? ProductNameEN { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:23
    public string? ProductBarCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:24
    public string? ProductCodeNetwork { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:25
    public string? ProductCodeBase { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:26
    public string? ProductCodeRoot { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:27
    public string? ProductImagePathList { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:28
    public string? ProductFilePathList { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:29
    public string? FlagSerial { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:30
    public string? FlagLot { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:31
    public double? ValConvert { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:32
    public string? VATRateCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:33
    public string? UnitCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:34
    public string? FlagSell { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:35
    public string? FlagBuy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:36
    public double? UPBuy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:37
    public double? UPSell { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:38
    public double? QtyMaxSt { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:39
    public double? QtyMinSt { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:40
    public double? QtyEffSt { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:41
    public string? ListOfPrdDynamicFieldValue { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ProductController.cs:4766
    [NotMapped]
    public string? ListAttribute { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\Mst_ProductController.cs:4778
    [NotMapped]
    public string? ListBOM { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:42
    public string? ProductStd { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:43
    public string? ProductExpiry { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:44
    public string? ProductQuyCach { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:45
    public string? ProductMnfUrl { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:46
    public string? ProductIntro { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:47
    public string? ProductUserGuide { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:48
    public string? ProductDrawing { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:49
    public string? ProductOrigin { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:50
    public string? FlagFG { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:51
    public string? DTimeUsed { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:52
    public string? CustomField1 { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:53
    public string? CustomField2 { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:54
    public string? CustomField3 { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:55
    public string? CustomField4 { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:56
    public string? CustomField5 { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:57
    public string? CreateDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:58
    public string? CreateBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:59
    public string? LUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:60
    public string? LUBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:61
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:62
    public string? Remark { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:63
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:64
    public string? LogLUBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\ProductCentrer\Mst_Product.cs:79
    public string? CodeGuid { get; set; }
}

public class Mst_ProductConfiguration : IEntityTypeConfiguration<Mst_Product>
{
    public void Configure(EntityTypeBuilder<Mst_Product> builder)
    {
        builder.ToTable("Mst_Product");
        // Khoá chính kết hợp (OrgID, ProductCode) theo DDL và SQL nguồn Product.cs:1221, 1293
        builder.HasKey(x => new { x.OrgID, x.ProductCode });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.ProductCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.ProductLevelSys).HasMaxLength(50);
        builder.Property(x => x.ProductCodeUser).HasMaxLength(50);
        builder.Property(x => x.BrandCode).HasMaxLength(50);
        builder.Property(x => x.ProductType).HasMaxLength(50);
        builder.Property(x => x.ProductGrpCode).HasMaxLength(50);
        builder.Property(x => x.ProductName).HasMaxLength(250);
        builder.Property(x => x.ProductNameEN).HasMaxLength(250);
        builder.Property(x => x.ProductBarCode).HasMaxLength(50);
        builder.Property(x => x.ProductCodeNetwork).HasMaxLength(50);
        builder.Property(x => x.ProductCodeBase).HasMaxLength(50);
        builder.Property(x => x.ProductCodeRoot).HasMaxLength(50);
        builder.Property(x => x.UnitCode).HasMaxLength(50);
        builder.Property(x => x.VATRateCode).HasMaxLength(50);
        builder.Property(x => x.FlagSerial).HasMaxLength(1);
        builder.Property(x => x.FlagLot).HasMaxLength(1);
        builder.Property(x => x.FlagSell).HasMaxLength(1);
        builder.Property(x => x.FlagBuy).HasMaxLength(1);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.CreateDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.CreateBy).HasMaxLength(50);
        builder.Property(x => x.LUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LUBy).HasMaxLength(50);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.CodeGuid).HasMaxLength(50);
    }
}

public class Mst_ProductSeeder : IPortSeeder
{
    // Order danh mục 10-99 (chọn 55 chưa dùng trong Models/Port)
    public int Order => 55;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Product>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            // 1. Sản phẩm gốc 01: hàng hóa có 2 đơn vị/sản phẩm cơ sở
            new Mst_Product
            {
                OrgID = "1001",
                ProductCode = "PRD_ROOT_01",
                NetworkID = "1001",
                ProductLevelSys = "ROOTPRD",
                ProductCodeUser = "SP001",
                ProductName = "Sản phẩm mẫu 01",
                ProductNameEN = "Sample Product 01",
                ProductGrpCode = "GRP01",
                BrandCode = "BRAND01",
                ProductType = "PRODUCT",
                UnitCode = "CAI",
                FlagActive = "1",
                FlagBuy = "1",
                FlagSell = "1",
                UPBuy = 10000.0,
                UPSell = 15000.0,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            // Sản phẩm cơ sở 1.1 cho PRD_ROOT_01 (BASEPRD)
            new Mst_Product
            {
                OrgID = "1001",
                ProductCode = "PRD_BASE_01_1",
                NetworkID = "1001",
                ProductLevelSys = "BASEPRD",
                ProductCodeUser = "SP001_HOP",
                ProductName = "Sản phẩm mẫu 01 - Hộp",
                ProductCodeRoot = "PRD_ROOT_01",
                ProductCodeBase = "PRD_ROOT_01",
                ProductGrpCode = "GRP01",
                BrandCode = "BRAND01",
                ProductType = "PRODUCT",
                UnitCode = "HOP",
                FlagActive = "1",
                FlagBuy = "1",
                FlagSell = "1",
                UPBuy = 100000.0,
                UPSell = 140000.0,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            // Sản phẩm cơ sở 1.2 cho PRD_ROOT_01 (BASEPRD)
            new Mst_Product
            {
                OrgID = "1001",
                ProductCode = "PRD_BASE_01_2",
                NetworkID = "1001",
                ProductLevelSys = "BASEPRD",
                ProductCodeUser = "SP001_THUNG",
                ProductName = "Sản phẩm mẫu 01 - Thùng",
                ProductCodeRoot = "PRD_ROOT_01",
                ProductCodeBase = "PRD_ROOT_01",
                ProductGrpCode = "GRP01",
                BrandCode = "BRAND01",
                ProductType = "PRODUCT",
                UnitCode = "THUNG",
                FlagActive = "1",
                FlagBuy = "1",
                FlagSell = "1",
                UPBuy = 1000000.0,
                UPSell = 1350000.0,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            // Sản phẩm cấp 2 cho PRD_ROOT_01 (L2PRD)
            new Mst_Product
            {
                OrgID = "1001",
                ProductCode = "PRD_L2_01_1",
                NetworkID = "1001",
                ProductLevelSys = "L2PRD",
                ProductCodeUser = "SP001_L2",
                ProductName = "Sản phẩm mẫu 01 - Cấp 2",
                ProductCodeRoot = "PRD_ROOT_01",
                ProductCodeBase = "PRD_ROOT_01",
                ProductGrpCode = "GRP01",
                BrandCode = "BRAND01",
                ProductType = "PRODUCT",
                UnitCode = "LOC",
                FlagActive = "1",
                FlagBuy = "1",
                FlagSell = "1",
                UPBuy = 50000.0,
                UPSell = 70000.0,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            // 2. Sản phẩm gốc 02: dịch vụ không có đơn vị quy đổi (CountPrdBase = 1)
            new Mst_Product
            {
                OrgID = "1001",
                ProductCode = "PRD_ROOT_02",
                NetworkID = "1001",
                ProductLevelSys = "ROOTPRD",
                ProductCodeUser = "SP002",
                ProductName = "Dịch vụ bảo trì 02",
                ProductNameEN = "Maintenance Service 02",
                ProductGrpCode = "GRP02",
                BrandCode = "BRAND02",
                ProductType = "SERVICE",
                UnitCode = "GIO",
                FlagActive = "1",
                FlagBuy = "0",
                FlagSell = "1",
                UPBuy = 0.0,
                UPSell = 50000.0,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            // 3. Sản phẩm gốc 03: combo ngừng hoạt động (FlagActive = "0")
            new Mst_Product
            {
                OrgID = "1001",
                ProductCode = "PRD_ROOT_03",
                NetworkID = "1001",
                ProductLevelSys = "ROOTPRD",
                ProductCodeUser = "SP003",
                ProductName = "Combo thiết bị 03",
                ProductNameEN = "Device Combo 03",
                ProductGrpCode = "GRP01",
                BrandCode = "BRAND01",
                ProductType = "COMBO",
                UnitCode = "BO",
                FlagActive = "0",
                FlagBuy = "1",
                FlagSell = "1",
                UPBuy = 20000.0,
                UPSell = 35000.0,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            // 4. Sản phẩm gốc 99: thuộc OrgID 9999
            new Mst_Product
            {
                OrgID = "9999",
                ProductCode = "PRD_ROOT_99",
                NetworkID = "1001",
                ProductLevelSys = "ROOTPRD",
                ProductCodeUser = "SP099",
                ProductName = "Sản phẩm Org khác",
                ProductNameEN = "Other Org Product",
                ProductGrpCode = "GRP01",
                BrandCode = "BRAND01",
                ProductType = "PRODUCT",
                UnitCode = "CAI",
                FlagActive = "1",
                FlagBuy = "1",
                FlagSell = "1",
                UPBuy = 10000.0,
                UPSell = 15000.0,
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            }
        );
        await db.SaveChangesAsync();
    }
}
