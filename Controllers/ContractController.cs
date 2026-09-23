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
        ViewBag.ApproveStats = await svc.ApproveStatsAsync(id);
        ViewBag.UserAssignments = await svc.UserAssignmentsAsync(id);
        ViewBag.SendHistory = await svc.SendHistoryAsync(id);
        ViewBag.VerifyOtps = await svc.VerifyOtpsAsync(id);
        return View(c);
    }

    // ── Lịch sử gửi hợp đồng (Contract_SendHist) ─────────────────────
    // Ghi 1 bản ghi gửi cho 1 bên qua 1 kênh — port từ Contract_SendHist (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSendHist(int id, int? partyId, ChannelType channel, BulletinType bulletin, string? infoReceive, string? remark)
    {
        try
        {
            await svc.AddSendHistAsync(id, partyId, channel, bulletin, infoReceive, remark, "web");
            TempData["Success"] = "Đã ghi nhận gửi hợp đồng.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Gửi lại các bản tin đã chọn — port từ ContractReSendHist (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(int id, int[]? sendHistIds)
    {
        var (ok, msg) = await svc.ResendAsync(id, (sendHistIds ?? []).ToList(), "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
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

    // ── Phê duyệt hợp đồng (Contract_Contract_Approved) ──────────────
    // Người kiểm tra phê duyệt hợp đồng — port từ WAS_Contract_Contract_Approved (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string userCode, string? remark)
    {
        var (ok, msg) = await svc.ApproveAsync(id, userCode, remark);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Phân quyền hợp đồng (Contract_UserInContract) ────────────────
    // Ghi đè toàn bộ danh sách người dùng được phân quyền — port từ Decentralized (QContract).
    // Mỗi dòng: mã | tên | email (tên/email tùy chọn).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUserAssignments(int id, string? userCodes)
    {
        var users = new List<ContractUserInContract>();
        foreach (var raw in (userCodes ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = raw.Split('|');
            var code = parts.Length > 0 ? parts[0].Trim() : "";
            var name = parts.Length > 1 ? parts[1].Trim() : "";
            var email = parts.Length > 2 ? parts[2].Trim() : "";
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name)) continue;
            users.Add(new ContractUserInContract { UserCode = code, UserName = name, Email = email });
        }
        var (ok, msg) = await svc.SaveUserAssignmentsAsync(id, users, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Hợp đồng mẫu (Contract_TempContract) ─────────────────────────
    // Danh mục hợp đồng mẫu — port từ Contract_TempContract (QContract).
    public async Task<IActionResult> Templates()
    {
        ViewBag.Types = await svc.TypesAsync();
        return View(await svc.TemplatesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate(int id, string name, int? typeId, string? body, string? remark, bool active)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên hợp đồng mẫu.";
            return RedirectToAction(nameof(Templates));
        }
        try
        {
            await svc.SaveTemplateAsync(new ContractTemplate
            {
                Id = id, Name = name.Trim(), TypeId = typeId, Body = body ?? "", Remark = remark, Active = active
            }, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật hợp đồng mẫu." : "Đã thêm hợp đồng mẫu.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Templates));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var (ok, msg) = await svc.DeleteTemplateAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Templates));
    }

    // Soạn hợp đồng mới từ mẫu — port từ luồng Create(tcontractcode) (QContract).
    public async Task<IActionResult> CreateFromTemplate(int id)
    {
        var t = (await svc.TemplatesAsync()).FirstOrDefault(x => x.Id == id);
        if (t == null) return NotFound();
        ViewBag.Template = t;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromTemplate(int id, string title, decimal value, string? body,
        string partyAName, string? partyAEmail, string partyBName, string? partyBEmail)
    {
        if (string.IsNullOrWhiteSpace(partyAName) || string.IsNullOrWhiteSpace(partyBName))
        {
            TempData["Error"] = "Cần tên 2 bên.";
            return RedirectToAction(nameof(CreateFromTemplate), new { id });
        }
        var parties = new List<ContractParty>
        {
            new() { Name = partyAName.Trim(), Email = partyAEmail, Role = PartyRole.PartyA },
            new() { Name = partyBName.Trim(), Email = partyBEmail, Role = PartyRole.PartyB },
        };
        try
        {
            var newId = await svc.CreateFromTemplateAsync(id, title, value, body, parties, "web");
            TempData["Success"] = "Đã soạn hợp đồng từ mẫu.";
            return RedirectToAction(nameof(Detail), new { id = newId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(CreateFromTemplate), new { id });
        }
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

    // ── Quy tắc đánh số hợp đồng theo loại (Mst_ContractTypeContractNo) ──
    // Danh mục quy tắc sinh số hợp đồng cho từng loại — port từ Mst_ContractTypeContractNo (QContract).
    public async Task<IActionResult> NumberRules()
    {
        ViewBag.Types = await svc.TypesAsync();
        return View(await svc.NumberRulesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNumberRule(int typeId, Typefix typefixCode, string? typefixInput, int seqNumberLength, int numberStart, bool active)
    {
        if (typeId <= 0)
        {
            TempData["Error"] = "Cần chọn loại hợp đồng.";
            return RedirectToAction(nameof(NumberRules));
        }
        try
        {
            await svc.SaveNumberRuleAsync(new ContractNumberRule
            {
                TypeId = typeId, TypefixCode = typefixCode, TypefixInput = typefixInput?.Trim() ?? "",
                SeqNumberLength = seqNumberLength, NumberStart = numberStart, Active = active
            });
            TempData["Success"] = "Đã lưu quy tắc đánh số.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(NumberRules));
    }

    // Xem trước các số hợp đồng kế tiếp của 1 loại — port từ Seq_ContractNo_Get (QContract).
    public async Task<IActionResult> PreviewNumbers(int typeId, int amount = 5)
    {
        var numbers = await svc.PreviewNumbersAsync(typeId, amount);
        return Json(new { success = true, numbers });
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

    // ── Hủy hợp đồng bởi một bên (Contract_ContractParty_Cancel) ─────
    // Một bên (Bên A/B) hủy hợp đồng — port từ Contract_ContractParty_Cancel (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelByParty(int id, int partyId, string? remark)
    {
        var (ok, msg) = await svc.CancelByPartyAsync(id, partyId, remark, "web");
        TempData[ok ? "Success" : "Error"] = msg;
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

    // ── Mã OTP xác thực ký hợp đồng (Contract_ContractVerifyOtp) ─────
    // Sinh mã OTP cho 1 bên — port từ WAS_Contract_ContractVerifyOtp_Save (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateVerifyOtp(int id, int partyId, int validMinutes)
    {
        try
        {
            var otp = await svc.GenerateVerifyOtpAsync(id, partyId, validMinutes, "web");
            TempData["Success"] = $"Mã OTP (demo): {otp.OtpCode} — cho {otp.UserCodeSign}, hết hạn {otp.EndDate:HH:mm:ss}.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Xác thực mã OTP — port từ Contract_ContractVerifyOtp_SaveX (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(int id, string otpCode, string? userCodeSign)
    {
        var (ok, msg) = await svc.VerifyOtpAsync(id, otpCode, userCodeSign ?? "");
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
