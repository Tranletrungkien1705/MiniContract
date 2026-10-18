using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_ContractParty
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9842-9843 (join lọc),
//        10130-10139 (select Contract_ContractParty)
// GAP: DDL/model đầy đủ KHÔNG nằm trong source_excerpt.txt (đề bài cấm đọc file ngoài excerpt).
//      Chỉ khai cột mà SQL nguồn CHẮC CHẮN dùng — xem .parity-run\conflicts\10_conflict.md.
public class Contract_ContractParty
{
    // QContract.cs:9843, 10136 — khoá join
    public string ContractCode { get; set; } = "";

    // Contract_ContractController.cs:620 (gate strWhereClause_Contract_ContractSearch: Contract_ContractParty.ContractType)
    public string? ContractType { get; set; }

    // Contract_ContractController.cs:621 (gate: Contract_ContractParty.CustomerCode)
    public string? CustomerCode { get; set; }
}

public class Contract_ContractPartyConfiguration : IEntityTypeConfiguration<Contract_ContractParty>
{
    public void Configure(EntityTypeBuilder<Contract_ContractParty> builder)
    {
        builder.ToTable("Contract_ContractParty");
        builder.HasKey(x => x.ContractCode);

        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.ContractType).HasMaxLength(50);
        builder.Property(x => x.CustomerCode).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_ContractParty (nghiệp vụ, Order 200-299).
/// Dữ liệu KHÔNG lấy từ nguồn (nguồn không có seed) — chỉ để chạy thử bộ lọc ContractType/CustomerCode.
/// </summary>
public class Contract_ContractPartySeeder : IPortSeeder
{
    // Order nghiệp vụ 200-299 (PortInfra.cs:16-17); 210, 211 đã dùng
    public int Order => 212;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_ContractParty>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Contract_ContractParty { ContractCode = "HD0001", ContractType = "HD_KT", CustomerCode = "KH001" },
            new Contract_ContractParty { ContractCode = "HD0002", ContractType = "HD_LD", CustomerCode = "KH002" },
            new Contract_ContractParty { ContractCode = "HD0003", ContractType = "HD_KT", CustomerCode = "KH003" }
        );

        await db.SaveChangesAsync();
    }
}
