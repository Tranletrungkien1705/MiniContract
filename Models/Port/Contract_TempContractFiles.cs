using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniContract.Models.Port
{
    // port 1:1 Contract_TempContractController.cs — Contract_TempContractFiles
    // Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:6625-6658, 6197-6221
    [Table("Contract_TempContractFiles")]
    public class Contract_TempContractFiles
    {
        public string TContractCode { get; set; } = string.Empty;
        public string? Idx { get; set; }
        public string? OrgID { get; set; }
        public string? NetworkID { get; set; }
        public string? ContractFilePath { get; set; }
        public string? ContractFileName { get; set; }
        public string? ContractFileDesc { get; set; }
        public string? RefDocType { get; set; }
        public string? FlagPublic { get; set; }
        public string? FlagActive { get; set; }
        public string? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
    }

    public class Contract_TempContractFilesConfiguration : IEntityTypeConfiguration<Contract_TempContractFiles>
    {
        public void Configure(EntityTypeBuilder<Contract_TempContractFiles> builder)
        {
            builder.ToTable("Contract_TempContractFiles");
            builder.HasKey(x => new { x.TContractCode, x.Idx });
        }
    }
}
