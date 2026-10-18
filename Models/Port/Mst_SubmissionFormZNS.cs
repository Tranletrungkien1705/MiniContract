using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_SubmissionFormController.cs — Mst_SubmissionFormZNS
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\20220906.1530.NangCapMoi\Mst_SubmissionForm.sql:3-18
public class Mst_SubmissionFormZNS
{
    public string SubFormCode { get; set; } = "";
    public string ParamContractCodeZNS { get; set; } = "";
    public string? SourceDataType { get; set; }
    public string? ParamContractCode { get; set; }
    public string? ParamValue { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    [NotMapped]
    public string? ParamContractName { get; set; }

    [NotMapped]
    public string? ParamContractType { get; set; }
}

public class Mst_SubmissionFormZNSConfiguration : IEntityTypeConfiguration<Mst_SubmissionFormZNS>
{
    public void Configure(EntityTypeBuilder<Mst_SubmissionFormZNS> builder)
    {
        builder.ToTable("Mst_SubmissionFormZNS");
        builder.HasKey(x => new { x.SubFormCode, x.ParamContractCodeZNS });

        builder.Property(x => x.SubFormCode).HasMaxLength(50);
        builder.Property(x => x.ParamContractCodeZNS).HasMaxLength(50);
        builder.Property(x => x.SourceDataType).HasMaxLength(50);
        builder.Property(x => x.ParamContractCode).HasMaxLength(50);
        builder.Property(x => x.ParamValue).HasMaxLength(200);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_SubmissionFormZNSSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 39;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_SubmissionFormZNS>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_SubmissionFormZNS
            {
                SubFormCode = "SF_ZALO_01",
                ParamContractCodeZNS = "contract_code",
                SourceDataType = "SYSTEM",
                ParamContractCode = "CONTRACTCODEOS",
                ParamValue = "HD-2024-001",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-03 10:00:00",
                LogLUBy = "port"
            },
            new Mst_SubmissionFormZNS
            {
                SubFormCode = "SF_ZALO_01",
                ParamContractCodeZNS = "payment_amount",
                SourceDataType = "SYSTEM",
                ParamContractCode = "VALPAYMENTED",
                ParamValue = "5000000",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-03 10:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
