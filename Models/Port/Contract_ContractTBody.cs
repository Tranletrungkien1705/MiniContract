using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_ContractTBody
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9959-9960 (left join lấy ContractFilePath),
//        10050-10060 (select Contract_ContractTBody), 5466-5489 (Save map: TContractBody, FlagContractAvailable,
//        FlagIsFilePath, ContractFileName, ContractFilePath, ContractFileSpec)
public class Contract_ContractTBody
{
    // QContract.cs:9960, 10057 — khoá join
    public string ContractCode { get; set; } = "";

    // QContract.cs:5469 — model.ContractBody (nội dung hợp đồng)
    public string? TContractBody { get; set; }

    // QContract.cs:5470 — model.IsAttachFileContract ? "1" : "0"
    public string? FlagContractAvailable { get; set; }

    // QContract.cs:5471 — model.AttIsFilePath ? "1" : "0"
    public string? FlagIsFilePath { get; set; }

    // QContract.cs:5472 — model.AttFileName
    public string? ContractFileName { get; set; }

    // QContract.cs:5481 — nếu AttIsFilePath thì ContractFilePath = model.AttFileId
    public string? ContractFilePath { get; set; }

    // QContract.cs:5487 — nếu !AttIsFilePath thì đọc file temp → base64 (GAP: file system ngoài)
    public string? ContractFileSpec { get; set; }
}

public class Contract_ContractTBodyConfiguration : IEntityTypeConfiguration<Contract_ContractTBody>
{
    public void Configure(EntityTypeBuilder<Contract_ContractTBody> builder)
    {
        builder.ToTable("Contract_ContractTBody");
        builder.HasKey(x => x.ContractCode);

        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.ContractFilePath).HasMaxLength(500);
        builder.Property(x => x.TContractBody).HasMaxLength(8000);
        builder.Property(x => x.FlagContractAvailable).HasMaxLength(10);
        builder.Property(x => x.FlagIsFilePath).HasMaxLength(10);
        builder.Property(x => x.ContractFileName).HasMaxLength(500);
        builder.Property(x => x.ContractFileSpec).HasMaxLength(8000);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_ContractTBody (nghiệp vụ, Order 200-299).
/// Dữ liệu KHÔNG lấy từ nguồn (nguồn không có seed) — chỉ để chạy thử cột FilePathActual của DoSearch.
/// </summary>
public class Contract_ContractTBodySeeder : IPortSeeder
{
    // Order nghiệp vụ 200-299 (PortInfra.cs:16-17); 210-214 đã dùng
    public int Order => 215;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_ContractTBody>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Contract_ContractTBody
            {
                ContractCode = "HD0001",
                ContractFilePath = "HD0001_body.pdf"
            },
            new Contract_ContractTBody
            {
                ContractCode = "HD0004",
                ContractFilePath = "HD0004_body.pdf"
            }
        );

        await db.SaveChangesAsync();
    }
}
