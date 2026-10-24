using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// Sequence cho CustomerGrpCode (nguồn: Seq.cs SeqType.CustomerGrpCode)
public class Seq_CustomerGrpCode
{
    public int Id { get; set; }
    public string SeqValue { get; set; } = null!;
}

public class Seq_CustomerGrpCodeConfiguration : IEntityTypeConfiguration<Seq_CustomerGrpCode>
{
    public void Configure(EntityTypeBuilder<Seq_CustomerGrpCode> builder)
    {
        builder.ToTable("Seq_CustomerGrpCode");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SeqValue).HasMaxLength(50);
    }
}

public class Seq_CustomerGrpCodeSeeder : IPortSeeder
{
    public int Order => 19;

    public async Task SeedAsync(AppDbContext db)
    {
        var table = db.Set<Seq_CustomerGrpCode>();
        if (await table.AnyAsync()) return;

        table.Add(new Seq_CustomerGrpCode { SeqValue = "GRP_0001" });
        await db.SaveChangesAsync();
    }
}
