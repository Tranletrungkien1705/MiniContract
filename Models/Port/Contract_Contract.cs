using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Contract_ContractController.cs — Contract_Contract
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:9827-9867 (bảng tạm lọc),
//        9940-9966 (select Contract_Contract), 10186-10221 (Contract_Contract_Ref)
//        + QContract.cs:20042-20221 (Contract_Contract_SaveX_New20240312 — danh sách cột upsert, #31)
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

    // QContract.cs:9949 — cc.ContractFileSpec — không có cột ở đích (GAP)
    public string? TContractCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ContractRefNo { get; set; }
    public string? UserCodeCreate { get; set; }
    public string? UserNameCreate { get; set; }
    public string? PositionCreate { get; set; }
    public string? CreateBy { get; set; }

    // QContract.cs:20197-20210 — Contract_Contract_SaveX_New20240312 upsert (đủ cột để map model)
    public string? ContractNo { get; set; }
    public string? ContractName { get; set; }
    public string? OrgID { get; set; }
    public string? FlagContractAnnex { get; set; }
    public string? FlagOrgCreate { get; set; }        // 20153 — '1' cho org tạo
    public string? FlagSysGenAccessKey { get; set; }  // 20147 — '1'
    public string? FlagSendUserAll { get; set; }      // 20148 — '0'
    public string? FlagCreatePartnerSignFirst { get; set; } // 20149
    public string? FlagActive { get; set; }           // 20154 — '1'
    public string? EffDateStart { get; set; }
    public string? EffDateEnd { get; set; }
    public string? CurrencyName { get; set; }
    public string? CurrencyRate { get; set; }
    public string? NNTFullName { get; set; }          // 20150 — từ Mst_NNT.NNTFullName
    public string? ContactName { get; set; }          // 20151 — từ Mst_NNT.ContactName
    public string? PhoneNoCreate { get; set; }        // 20210 — từ Sys_User.PhoneNo
    public string? EMailCreate { get; set; }          // 20211 — từ Sys_User.EMail
    public string? ContractFilePath { get; set; }     // cột dùng ở approve/mail

    // zTemp.cs:2045-2048, 2103-2106 — ESign/Contract_ContractParty_Cancel
    public string? CancelDTimeUTC { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // zTemp.cs:3753-3761 — Contract_ContractParty_FinishX UPDATE Contract_Contract
    // (ContractFinishReasonCode, ContractFinishReasonName, FinishDescription, FinishFileDesc)
    public string? ContractFinishReasonCode { get; set; }
    public string? ContractFinishReasonName { get; set; }
    public string? FinishDescription { get; set; }
    public string? FinishFileDesc { get; set; }
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
        builder.Property(x => x.CancelDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
        builder.Property(x => x.ContractNo).HasMaxLength(50);
        builder.Property(x => x.ContractName).HasMaxLength(250);
        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.FlagContractAnnex).HasMaxLength(10);
        builder.Property(x => x.FlagOrgCreate).HasMaxLength(10);
        builder.Property(x => x.FlagSysGenAccessKey).HasMaxLength(10);
        builder.Property(x => x.FlagSendUserAll).HasMaxLength(10);
        builder.Property(x => x.FlagCreatePartnerSignFirst).HasMaxLength(10);
        builder.Property(x => x.FlagActive).HasMaxLength(10);
        builder.Property(x => x.EffDateStart).HasMaxLength(50);
        builder.Property(x => x.EffDateEnd).HasMaxLength(50);
        builder.Property(x => x.CurrencyName).HasMaxLength(100);
        builder.Property(x => x.CurrencyRate).HasMaxLength(50);
        builder.Property(x => x.NNTFullName).HasMaxLength(500);
        builder.Property(x => x.ContactName).HasMaxLength(250);
        builder.Property(x => x.PhoneNoCreate).HasMaxLength(50);
        builder.Property(x => x.EMailCreate).HasMaxLength(250);
        builder.Property(x => x.ContractFinishReasonCode).HasMaxLength(50);
        builder.Property(x => x.ContractFinishReasonName).HasMaxLength(250);
        builder.Property(x => x.FinishDescription).HasMaxLength(500);
        builder.Property(x => x.FinishFileDesc).HasMaxLength(500);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu Contract_Contract (nghiệp vụ, Order 200-299).
/// Dữ liệu KHÔNG lấy từ nguồn (nguồn không có seed) — chỉ để chạy thử 2 bộ lọc ra số dòng khác nhau.
/// #11 DoSearch: 4 dòng — lọc ContractNo="HD0003/2026" ra 1 dòng, lọc OrgID="7200000002" ra 1 dòng (khác dòng).
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
                ContractName = "Hợp đồng dịch vụ tư vấn CNTT",
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
                ContractName = "Hợp đồng kinh tế thương mại",
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
                ContractName = "Hợp đồng lao động xác định thời hạn",
                ContractDateUTC = "2026-03-20 10:00:00",
                CreateDTimeUTC = "2026-03-20 10:00:00",
                ContractStatus = "ONPROCESS",
                ContractFilePath = "HD0003.pdf",
                CurrencyCode = "USD",
                ContractRefNo = "HD0001",
                UserCodeCreate = "U01",
                UserNameCreate = "admin@solution.hn",
                PositionCreate = "Giám đốc",
                CreateBy = "U01",
                ContractNo = "HD0003/2026"
            },
            // Dòng thêm cho #11 DoSearch: lọc ContractNo (gate :7985-7988) ra 1 dòng khác với lọc OrgID
            new Contract_Contract
            {
                ContractCode = "HD0004",
                ContractName = "Hợp đồng mua bán thiết bị",
                ContractDateUTC = "2026-04-01 11:00:00",
                CreateDTimeUTC = "2026-04-01 11:00:00",
                ContractStatus = "PENDING",
                ContractFilePath = "HD0004.pdf",
                CurrencyCode = "VND",
                ContractRefNo = "HD0002",
                UserCodeCreate = "U02",
                UserNameCreate = "tester@solution.hn",
                PositionCreate = "Nhân viên",
                CreateBy = "U02",
                ContractNo = "HD0004/2026",
                OrgID = "7200000002"
            }
        );

        await db.SaveChangesAsync();
    }
}
