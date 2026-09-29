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
        return View(c);
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

    // ══ Màn "Thông tin chung" — port 1:1 Views/Contract_Contract/Contract_Contract_Info.cshtml ══
    // Nguồn là partial được Update.cshtml:238 nhúng; ở đích gộp thành 1 trang Info có form lưu.
    public async Task<IActionResult> Info(int id)
    {
        var c = await svc.GetAsync(id);
        if (c == null) return NotFound();
        ViewBag.Types = await svc.TypesAsync();
        return View(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Info(int id, string? contractNo, string? contractName, string? tContractCode,
        int? typeId, DateTime? contractDate, DateTime? effDateStart, DateTime? effDateEnd,
        string? contractRefNo, bool flagContractAnnex, string[]? channelTypeContract, string[]? channelTypeOTP,
        bool flagCheckOTP, decimal valContract, string? currencyCode, decimal currencyRate, decimal valPaymented,
        bool flagCheckOrderSign, bool flagSysGenAccessKey, string? remark)
    {
        var input = new ContractInfoInput(contractNo, contractName, tContractCode, typeId,
            contractDate, effDateStart, effDateEnd, contractRefNo, flagContractAnnex,
            string.Join(',', channelTypeContract ?? []), string.Join(',', channelTypeOTP ?? []), flagCheckOTP,
            valContract, currencyCode, currencyRate, valPaymented,
            flagCheckOrderSign, flagSysGenAccessKey, remark);
        var (ok, msg) = await svc.SaveInfoAsync(id, input);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Info), new { id });
    }

    /// <summary>Kiểm trùng số HĐ ngay khi rời ô nhập — port 1:1 AJAX nguồn CheckContractNoExists (Modal.cshtml:1225).</summary>
    [HttpPost]
    public async Task<IActionResult> CheckContractNo(int id, string? contractNo)
    {
        var (ok, msg) = await svc.CheckContractNoAsync(id, contractNo);
        return Json(new { ok, msg });
    }

    // ══ Màn "Thông tin các bên" — port 1:1 Views/Contract_Contract/BindData_Contract_Party_Info.cshtml ══
    public async Task<IActionResult> PartyInfo(int id)
    {
        var c = await svc.GetAsync(id);
        if (c == null) return NotFound();
        ViewBag.Rows = await svc.PartyGridAsync(id);
        return View(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PartyInfo(int id, int partyId, string? customerCode, string? customerCodeSys,
        string? customerName, string? taxCode, string? customerEmail, string? customerPhoneNo,
        string? customerMobilePhone, string? facebook, string? zaloUserID, string? customerAddress,
        string? customerWebsite, string? bankAccountNo, string? bankName, string? bankCode,
        string? representName, string? representPosition, decimal valContract)
    {
        var input = new PartyInfoInput(customerCode, customerCodeSys, customerName, taxCode,
            customerEmail, customerPhoneNo, customerMobilePhone, facebook, zaloUserID,
            customerAddress, customerWebsite, bankAccountNo, bankName, bankCode,
            representName, representPosition, valContract);
        var (ok, msg) = await svc.SavePartyAsync(id, partyId, input);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(PartyInfo), new { id });
    }

    // ══ Màn "Lịch sử gửi / Gửi HĐ-TL" — port 1:1 BindData_Contract_SendHist + ShowPopup_Contract_SendHist ══
    /// <summary>
    /// Tiêu đề và quyền gửi lấy đúng nhánh nguồn (Contract_ContractController.cs:11198-11205):
    /// bulletinType = OTP ⇒ canSend = false, tiêu đề "Lịch sử gửi OTP";
    /// ngược lại tiêu đề là "Gửi HĐ/TL" khi được gửi, "Lịch sử gửi HĐ/TL" khi chỉ xem.
    /// </summary>
    public async Task<IActionResult> SendHist(int id, BulletinKind bulletinType = BulletinKind.Contract)
    {
        var c = await svc.GetAsync(id);
        if (c == null) return NotFound();
        bool canSend = bulletinType != BulletinKind.Otp
            && c.Status is ContractStatus.Sent or ContractStatus.PartiallySigned;
        ViewBag.CanSend = canSend;
        ViewBag.BulletinType = bulletinType;
        ViewBag.PopupTitle = bulletinType == BulletinKind.Otp
            ? "Lịch sử gửi OTP"
            : canSend ? "Gửi HĐ/TL" : "Lịch sử gửi HĐ/TL";
        ViewBag.Rows = await svc.SendHistAsync(id, bulletinType);
        return View(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReSendHist(int id, BulletinKind bulletinType,
        int[]? histIds, string[]? infoReceives, ChannelKind[]? channelTypes)
    {
        var (ok, msg) = await svc.ResendAsync(id, bulletinType, histIds ?? [], infoReceives ?? [], channelTypes ?? []);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(SendHist), new { id, bulletinType });
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
