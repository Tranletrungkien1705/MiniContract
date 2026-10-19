using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 ESignController.cs / Contract_ContractParty_Cancel — Contract_Contract_HistAction
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:40250-40360 (Contract_Contract_HistActionX)
public class Contract_Contract_HistAction
{
    // QContract.cs:40296, 40341
    public string ContractCode { get; set; } = "";

    // QContract.cs:40297, 40342
    public int Version { get; set; }

    // QContract.cs:40298, 40343
    public string? UserCode { get; set; }

    // QContract.cs:40299, 40344
    public string? UserName { get; set; }

    // QContract.cs:40300, 40345
    public string? Description { get; set; }

    // QContract.cs:40301, 40346
    public string? LogLUDTimeUTC { get; set; }

    // QContract.cs:40302, 40347
    public string? LogLUBy { get; set; }

    // QContract.cs:40303, 40348
    public string? Remark { get; set; }

    // QContract.cs:40304, 40349
    public string? FunctionName { get; set; }

    // QContract.cs:40305, 40350
    public string? FunctionActionType { get; set; }

    // QContract.cs:40306, 40351
    public string? FunctionRemark { get; set; }

    // QContract.cs:40307, 40352
    public string? HistRefType { get; set; }

    // QContract.cs:40308-40311, 40353-40356
    public string? RefCode00 { get; set; }
    public string? RefCode01 { get; set; }
    public string? RefCode02 { get; set; }
    public string? RefCode03 { get; set; }
}

public class Contract_Contract_HistActionConfiguration : IEntityTypeConfiguration<Contract_Contract_HistAction>
{
    public void Configure(EntityTypeBuilder<Contract_Contract_HistAction> builder)
    {
        builder.ToTable("Contract_Contract_HistAction");
        builder.HasKey(x => new { x.ContractCode, x.Version });

        builder.Property(x => x.ContractCode).HasMaxLength(50);
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.UserCode).HasMaxLength(50);
        builder.Property(x => x.UserName).HasMaxLength(250);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.Property(x => x.FunctionName).HasMaxLength(100);
        builder.Property(x => x.FunctionActionType).HasMaxLength(100);
        builder.Property(x => x.FunctionRemark).HasMaxLength(500);
        builder.Property(x => x.HistRefType).HasMaxLength(50);
        builder.Property(x => x.RefCode00).HasMaxLength(50);
        builder.Property(x => x.RefCode01).HasMaxLength(50);
        builder.Property(x => x.RefCode02).HasMaxLength(50);
        builder.Property(x => x.RefCode03).HasMaxLength(50);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_Contract_HistAction (nghiệp vụ, Order 200-299).
/// </summary>
public class Contract_Contract_HistActionSeeder : IPortSeeder
{
    // Order nghiệp vụ 200-299 (PortInfra.cs:16-17); 210, 211, 212 đã dùng
    public int Order => 213;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Contract_Contract_HistAction>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Contract_Contract_HistAction
            {
                ContractCode = "HD0001",
                Version = 1,
                UserCode = "U01",
                UserName = "admin@solution.hn",
                Description = "Tạo mới hợp đồng",
                LogLUDTimeUTC = "2026-01-10 08:00:00",
                LogLUBy = "U01",
                FunctionName = "Contract_Contract_Create",
                FunctionActionType = "Create"
            },
            new Contract_Contract_HistAction
            {
                ContractCode = "HD0002",
                Version = 1,
                UserCode = "U02",
                UserName = "tester@solution.hn",
                Description = "Ký hợp đồng",
                LogLUDTimeUTC = "2026-02-15 09:30:00",
                LogLUBy = "U02",
                FunctionName = "Contract_Contract_Sign",
                FunctionActionType = "Sign"
            }
        );

        await db.SaveChangesAsync();
    }
}
