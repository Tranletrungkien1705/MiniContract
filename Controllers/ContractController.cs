using Microsoft.AspNetCore.Mvc;
using MiniContract.Models;
using MiniContract.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MiniContract.Controllers;

public class ContractController(IContractService svc) : Controller
{
    public async Task<IActionResult> Index(ContractStatus? status, string? q)
    {
        ViewBag.Status = status; ViewBag.Q = q;
        return View(await svc.ListAsync(status, q));
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Types = await svc.TypesAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, int? typeId, decimal value, string body,
        string partyAName, string? partyAEmail, string partyBName, string? partyBEmail)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(partyAName) || string.IsNullOrWhiteSpace(partyBName))
        {
            TempData["Error"] = "Cần tiêu đề và tên 2 bên.";
            ViewBag.Types = await svc.TypesAsync();
            return View();
        }
        var c = new Contract { Title = title.Trim(), TypeId = typeId, Value = value, Body = body ?? "", CreatedBy = "web" };
        var parties = new List<ContractParty>
        {
            new() { Name = partyAName.Trim(), Email = partyAEmail, Role = PartyRole.PartyA },
            new() { Name = partyBName.Trim(), Email = partyBEmail, Role = PartyRole.PartyB },
        };
        var id = await svc.CreateAsync(c, parties);
        TempData["Success"] = "Đã tạo hợp đồng nháp.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var c = await svc.GetAsync(id);
        if (c == null) return NotFound();
        ViewBag.Annexes = await svc.AnnexesAsync(id);
        ViewBag.History = await svc.HistoryAsync(id);
        ViewBag.Elements = await svc.ElementsAsync(id);
        ViewBag.ElementStats = await svc.ElementStatsAsync(id);
        ViewBag.Checkers = await svc.CheckersAsync(id);
        ViewBag.CheckerStats = await svc.CheckerStatsAsync(id);
        return View(c);
    }

    // ── Ô ký trên hợp đồng (Contract_ContractElement) ────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddElement(int id, string elementName, ElementType type, int? partyId,
        int pageIdx, double elementX, double elementY, double elementWidth, double elementHeight)
    {
        if (string.IsNullOrWhiteSpace(elementName))
        {
            TempData["Error"] = "Cần tên ô ký.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        try
        {
            await svc.AddElementAsync(id, new ContractElement
            {
                ElementName = elementName.Trim(), Type = type, PartyId = partyId,
                PageIdx = pageIdx <= 0 ? 1 : pageIdx,
                ElementX = elementX, ElementY = elementY,
                ElementWidth = elementWidth, ElementHeight = elementHeight
            });
            TempData["Success"] = "Đã thêm ô ký.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SignElement(int id, int elementId, string? signerName)
    {
        var (ok, msg) = await svc.SignElementAsync(elementId, signerName ?? "", "web", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Người kiểm tra hợp đồng (Contract_Checker) ───────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChecker(int id, string userName, string? position, int idx, bool sequential)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            TempData["Error"] = "Cần tên người kiểm tra.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        try
        {
            await svc.AddCheckerAsync(id, new ContractChecker
            {
                UserName = userName.Trim(), Position = position, Idx = idx, Sequential = sequential
            });
            TempData["Success"] = "Đã thêm người kiểm tra.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptCheck(int id, int checkerId, string? remark)
    {
        var (ok, msg) = await svc.AcceptCheckAsync(id, checkerId, remark);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Lý do kết thúc hợp đồng (Mst_FinishedContractReason) ─────────
    // Danh mục lý do kết thúc/chấm dứt hợp đồng — port từ Mst_FinishedContractReason (QContract).
    public async Task<IActionResult> FinishReasons()
    {
        return View(await svc.FinishReasonsAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFinishReason(string name, FinishType type, string? code, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên lý do kết thúc.";
            return RedirectToAction(nameof(FinishReasons));
        }
        try
        {
            await svc.AddFinishReasonAsync(new FinishedContractReason
            {
                Name = name.Trim(), Type = type, Code = code?.Trim() ?? "", Description = description
            });
            TempData["Success"] = "Đã thêm lý do kết thúc.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(FinishReasons));
    }

    // Kết thúc/chấm dứt hợp đồng theo lý do — port từ Contract_ContractParty_Finish (QContract).
    public async Task<IActionResult> Finish(int id)
    {
        var c = await svc.GetAsync(id);
        if (c == null) return NotFound();
        if (c.Status is ContractStatus.Cancelled or ContractStatus.Finished)
        {
            TempData["Error"] = "Hợp đồng đã hủy hoặc đã kết thúc.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        ViewBag.Reasons = await svc.FinishReasonsAsync(activeOnly: true);
        return View(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, int reasonId, string? description)
    {
        var (ok, msg) = await svc.FinishContractAsync(id, reasonId, description, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Ghi chú xử lý vào nhật ký thao tác (audit trail) của hợp đồng.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRemark(int id, string remark)
    {
        await svc.AddRemarkAsync(id, "web", remark);
        TempData["Success"] = "Đã ghi chú vào nhật ký hợp đồng.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Phụ lục hợp đồng ─────────────────────────────
    public async Task<IActionResult> CreateAnnex(int id)
    {
        var parent = await svc.GetAsync(id);
        if (parent == null) return NotFound();
        if (parent.IsAnnex) { TempData["Error"] = "Không thể tạo phụ lục của một phụ lục."; return RedirectToAction(nameof(Detail), new { id }); }
        ViewBag.Parent = parent;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnex(int id, string title, decimal value, string body,
        string partyAName, string? partyAEmail, string partyBName, string? partyBEmail)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(partyAName) || string.IsNullOrWhiteSpace(partyBName))
        {
            TempData["Error"] = "Cần tiêu đề và tên 2 bên.";
            return RedirectToAction(nameof(CreateAnnex), new { id });
        }
        var annex = new Contract { Title = title.Trim(), Value = value, Body = body ?? "", CreatedBy = "web" };
        var parties = new List<ContractParty>
        {
            new() { Name = partyAName.Trim(), Email = partyAEmail, Role = PartyRole.PartyA },
            new() { Name = partyBName.Trim(), Email = partyBEmail, Role = PartyRole.PartyB },
        };
        try
        {
            var annexId = await svc.CreateAnnexAsync(id, annex, parties);
            TempData["Success"] = "Đã tạo phụ lục nháp.";
            return RedirectToAction(nameof(Detail), new { id = annexId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id)
    {
        try { await svc.SendAsync(id); TempData["Success"] = "Đã gửi hợp đồng cho các bên ký."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try { await svc.CancelAsync(id); TempData["Success"] = "Đã hủy hợp đồng."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SignCks(int id, int partyId)
    {
        var (ok, msg) = await svc.SignCksAsync(id, partyId);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult OtpGenerate(int id, int partyId)
    {
        var code = svc.OtpGenerate(partyId);
        TempData["Success"] = $"Mã OTP (demo): {code} — nhập để ký (hết hạn 5 phút).";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SignOtp(int id, int partyId, string otp)
    {
        var (ok, msg) = await svc.SignOtpAsync(id, partyId, otp);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Link ký công khai ────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSignLink(int id, int partyId, int validHours)
    {
        try
        {
            var link = await svc.CreateSignLinkAsync(id, partyId, validHours, "web");
            var url = Url.Action("PublicSign", "Sign", new { token = link.Token }, Request.Scheme);
            TempData["Success"] = $"Đã tạo link ký (hết hạn {link.EndDate:dd/MM/yyyy HH:mm}): {url}";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSignLink(int id, int linkId)
    {
        try { await svc.RevokeSignLinkAsync(linkId, "web"); TempData["Success"] = "Đã thu hồi link ký."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Bản thể hiện PDF của hợp đồng (kèm trạng thái ký các bên)
    public async Task<IActionResult> Pdf(int id)
    {
        var c = await svc.GetAsync(id);
        if (c == null) return NotFound();
        var (statusText, _) = Ui.Status(c.Status);
        var bytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));
                page.Header().Column(col =>
                {
                    col.Item().Text("HỢP ĐỒNG").FontSize(20).Bold().AlignCenter();
                    col.Item().Text(c.Title).FontSize(13).AlignCenter();
                    col.Item().Text($"Số: {c.Code} — Trạng thái: {statusText}").FontSize(9).AlignCenter().FontColor(Colors.Grey.Darken1);
                });
                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Giá trị: {c.Value:N0} đ");
                    col.Item().Text("Nội dung:").Bold();
                    col.Item().Text(c.Body);
                    col.Item().PaddingTop(10).Text("Các bên & chữ ký:").Bold();
                    foreach (var p in c.Parties.OrderBy(x => x.SignOrder))
                    {
                        var sig = c.Signatures.FirstOrDefault(s => s.PartyId == p.Id);
                        var line = $"• {Ui.Role(p.Role)}: {p.Name}" +
                                   (p.HasSigned ? $"  ✔ đã ký ({(sig != null ? Ui.Method(sig.Method) : "")}) lúc {p.SignedAt:dd/MM/yyyy HH:mm}" : "  ☐ chưa ký");
                        col.Item().Text(line);
                    }
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("MiniContract — bản thể hiện điện tử. ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span($"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
        return File(bytes, "application/pdf", $"HopDong-{c.Code}.pdf");
    }
}
