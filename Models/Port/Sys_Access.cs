using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Sys_AccessController.cs — Sys_Access
// Nguồn Model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Access.cs:9-31
// Nguồn BIZ:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:445-560 / 221-443 (Sys_Access_Get)
public class Sys_Access
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Access.cs:11
    public string GroupCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Access.cs:13
    public string ObjectCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Access.cs:15
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Access.cs:17
    public string? LogLUBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Access.cs:20-30 (các cột join từ Sys_Object)
    [NotMapped]
    public string? so_ObjectCode { get; set; }

    [NotMapped]
    public string? so_ObjectName { get; set; }

    [NotMapped]
    public string? so_ServiceCode { get; set; }

    [NotMapped]
    public string? so_ObjectType { get; set; }

    [NotMapped]
    public string? so_FlagExecModal { get; set; }

    [NotMapped]
    public string? so_FlagActive { get; set; }
}

public class Sys_AccessConfiguration : IEntityTypeConfiguration<Sys_Access>
{
    public void Configure(EntityTypeBuilder<Sys_Access> builder)
    {
        builder.ToTable("Sys_Access");
        // Khoá chính kết hợp GroupCode + ObjectCode theo Model D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Access.cs:11-13
        builder.HasKey(x => new { x.GroupCode, x.ObjectCode });

        builder.Property(x => x.GroupCode).HasMaxLength(100);
        builder.Property(x => x.ObjectCode).HasMaxLength(100);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Sys_AccessSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17, chưa dùng 61, chạy sau Sys_ObjectSeeder 60)
    public int Order => 61;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Sys_Access>();
        if (await set.AnyAsync()) return;

        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        set.AddRange(
            new Sys_Access
            {
                GroupCode = "ADMIN",
                ObjectCode = "MNU_CONTRACT",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            },
            new Sys_Access
            {
                GroupCode = "SALE_MGR",
                ObjectCode = "MNU_CONTRACT",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            },
            new Sys_Access
            {
                GroupCode = "ADMIN",
                ObjectCode = "MNU_CUSTOMER",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            },
            new Sys_Access
            {
                GroupCode = "ADMIN",
                ObjectCode = "MNU_SYSTEM",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
