using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniContract.Models.Port
{
    [Table("Contract_TempElement")]
    public class Contract_TempElement
    {
        [Key]
        public string TContractCode { get; set; } = string.Empty;
        public string? ElementCode { get; set; }
        public string? ElementJson { get; set; }
        public string? FlagActive { get; set; }
        public DateTime? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
    }

    public class Contract_TempElementConfiguration : IEntityTypeConfiguration<Contract_TempElement>
    {
        public void Configure(EntityTypeBuilder<Contract_TempElement> builder)
        {
            builder.ToTable("Contract_TempElement");
            builder.HasKey(x => x.TContractCode);
        }
    }
}
