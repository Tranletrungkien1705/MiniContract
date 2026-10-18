using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port
{
    public class Contract_Attribute_Temp
    {
        public string AttributeContractCode { get; set; } = string.Empty;
        public string? TContractCode { get; set; }
    }

    public class Contract_TempChecker
    {
        public string? TContractCode { get; set; }
        public string? UserCode { get; set; }
        public string? FlagChecker { get; set; }
    }

    public class Contract_TempContractParty
    {
        public string? TContractCode { get; set; }
        public string? PartyCode { get; set; }
        public string? Description { get; set; }
        public string? CustomerCodeSys { get; set; }
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
