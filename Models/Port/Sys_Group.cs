using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Sys_GroupController.cs — Sys_Group
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Group.cs:9-23
public class Sys_Group
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Group.cs:11
    public string GroupCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Group.cs:13
    public string? GroupName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Group.cs:15
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Group.cs:17
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Group.cs:19
    public string? LogLUBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Group.cs:21
    public string? MST { get; set; }
}

public class Sys_GroupConfiguration : IEntityTypeConfiguration<Sys_Group>
{
    public void Configure(EntityTypeBuilder<Sys_Group> builder)
    {
        builder.ToTable("Sys_Group");
        builder.HasKey(x => x.GroupCode);
        builder.Property(x => x.GroupCode).HasMaxLength(100);
        builder.Property(x => x.GroupName).HasMaxLength(250);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(100);
        builder.Property(x => x.MST).HasMaxLength(50);
    }
}

public class Sys_GroupSeeder : IPortSeeder
{
    // Order danh mục hệ thống 62 (10-99, chưa dùng trong Models/Port)
    public int Order => 62;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Sys_Group>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Sys_Group
            {
                GroupCode = "GRP_ADMIN",
                GroupName = "Nhóm Quản trị hệ thống",
                FlagActive = "1",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Sys_Group
            {
                GroupCode = "GRP_MANAGER",
                GroupName = "Nhóm Quản lý hợp đồng",
                FlagActive = "1",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Sys_Group
            {
                GroupCode = "GRP_STAFF",
                GroupName = "Nhóm Nhân viên kinh doanh",
                FlagActive = "1",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Sys_Group
            {
                GroupCode = "GRP_ACCOUNTANT",
                GroupName = "Nhóm Kế toán",
                FlagActive = "1",
                MST = "0100000001",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            },
            new Sys_Group
            {
                GroupCode = "GRP_INACTIVE",
                GroupName = "Nhóm Đã ngưng hoạt động",
                FlagActive = "0",
                MST = "0100000002",
                LogLUBy = "port",
                LogLUDTimeUTC = "2026-10-18 00:00:00"
            }
        );

        await db.SaveChangesAsync();
    }
}
