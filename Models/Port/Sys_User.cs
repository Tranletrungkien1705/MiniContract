using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Map_UserInNotifyTypeController.cs — Sys_User
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Sys_User.cs:9-52
public class Sys_User
{
    public string UserCode { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? MST { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Position { get; set; }
    public string? FlagActive { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
}

public class Sys_UserConfiguration : IEntityTypeConfiguration<Sys_User>
{
    public void Configure(EntityTypeBuilder<Sys_User> builder)
    {
        builder.ToTable("Sys_User");
        builder.HasKey(x => x.UserCode);
        builder.Property(x => x.UserCode).HasMaxLength(100);
        builder.Property(x => x.NetworkID).HasMaxLength(100);
        builder.Property(x => x.OrgID).HasMaxLength(100);
        builder.Property(x => x.UserName).HasMaxLength(250);
        builder.Property(x => x.UserPassword).HasMaxLength(250);
        builder.Property(x => x.PhoneNo).HasMaxLength(50);
        builder.Property(x => x.EMail).HasMaxLength(250);
        builder.Property(x => x.MST).HasMaxLength(50);
        builder.Property(x => x.DepartmentCode).HasMaxLength(100);
        builder.Property(x => x.Position).HasMaxLength(100);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.FlagSysAdmin).HasMaxLength(1);
        builder.Property(x => x.FlagNNTAdmin).HasMaxLength(1);
    }
}

public class Sys_UserSeeder : IPortSeeder
{
    // Order Map_/Sys_ 100-199 (PortInfra.cs:16-17)
    public int Order => 101;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Sys_User>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Sys_User
            {
                UserCode = "U01",
                NetworkID = "1001",
                OrgID = "7200000001",
                UserName = "admin@solution.hn",
                EMail = "admin@solution.hn",
                MST = "0100000001",
                DepartmentCode = "BGD",
                Position = "Giám đốc",
                FlagActive = "1",
                FlagSysAdmin = "1"
            },
            new Sys_User
            {
                UserCode = "U02",
                NetworkID = "1001",
                OrgID = "7200000001",
                UserName = "tester@solution.hn",
                EMail = "tester@solution.hn",
                MST = "0100000001",
                DepartmentCode = "KT",
                Position = "Nhân viên",
                FlagActive = "1",
                FlagSysAdmin = "0"
            },
            new Sys_User
            {
                UserCode = "U03",
                NetworkID = "1001",
                OrgID = "7200000001",
                UserName = "dev@solution.hn",
                EMail = "dev@solution.hn",
                MST = "0100000002",
                DepartmentCode = "KT",
                Position = "Lập trình viên",
                FlagActive = "1",
                FlagSysAdmin = "0"
            }
        );
        await db.SaveChangesAsync();
    }
}
