using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_SubmissionFormController.cs — Mst_ParamContractSubmissForm
// Nguồn DDL: D:\idocNet\2020.6.QContract\Dev\V10\05.Refs\Migrate\QuyenNL\20240118.Standardized.80233492000.By.4221896000.sql:51-61
// và MasterData.cs:16417-16796
public class Mst_ParamContractSubmissForm
{
    public string ParamContractCode { get; set; } = "";
    public string? ParamContractType { get; set; }
    public string? ParamContractName { get; set; }
    public string? ParamContractDes { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_ParamContractSubmissFormConfiguration : IEntityTypeConfiguration<Mst_ParamContractSubmissForm>
{
    public void Configure(EntityTypeBuilder<Mst_ParamContractSubmissForm> builder)
    {
        builder.ToTable("Mst_ParamContractSubmissForm");
        builder.HasKey(x => x.ParamContractCode);

        builder.Property(x => x.ParamContractCode).HasMaxLength(50);
        builder.Property(x => x.ParamContractType).HasMaxLength(50);
        builder.Property(x => x.ParamContractName).HasMaxLength(50);
        builder.Property(x => x.ParamContractDes).HasMaxLength(200);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_ParamContractSubmissFormSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 37;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_ParamContractSubmissForm>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_ParamContractSubmissForm
            {
                ParamContractCode = "CONTRACTCODEOS",
                ParamContractType = "Contract_Contract",
                ParamContractName = "ContractCodeOS",
                ParamContractDes = "Số HĐ/TL liên quan",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-01 10:00:00",
                LogLUBy = "port"
            },
            new Mst_ParamContractSubmissForm
            {
                ParamContractCode = "VALPAYMENTED",
                ParamContractType = "Contract_ContractParty",
                ParamContractName = "ValPaymented",
                ParamContractDes = "Giá trị đã thanh toán",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-02 10:00:00",
                LogLUBy = "port"
            },
            new Mst_ParamContractSubmissForm
            {
                ParamContractCode = "VALREMAIN",
                ParamContractType = "Contract_ContractParty",
                ParamContractName = "ValRemain",
                ParamContractDes = "Giá trị còn lại",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-03 10:00:00",
                LogLUBy = "port"
            },
            new Mst_ParamContractSubmissForm
            {
                ParamContractCode = "UNITPRICE",
                ParamContractType = "Contract_ContractDtl",
                ParamContractName = "UnitPrice",
                ParamContractDes = "Đơn giá",
                FlagActive = "1",
                LogLUDTimeUTC = "2024-01-04 10:00:00",
                LogLUBy = "port"
            },
            new Mst_ParamContractSubmissForm
            {
                ParamContractCode = "INACTIVE_PARAM",
                ParamContractType = "Contract_Contract",
                ParamContractName = "InactiveParam",
                ParamContractDes = "Tham số ngừng dùng",
                FlagActive = "0",
                LogLUDTimeUTC = "2024-01-05 10:00:00",
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
