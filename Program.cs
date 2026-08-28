using Microsoft.EntityFrameworkCore;
using MiniContract.Data;
using MiniContract.Models;
using MiniContract.Services;
using Serilog;

// Npgsql: DateTime Kind Local/Unspecified → timestamp without time zone
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
FleetObs.ConfigureLogger("minicontract");
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=minicontract.db";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (DbUtil.IsPostgres(conn)) o.UseNpgsql(DbUtil.ToNpgsql(conn));
    else o.UseSqlite(conn);
});
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddSingleton<ISignatureService, SignatureService>();
builder.Services.AddSingleton<OtpService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddFleetObs();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseFleetObs();

// Multi-tenant: org = cookie org_key (UI) hoặc header X-Api-Key (API). Đặt TRƯỚC khi dựng AppDbContext.
app.Use(async (ctx, next) =>
{
    var key = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(key)) ctx.Request.Cookies.TryGetValue(TenantContext.CookieName, out key);
    if (!string.IsNullOrWhiteSpace(key))
    {
        using var lookup = app.Services.CreateScope();
        var ldb = lookup.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await ldb.Orgs.FirstOrDefaultAsync(o => o.ApiKey == key);
        if (org != null) ctx.RequestServices.GetRequiredService<ITenantContext>().OrgId = org.Id;
    }
    await next();
});

app.UseStaticFiles();
app.MapGet("/healthz", () => "ok");

// Đăng ký tổ chức mới (nhận khách)
app.MapPost("/api/orgs/register", async (RegisterOrgDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần Name." });
    var org = new Org { Name = dto.Name.Trim(), ApiKey = "ctr_" + Guid.NewGuid().ToString("N") };
    db.Orgs.Add(org);
    await db.SaveChangesAsync();
    return Results.Ok(new { orgId = org.Id, apiKey = org.ApiKey, note = "Header X-Api-Key (API) hoặc cookie org_key (UI qua /Org)." });
});

// API tạo hợp đồng từ hệ thống ngoài (VD MiniDMS tạo HĐ đại lý)
app.MapPost("/api/contracts", async (CreateContractDto dto, IContractService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title)) return Results.BadRequest(new { error = "Cần Title." });
    var c = new Contract { Title = dto.Title, Body = dto.Body ?? "", Value = dto.Value, CreatedBy = "api" };
    var parties = (dto.Parties ?? []).Select(p => new ContractParty
    {
        Name = p.Name, Email = p.Email, TaxCode = p.TaxCode,
        Role = p.Role?.Equals("A", StringComparison.OrdinalIgnoreCase) == true ? PartyRole.PartyA : PartyRole.PartyB
    }).ToList();
    var id = await svc.CreateAsync(c, parties);
    var created = await svc.GetAsync(id);
    return Results.Ok(new { id, code = created!.Code, status = created.Status.ToString() });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record RegisterOrgDto(string Name);
record CreateContractDto(string Title, string? Body, decimal Value, List<PartyDto>? Parties);
record PartyDto(string Name, string? Email, string? TaxCode, string? Role);
