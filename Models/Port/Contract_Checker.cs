using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_Checker
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Contract_Contract\Contract_Checker.cs:9-30
// BIZ:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\zTemp.cs:5235-5305 (Contract_Checker_CheckDB),
//        5306-6016 (Contract_Checker_Accept / Contract_Checker_AcceptX)
public class Contract_Checker
{
    // zTemp.cs:5250, 5514 — khoá chính kép (ContractCode, UserCode)
    public string ContractCode { get; set; } = "";
    public string UserCode { get; set; } = "";
    public string? Idx { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? UserName { get; set; }
    public string? Position { get; set; }
    public string? FlagChecker { get; set; } // 1: người kiểm tra, 0: người ký (zTemp.cs:5390, 5392)
    public string? FlagCheck { get; set; }   // 1: đã kiểm tra, 0: chưa kiểm tra (zTemp.cs:5389, 5486, 5501)
    public string? FlagSeq { get; set; }     // 1: kiểm tra theo thứ tự, 0: không cần (zTemp.cs:5387)
    public string? Remark { get; set; }
    public string? ACAvatar { get; set; }
    public string? CheckDTimeUTC { get; set; } // thời gian kiểm tra (zTemp.cs:5487, 5502)
    public string? ApprovedDTimeUTC { get; set; }
    public string? FlagActive { get; set; }  // cờ hoạt động (zTemp.cs:5291)
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Contract_CheckerConfiguration : IEntityTypeConfiguration<Contract_Checker>
{
    public void Configure(EntityTypeBuilder<Contract_Checker> builder)
    {
        builder.ToTable("Contract_Checker");
        builder.HasKey(x => new { x.ContractCode, x.UserCode });
        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.UserCode).HasMaxLength(100);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_Checker (nghiệp vụ, Order 200-299; 210-217 đã dùng -> chọn 218).
/// </summary>
public class Contract_CheckerSeeder : IPortSeeder
{
    public int Order => 218;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_Checker>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Contract_Checker
            {
                ContractCode = "HD0001",
                UserCode = "U01",
                Idx = "1",
                NetworkID = "1001",
                OrgID = "7200000001",
                UserName = "admin@solution.hn",
                Position = "Giám đốc",
                FlagChecker = "1",
                FlagCheck = "0",
                FlagSeq = "0",
                FlagActive = "1"
            },
            new Contract_Checker
            {
                ContractCode = "HD0001",
                UserCode = "U02",
                Idx = "2",
                NetworkID = "1001",
                OrgID = "7200000001",
                UserName = "tester@solution.hn",
                Position = "Nhân viên",
                FlagChecker = "0", // FlagChecker = 0 để test ca InvalidChecker
                FlagCheck = "0",
                FlagSeq = "0",
                FlagActive = "1"
            },
            new Contract_Checker
            {
                ContractCode = "HD0002",
                UserCode = "U01",
                Idx = "1",
                NetworkID = "1001",
                OrgID = "7200000001",
                UserName = "admin@solution.hn",
                Position = "Giám đốc",
                FlagChecker = "1",
                FlagCheck = "0",
                FlagSeq = "0",
                FlagActive = "0" // FlagActive = 0 để test ca CheckDB_FlagActiveNotMatched
            },
            new Contract_Checker
            {
                ContractCode = "HD0003",
                UserCode = "U01",
                Idx = "1",
                NetworkID = "1001",
                OrgID = "7200000001",
                UserName = "admin@solution.hn",
                Position = "Giám đốc",
                FlagChecker = "1",
                FlagCheck = "0",
                FlagSeq = "0",
                FlagActive = "1"
            }
        );

        await db.SaveChangesAsync();
    }
}
