using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 ESignController.cs — Contract_ContractVerifyOtp
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:42095-42310 (Contract_ContractVerifyOtp_SaveX)
//        DDL: 20220527.QUYNHNTN..AddTable.Contract_ContractVerifyOtp.sql
//        Model: idn.Skycic.QContract.Common.Models.Contract_ContractVerifyOtp.cs
public class Contract_ContractVerifyOtp
{
    // OtpID bigint IDENTITY(1,1) PRIMARY KEY
    public long OtpID { get; set; }

    // OtpCode Varchar50 NOT NULL
    public string OtpCode { get; set; } = "";

    // ContractCode Varchar50 NOT NULL
    public string ContractCode { get; set; } = "";

    // UserCodeSign Varchar50
    public string? UserCodeSign { get; set; }

    // CreateDate MyDateTime NOT NULL
    public string? CreateDate { get; set; }

    // EndDate MyDateTime NOT NULL
    public string? EndDate { get; set; }

    // FlagActive MyStatus
    public string? FlagActive { get; set; }

    // LogLUDTimeUTC MyDateTime
    public string? LogLUDTimeUTC { get; set; }

    // LogLUBy Varchar50
    public string? LogLUBy { get; set; }
}

public class Contract_ContractVerifyOtpConfiguration : IEntityTypeConfiguration<Contract_ContractVerifyOtp>
{
    public void Configure(EntityTypeBuilder<Contract_ContractVerifyOtp> builder)
    {
        builder.ToTable("Contract_ContractVerifyOtp");
        builder.HasKey(x => x.OtpID);

        builder.Property(x => x.OtpID).ValueGeneratedOnAdd();
        builder.Property(x => x.OtpCode).HasMaxLength(50);
        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.UserCodeSign).HasMaxLength(50);
        builder.Property(x => x.CreateDate).HasMaxLength(50);
        builder.Property(x => x.EndDate).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(50);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_ContractVerifyOtp (nghiệp vụ, Order 200-299).
/// </summary>
public class Contract_ContractVerifyOtpSeeder : IPortSeeder
{
    // Order nghiệp vụ 200-299; 210, 211, 212, 213 đã dùng
    public int Order => 214;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_ContractVerifyOtp>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Contract_ContractVerifyOtp
            {
                OtpCode = "A1B2C3",
                ContractCode = "HD0001",
                UserCodeSign = "CUST001",
                CreateDate = "2026-04-01 10:00:00",
                EndDate = "2026-04-01 10:02:00",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-04-01 10:00:00",
                LogLUBy = "port"
            },
            new Contract_ContractVerifyOtp
            {
                // Dòng mẫu có EndDate trong tương lai xa (để test ca lỗi Contract_ContractVerifyOtp_SaveX_NotTimeOut)
                OtpCode = "ACTIVE",
                ContractCode = "HD0002",
                UserCodeSign = "CUST002",
                CreateDate = "2026-04-01 10:00:00",
                EndDate = "2099-12-31 23:59:59",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-04-01 10:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
