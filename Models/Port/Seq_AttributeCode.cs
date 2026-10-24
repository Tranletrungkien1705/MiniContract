using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

public class Seq_AttributeCode
{
    public int Id { get; set; }
    public string SeqValue { get; set; } = null!;
}

public class Seq_AttributeCodeConfiguration : IEntityTypeConfiguration<Seq_AttributeCode>
{
    public void Configure(EntityTypeBuilder<Seq_AttributeCode> builder)
    {
        builder.ToTable("Seq_AttributeCode");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SeqValue).HasMaxLength(50);
    }
}

public class Seq_AttributeCodeSeeder : IPortSeeder
{
    public int Order => 5;

    public async Task SeedAsync(AppDbContext db)
    {
        var table = db.Set<Seq_AttributeCode>();
        if (await table.AnyAsync()) return;

        table.Add(new Seq_AttributeCode { SeqValue = "ATTR_000" });
        await db.SaveChangesAsync();
    }
}
