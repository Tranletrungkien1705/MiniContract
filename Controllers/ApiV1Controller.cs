using Microsoft.AspNetCore.Mvc;
using MiniContract.Data;
using MiniContract.Models;
using MiniContract.Services;

namespace MiniContract.Controllers;

/// <summary>
/// API JSON cho SPA React. DTO phẳng. Dashboard cache Redis 30s theo tenant (X-Cache).
/// Vòng đời HĐ: Draft → Sent → PartiallySigned → Completed (đủ chữ ký) / Cancelled. Ký CKS (XML-DSig) hoặc OTP.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ApiV1Controller(IContractService svc, ICache cache, ITenantContext tenant) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var key = $"ct:dash:{tenant.OrgId}";
        var hit = await cache.GetAsync<DashDto>(key);
        if (hit != null) { Response.Headers["X-Cache"] = "HIT"; return Ok(hit); }
        var d = await svc.DashboardAsync();
        var dto = new DashDto(d.Total, d.Draft, d.AwaitingSign, d.Completed, d.TotalValue,
            d.ByStatus.Select(x => new ByStatusDto(x.Status, x.Count)).ToList());
        await cache.SetAsync(key, dto, TimeSpan.FromSeconds(30));
        Response.Headers["X-Cache"] = "MISS";
        return Ok(dto);
    }

    [HttpGet("types")]
    public async Task<IActionResult> Types()
        => Ok((await svc.TypesAsync()).Select(t => new { t.Id, t.Name, t.Code, t.BodyTemplate }));

    [HttpGet("contracts")]
    public async Task<IActionResult> Contracts([FromQuery] ContractStatus? status, [FromQuery] string? q)
        => Ok((await svc.ListAsync(status, q)).Select(ToListDto));

    [HttpGet("contracts/{id:int}")]
    public async Task<IActionResult> Contract(int id)
    {
        var c = await svc.GetAsync(id);
        return c == null ? NotFound(new { error = "Không tìm thấy hợp đồng." }) : Ok(ToDetailDto(c));
    }

    [HttpPost("contracts")]
    public async Task<IActionResult> Create([FromBody] ContractReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Title)) return BadRequest(new { error = "Cần tiêu đề hợp đồng." });
        var parties = (r.Parties ?? new()).Where(p => !string.IsNullOrWhiteSpace(p.Name)).Select(p => new ContractParty
        {
            Name = p.Name.Trim(), TaxCode = p.TaxCode, Email = p.Email, Phone = p.Phone, Role = (PartyRole)p.Role
        }).ToList();
        if (parties.Count == 0) return BadRequest(new { error = "Cần ít nhất 1 bên tham gia." });
        var id = await svc.CreateAsync(new Contract
        {
            Title = r.Title.Trim(), TypeId = r.TypeId, Body = r.Body ?? "", Value = r.Value, Note = r.Note, CreatedBy = "api"
        }, parties);
        return Ok(new { id });
    }

    [HttpPost("contracts/{id:int}/send")]
    public async Task<IActionResult> Send(int id)
    {
        try { await svc.SendAsync(id); return Ok(new { ok = true }); }
        catch (Exception e) { return BadRequest(new { ok = false, error = e.Message }); }
    }

    [HttpPost("contracts/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        try { await svc.CancelAsync(id); return Ok(new { ok = true }); }
        catch (Exception e) { return BadRequest(new { ok = false, error = e.Message }); }
    }

    [HttpPost("contracts/{id:int}/parties/{pid:int}/sign-cks")]
    public async Task<IActionResult> SignCks(int id, int pid)
    {
        var (ok, msg) = await svc.SignCksAsync(id, pid);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // Cấp mã OTP cho 1 bên (demo trả thẳng mã; thực tế gửi SMS/email).
    [HttpPost("contracts/{id:int}/parties/{pid:int}/otp")]
    public IActionResult Otp(int id, int pid) => Ok(new { code = svc.OtpGenerate(pid) });

    [HttpPost("contracts/{id:int}/parties/{pid:int}/sign-otp")]
    public async Task<IActionResult> SignOtp(int id, int pid, [FromBody] OtpReq r)
    {
        var (ok, msg) = await svc.SignOtpAsync(id, pid, r.Code ?? "");
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    private static object ToListDto(Contract c) => new
    {
        c.Id, c.Code, c.Title, type = c.Type?.Name, c.Value, status = (int)c.Status, statusText = Ui.Status(c.Status).text, statusCss = Ui.Status(c.Status).css,
        parties = c.Parties.Count, signed = c.SignedCount, c.CreatedAt, c.SentAt, c.CompletedAt
    };
    private static object ToDetailDto(Contract c) => new
    {
        c.Id, c.Code, c.Title, c.TypeId, type = c.Type?.Name, c.Body, c.Value, c.Note,
        status = (int)c.Status, statusText = Ui.Status(c.Status).text, statusCss = Ui.Status(c.Status).css,
        c.CreatedAt, c.SentAt, c.CompletedAt,
        parties = c.Parties.OrderBy(p => p.SignOrder).Select(p => new
        {
            p.Id, p.Name, p.TaxCode, p.Email, p.Phone, role = Ui.Role(p.Role), p.SignOrder, p.HasSigned, p.SignedAt
        }),
        signatures = c.Signatures.Select(s => new { s.SignerName, method = Ui.Method(s.Method), s.CertSubject, s.SignedAt })
    };
}

public record DashDto(int Total, int Draft, int AwaitingSign, int Completed, decimal TotalValue, List<ByStatusDto> ByStatus);
public record ByStatusDto(string Status, int Count);

public class PartyReq { public string Name { get; set; } = ""; public string? TaxCode { get; set; } public string? Email { get; set; } public string? Phone { get; set; } public int Role { get; set; } }
public class ContractReq
{
    public string Title { get; set; } = ""; public int? TypeId { get; set; } public string? Body { get; set; }
    public decimal Value { get; set; } public string? Note { get; set; } public List<PartyReq>? Parties { get; set; }
}
public class OtpReq { public string? Code { get; set; } }
