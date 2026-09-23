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
        ViewBag.Signers = await svc.SignersAsync(id);
        ViewBag.SignerStats = await svc.SignerStatsAsync(id);
        ViewBag.SendHistory = await svc.SendHistoryAsync(id);
        ViewBag.VerifyOtps = await svc.VerifyOtpsAsync(id);
        ViewBag.PartySignStats = await svc.PartySignStatsAsync(id);
        ViewBag.Details = await svc.DetailsAsync(id);
        ViewBag.DetailStats = await svc.DetailStatsAsync(id);
        ViewBag.Attributes = await svc.AttributesAsync(id);
        ViewBag.AttributeDetails = await svc.AttributeDetailsAsync(id);
        ViewBag.AttributeMasters = await svc.AttributeMastersAsync(activeOnly: true);
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

    // ── Người ký của hợp đồng (Contract_ContractUser) ────────────────
    // Thêm 1 người ký cho một bên — port từ Contract_ContractUser_CheckDB (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSigner(int id, string userNameSign, int? partyId, string? userEmail,
        string? userPhone, string? userZalo, int idx)
    {
        if (string.IsNullOrWhiteSpace(userNameSign))
        {
            TempData["Error"] = "Cần tên người ký.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        try
        {
            await svc.AddSignerAsync(id, new ContractSigner
            {
                UserNameSign = userNameSign.Trim(), PartyId = partyId, UserEmail = userEmail?.Trim(),
                UserPhone = userPhone?.Trim(), UserZalo = userZalo?.Trim(), Idx = idx
            }, "web");
            TempData["Success"] = "Đã thêm người ký.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Xác nhận ký của 1 người ký — port từ Contract_ContractUser_ConfirmX (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmSigner(int id, int signerId)
    {
        var (ok, msg) = await svc.ConfirmSignerAsync(id, signerId, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Đánh dấu đã gửi yêu cầu ký cho 1 người ký — port từ Contract_ContractUser_UpdateFlagSendUserX (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSignerSent(int id, int signerId)
    {
        var (ok, msg) = await svc.MarkSignerSentAsync(id, signerId, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Ký hợp đồng bởi một bên (Contract_Contract_PartySign) ────────
    // Một bên ký hợp đồng (kèm xác thực OTP/token/phiên bản file) — port từ
    // WAS_Contract_Contract_PartySign (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PartySign(int id, int partyId, string? userCodeSign, string? userNameSign,
        string? userToken, string? otpCode, string? fileVersion)
    {
        var (ok, msg) = await svc.PartySignAsync(id, partyId, userCodeSign, userNameSign, userToken, otpCode, fileVersion, "web");
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

    // ── Nhóm hợp đồng mẫu (Contract_TempGroup) ───────────────────────
    // Danh mục nhóm hợp đồng mẫu + thuộc tính dùng chung — port từ Contract_TempGroup (QContract).
    public async Task<IActionResult> TemplateGroups()
    {
        return View(await svc.TemplateGroupsAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplateGroup(int id, string name, string? code, string? body,
        string? contractName, string? remark, bool active, string? attributes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhóm hợp đồng mẫu.";
            return RedirectToAction(nameof(TemplateGroups));
        }
        // Mỗi dòng thuộc tính: mã | giá trị
        var attrs = new List<ContractAttributeGroup>();
        foreach (var raw in (attributes ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = raw.Split('|');
            var ac = parts.Length > 0 ? parts[0].Trim() : "";
            var av = parts.Length > 1 ? parts[1].Trim() : "";
            if (string.IsNullOrWhiteSpace(ac) && string.IsNullOrWhiteSpace(av)) continue;
            attrs.Add(new ContractAttributeGroup { AttributeCode = ac, AttributeValue = av });
        }
        try
        {
            await svc.SaveTemplateGroupAsync(new ContractTemplateGroup
            {
                Id = id, Name = name.Trim(), Code = code?.Trim() ?? "", Body = body,
                ContractName = contractName, Remark = remark, Active = active
            }, attrs, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật nhóm hợp đồng mẫu." : "Đã thêm nhóm hợp đồng mẫu.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(TemplateGroups));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplateGroup(int id)
    {
        var (ok, msg) = await svc.DeleteTemplateGroupAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(TemplateGroups));
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

    // ── Cập nhật ghi chú của một bên (Contract_Contract_Party_UpdateRemark) ──
    // Cập nhật Remark của một bên — port từ WAS_Contract_Contract_Party_UpdateRemark (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePartyRemark(int id, int partyId, string? remark)
    {
        var (ok, msg) = await svc.UpdatePartyRemarkAsync(id, partyId, remark, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Cập nhật hợp đồng sau phê duyệt (Contract_ContractParty_UpdAfterApproved) ──
    // Cập nhật giá trị hợp đồng/đã thanh toán/loại/ghi chú của một bên sau khi đã phê duyệt —
    // port từ WAS_Contract_ContractParty_UpdAfterApproved (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAfterApproved(int id, int partyId, decimal valContract, decimal valPaymented,
        string? contractType, string? contractTypeName, string? remark)
    {
        var (ok, msg) = await svc.UpdateAfterApprovedAsync(id, partyId, valContract, valPaymented,
            contractType, contractTypeName, remark, "web");
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

    // ── Chữ ký số của tổ chức (Mst_OrgCKS) ───────────────────────────
    // Danh mục chứng thư số (CA) của tổ chức — port từ Mst_OrgCKS (QContract).
    public async Task<IActionResult> Certificates()
    {
        return View(await svc.CertificatesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCertificate(int id, string caNumber, string? caOrg,
        DateTime? effectiveFrom, DateTime? effectiveTo, string? ctsPath, string? ctsPwd, bool active)
    {
        if (string.IsNullOrWhiteSpace(caNumber))
        {
            TempData["Error"] = "Cần số chứng thư (CANumber).";
            return RedirectToAction(nameof(Certificates));
        }
        try
        {
            await svc.SaveCertificateAsync(new OrgCertificate
            {
                Id = id, CANumber = caNumber.Trim(), CAOrg = caOrg?.Trim(),
                EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo,
                CtsPath = ctsPath?.Trim(), CtsPwd = ctsPwd, Active = active
            }, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật chứng thư số." : "Đã thêm chứng thư số.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Certificates));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCertificate(int id)
    {
        var (ok, msg) = await svc.DeleteCertificateAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Certificates));
    }

    // ── Cấu hình ký của tổ chức (Mst_OrgSignConfig) ──────────────────
    // Danh mục cấu hình ký (REMOTE/SERVER/USBTOKEN) của tổ chức — port từ Mst_OrgSignConfig (QContract).
    public async Task<IActionResult> SignConfigs()
    {
        return View(await svc.SignConfigsAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSignConfig(int id, SignType signType, string? networkId, string orgCode,
        string? caNumber, string? caOrg, DateTime? effectiveFrom, DateTime? effectiveTo,
        string? serverSignFilePath, string? serverSignPassword, string? supplierCode,
        string? remoteSignAgreementUUID, string? remoteSignPassCode, string? authenCode, bool active)
    {
        if (string.IsNullOrWhiteSpace(orgCode))
        {
            TempData["Error"] = "Cần mã tổ chức (OrgID).";
            return RedirectToAction(nameof(SignConfigs));
        }
        try
        {
            await svc.SaveSignConfigAsync(new OrgSignConfig
            {
                Id = id, SignType = signType, NetworkID = networkId?.Trim(), OrgCode = orgCode.Trim(),
                CANumber = caNumber?.Trim(), CAOrg = caOrg?.Trim(),
                EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo,
                ServerSignFilePath = serverSignFilePath?.Trim(), ServerSignPassword = serverSignPassword,
                SupplierCode = supplierCode?.Trim(), RemoteSignAgreementUUID = remoteSignAgreementUUID?.Trim(),
                RemoteSignPassCode = remoteSignPassCode, AuthenCode = authenCode?.Trim(), Active = active
            }, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật cấu hình ký." : "Đã thêm cấu hình ký.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SignConfigs));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSignConfig(int id)
    {
        var (ok, msg) = await svc.DeleteSignConfigAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(SignConfigs));
    }

    // ── File hợp đồng (Contract_Contract_UpdateFilePath) ─────────────
    // Cập nhật file bản thể hiện (PDF) đã ký của hợp đồng — port từ
    // WAS_Contract_Contract_UpdateFilePath (QContract). ContractFileName bắt buộc.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFile(int id, string fileName, string? filePath, string? fileVersion)
    {
        var (ok, msg) = await svc.UpdateFileAsync(id, fileName, filePath, fileVersion, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Cấu hình loại hợp đồng (Mst_ContractTypeDtl) ─────────────────
    // Danh mục cấu hình kênh gửi HĐ/OTP + tự sinh số theo loại — port từ Mst_ContractTypeDtl (QContract).
    public async Task<IActionResult> TypeConfigs()
    {
        ViewBag.Types = await svc.TypesAsync();
        return View(await svc.TypeConfigsAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTypeConfig(int id, int typeId, bool genContractNo,
        bool emailContract, bool smsContract, bool zaloContract,
        bool emailOtp, bool smsOtp, bool zaloOtp, bool active, string? remark)
    {
        if (typeId <= 0)
        {
            TempData["Error"] = "Cần chọn loại hợp đồng.";
            return RedirectToAction(nameof(TypeConfigs));
        }
        try
        {
            await svc.SaveTypeConfigAsync(new ContractTypeConfig
            {
                Id = id, TypeId = typeId, GenContractNo = genContractNo,
                EmailContract = emailContract, SmsContract = smsContract, ZaloContract = zaloContract,
                EmailOtp = emailOtp, SmsOtp = smsOtp, ZaloOtp = zaloOtp,
                Active = active, Remark = remark
            }, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật cấu hình loại hợp đồng." : "Đã thêm cấu hình loại hợp đồng.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(TypeConfigs));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTypeConfig(int id)
    {
        var (ok, msg) = await svc.DeleteTypeConfigAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(TypeConfigs));
    }

    // ── Cấu hình kênh gửi của tổ chức (Mst_Channel) ──────────────────
    // Cấu hình kênh gửi HĐ/OTP/AccessKey + thông số từng kênh (Email/SMS/Zalo) —
    // port từ Mst_Channel (QContract). Mỗi tổ chức có 1 cấu hình kênh.
    public async Task<IActionResult> ChannelConfigs()
    {
        return View(await svc.ChannelConfigAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveChannelConfig(int id, string? networkId,
        ChannelType contractChannel, ChannelType otpChannel, bool active,
        string? mailFrom, string? apisSendMail, string? apiKeySendMail, string? solutionCodeSendMail,
        string? displayNameMailFrom, string? subFormCodeEmailContract, string? subFormCodeEmailOtp,
        string? smsBrandName, string? subFormCodeContractSms, string? subFormCodeSmsOtp,
        string? zaloOaId, string? appId, string? accessToken, string? refreshToken, string? appSecret,
        string? subFormCodeContractZaloUserId, string? subFormCodeOtp)
    {
        try
        {
            var cfg = new ChannelConfig
            {
                Id = id, NetworkID = networkId?.Trim(),
                ContractChannel = contractChannel, OtpChannel = otpChannel, Active = active,
                Email = new ChannelEmailConfig
                {
                    MailFrom = mailFrom?.Trim(), APIsSendMail = apisSendMail?.Trim(),
                    ApiKeySendMail = apiKeySendMail, SolutionCodeSendMail = solutionCodeSendMail?.Trim(),
                    DisplayNameMailFrom = displayNameMailFrom?.Trim(),
                    SubFormCodeEmailContract = subFormCodeEmailContract?.Trim(),
                    SubFormCodeEmailOtp = subFormCodeEmailOtp?.Trim(), Active = active
                },
                Sms = new ChannelSmsConfig
                {
                    SmsBrandName = smsBrandName?.Trim(),
                    SubFormCodeContractSms = subFormCodeContractSms?.Trim(),
                    SubFormCodeSmsOtp = subFormCodeSmsOtp?.Trim(), Active = active
                },
                Zalo = new ChannelZaloConfig
                {
                    ZaloOaId = zaloOaId?.Trim(), AppId = appId?.Trim(),
                    AccessToken = accessToken, RefreshToken = refreshToken, AppSecret = appSecret,
                    SubFormCodeContractZaloUserId = subFormCodeContractZaloUserId?.Trim(),
                    SubFormCodeOtp = subFormCodeOtp?.Trim(), Active = active
                }
            };
            await svc.SaveChannelConfigAsync(cfg, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật cấu hình kênh gửi." : "Đã thêm cấu hình kênh gửi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(ChannelConfigs));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChannelConfig(int id)
    {
        var (ok, msg) = await svc.DeleteChannelConfigAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(ChannelConfigs));
    }

    // ── Mẫu nội dung gửi (Mst_SubmissionForm) ────────────────────────
    // Danh mục mẫu nội dung gửi theo kênh (Email/SMS/Zalo) + loại bản tin (HĐ/OTP) —
    // port từ Mst_SubmissionForm (QContract). Mỗi mẫu gồm nội dung (tiêu đề + thân)
    // và tham số ZNS (khi gửi qua Zalo).
    public async Task<IActionResult> SubmissionForms()
    {
        return View(await svc.SubmissionFormsAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSubmissionForm(int id, string subFormCode, string subFormName,
        ChannelType channelType, BulletinType bulletinType, string? idZns, bool active,
        string? messages, string? znsParams)
    {
        try
        {
            // Nội dung mẫu: mỗi dòng "tiêu đề | nội dung".
            var msgs = new List<SubmissionFormMessage>();
            foreach (var line in (messages ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|', 2);
                var title = parts[0].Trim();
                var body = parts.Length > 1 ? parts[1].Trim() : "";
                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body)) continue;
                msgs.Add(new SubmissionFormMessage { SubTitle = title, Message = body });
            }
            // Tham số ZNS: mỗi dòng "mã tham số ZNS | nguồn dữ liệu | mã tham số hệ thống | giá trị".
            var zns = new List<SubmissionFormZns>();
            foreach (var line in (znsParams ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length < 3) continue;
                zns.Add(new SubmissionFormZns
                {
                    ParamContractCodeZns = parts[0].Trim(),
                    SourceDataType = parts.Length > 1 ? parts[1].Trim() : null,
                    ParamContractCode = parts[2].Trim(),
                    ParamValue = parts.Length > 3 ? parts[3].Trim() : null
                });
            }
            var form = new SubmissionForm
            {
                Id = id, SubFormCode = subFormCode, SubFormName = subFormName,
                ChannelType = channelType, BulletinType = bulletinType,
                IdZns = idZns, Active = active
            };
            await svc.SaveSubmissionFormAsync(form, msgs, zns, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật mẫu gửi." : "Đã thêm mẫu gửi.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(SubmissionForms));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubmissionForm(int id)
    {
        var (ok, msg) = await svc.DeleteSubmissionFormAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(SubmissionForms));
    }

    // ── Chi tiết hợp đồng (Contract_ContractDtl) ─────────────────────
    // Thêm 1 dòng chi tiết (hàng hóa/dịch vụ) — port từ Contract_ContractDtl (QContract).
    // Các giá trị tiền (thành tiền/thuế/chiết khấu) được tính lại ở service theo công thức QContract.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDetail(int id, string specName, string? specCode, string? unitName,
        decimal unitPrice, decimal qty, decimal vatRate, decimal discountRate, string? remark)
    {
        if (string.IsNullOrWhiteSpace(specName))
        {
            TempData["Error"] = "Cần tên hàng hóa/dịch vụ.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        try
        {
            await svc.AddDetailAsync(id, new ContractDetail
            {
                SpecName = specName.Trim(), SpecCode = specCode?.Trim() ?? "", UnitName = unitName?.Trim(),
                UnitPrice = unitPrice, Qty = qty, VATRate = vatRate, DiscountRate = discountRate,
                Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim()
            }, "web");
            TempData["Success"] = "Đã thêm dòng chi tiết.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Xóa 1 dòng chi tiết — port từ Contract_ContractDtl (QContract).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDetail(int id, int detailId)
    {
        var (ok, msg) = await svc.DeleteDetailAsync(id, detailId, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Trường động của hợp đồng (Contract_Attribute_Contract) ───────
    // Danh mục trường động (Mst_Attribute_Contract) — port từ Mst_Attribute_Contract (QContract).
    public async Task<IActionResult> AttributeMasters()
    {
        return View(await svc.AttributeMastersAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttributeMaster(int id, string code, string? name, string? defaultValues, bool active)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Cần mã trường động.";
            return RedirectToAction(nameof(AttributeMasters));
        }
        try
        {
            await svc.SaveAttributeMasterAsync(new AttributeContract
            {
                Id = id, Code = code.Trim(), Name = name?.Trim() ?? "", DefaultValues = defaultValues, Active = active
            }, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật trường động." : "Đã thêm trường động.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AttributeMasters));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttributeMaster(int id)
    {
        var (ok, msg) = await svc.DeleteAttributeMasterAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(AttributeMasters));
    }

    // Lưu trường động cấp hợp đồng — GHI ĐÈ TOÀN BỘ (delete all + insert all).
    // Mỗi dòng: mã trường | giá trị.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttributes(int id, string? attributes)
    {
        var list = new List<ContractAttribute>();
        foreach (var raw in (attributes ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = raw.Split('|', 2);
            var code = parts[0].Trim();
            var value = parts.Length > 1 ? parts[1].Trim() : "";
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(value)) continue;
            list.Add(new ContractAttribute { AttributeContractCode = code, AttributeValue = value });
        }
        var (ok, msg) = await svc.SaveAttributesAsync(id, list, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // Lưu trường động cấp chi tiết — GHI ĐÈ TOÀN BỘ. Mỗi dòng: thứ tự | mã trường | giá trị.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttributeDetails(int id, string? attributes)
    {
        var list = new List<ContractAttributeDtl>();
        foreach (var raw in (attributes ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = raw.Split('|');
            var idx = parts.Length > 0 && int.TryParse(parts[0].Trim(), out var n) ? n : 1;
            var code = parts.Length > 1 ? parts[1].Trim() : "";
            var value = parts.Length > 2 ? parts[2].Trim() : "";
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(value)) continue;
            list.Add(new ContractAttributeDtl { Idx = idx, AttributeContractCode = code, AttributeValue = value });
        }
        var (ok, msg) = await svc.SaveAttributeDetailsAsync(id, list, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Quản lý thông báo (Mst_NotifyType + Map_UserInNotifyType) ────
    // Danh mục loại thông báo + cài đặt bật/tắt theo người dùng — port từ
    // Mst_NotifyType + Map_UserInNotifyType (QContract).
    public async Task<IActionResult> NotifyTypes()
    {
        ViewBag.Mappings = await svc.UserNotifyTypesAsync();
        return View(await svc.NotifyTypesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotifyType(int id, string notifyTypeCode, string? notifyDesc, bool defaultActive, bool active)
    {
        if (string.IsNullOrWhiteSpace(notifyTypeCode))
        {
            TempData["Error"] = "Cần mã loại thông báo (NotifyType).";
            return RedirectToAction(nameof(NotifyTypes));
        }
        try
        {
            await svc.SaveNotifyTypeAsync(new NotifyType
            {
                Id = id, NotifyTypeCode = notifyTypeCode.Trim(), NotifyDesc = notifyDesc,
                DefaultActive = defaultActive, Active = active
            }, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật loại thông báo." : "Đã thêm loại thông báo.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(NotifyTypes));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNotifyType(int id)
    {
        var (ok, msg) = await svc.DeleteNotifyTypeAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(NotifyTypes));
    }

    // Lưu cài đặt bật/tắt thông báo — GHI ĐÈ theo cặp (UserCode, NotifyType).
    // Mỗi dòng: mã người dùng | mã loại thông báo | bật/tắt (1/0).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUserNotifyTypes(string? mappings)
    {
        var list = new List<UserNotifyType>();
        foreach (var raw in (mappings ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = raw.Split('|');
            var user = parts.Length > 0 ? parts[0].Trim() : "";
            var type = parts.Length > 1 ? parts[1].Trim() : "";
            var flag = parts.Length > 2 ? parts[2].Trim() : "1";
            if (string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(type)) continue;
            list.Add(new UserNotifyType
            {
                UserCode = user, NotifyTypeCode = type,
                FlagNotify = flag is "1" or "true" or "on" or "bật" or "bat"
            });
        }
        var (ok, msg) = await svc.SaveUserNotifyTypesAsync(list, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(NotifyTypes));
    }

    // ── Danh mục loại mẫu in (Mst_TempType) ──────────────────────────
    // Danh mục loại mẫu in (print template type) — port từ Mst_TempType (QContract).
    public async Task<IActionResult> TempTypes()
    {
        return View(await svc.TempTypesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTempType(int id, string code, string name, string? description,
        string size, string imageFilePath, string? remark, bool active)
    {
        try
        {
            await svc.SaveTempTypeAsync(new TempType
            {
                Id = id, Code = code?.Trim() ?? "", Name = name?.Trim() ?? "", Description = description,
                Size = size?.Trim() ?? "", ImageFilePath = imageFilePath?.Trim() ?? "", Remark = remark, Active = active
            }, "web");
            TempData["Success"] = id > 0 ? "Đã cập nhật loại mẫu in." : "Đã thêm loại mẫu in.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(TempTypes));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTempType(int id)
    {
        var (ok, msg) = await svc.DeleteTempTypeAsync(id, "web");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(TempTypes));
    }
}
