using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_ProductController.Mst_Attribute_Create
public class Mst_Attribute
{
    public string AttributeCode { get; set; } = null!;
    public string? AttributeName { get; set; }
}

public class Mst_AttributeConfiguration : IEntityTypeConfiguration<Mst_Attribute>
{
    public void Configure(EntityTypeBuilder<Mst_Attribute> builder)
    {
        builder.ToTable("Mst_Attribute");
        builder.HasKey(x => x.AttributeCode);
        builder.Property(x => x.AttributeCode).HasMaxLength(50);
        builder.Property(x => x.AttributeName).HasMaxLength(255);
    }
}

public class Mst_AttributeSeeder : IPortSeeder
{
    public int Order => 17;

    public async Task SeedAsync(AppDbContext db)
    {
        var table = db.Set<Mst_Attribute>();
        if (await table.AnyAsync()) return;

        table.AddRange(
            new Mst_Attribute { AttributeCode = "ATTR_001", AttributeName = "Màu sắc" },
            new Mst_Attribute { AttributeCode = "ATTR_002", AttributeName = "Kích thước" }
        );
        await db.SaveChangesAsync();
    }
}
