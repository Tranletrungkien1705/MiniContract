using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 DashboardController.cs — Contract_ContractUser
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Report.cs:638-667
//        D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Contract_Contract\Contract_ContractUser.cs:9-32
public class Contract_ContractUser
{
    public string ContractCode { get; set; } = "";
    public string PartyCode { get; set; } = "";
    public string Idx { get; set; } = "";
    public string? UserCodeSysSign { get; set; }
    public string? UserCodeSign { get; set; }
    public string? UserToken { get; set; }
    public string? UserNameSign { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public string? UserZalo { get; set; }
    public string? UserSignSatus { get; set; } // Giữ đúng tên cột nguồn (lỗi chính tả Satus)
    public string? ConfirmDTimeUTC { get; set; }
    public string? ConfirmBy { get; set; }
    public string? SendDateUTC { get; set; }
    public string? SignDetailJson { get; set; }
    public string? SendBy { get; set; }
    public string? FlagSendUser { get; set; }
    public string? AccessKey { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Contract_ContractUserConfiguration : IEntityTypeConfiguration<Contract_ContractUser>
{
    public void Configure(EntityTypeBuilder<Contract_ContractUser> builder)
    {
        builder.ToTable("Contract_ContractUser");
        builder.HasKey(x => new { x.ContractCode, x.PartyCode, x.Idx });

        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.PartyCode).HasMaxLength(50);
        builder.Property(x => x.Idx).HasMaxLength(50);
        builder.Property(x => x.UserCodeSysSign).HasMaxLength(50);
        builder.Property(x => x.UserCodeSign).HasMaxLength(50);
        builder.Property(x => x.UserToken).HasMaxLength(200);
        builder.Property(x => x.UserNameSign).HasMaxLength(250);
        builder.Property(x => x.UserEmail).HasMaxLength(250);
        builder.Property(x => x.UserPhone).HasMaxLength(50);
        builder.Property(x => x.UserZalo).HasMaxLength(50);
        builder.Property(x => x.UserSignSatus).HasMaxLength(50);
        builder.Property(x => x.ConfirmDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.ConfirmBy).HasMaxLength(50);
        builder.Property(x => x.SendDateUTC).HasMaxLength(50);
        builder.Property(x => x.SendBy).HasMaxLength(50);
        builder.Property(x => x.FlagSendUser).HasMaxLength(10);
        builder.Property(x => x.AccessKey).HasMaxLength(200);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_ContractUser (nghiệp vụ, Order >= 200).
/// Khớp với Contract_ContractSeeder (Order 211) và Sys_UserSeeder (Order 101).
/// </summary>
public class Contract_ContractUserSeeder : IPortSeeder
{
    public int Order => 217;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_ContractUser>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Contract_ContractUser
            {
                ContractCode = "HD0001",
                PartyCode = "PARTY_A",
                Idx = "1",
                UserEmail = "U01",
                UserNameSign = "admin@solution.hn",
                UserSignSatus = "PENDING"
            },
            new Contract_ContractUser
            {
                ContractCode = "HD0003",
                PartyCode = "PARTY_A",
                Idx = "1",
                UserEmail = "U01",
                UserNameSign = "admin@solution.hn",
                UserSignSatus = "PENDING"
            },
            new Contract_ContractUser
            {
                ContractCode = "HD0003",
                PartyCode = "PARTY_B",
                Idx = "2",
                UserEmail = "U02",
                UserNameSign = "tester@solution.hn",
                UserSignSatus = "PENDING"
            },
            new Contract_ContractUser
            {
                ContractCode = "HD0004",
                PartyCode = "PARTY_A",
                Idx = "1",
                UserEmail = "U02",
                UserNameSign = "tester@solution.hn",
                UserSignSatus = "PENDING"
            },
            new Contract_ContractUser
            {
                ContractCode = "HD0002",
                PartyCode = "PARTY_A",
                Idx = "1",
                UserEmail = "U01",
                UserNameSign = "admin@solution.hn",
                UserSignSatus = "CONFIRMED"
            }
        );

        await db.SaveChangesAsync();
    }
}
