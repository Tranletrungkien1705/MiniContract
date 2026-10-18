using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_OrganizationController.cs — Mst_NNT
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:9-141
public class Mst_NNT
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:11
    public string MST { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:13
    public string? OrgID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:15
    public string? NNTFullName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:16
    public string? NNTShortName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:17
    public string? NetworkID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:19
    public string? MSTParent { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:21
    public string? MSTBUCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:23
    public string? MSTBUPattern { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:25
    public string? MSTLevel { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:26
    public string? AreaCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:28
    public string? ProvinceCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:30
    public string? DistrictCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:34
    public string? DLCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:36
    public string? NNTAddress { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:38
    public string? NNTMobile { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:40
    public string? NNTPhone { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:42
    public string? NNTFax { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:44
    public string? PresentBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:46
    public string? BusinessRegNo { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:48
    public string? NNTPosition { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:50
    public string? PresentIDNo { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:52
    public string? PresentIDType { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:54
    public string? GovTaxID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:56
    public string? ContactName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:58
    public string? ContactPhone { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:60
    public string? ContactEmail { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:62
    public string? Website { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:64
    public string? CANumber { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:66
    public string? CAOrg { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:68
    public string? CAEffDTimeUTCStart { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:70
    public string? CAEffDTimeUTCEnd { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:72
    public string? PackageCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:74
    public string? CreatedDate { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:76
    public string? CreateDTime { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:78
    public string? CreateBy { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:80
    public string? AccNo { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:82
    public string? AccHolder { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:84
    public string? BankName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:86
    public string? BizType { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:88
    public string? BizFieldCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:90
    public string? BizSizeCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:92
    public string? DealerType { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:94
    public string? RegisterStatus { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:96
    public string? TCTStatus { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:98
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:100
    public string? Remark { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:140
    public string? SignType { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:102
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_NNT.cs:104
    public string? LogLUBy { get; set; }
}

public class Mst_NNTConfiguration : IEntityTypeConfiguration<Mst_NNT>
{
    public void Configure(EntityTypeBuilder<Mst_NNT> builder)
    {
        builder.ToTable("Mst_NNT");
        builder.HasKey(x => x.MST);

        builder.Property(x => x.MST).HasMaxLength(50);
        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.NNTFullName).HasMaxLength(250);
        builder.Property(x => x.NNTShortName).HasMaxLength(100);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.MSTParent).HasMaxLength(50);
        builder.Property(x => x.MSTBUCode).HasMaxLength(50);
        builder.Property(x => x.MSTBUPattern).HasMaxLength(250);
        builder.Property(x => x.MSTLevel).HasMaxLength(50);
        builder.Property(x => x.AreaCode).HasMaxLength(50);
        builder.Property(x => x.ProvinceCode).HasMaxLength(50);
        builder.Property(x => x.DistrictCode).HasMaxLength(50);
        builder.Property(x => x.DLCode).HasMaxLength(50);
        builder.Property(x => x.NNTAddress).HasMaxLength(500);
        builder.Property(x => x.NNTMobile).HasMaxLength(50);
        builder.Property(x => x.NNTPhone).HasMaxLength(50);
        builder.Property(x => x.NNTFax).HasMaxLength(50);
        builder.Property(x => x.PresentBy).HasMaxLength(150);
        builder.Property(x => x.BusinessRegNo).HasMaxLength(100);
        builder.Property(x => x.NNTPosition).HasMaxLength(100);
        builder.Property(x => x.PresentIDNo).HasMaxLength(50);
        builder.Property(x => x.PresentIDType).HasMaxLength(50);
        builder.Property(x => x.GovTaxID).HasMaxLength(50);
        builder.Property(x => x.ContactName).HasMaxLength(150);
        builder.Property(x => x.ContactPhone).HasMaxLength(50);
        builder.Property(x => x.ContactEmail).HasMaxLength(150);
        builder.Property(x => x.Website).HasMaxLength(250);
        builder.Property(x => x.CANumber).HasMaxLength(250);
        builder.Property(x => x.CAOrg).HasMaxLength(250);
        builder.Property(x => x.CAEffDTimeUTCStart).HasMaxLength(50);
        builder.Property(x => x.CAEffDTimeUTCEnd).HasMaxLength(50);
        builder.Property(x => x.PackageCode).HasMaxLength(50);
        builder.Property(x => x.CreatedDate).HasMaxLength(50);
        builder.Property(x => x.CreateDTime).HasMaxLength(50);
        builder.Property(x => x.CreateBy).HasMaxLength(50);
        builder.Property(x => x.AccNo).HasMaxLength(50);
        builder.Property(x => x.AccHolder).HasMaxLength(150);
        builder.Property(x => x.BankName).HasMaxLength(250);
        builder.Property(x => x.BizType).HasMaxLength(50);
        builder.Property(x => x.BizFieldCode).HasMaxLength(50);
        builder.Property(x => x.BizSizeCode).HasMaxLength(50);
        builder.Property(x => x.DealerType).HasMaxLength(50);
        builder.Property(x => x.RegisterStatus).HasMaxLength(50);
        builder.Property(x => x.TCTStatus).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.SignType).HasMaxLength(50);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_NNTSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17). Mst_Area có Order=20, chọn Order=25 để Mst_Area được tạo trước.
    public int Order => 25;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_NNT>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_NNT
            {
                MST = "0100000001",
                OrgID = "7200000001",
                NNTFullName = "Công ty TNHH Giải Pháp Số Hà Nội",
                NNTShortName = "Giải Pháp Số HN",
                NetworkID = "1001",
                AreaCode = "MB",
                ProvinceCode = "01",
                DistrictCode = "001",
                NNTAddress = "Số 1 Cầu Giấy, Hà Nội",
                NNTPhone = "02431234567",
                NNTMobile = "0987654321",
                ContactEmail = "contact@solution.hn",
                ContactName = "Nguyễn Văn A",
                PresentBy = "Nguyễn Văn A",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_NNT
            {
                MST = "0100000002",
                OrgID = "7200000001",
                NNTFullName = "Chi nhánh TP Hồ Chí Minh",
                NNTShortName = "CN HCM",
                NetworkID = "1001",
                AreaCode = "MN",
                ProvinceCode = "79",
                DistrictCode = "760",
                NNTAddress = "Số 10 Quận 1, TP Hồ Chí Minh",
                NNTPhone = "02831234567",
                NNTMobile = "0912345678",
                ContactEmail = "hcm@solution.hn",
                ContactName = "Trần Thị B",
                PresentBy = "Trần Thị B",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            },
            new Mst_NNT
            {
                MST = "0100000003",
                OrgID = "7200000002",
                NNTFullName = "Văn phòng Đại diện Miền Trung",
                NNTShortName = "VPĐD MT",
                NetworkID = "1001",
                AreaCode = "MT",
                ProvinceCode = "48",
                DistrictCode = "490",
                NNTAddress = "Số 5 Hải Châu, Đà Nẵng",
                NNTPhone = "02361234567",
                NNTMobile = "0905123456",
                ContactEmail = "danang@solution.hn",
                ContactName = "Lê Văn C",
                PresentBy = "Lê Văn C",
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                LogLUBy = "port"
            }
        );
        await db.SaveChangesAsync();
    }
}
