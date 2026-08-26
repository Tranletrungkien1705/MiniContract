namespace MiniContract.Data;

/// <summary>Ngữ cảnh tenant của request. Middleware set OrgId (cookie org_key / header X-Api-Key), DbContext lọc.</summary>
public interface ITenantContext
{
    Guid OrgId { get; set; }
}

public sealed class TenantContext : ITenantContext
{
    /// <summary>Org mặc định (dữ liệu seed + khi chưa chọn tổ chức). Cố định để ổn định qua các lần khởi động.</summary>
    public static readonly Guid DefaultOrgId = new("55555555-5555-5555-5555-555555555555");
    public const string DefaultApiKey = "demo-contract";
    public const string CookieName = "org_key";

    public Guid OrgId { get; set; } = DefaultOrgId;
}
