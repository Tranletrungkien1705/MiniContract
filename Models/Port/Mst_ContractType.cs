using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ContractTypeController.cs — Mst_ContractType
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20200402.z11.CreateTableTQcontract.sql:78-99
// Nguồn Model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:9-20
public class Mst_ContractType
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:11
    public string NetworkID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:12
    public string ContractType { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:13
    public string OrgID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:14
    public string ContractTypeName { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:15
    public string? Description { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:16
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:17
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ContractType.cs:18
    public string? LogLUBy { get; set; }
}

public class Mst_ContractTypeConfiguration : IEntityTypeConfiguration<Mst_ContractType>
{
    public void Configure(EntityTypeBuilder<Mst_ContractType> builder)
    {
        builder.ToTable("Mst_ContractType");
        // Khoá chính kết hợp (NetworkID, ContractType) theo DDL 20200402.z11.CreateTableTQcontract.sql:91-94
        // và SQL nguồn Master.cs:45080-45081, 45124-45125
        builder.HasKey(x => new { x.NetworkID, x.ContractType });

        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.ContractType).HasMaxLength(50);
        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.ContractTypeName).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(400);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ContractTypeSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17), chọn 40 chưa dùng
    public int Order => 11;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ContractType>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_ContractType
            {
                NetworkID = "1001",
                ContractType = "HD_KT",
                OrgID = "7200000001",
                ContractTypeName = "Hợp đồng kinh tế",
                Description = "Hợp đồng kinh tế thương mại",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 00:00:00",
                LogLUBy = "port"
            },
            new Mst_ContractType
            {
                NetworkID = "1001",
                ContractType = "HD_LD",
                OrgID = "7200000001",
                ContractTypeName = "Hợp đồng lao động",
                Description = "Hợp đồng lao động chính thức",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 00:00:00",
                LogLUBy = "port"
            },
            new Mst_ContractType
            {
                NetworkID = "1001",
                ContractType = "HD_DV",
                OrgID = "7200000001",
                ContractTypeName = "Hợp đồng dịch vụ",
                Description = "Hợp đồng cung cấp dịch vụ",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 00:00:00",
                LogLUBy = "port"
            },
            new Mst_ContractType
            {
                NetworkID = "1001",
                ContractType = "HD_MB",
                OrgID = "7200000001",
                ContractTypeName = "Hợp đồng mua bán",
                Description = "Hợp đồng mua bán hàng hóa",
                FlagActive = "0",
                LogLUDTimeUTC = "2026-10-18 00:00:00",
                LogLUBy = "port"
            },
            new Mst_ContractType
            {
                NetworkID = "1002",
                ContractType = "HD_VAY",
                OrgID = "7200000002",
                ContractTypeName = "Hợp đồng vay vốn",
                Description = "Hợp đồng vay vốn tín dụng",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 00:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
