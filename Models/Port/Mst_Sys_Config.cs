using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_Sys_ConfigController.cs — Mst_Sys_Config
// Nguồn DDL:   D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220709.Contract_Contract\20220718.z1.Insert.Mst_SysConfig.sql:4-18
// Nguồn Model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:9-20
// Nguồn BIZ:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:7224-7312 (WAS), 7313-7439 (Get), 7441-7597 (GetX)
public class Mst_Sys_Config
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:11
    public string OrgID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:12
    public string NetworkID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:13
    public string? FlagCurrency { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:14
    public string? FlagColmnConfig { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:16
    public string? FlagOTP { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:17
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_Sys_Config.cs:18
    public string? LogLUBy { get; set; }
}

public class Mst_Sys_ConfigConfiguration : IEntityTypeConfiguration<Mst_Sys_Config>
{
    public void Configure(EntityTypeBuilder<Mst_Sys_Config> builder)
    {
        builder.ToTable("Mst_Sys_Config");
        // Khoá chính OrgID theo DDL 20220718.z1.Insert.Mst_SysConfig.sql:16-18
        builder.HasKey(x => x.OrgID);

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.FlagCurrency).HasMaxLength(1);
        builder.Property(x => x.FlagColmnConfig).HasMaxLength(1);
        builder.Property(x => x.FlagOTP).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_Sys_ConfigSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17, chưa dùng 12)
    public int Order => 12;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Sys_Config>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Sys_Config
            {
                OrgID = "7200000001",
                NetworkID = "1001",
                FlagCurrency = "0",
                FlagColmnConfig = "0",
                FlagOTP = "1",
                LogLUDTimeUTC = "2024-01-01 00:00:00",
                LogLUBy = "SYS.ADMIN"
            },
            new Mst_Sys_Config
            {
                OrgID = "7200000002",
                NetworkID = "1001",
                FlagCurrency = "1",
                FlagColmnConfig = "1",
                FlagOTP = "0",
                LogLUDTimeUTC = "2024-01-01 00:00:00",
                LogLUBy = "SYS.ADMIN"
            },
            new Mst_Sys_Config
            {
                OrgID = "0",
                NetworkID = "0",
                FlagCurrency = "0",
                FlagColmnConfig = "0",
                FlagOTP = "1",
                LogLUDTimeUTC = "2024-01-01 00:00:00",
                LogLUBy = "SYS.ADMIN"
            }
        );

        await db.SaveChangesAsync();
    }
}
