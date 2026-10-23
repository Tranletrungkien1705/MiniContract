using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port
{
    public class Contract_Attribute_Temp
    {
        public string AttributeContractCode { get; set; } = string.Empty;
        public string? TContractCode { get; set; }
        public string? AttributeValue { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public string? FlagActive { get; set; }
        public string? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
    }

    public class Contract_TempChecker
    {
        public string? TContractCode { get; set; }
        public string? UserCode { get; set; }
        public string? FlagChecker { get; set; }
    }

    // port 1:1 Contract_TempContractController.cs — Contract_TempContractParty
    // Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:6530-6578, 5874-5934
    public class Contract_TempContractParty
    {
        public string? TContractCode { get; set; }
        public string? PartyCode { get; set; }
        public string? OrgID { get; set; }
        public string? MST { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerCodeSys { get; set; }
        public string? BankAccountNo { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhoneNo { get; set; }
        public string? CustomerWebsite { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? RepresentName { get; set; }
        public string? RepresentPosition { get; set; }
        public string? Description { get; set; }
        public string? FlagActive { get; set; }
        public string? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
    }

    public class Mst_ParamContractElement : IPortSeeder
    {
        public string ParamContractCode { get; set; } = string.Empty;
        public string? ParamContractType { get; set; }
        public string? ParamContractName { get; set; }
        public string? ParamContractDes { get; set; }
        public string? FlagActive { get; set; }

        public int Order => 14;

        public void Seed(AppDbContext db)
        {
            if (!db.Set<Mst_ParamContractElement>().Any(x => x.ParamContractCode == "PARAM01"))
            {
                db.Set<Mst_ParamContractElement>().Add(new Mst_ParamContractElement
                {
                    ParamContractCode = "PARAM01",
                    ParamContractType = "DEFAULT",
                    ParamContractName = "Param 1",
                    ParamContractDes = "Mô tả param 1",
                    FlagActive = "1"
                });
                db.SaveChanges();
            }
        }

        public async Task SeedAsync(AppDbContext db)
        {
            Seed(db);
            await Task.CompletedTask;
        }
    }

    public class Contract_TempExtrasContextModelSnapshot : IEntityTypeConfiguration<Contract_Attribute_Temp>,
        IEntityTypeConfiguration<Contract_TempChecker>,
        IEntityTypeConfiguration<Contract_TempContractParty>, IEntityTypeConfiguration<Mst_ParamContractElement>
    {
        public void Configure(EntityTypeBuilder<Contract_Attribute_Temp> builder)
        {
            builder.ToTable("Contract_Attribute_Temp");
            builder.HasKey(x => x.AttributeContractCode);
        }

        public void Configure(EntityTypeBuilder<Contract_TempChecker> builder)
        {
            builder.ToTable("Contract_TempChecker");
            builder.HasKey(x => new { x.TContractCode, x.UserCode });
        }

        public void Configure(EntityTypeBuilder<Contract_TempContractParty> builder)
        {
            builder.ToTable("Contract_TempContractParty");
            builder.HasKey(x => new { x.TContractCode, x.PartyCode });
        }

        public void Configure(EntityTypeBuilder<Mst_ParamContractElement> builder)
        {
            builder.ToTable("Mst_ParamContractElement");
            builder.HasKey(x => x.ParamContractCode);
        }
    }
}
