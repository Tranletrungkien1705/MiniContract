using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port
{
    [Table("Mst_District")]
    public class Mst_District
    {
        [StringLength(50)]
        public string DistrictCode { get; set; }

        [Key]
        [StringLength(50)]
        public string ProvinceCode { get; set; }

        [StringLength(250)]
        public string DistrictName { get; set; }

        [StringLength(1)]
        public string FlagActive { get; set; }

        public DateTime? LogLUDTimeUTC { get; set; }

        [StringLength(50)]
        public string LogLUBy { get; set; }
    }

    public class Mst_DistrictConfiguration : IEntityTypeConfiguration<Mst_District>
    {
        public void Configure(EntityTypeBuilder<Mst_District> builder)
        {
            builder.HasKey(x => new { x.ProvinceCode, x.DistrictCode });
            builder.ToTable("Mst_District");
        }
    }

    public class Mst_DistrictSeeder : IPortSeeder
    {
        public int Order => 15;

        public async Task SeedAsync(AppDbContext db)
        {
            if (!await db.Set<Mst_District>().AnyAsync(x => x.ProvinceCode == "101" && x.DistrictCode == "10101"))
            {
                db.Set<Mst_District>().Add(new Mst_District
                {
                    ProvinceCode = "101",
                    DistrictCode = "10101",
                    DistrictName = "Quận Ba Đình",
                    FlagActive = "1",
                    LogLUDTimeUTC = DateTime.UtcNow,
                    LogLUBy = "admin"
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
