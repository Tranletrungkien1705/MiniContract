using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_FinishedContractReasonController.cs — Mst_FinishedContractReason
// Nguồn:
// Model: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:9-20
// Biz:   D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\Master.cs:46581-46636, 46761-46772
public class Mst_FinishedContractReason
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:11
    public string NetworkID { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:12
    public string ContractFinishReasonCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:13
    public string? ContractFinishReasonName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:14
    public string? FinishType { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:15
    public string? FinishDescription { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:16
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:17
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\QContract\Mst_FinishedContractReason.cs:18
    public string? LogLUBy { get; set; }
}

public class Mst_FinishedContractReasonConfiguration : IEntityTypeConfiguration<Mst_FinishedContractReason>
{
    public void Configure(EntityTypeBuilder<Mst_FinishedContractReason> builder)
    {
        builder.ToTable("Mst_FinishedContractReason");
        // Khóa chính kết hợp (NetworkID, ContractFinishReasonCode) theo Master.cs:45502-45503, 46629-46630
        builder.HasKey(x => new { x.NetworkID, x.ContractFinishReasonCode });

        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.ContractFinishReasonCode).HasMaxLength(50);
        builder.Property(x => x.ContractFinishReasonName).HasMaxLength(250);
        builder.Property(x => x.FinishType).HasMaxLength(50);
        builder.Property(x => x.FinishDescription).HasMaxLength(500);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_FinishedContractReasonSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17; 20-27 đã dùng)
    public int Order => 28;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_FinishedContractReason>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_FinishedContractReason
            {
                NetworkID = "1001",
                ContractFinishReasonCode = "REASON01",
                ContractFinishReasonName = "Hết hạn hợp đồng",
                FinishType = "FINISHED",
                FinishDescription = "Chấm dứt do hợp đồng hết hạn",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_FinishedContractReason
            {
                NetworkID = "1001",
                ContractFinishReasonCode = "REASON02",
                ContractFinishReasonName = "Hai bên thỏa thuận chấm dứt",
                FinishType = "FINISHED",
                FinishDescription = "Thỏa thuận giữa 2 bên",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_FinishedContractReason
            {
                NetworkID = "1001",
                ContractFinishReasonCode = "REASON03",
                ContractFinishReasonName = "Một bên đơn phương chấm dứt",
                FinishType = "FINISHED",
                FinishDescription = "Đơn phương chấm dứt hợp đồng",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_FinishedContractReason
            {
                NetworkID = "1001",
                ContractFinishReasonCode = "REASON04",
                ContractFinishReasonName = "Vi phạm nghĩa vụ hợp đồng",
                FinishType = "FINISHED",
                FinishDescription = "Vi phạm điều khoản nghĩa vụ",
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_FinishedContractReason
            {
                NetworkID = "1002",
                ContractFinishReasonCode = "REASON05",
                ContractFinishReasonName = "Lý do bất khả kháng",
                FinishType = "FINISHED",
                FinishDescription = "Thiên tai dịch bệnh bất khả kháng",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
