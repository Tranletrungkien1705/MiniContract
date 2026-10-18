using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_CustomerController.cs — Mst_Customer
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Customer\Mst_Customer.cs:11-93
// và Customer.cs:1181-1250
public class Mst_Customer
{
    public string OrgID { get; set; } = "";
    public string CustomerCodeSys { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerType { get; set; }
    public string? CustomerGrpCode { get; set; }
    public string? CustomerSourceCode { get; set; }
    public string? CustomerSourceNameActual { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerNameEN { get; set; }
    public string? CustomerGender { get; set; }
    public string? CustomerPhoneNo { get; set; }
    public string? CustomerMobilePhone { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? WardCode { get; set; }
    public string? AreaCode { get; set; }
    public string? CustomerAvatarName { get; set; }
    public string? CustomerAvatarSpec { get; set; }
    public string? FlagCustomerAvatarPath { get; set; }
    public string? CustomerAvatarPath { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerDateOfBirth { get; set; }
    public string? GovIDType { get; set; }
    public string? GovIDCardNo { get; set; }
    public string? GovIDCardDate { get; set; }
    public string? GovIDCardPlace { get; set; }
    public string? TaxCode { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? RepresentName { get; set; }
    public string? RepresentPosition { get; set; }
    public string? UserCodeOwner { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Fax { get; set; }
    public string? Facebook { get; set; }
    public string? InvoiceCustomerName { get; set; }
    public string? InvoiceCustomerAddress { get; set; }
    public string? InvoiceOrgName { get; set; }
    public string? InvoiceEmailSend { get; set; }
    public string? MST { get; set; }
    public string? ListOfCustDynamicFieldValue { get; set; }
    public string? FlagDealer { get; set; }
    public string? FlagSupplier { get; set; }
    public string? FlagEndUser { get; set; }
    public string? FlagShipper { get; set; }
    public string? FlagBank { get; set; }
    public string? FlagInsurrance { get; set; }
    public string? DTimeUsed { get; set; }
    public string? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public string? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public string? FlagActive { get; set; }
    public string? GLN { get; set; }
    public string? Remark { get; set; }
    public string? Coordinates { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // Các thuộc tính đối chiếu/nối bảng nguồn (Customer.cs:1184-1249)
    public string? Org_NNTFullName { get; set; }
    public string? Network_NNTFullName { get; set; }
    public string? mct_CustomerTypeName { get; set; }
    public string? mcg_CustomerGrpName { get; set; }
    public string? mcs_CustomerSourceName { get; set; }
    public string? mp_ProvinceName { get; set; }
    public string? md_DistrictName { get; set; }
    public string? mw_WardName { get; set; }
    public string? ma_AreaName { get; set; }
    public string? mg_GovIDTypeName { get; set; }
}

public class Mst_CustomerConfiguration : IEntityTypeConfiguration<Mst_Customer>
{
    public void Configure(EntityTypeBuilder<Mst_Customer> builder)
    {
        builder.ToTable("Mst_Customer");
        builder.HasKey(x => new { x.OrgID, x.CustomerCodeSys });

        builder.Property(x => x.OrgID).HasMaxLength(50);
        builder.Property(x => x.CustomerCodeSys).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.CustomerCode).HasMaxLength(50);
        builder.Property(x => x.CustomerType).HasMaxLength(50);
        builder.Property(x => x.CustomerGrpCode).HasMaxLength(250);
        builder.Property(x => x.CustomerSourceCode).HasMaxLength(50);
        builder.Property(x => x.CustomerSourceNameActual).HasMaxLength(250);
        builder.Property(x => x.CustomerName).HasMaxLength(250);
        builder.Property(x => x.CustomerNameEN).HasMaxLength(250);
        builder.Property(x => x.CustomerGender).HasMaxLength(10);
        builder.Property(x => x.CustomerPhoneNo).HasMaxLength(50);
        builder.Property(x => x.CustomerMobilePhone).HasMaxLength(50);
        builder.Property(x => x.ProvinceCode).HasMaxLength(50);
        builder.Property(x => x.DistrictCode).HasMaxLength(50);
        builder.Property(x => x.WardCode).HasMaxLength(50);
        builder.Property(x => x.AreaCode).HasMaxLength(50);
        builder.Property(x => x.CustomerAvatarName).HasMaxLength(250);
        builder.Property(x => x.CustomerAvatarSpec).HasMaxLength(250);
        builder.Property(x => x.FlagCustomerAvatarPath).HasMaxLength(1);
        builder.Property(x => x.CustomerAvatarPath).HasMaxLength(500);
        builder.Property(x => x.CustomerAddress).HasMaxLength(500);
        builder.Property(x => x.CustomerEmail).HasMaxLength(250);
        builder.Property(x => x.CustomerDateOfBirth).HasMaxLength(50);
        builder.Property(x => x.GovIDType).HasMaxLength(50);
        builder.Property(x => x.GovIDCardNo).HasMaxLength(50);
        builder.Property(x => x.GovIDCardDate).HasMaxLength(50);
        builder.Property(x => x.GovIDCardPlace).HasMaxLength(250);
        builder.Property(x => x.TaxCode).HasMaxLength(50);
        builder.Property(x => x.BankCode).HasMaxLength(50);
        builder.Property(x => x.BankName).HasMaxLength(250);
        builder.Property(x => x.BankAccountNo).HasMaxLength(50);
        builder.Property(x => x.RepresentName).HasMaxLength(250);
        builder.Property(x => x.RepresentPosition).HasMaxLength(250);
        builder.Property(x => x.UserCodeOwner).HasMaxLength(50);
        builder.Property(x => x.ContactName).HasMaxLength(250);
        builder.Property(x => x.ContactPhone).HasMaxLength(50);
        builder.Property(x => x.ContactEmail).HasMaxLength(250);
        builder.Property(x => x.Fax).HasMaxLength(50);
        builder.Property(x => x.Facebook).HasMaxLength(250);
        builder.Property(x => x.InvoiceCustomerName).HasMaxLength(250);
        builder.Property(x => x.InvoiceCustomerAddress).HasMaxLength(500);
        builder.Property(x => x.InvoiceOrgName).HasMaxLength(250);
        builder.Property(x => x.InvoiceEmailSend).HasMaxLength(250);
        builder.Property(x => x.MST).HasMaxLength(50);
        builder.Property(x => x.ListOfCustDynamicFieldValue).HasMaxLength(4000);
        builder.Property(x => x.FlagDealer).HasMaxLength(1);
        builder.Property(x => x.FlagSupplier).HasMaxLength(1);
        builder.Property(x => x.FlagEndUser).HasMaxLength(1);
        builder.Property(x => x.FlagShipper).HasMaxLength(1);
        builder.Property(x => x.FlagBank).HasMaxLength(1);
        builder.Property(x => x.FlagInsurrance).HasMaxLength(1);
        builder.Property(x => x.DTimeUsed).HasMaxLength(50);
        builder.Property(x => x.CreateDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.CreateBy).HasMaxLength(50);
        builder.Property(x => x.LUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LUBy).HasMaxLength(50);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.GLN).HasMaxLength(50);
        builder.Property(x => x.Remark).HasMaxLength(500);
        builder.Property(x => x.Coordinates).HasMaxLength(100);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);

        // Bỏ qua các trường unmapped tính từ bảng liên kết
        builder.Ignore(x => x.Org_NNTFullName);
        builder.Ignore(x => x.Network_NNTFullName);
        builder.Ignore(x => x.mct_CustomerTypeName);
        builder.Ignore(x => x.mcg_CustomerGrpName);
        builder.Ignore(x => x.mcs_CustomerSourceName);
        builder.Ignore(x => x.mp_ProvinceName);
        builder.Ignore(x => x.md_DistrictName);
        builder.Ignore(x => x.mw_WardName);
        builder.Ignore(x => x.ma_AreaName);
        builder.Ignore(x => x.mg_GovIDTypeName);
    }
}

public class Mst_CustomerSeeder : IPortSeeder
{
    // Order danh mục 10-99 (PortInfra.cs:16-17)
    public int Order => 45;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Customer>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Customer
            {
                OrgID = "7200000001",
                CustomerCodeSys = "CUS001",
                CustomerCode = "KH001",
                CustomerName = "Công ty Cổ phần Alpha",
                NetworkID = "1001",
                CustomerType = "ENTERPRISE",
                CustomerSourceCode = "SRC01",
                AreaCode = "MB",
                CustomerMobilePhone = "0901234567",
                CustomerEmail = "alpha@domain.vn",
                MST = "0101234567",
                ContactName = "Nguyễn Văn Alpha",
                ContactEmail = "alpha@domain.vn",
                FlagDealer = "1",
                FlagEndUser = "1",
                FlagSupplier = "0",
                FlagShipper = "0",
                FlagBank = "0",
                FlagInsurrance = "0",
                CreateDTimeUTC = "2026-10-18 10:00:00",
                CreateBy = "port",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 10:00:00",
                LogLUBy = "port"
            },
            new Mst_Customer
            {
                OrgID = "7200000001",
                CustomerCodeSys = "CUS002",
                CustomerCode = "KH002",
                CustomerName = "Tập đoàn Beta",
                NetworkID = "1001",
                CustomerType = "ENTERPRISE",
                CustomerSourceCode = "SRC02",
                AreaCode = "MN",
                CustomerMobilePhone = "0912345678",
                CustomerEmail = "beta@domain.vn",
                ContactName = "Trần Thị Beta",
                ContactEmail = "beta@domain.vn",
                FlagDealer = "0",
                FlagEndUser = "0",
                FlagSupplier = "1",
                FlagShipper = "1",
                FlagBank = "0",
                FlagInsurrance = "0",
                CreateDTimeUTC = "2026-10-18 11:00:00",
                CreateBy = "port",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 11:00:00",
                LogLUBy = "port"
            },
            new Mst_Customer
            {
                OrgID = "7200000001",
                CustomerCodeSys = "CUS003",
                CustomerCode = "KH003",
                CustomerName = "Nguyễn Văn C",
                NetworkID = "1001",
                CustomerType = "PERSONAL",
                CustomerSourceCode = "SRC03",
                AreaCode = "MB",
                CustomerMobilePhone = "0923456789",
                CustomerEmail = "vanc@domain.vn",
                ContactName = "Nguyễn Văn C",
                ContactEmail = "vanc@domain.vn",
                FlagDealer = "0",
                FlagEndUser = "0",
                FlagSupplier = "0",
                FlagShipper = "0",
                FlagBank = "1",
                FlagInsurrance = "0",
                CreateDTimeUTC = "2026-10-18 12:00:00",
                CreateBy = "port",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 12:00:00",
                LogLUBy = "port"
            },
            new Mst_Customer
            {
                OrgID = "7200000001",
                CustomerCodeSys = "CUS004",
                CustomerCode = "KH004",
                CustomerName = "Công ty TNHH Gamma Cũ",
                NetworkID = "1001",
                CustomerType = "VIP",
                CustomerSourceCode = "SRC01",
                AreaCode = "MT",
                CustomerMobilePhone = "0934567890",
                CustomerEmail = "gamma@domain.vn",
                ContactName = "Lê Văn Gamma",
                ContactEmail = "gamma@domain.vn",
                FlagDealer = "0",
                FlagEndUser = "0",
                FlagSupplier = "0",
                FlagShipper = "0",
                FlagBank = "0",
                FlagInsurrance = "1",
                CreateDTimeUTC = "2026-10-18 13:00:00",
                CreateBy = "port",
                FlagActive = "0",
                LogLUDTimeUTC = "2026-10-18 13:00:00",
                LogLUBy = "port"
            },
            new Mst_Customer
            {
                OrgID = "7200000002",
                CustomerCodeSys = "CUS005",
                CustomerCode = "KH005",
                CustomerName = "Công ty Delta Chi Nhánh 2",
                NetworkID = "1002",
                CustomerType = "ENTERPRISE",
                CustomerSourceCode = "SRC01",
                AreaCode = "MB",
                CustomerMobilePhone = "0945678901",
                CustomerEmail = "delta@domain.vn",
                ContactName = "Phạm Văn Delta",
                ContactEmail = "delta@domain.vn",
                FlagDealer = "1",
                FlagEndUser = "1",
                FlagSupplier = "0",
                FlagShipper = "0",
                FlagBank = "0",
                FlagInsurrance = "0",
                CreateDTimeUTC = "2026-10-18 14:00:00",
                CreateBy = "port",
                FlagActive = "1",
                LogLUDTimeUTC = "2026-10-18 14:00:00",
                LogLUBy = "port"
            }
        );

        await db.SaveChangesAsync();
    }
}
