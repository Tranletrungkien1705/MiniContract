using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port
{
    [Table("Contract_TempContract")]
    public class Contract_TempContract
    {
        [Key]
        [StringLength(50)]
        public string TContractCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string? NetworkID { get; set; }

        [StringLength(50)]
        public string? OrgID { get; set; }

        [StringLength(250)]
        public string? TContracName { get; set; }

        [StringLength(50)]
        public string? ContractType { get; set; }

        [StringLength(50)]
        public string? FlagActive { get; set; }

        public DateTime? CreateDTimeUTC { get; set; }

        [StringLength(50)]
        public string? CreateLUBy { get; set; }
    }

    public class Contract_TempContractConfiguration : IEntityTypeConfiguration<Contract_TempContract>
    {
        public void Configure(EntityTypeBuilder<Contract_TempContract> builder)
        {
            builder.HasKey(x => x.TContractCode);
            builder.ToTable("Contract_TempContract");
        }
    }

    public class Contract_TempContractSeeder : IPortSeeder
    {
        public int Order => 210;

        public async Task SeedAsync(AppDbContext db)
        {
            if (!await db.Set<Contract_TempContract>().AnyAsync(x => x.TContractCode == "TCONT001"))
            {
                db.Set<Contract_TempContract>().Add(new Contract_TempContract
                {
                    TContractCode = "TCONT001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    TContracName = "Hop dong mau 01",
                    ContractType = "TYPE01",
                    FlagActive = "1",
                    CreateDTimeUTC = DateTime.UtcNow,
                    CreateLUBy = "port"
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
