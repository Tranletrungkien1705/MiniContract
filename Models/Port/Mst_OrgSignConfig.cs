using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_OrgCKSController.cs — Mst_OrgSignConfig
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220709.Contract_Contract\20220718.z1.insertBangCKS_LoaiKy.sql:4-30
// và Model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_OrgSignConfig.cs
public class Mst_OrgSignConfig
{
    public long AutoID { get; set; }
    public string? SignType { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? CANumber { get; set; }
    public string? CAOrg { get; set; }
    public string? CAEffDTimeUTCStart { get; set; }
    public string? CAEffDTimeUTCEnd { get; set; }
    public string? CTSPath { get; set; }
    public string? CTSPwd { get; set; }
    public string? ServerSignFilePath { get; set; }
    public string? ServerSignPassword { get; set; }
    public string? SupplierCode { get; set; }
    public string? RemoteSignAgreementUUID { get; set; }
    public string? RemoteSignPassCode { get; set; }
    public string? AuthenCode { get; set; }
    public string? UpdateDTimeUTC { get; set; }
    public string? UpdateBy { get; set; }
    public string? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_OrgSignConfigConfiguration : IEntityTypeConfiguration<Mst_OrgSignConfig>
{
    public void Configure(EntityTypeBuilder<Mst_OrgSignConfig> builder)
    {
        builder.ToTable("Mst_OrgSignConfig");
        builder.HasKey(x => x.AutoID);
        builder.Property(x => x.AutoID).ValueGeneratedOnAdd();

        builder.Property(x => x.SignType).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.CANumber).HasMaxLength(50);
        builder.Property(x => x.CAOrg).HasMaxLength(200);
        builder.Property(x => x.CAEffDTimeUTCStart).HasMaxLength(50);
        builder.Property(x => x.CAEffDTimeUTCEnd).HasMaxLength(50);
        builder.Property(x => x.CTSPath).HasMaxLength(400);
        builder.Property(x => x.CTSPwd).HasMaxLength(50);
        builder.Property(x => x.ServerSignFilePath).HasMaxLength(200);
        builder.Property(x => x.ServerSignPassword).HasMaxLength(200);
        builder.Property(x => x.SupplierCode).HasMaxLength(50);
        builder.Property(x => x.RemoteSignAgreementUUID).HasMaxLength(200);
        builder.Property(x => x.RemoteSignPassCode).HasMaxLength(200);
        builder.Property(x => x.AuthenCode).HasMaxLength(200);
        builder.Property(x => x.UpdateDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.UpdateBy).HasMaxLength(50);
        builder.Property(x => x.CreateDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.CreateBy).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_OrgSignConfigSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 40;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_OrgSignConfig>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_OrgSignConfig
            {
                OrgID = "7200000001",
                SignType = "REMOTE",
                NetworkID = "1001",
                CANumber = "CA7200000001",
                CAOrg = "VNPT-CA",
                CAEffDTimeUTCStart = "2026-01-01 00:00:00",
                CAEffDTimeUTCEnd = "2027-01-01 00:00:00",
                SupplierCode = "VNPT",
                RemoteSignAgreementUUID = "uuid-vnpt-001",
                RemoteSignPassCode = "pass123",
                AuthenCode = "SMS_OTP",
                FlagActive = "1",
                CreateDTimeUTC = "2026-01-01 00:00:00",
                CreateBy = "seed",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "seed"
            },
            new Mst_OrgSignConfig
            {
                OrgID = "7200000001",
                SignType = "USBTOKEN",
                NetworkID = "1001",
                CANumber = "CAUSB000001",
                CAOrg = "VIETTEL-CA",
                CAEffDTimeUTCStart = "2026-01-01 00:00:00",
                CAEffDTimeUTCEnd = "2027-01-01 00:00:00",
                FlagActive = "1",
                CreateDTimeUTC = "2026-01-01 00:00:00",
                CreateBy = "seed",
                LogLUDTimeUTC = "2026-01-01 00:00:00",
                LogLUBy = "seed"
            }
        );

        await db.SaveChangesAsync();
    }
}
