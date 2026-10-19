using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 AccountController.cs — Mst_CTSRemoteSupplier
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220709.Contract_Contract\20220718.z1.insertBangCKS_LoaiKy.sql:62-72
// Nguồn Gate: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\AccountController.cs:404-424 (List_Mst_CTSRemoteSupplier_Async)
// Nguồn Base: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\BaseController.cs:1163-1188 (List_Mst_CTSRemoteSupplier_Async, WA_Mst_CTSRemoteSupplierLogin_Get)
public class Mst_CTSRemoteSupplier
{
    public string SupplierCode { get; set; } = "";
    public string? API { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? rpCode { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_CTSRemoteSupplierConfiguration : IEntityTypeConfiguration<Mst_CTSRemoteSupplier>
{
    public void Configure(EntityTypeBuilder<Mst_CTSRemoteSupplier> builder)
    {
        builder.ToTable("Mst_CTSRemoteSupplier");
        builder.HasKey(x => x.SupplierCode);
        builder.Property(x => x.SupplierCode).HasMaxLength(50);
        builder.Property(x => x.API).HasMaxLength(200);
        builder.Property(x => x.Username).HasMaxLength(200);
        builder.Property(x => x.Password).HasMaxLength(200);
        builder.Property(x => x.rpCode).HasMaxLength(200);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_CTSRemoteSupplierSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17; chọn 48 chưa dùng)
    public int Order => 48;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CTSRemoteSupplier>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CTSRemoteSupplier
            {
                SupplierCode = "VNPT",
                API = "https://api.vnpt-ca.vn",
                Username = "vnpt_user",
                Password = "vnpt_password",
                rpCode = "VNPT_RP",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_CTSRemoteSupplier
            {
                SupplierCode = "VIETTEL",
                API = "https://api.viettel-ca.vn",
                Username = "viettel_user",
                Password = "viettel_password",
                rpCode = "VIETTEL_RP",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
