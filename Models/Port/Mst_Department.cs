using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniContract.Data;

namespace MiniContract.Models.Port;

// port 1:1 Mst_DepartmentController.cs — Mst_Department
// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:8-31
public class Mst_Department
{
    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:10
    public string DepartmentCode { get; set; } = "";

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:12
    public string? NetworkID { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:14
    public string? DepartmentCodeParent { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:16
    public string? DepartmentBUCode { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:18
    public string? DepartmentBUPattern { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:20
    public string? DepartmentLevel { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:22
    public string? MST { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:24
    public string? DepartmentName { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:26
    public string? FlagActive { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:28
    public string? LogLUDTimeUTC { get; set; }

    // D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Models\Mst_Department.cs:30
    public string? LogLUBy { get; set; }
}

public class Mst_DepartmentConfiguration : IEntityTypeConfiguration<Mst_Department>
{
    public void Configure(EntityTypeBuilder<Mst_Department> builder)
    {
        builder.ToTable("Mst_Department");
        // Khoá chính DepartmentCode theo DDL và SQL nguồn Master.cs:35648, 36696, 36739
        builder.HasKey(x => x.DepartmentCode);

        builder.Property(x => x.DepartmentCode).HasMaxLength(50);
        builder.Property(x => x.NetworkID).HasMaxLength(50);
        builder.Property(x => x.DepartmentCodeParent).HasMaxLength(50);
        builder.Property(x => x.DepartmentBUCode).HasMaxLength(100);
        builder.Property(x => x.DepartmentBUPattern).HasMaxLength(250);
        builder.Property(x => x.DepartmentLevel).HasMaxLength(10);
        builder.Property(x => x.MST).HasMaxLength(50);
        builder.Property(x => x.DepartmentName).HasMaxLength(250);
        builder.Property(x => x.FlagActive).HasMaxLength(1);
        builder.Property(x => x.LogLUDTimeUTC).HasMaxLength(50);
        builder.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

public class Mst_DepartmentSeeder : IPortSeeder
{
    // Order danh mục 10-99 (chọn 50 chưa dùng trong Models/Port)
    public int Order => 50;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Department>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Department
            {
                DepartmentCode = "DEPT_BGD",
                DepartmentName = "Ban Giám Đốc",
                NetworkID = "1001",
                MST = "0101234567",
                DepartmentLevel = "1",
                DepartmentCodeParent = null,
                DepartmentBUCode = "HO",
                DepartmentBUPattern = "HO%",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            new Mst_Department
            {
                DepartmentCode = "DEPT_KT",
                DepartmentName = "Phòng Kế Toán",
                NetworkID = "1001",
                MST = "0101234567",
                DepartmentLevel = "2",
                DepartmentCodeParent = "DEPT_BGD",
                DepartmentBUCode = "HO.DEPT_KT",
                DepartmentBUPattern = "HO.DEPT_KT%",
                FlagActive = "1",
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            new Mst_Department
            {
                DepartmentCode = "DEPT_KD_INACT",
                DepartmentName = "Phòng Kinh Doanh Cũ",
                NetworkID = "1001",
                MST = "0101234567",
                DepartmentLevel = "2",
                DepartmentCodeParent = "DEPT_BGD",
                DepartmentBUCode = "HO.DEPT_KD_INACT",
                DepartmentBUPattern = "HO.DEPT_KD_INACT%",
                FlagActive = "0",
                LogLUBy = "port",
                LogLUDTimeUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            }
        );
        await db.SaveChangesAsync();
    }
}

// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Extensions\Mst_DepartmentExt.cs:10-70
[NotMapped]
public class Mst_DepartmentExt : Mst_Department
{
    private Mst_DepartmentExt? _parent;
    [System.Text.Json.Serialization.JsonIgnore]
    public Mst_DepartmentExt Parent
    {
        get => _parent ??= new Mst_DepartmentExt { DepartmentCode = DepartmentCodeParent ?? "" };
        set => _parent = value;
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public List<Mst_DepartmentExt> Children { get; set; } = new();

    public int HLevel { get; set; }

    public string HlevelTitle
    {
        get
        {
            if (HLevel > 0)
            {
                var l = "";
                for (var i = 1; i <= HLevel; ++i)
                {
                    l += "|--";
                }
                return string.Format("{0}{1}", l, DepartmentName);
            }
            return DepartmentName?.ToString() ?? "";
        }
    }

    public string HlevelCode
    {
        get
        {
            if (HLevel > 0)
            {
                var l = "";
                for (var i = 1; i <= HLevel; ++i)
                {
                    l += "|--";
                }
                return string.Format("{0}{1}", l, DepartmentCode);
            }
            return DepartmentCode?.ToString() ?? "";
        }
    }
}

// Nguồn: D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Common\Extensions\Mst_DepartmentExtension.cs:9-74
public static class Mst_DepartmentExtension
{
    public static List<Mst_DepartmentExt> GetGroupBaseList(List<Mst_DepartmentExt> s)
    {
        var listPageBase = new List<Mst_DepartmentExt>();
        if (s != null)
        {
            listPageBase.AddRange(s);
        }
        return listPageBase;
    }

    public static void BuildGroupBaseTree(this Mst_DepartmentExt groupBase, List<Mst_DepartmentExt> groupBaseList)
    {
        if (groupBase.Children == null) groupBase.Children = new List<Mst_DepartmentExt>();

        foreach (var c in groupBaseList)
        {
            if (c.DepartmentCodeParent != null && string.Equals(c.DepartmentCodeParent, groupBase.DepartmentCode, StringComparison.OrdinalIgnoreCase))
            {
                if (!groupBase.Children.Contains(c))
                {
                    groupBase.Children.Add(c);
                    c.Parent = groupBase;
                    BuildGroupBaseTree(c, groupBaseList);
                }
            }
        }
    }

    public static List<Mst_DepartmentExt> ToGroupBaseTree(List<Mst_DepartmentExt> groupBaseList)
    {
        var list = new List<Mst_DepartmentExt>();
        foreach (var groupBase in groupBaseList)
        {
            if (string.IsNullOrWhiteSpace(groupBase.DepartmentCodeParent))
            {
                if (groupBase.Children == null || groupBase.Children.Count == 0)
                {
                    groupBase.BuildGroupBaseTree(groupBaseList);
                }
                list.Add(groupBase);
            }
        }
        return list;
    }

    public static List<Mst_DepartmentExt> ToFlatGroupBaseTree(this List<Mst_DepartmentExt> groupBaseList, int level = 0)
    {
        var list = new List<Mst_DepartmentExt>();
        foreach (var groupBase in groupBaseList)
        {
            groupBase.HLevel = level;
            list.Add(groupBase);
            if (groupBase.Children != null && groupBase.Children.Count > 0)
            {
                list.AddRange(groupBase.Children.ToFlatGroupBaseTree(level + 1));
            }
        }
        return list;
    }
}

