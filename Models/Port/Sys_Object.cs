using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Sys_AccessController.cs — Sys_Object
// Nguồn Model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:9-26
// Nguồn BIZ:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\System.cs:14765-14900 (Sys_Object_Get)
public class Sys_Object
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:11
    public string ObjectCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:13
    public string? ObjectName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:15
    public string? ServiceCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:17
    public string? ObjectType { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:19
    public string? FlagExecModal { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:21
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:23
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:25
    public string? LogLUBy { get; set; }
}

public class Sys_ObjectConfiguration : IEntityTypeConfiguration<Sys_Object>
{
    public void Configure(EntityTypeBuilder<Sys_Object> builder)
    {
        builder.ToTable("Sys_Object");
        // Khoá chính ObjectCode theo Model D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_Object.cs:11
        builder.HasKey(x => x.ObjectCode);

        builder.Property(x => x.ObjectCode).HasMaxLength(100);
        builder.Property(x => x.ObjectName).HasMaxLength(250);
        builder.Property(x => x.ServiceCode).HasMaxLength(100);
        builder.Property(x => x.ObjectType).HasMaxLength(50);
        builder.Property(x => x.FlagExecModal).HasMaxLength(1);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Sys_ObjectSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17, chưa dùng 60)
    public int Order => 60;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Sys_Object>();
        if (await set.AnyAsync()) return;

        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        set.AddRange(
            new Sys_Object
            {
                ObjectCode = "MNU_CONTRACT",
                ObjectName = "Quản lý hợp đồng",
                ServiceCode = "QContract",
                ObjectType = "MENU",
                FlagExecModal = "0",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            },
            new Sys_Object
            {
                ObjectCode = "MNU_CUSTOMER",
                ObjectName = "Quản lý khách hàng",
                ServiceCode = "QContract",
                ObjectType = "MENU",
                FlagExecModal = "0",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            },
            new Sys_Object
            {
                ObjectCode = "MNU_SYSTEM",
                ObjectName = "Quản trị hệ thống",
                ServiceCode = "QContract",
                ObjectType = "MENU",
                FlagExecModal = "0",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            },
            new Sys_Object
            {
                ObjectCode = "MNU_ARCHIVED",
                ObjectName = "Kho lưu trữ (Ẩn)",
                ServiceCode = "QContract",
                ObjectType = "MENU",
                FlagExecModal = "0",
                FlagActive = "0",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            },
            new Sys_Object
            {
                ObjectCode = "BTN_SAVE",
                ObjectName = "Nút lưu hợp đồng",
                ServiceCode = "QContract",
                ObjectType = "BUTTON",
                FlagExecModal = "0",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
