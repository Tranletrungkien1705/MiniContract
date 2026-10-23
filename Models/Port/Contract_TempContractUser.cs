using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniContract.Models.Port
{
    // port 1:1 Contract_TempContractController.cs — Contract_TempContractUser
    // Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Biz\QContract.cs:6581-6623, 6081-6106
    [Table("Contract_TempContractUser")]
    public class Contract_TempContractUser
    {
        public string TContractCode { get; set; } = string.Empty;
        public string? PartyCode { get; set; }
        public string? UserCodeSysSign { get; set; }
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public string? Idx { get; set; }
        public string? UserCodeSign { get; set; }
        public string? UserNameSign { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string? UserZalo { get; set; }
        public string? SignDetailJson { get; set; }
        public string? FlagActive { get; set; }
        public string? AccessKey { get; set; }
        public string? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
    }

    public class Contract_TempContractUserConfiguration : IEntityTypeConfiguration<Contract_TempContractUser>
    {
        public void Configure(EntityTypeBuilder<Contract_TempContractUser> builder)
        {
            builder.ToTable("Contract_TempContractUser");
            builder.HasKey(x => new { x.TContractCode, x.Idx });
        }
    }
}
