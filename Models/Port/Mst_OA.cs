using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ChannelController.cs — Mst_OA
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20240228.z11.UpdDB.02.MasterData.sql:3-10
public class Mst_OA
{
    public string OAID { get; set; } = "";
    public string? OAName { get; set; }
    public string? OAAvatar { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_OAConfiguration : IEntityTypeConfiguration<Mst_OA>
{
    public void Configure(EntityTypeBuilder<Mst_OA> builder)
    {
        builder.ToTable("Mst_OA");
        builder.HasKey(x => x.OAID);

        builder.Property(x => x.OAID).HasMaxLength(50);
        builder.Property(x => x.OAName).HasMaxLength(200);
        builder.Property(x => x.OAAvatar).HasMaxLength(400);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_OASeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 32;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_OA>();
        if (await set.AnyAsync()) return;

        set.Add(new Mst_OA
        {
            OAID = "1358767413636684272",
            OAName = "Trợ lý số iGoss",
            OAAvatar = "https://s160-ava-talk.zadn.vn/8/8/a/5/1/160/e839a4ba6391c7f4d4940047f142312f.jpg",
            FlagActive = "1",
            LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = "port"
        });
        await db.SaveChangesAsync();
    }
}
