using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Map_UserInNotifyTypeController.cs — Mst_ManageNotify
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_ManageNotify.cs:9-17
public class Mst_ManageNotify
{
    public string UserCode { get; set; } = "";
    public string? UserName { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ManageNotifyConfiguration : IEntityTypeConfiguration<Mst_ManageNotify>
{
    public void Configure(EntityTypeBuilder<Mst_ManageNotify> builder)
    {
        builder.ToTable("Mst_ManageNotify");
        builder.HasKey(x => x.UserCode);
        builder.Property(x => x.UserCode).HasMaxLength(100);
        builder.Property(x => x.UserName).HasMaxLength(250);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(100);
    }
}

public class Mst_ManageNotifySeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 66;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ManageNotify>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_ManageNotify
            {
                UserCode = "U01",
                UserName = "Nguyễn Văn Admin",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            },
            new Mst_ManageNotify
            {
                UserCode = "U02",
                UserName = "Trần Thị Tester",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            },
            new Mst_ManageNotify
            {
                UserCode = "U03",
                UserName = "Lê Văn Dev",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
