using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port
{
    [Table("Mst_Ward")]
    public class Mst_Ward
    {
        [StringLength(50)]
        public string WardCode { get; set; }

        [StringLength(50)]
        public string DistrictCode { get; set; }

        [Key]
        [StringLength(50)]
        public string ProvinceCode { get; set; }

        [StringLength(250)]
        public string WardName { get; set; }

        [StringLength(1)]
        public string FlagActive { get; set; }

        public DateTime? LogLUDTimeUTC { get; set; }

        [StringLength(50)]
        public string LogLUBy { get; set; }
    }

    public class Mst_WardConfiguration : IEntityTypeConfiguration<Mst_Ward>
    {
        public void Configure(EntityTypeBuilder<Mst_Ward> builder)
        {
            builder.HasKey(x => new { x.ProvinceCode, x.DistrictCode, x.WardCode });
            builder.ToTable("Mst_Ward");
        }
    }

    public class Mst_WardSeeder : IPortSeeder
    {
        public int Order => 16;

        public async Task SeedAsync(AppDbContext db)
        {
            if (!await db.Set<Mst_Ward>().AnyAsync(x => x.ProvinceCode == "101" && x.DistrictCode == "10101" && x.WardCode == "1010101"))
            {
                db.Set<Mst_Ward>().Add(new Mst_Ward
                {
                    ProvinceCode = "101",
                    DistrictCode = "10101",
                    WardCode = "1010101",
                    WardName = "Phường Phúc Xá",
                    FlagActive = "1",
                    LogLUDTimeUTC = DateTime.UtcNow,
                    LogLUBy = "admin"
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
