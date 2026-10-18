using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_Sys_ConfigController.cs — Mst_Attribute_Contract
// Nguồn DDL:   D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20200402.z11.CreateTableTQcontract.sql:105-119
// Nguồn Model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:9-19
// Nguồn BIZ:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7974-8070 (WAS), 8071-8227 (Get), 8923-9052 (GetX)
public class Mst_Attribute_Contract
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:11
    public string AttributeContractCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:12
    public string NetworkID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:13
    public string AttributeName { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:14
    public string? DefaultValues { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:15
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:16
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Attribute_Contract.cs:17
    public string? LogLUBy { get; set; }
}

public class Mst_Attribute_ContractConfiguration : IEntityTypeConfiguration<Mst_Attribute_Contract>
{
    public void Configure(EntityTypeBuilder<Mst_Attribute_Contract> builder)
    {
        builder.ToTable("Mst_Attribute_Contract");
        // Khoá chính AttributeContractCode theo CreateTableTQcontract.sql:116-119
        builder.HasKey(x => x.AttributeContractCode);

        builder.Property(x => x.AttributeContractCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.AttributeName).HasMaxLength(200);
        builder.Property(x => x.DefaultValues).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_Attribute_ContractSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17, chưa dùng 13)
    public int Order => 13;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Attribute_Contract>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Attribute_Contract
            {
                AttributeContractCode = "ATTR01",
                NetworkID = "1001",
                AttributeName = "Số hợp đồng gốc",
                DefaultValues = "",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-01 00:00:00",
                LogLUBy = "SYS.ADMIN"
            },
            new Mst_Attribute_Contract
            {
                AttributeContractCode = "ATTR02",
                NetworkID = "1001",
                AttributeName = "Ngày ký phụ lục",
                DefaultValues = "",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-01 00:00:00",
                LogLUBy = "SYS.ADMIN"
            },
            new Mst_Attribute_Contract
            {
                AttributeContractCode = "ATTR03",
                NetworkID = "1001",
                AttributeName = "Ghi chú bổ sung",
                DefaultValues = "",
                FlagActive = "0",
                LogLUDTimeUTC = "2024-01-01 00:00:00",
                LogLUBy = "SYS.ADMIN"
            }
        );

        await db.SaveChangesAsync();
    }
}
