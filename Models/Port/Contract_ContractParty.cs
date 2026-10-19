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

    // zTemp.cs:1983-1985, 2073-2081, 2128-2133 — ESign/Contract_ContractParty_Cancel
    public string? OrgID { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? MST { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? ContractPartyStatus { get; set; }
    public string? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
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
        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.CustomerCodeSys).HasMaxLength(50);
        builder.Property(x => x.MST).HasMaxLength(50);
        builder.Property(x => x.CustomerName).HasMaxLength(250);
        builder.Property(x => x.CustomerEmail).HasMaxLength(250);
        builder.Property(x => x.ContractPartyStatus).HasMaxLength(50);
        builder.Property(x => x.CancelDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.CancelBy).HasMaxLength(50);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
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
            new Contract_ContractParty
            {
                ContractCode = "HD0001",
                ContractType = "HD_KT",
                CustomerCode = "KH001",
                MST = "0100109106",
                OrgID = "7200000001",
                CustomerCodeSys = "CUST001",
                CustomerName = "Công ty CP ABC",
                CustomerEmail = "abc@solution.hn",
                ContractPartyStatus = "PENDING"
            },
            new Contract_ContractParty
            {
                ContractCode = "HD0002",
                ContractType = "HD_LD",
                CustomerCode = "KH002",
                MST = "0100109107",
                OrgID = "7200000001",
                CustomerCodeSys = "CUST002",
                CustomerName = "Công ty CP XYZ",
                CustomerEmail = "xyz@solution.hn",
                ContractPartyStatus = "APPROVED"
            },
            new Contract_ContractParty
            {
                ContractCode = "HD0003",
                ContractType = "HD_KT",
                CustomerCode = "KH003",
                MST = "0100109108",
                OrgID = "7200000001",
                CustomerCodeSys = "CUST003",
                CustomerName = "Công ty CP DEF",
                CustomerEmail = "def@solution.hn",
                ContractPartyStatus = "ONPROCESS"
            }
        );

        await db.SaveChangesAsync();
    }
}
