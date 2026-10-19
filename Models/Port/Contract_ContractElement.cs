using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_ContractElement
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9846-9847 (join lọc),
//        10248-10258 (select Contract_ContractElement)
// GAP: DDL/model đầy đủ KHÔNG nằm trong source_excerpt.txt (đề bài cấm đọc file ngoài excerpt).
//      Chỉ khai cột mà SQL nguồn CHẮC CHẮN dùng — xem .parity-run\conflicts\11_conflict.md.
public class Contract_ContractElement
{
    // QContract.cs:9847, 10255 — khoá join
    public string ContractCode { get; set; } = "";

    // QContract.cs:10252 — cce.* (các cột còn lại của bảng KHÔNG có trong excerpt -> GAP)
}

public class Contract_ContractElementConfiguration : IEntityTypeConfiguration<Contract_ContractElement>
{
    public void Configure(EntityTypeBuilder<Contract_ContractElement> builder)
    {
        builder.ToTable("Contract_ContractElement");
        builder.HasKey(x => x.ContractCode);

        builder.Property(x => x.ContractCode).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_ContractElement (nghiệp vụ, Order 200-299).
/// Dữ liệu KHÔNG lấy từ nguồn (nguồn không có seed) — chỉ để bảng có dữ liệu khi chạy thử.
/// </summary>
public class Contract_ContractElementSeeder : IPortSeeder
{
    // Order nghiệp vụ 200-299 (PortInfra.cs:16-17); 210-215 đã dùng
    public int Order => 216;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_ContractElement>();
        if (await set.AnyAsync()) return;

        // Một dòng mỗi hợp đồng (khoá chính ContractCode)
        set.AddRange(
            new Contract_ContractElement { ContractCode = "HD0001" },
            new Contract_ContractElement { ContractCode = "HD0002" }
        );

        await db.SaveChangesAsync();
    }
}
