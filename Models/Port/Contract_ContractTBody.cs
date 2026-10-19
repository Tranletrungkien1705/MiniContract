using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_ContractTBody
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9959-9960 (left join lấy ContractFilePath),
//        10050-10060 (select Contract_ContractTBody)
// GAP: DDL/model đầy đủ KHÔNG nằm trong source_excerpt.txt (đề bài cấm đọc file ngoài excerpt).
//      Chỉ khai cột mà SQL nguồn CHẮC CHẮN dùng — xem .parity-run\conflicts\11_conflict.md.
public class Contract_ContractTBody
{
    // QContract.cs:9960, 10057 — khoá join
    public string ContractCode { get; set; } = "";

    // QContract.cs:9948 — cctb.ContractFilePath (khi ContractStatus = 'PENDING')
    public string? ContractFilePath { get; set; }

    // QContract.cs:10054 — cctb.* (các cột còn lại của bảng KHÔNG có trong excerpt -> GAP)
}

public class Contract_ContractTBodyConfiguration : IEntityTypeConfiguration<Contract_ContractTBody>
{
    public void Configure(EntityTypeBuilder<Contract_ContractTBody> builder)
    {
        builder.ToTable("Contract_ContractTBody");
        builder.HasKey(x => x.ContractCode);

        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.ContractFilePath).HasMaxLength(500);
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
