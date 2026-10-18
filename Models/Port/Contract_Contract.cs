using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9827-9867 (bảng tạm lọc),
//        9940-9966 (select Contract_Contract), 10186-10221 (Contract_Contract_Ref)
// GAP: DDL/model đầy đủ của Contract_Contract KHÔNG nằm trong source_excerpt.txt (đề bài cấm đọc file ngoài excerpt).
//      Entity dưới đây chỉ khai các cột mà SQL nguồn CHẮC CHẮN dùng (lọc/sắp xếp/join/chiếu) — xem .parity-run\conflicts\10_conflict.md.
public class Contract_Contract
{
    // QContract.cs:9831, 9954 — khoá join của mọi bảng con
    public string ContractCode { get; set; } = "";

    // QContract.cs:9832, 9851, 9962 — sắp xếp desc
    public string? ContractDateUTC { get; set; }

    // QContract.cs:9833, 9852, 9963 — sắp xếp desc
    public string? CreateDTimeUTC { get; set; }

    // QContract.cs:9948 — case when cc.ContractStatus = 'PENDING'
    public string? ContractStatus { get; set; }

    // QContract.cs:9949 — cc.ContractFilePath
    public string? ContractFilePath { get; set; }

    // QContract.cs:9956 — left join Contract_TempContract on cc.TContractCode
    public string? TContractCode { get; set; }

    // QContract.cs:9958 — left join Mst_CurrencyEx on cc.CurrencyCode
    public string? CurrencyCode { get; set; }

    // QContract.cs:10188, 10206, 10212, 10218 — Contract_Contract_Ref
    public string? ContractRefNo { get; set; }

    // QContract.cs:10072, 10086, 10092 — Contract_Checker (người tạo)
    public string? UserCodeCreate { get; set; }
    public string? UserNameCreate { get; set; }
    public string? PositionCreate { get; set; }
    public string? CreateBy { get; set; }
}

public class Contract_ContractConfiguration : IEntityTypeConfiguration<Contract_Contract>
{
    public void Configure(EntityTypeBuilder<Contract_Contract> builder)
    {
        builder.ToTable("Contract_Contract");
        builder.HasKey(x => x.ContractCode);

        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.ContractDateUTC).HasMaxLength(50);
        builder.Property(x => x.CreateDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.ContractStatus).HasMaxLength(50);
        builder.Property(x => x.ContractFilePath).HasMaxLength(500);
        builder.Property(x => x.TContractCode).HasMaxLength(50);
        builder.Property(x => x.CurrencyCode).HasMaxLength(50);
        builder.Property(x => x.ContractRefNo).HasMaxLength(50);
        builder.Property(x => x.UserCodeCreate).HasMaxLength(50);
        builder.Property(x => x.UserNameCreate).HasMaxLength(250);
        builder.Property(x => x.PositionCreate).HasMaxLength(100);
        builder.Property(x => x.CreateBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_Contract (nghiệp vụ, Order 200-299).
/// Dữ liệu KHÔNG lấy từ nguồn (nguồn không có seed) — chỉ để chạy thử 2 bộ lọc ra số dòng khác nhau.
/// </summary>
public class Contract_ContractSeeder : IPortSeeder
{
    // Order nghiệp vụ 200-299 (PortInfra.cs:16-17); 210 đã dùng (Contract_TempContract)
    public int Order => 211;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_Contract>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Contract_Contract
            {
                ContractCode = "HD0001",
                ContractDateUTC = "2026-01-10 08:00:00",
                CreateDTimeUTC = "2026-01-10 08:00:00",
                ContractStatus = "PENDING",
                ContractFilePath = "HD0001.pdf",
                TContractCode = "TCONT001",
                CurrencyCode = "VND",
                ContractRefNo = "HD0000",
                UserCodeCreate = "U01",
                UserNameCreate = "admin@solution.hn",
                PositionCreate = "Giám đốc",
                CreateBy = "U01"
            },
            new Contract_Contract
            {
                ContractCode = "HD0002",
                ContractDateUTC = "2026-02-15 09:30:00",
                CreateDTimeUTC = "2026-02-15 09:30:00",
                ContractStatus = "SIGNED",
                ContractFilePath = "HD0002.pdf",
                CurrencyCode = "VND",
                ContractRefNo = "HD0000",
                UserCodeCreate = "U02",
                UserNameCreate = "tester@solution.hn",
                PositionCreate = "Nhân viên",
                CreateBy = "U02"
            },
            new Contract_Contract
            {
                ContractCode = "HD0003",
                ContractDateUTC = "2026-03-20 10:00:00",
                CreateDTimeUTC = "2026-03-20 10:00:00",
                ContractStatus = "PENDING",
                ContractFilePath = "HD0003.pdf",
                CurrencyCode = "USD",
                ContractRefNo = "HD0001",
                UserCodeCreate = "U01",
                UserNameCreate = "admin@solution.hn",
                PositionCreate = "Giám đốc",
                CreateBy = "U01"
            }
        );

        await db.SaveChangesAsync();
    }
}
