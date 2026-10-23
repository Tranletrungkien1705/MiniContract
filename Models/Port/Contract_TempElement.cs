using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniContract.Models.Port
{
    // port 1:1 Contract_TempContractController.cs — Contract_TempElement
    // Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:6660-6707
    [Table("Contract_TempElement")]
    public class Contract_TempElement
    {
        public string TContractCode { get; set; } = string.Empty;
        public string? ElementCode { get; set; }
        public string? SignByCode { get; set; }
        public string? ElementType { get; set; }
        public string? ParamContractCode { get; set; }
        public string? PageIdx { get; set; }
        public string? ElementX { get; set; }
        public string? ElementY { get; set; }
        public string? ElementWidth { get; set; }
        public string? ElementHeight { get; set; }
        public string? PageWidth { get; set; }
        public string? PageHeight { get; set; }
        public string? ElementTypeValue { get; set; }
        public string? ElementJson { get; set; }
        public string? FlagActive { get; set; }
        public DateTime? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
        public string? ElementName { get; set; }
        public string? ElementDes { get; set; }
    }

    public class Contract_TempElementConfiguration : IEntityTypeConfiguration<Contract_TempElement>
    {
        public void Configure(EntityTypeBuilder<Contract_TempElement> builder)
        {
            builder.ToTable("Contract_TempElement");
            builder.HasKey(x => new { x.TContractCode, x.ElementCode });
        }
    }
}
