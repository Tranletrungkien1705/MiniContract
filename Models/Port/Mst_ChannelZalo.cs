using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ChannelController.cs — Mst_ChannelZalo
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:227-239
// và MasterData.cs:22241-22256
public class Mst_ChannelZalo
{
    public string OrgID { get; set; } = "";
    public string? SubFormCodeContractZaloUserId { get; set; }
    public string? SubFormCodeContractPhone { get; set; }
    public string? SubFormCodeOTP { get; set; }
    public string? SubFormCodeAccessKeyZaloUserId { get; set; }
    public string? SubFormCodeAccessKeyPhone { get; set; }
    public string? AppID { get; set; }
    public string? AppSecret { get; set; }
    public string? ZaloOAID { get; set; }
    public string? RefreshToken { get; set; }
    public string? AccessToken { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ChannelZaloConfiguration : IEntityTypeConfiguration<Mst_ChannelZalo>
{
    public void Configure(EntityTypeBuilder<Mst_ChannelZalo> builder)
    {
        builder.ToTable("Mst_ChannelZalo");
        builder.HasKey(x => x.OrgID);

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeContractZaloUserId).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeContractPhone).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeOTP).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeAccessKeyZaloUserId).HasMaxLength(50);
        builder.Property(x => x.SubFormCodeAccessKeyPhone).HasMaxLength(50);
        builder.Property(x => x.AppID).HasMaxLength(50);
        builder.Property(x => x.AppSecret).HasMaxLength(50);
        builder.Property(x => x.ZaloOAID).HasMaxLength(50);
        builder.Property(x => x.RefreshToken).HasMaxLength(2000);
        builder.Property(x => x.AccessToken).HasMaxLength(2000);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ChannelZaloSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 36;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ChannelZalo>();
        if (await set.AnyAsync()) return;

        set.Add(new Mst_ChannelZalo
        {
            OrgID = "7200000001",
            SubFormCodeContractPhone = "SF_ZALO_01",
            SubFormCodeOTP = "SF_ZALO_01",
            ZaloOAID = "1358767413636684272",
            FlagActive = "1",
            LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            LogLUBy = "port"
        });
        await db.SaveChangesAsync();
    }
}
