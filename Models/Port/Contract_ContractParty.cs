using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_ContractParty
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9842-9843 (join lọc),
//        10130-10139 (select Contract_ContractParty), 32974-32991 (update CONFIRMED),
//        zTemp.cs:581-769 (Contract_ContractParty_UpdEmailSendX — EmailSend/SendEmailDTimeUTC/SendEmailBy)
//        QContract.cs:5405-5436 (Save map từ model — PartyCode, CustomerAddress, ValContract, …)
//
// GAP-PK: SQL nguồn join UPDATE theo (ContractCode, MST) — QContract.cs:33012-33014, 33057-33060 —
//          tức 1 hợp đồng có thể có NHIỀU party khác MST. Đích giữ PK = ContractCode (nguyên trạng)
//          để KHÔNG phá test/hợp đồng cũ (Mst_ContractTypeControllerPortTests chèn tay bảng này
//          chỉ với cột ContractCode). Hệ quả: Save với > 1 party cùng ContractCode sẽ ghi đè
//          (không đúng nguồn cho use case nhiều bên). Cần migrate khi có nhu cầu.
public class Contract_ContractParty
{
    // QContract.cs:9843, 10136 — khoá join (nguồn dùng composite ContractCode+MST)
    public string ContractCode { get; set; } = "";

    // Gate:5425 / QContract.cs:34375 — MST của party
    public string? MST { get; set; }

    // Contract_ContractController.cs:620 — Contract_ContractParty.ContractType
    public string? ContractType { get; set; }

    // Contract_ContractController.cs:621 — Contract_ContractParty.CustomerCode
    public string? CustomerCode { get; set; }

    // QContract.cs:5405-5436 — Save map
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? PartyCode { get; set; }
    public string? Description { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNo { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerWebsite { get; set; }
    public string? BankAccountNo { get; set; }
    public string? BankName { get; set; }
    public string? RepresentName { get; set; }
    public string? RepresentPosition { get; set; }
    public string? ContractTypeName { get; set; }
    public string? Remark { get; set; }
    public string? FlagOrgCreate { get; set; }
    public string? ValContract { get; set; }
    public string? ValExchange { get; set; }
    public string? ValPaymented { get; set; }
    public string? ValRemain { get; set; }
    public string? UserToken { get; set; }

    // Trạng thái + audit
    public string? ContractPartyStatus { get; set; }
    public string? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // zTemp.cs:717-721 — Contract_ContractParty_UpdEmailSendX
    public string? EmailSend { get; set; }
    public string? SendEmailDTimeUTC { get; set; }
    public string? SendEmailBy { get; set; }
}

public class Contract_ContractPartyConfiguration : IEntityTypeConfiguration<Contract_ContractParty>
{
    public void Configure(EntityTypeBuilder<Contract_ContractParty> builder)
    {
        builder.ToTable("Contract_ContractParty");
        // PK = ContractCode (giữ nguyên entity cũ — xem GAP-PK trong comment lớp).
        builder.HasKey(x => x.ContractCode);

        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.MST).HasMaxLength(50);
        builder.Property(x => x.ContractType).HasMaxLength(50);
        builder.Property(x => x.CustomerCode).HasMaxLength(50);
        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.PartyCode).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CustomerCodeSys).HasMaxLength(50);
        builder.Property(x => x.CustomerName).HasMaxLength(250);
        builder.Property(x => x.CustomerAddress).HasMaxLength(500);
        builder.Property(x => x.CustomerPhoneNo).HasMaxLength(50);
        builder.Property(x => x.CustomerEmail).HasMaxLength(250);
        builder.Property(x => x.CustomerWebsite).HasMaxLength(250);
        builder.Property(x => x.BankAccountNo).HasMaxLength(50);
        builder.Property(x => x.BankName).HasMaxLength(250);
        builder.Property(x => x.RepresentName).HasMaxLength(250);
        builder.Property(x => x.RepresentPosition).HasMaxLength(100);
        builder.Property(x => x.ContractTypeName).HasMaxLength(250);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.Property(x => x.FlagOrgCreate).HasMaxLength(10);
        builder.Property(x => x.ValContract).HasMaxLength(50);
        builder.Property(x => x.ValExchange).HasMaxLength(50);
        builder.Property(x => x.ValPaymented).HasMaxLength(50);
        builder.Property(x => x.ValRemain).HasMaxLength(50);
        builder.Property(x => x.UserToken).HasMaxLength(500);
        builder.Property(x => x.ContractPartyStatus).HasMaxLength(50);
        builder.Property(x => x.CancelDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.CancelBy).HasMaxLength(50);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.EmailSend).HasMaxLength(1000);
        builder.Property(x => x.SendEmailDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.SendEmailBy).HasMaxLength(50);
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
