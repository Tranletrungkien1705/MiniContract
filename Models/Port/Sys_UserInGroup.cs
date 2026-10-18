using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Sys_GroupController.cs — Sys_UserInGroup
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_UserInGroup.cs:9-26
public class Sys_UserInGroup
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_UserInGroup.cs:11
    public string GroupCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_UserInGroup.cs:13
    public string UserCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_UserInGroup.cs:15
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_UserInGroup.cs:17
    public string? LogLUBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_UserInGroup.cs:19
    public string? MST { get; set; }

    // Thuộc tính mở rộng join với Sys_User (System.cs:2151-2154)
    [NotMapped]
    public string? su_UserCode { get; set; }

    [NotMapped]
    public string? su_UserName { get; set; }

    [NotMapped]
    public string? su_FlagSysAdmin { get; set; }

    [NotMapped]
    public string? su_FlagActive { get; set; }

    // Thuộc tính mở rộng join với Sys_Group (System.cs:2155-2157)
    [NotMapped]
    public string? sg_GroupCode { get; set; }

    [NotMapped]
    public string? sg_GroupName { get; set; }

    [NotMapped]
    public string? sg_FlagActive { get; set; }
}

public class Sys_UserInGroupConfiguration : IEntityTypeConfiguration<Sys_UserInGroup>
{
    public void Configure(EntityTypeBuilder<Sys_UserInGroup> builder)
    {
        builder.ToTable("Sys_UserInGroup");
        builder.HasKey(x => new { x.GroupCode, x.UserCode });
        builder.Property(x => x.GroupCode).HasMaxLength(100);
        builder.Property(x => x.UserCode).HasMaxLength(100);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(100);
        builder.Property(x => x.MST).HasMaxLength(50);
    }
}

public class Sys_UserInGroupSeeder : IPortSeeder
{
    // Order danh mục hệ thống 63 (10-99, chưa dùng trong Models/Port)
    public int Order => 63;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Sys_UserInGroup>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Sys_UserInGroup
            {
                GroupCode = "GRP_ADMIN",
                UserCode = "U01",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Sys_UserInGroup
            {
                GroupCode = "GRP_ADMIN",
                UserCode = "U02",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Sys_UserInGroup
            {
                GroupCode = "GRP_MANAGER",
                UserCode = "U02",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Sys_UserInGroup
            {
                GroupCode = "GRP_ACCOUNTANT",
                UserCode = "U03",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            }
        );

        await db.SaveChangesAsync();
    }
}
