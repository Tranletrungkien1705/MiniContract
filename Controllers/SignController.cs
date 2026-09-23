using Microsoft.AspNetCore.Mvc;
using MiniContract.Models;
using MiniContract.Services;

namespace MiniContract.Controllers;

/// <summary>
/// Ký công khai qua link (không cần đăng nhập) — port từ luồng Contract_ContractSignLink (QContract):
/// bên nhận link mở URL chứa token để xem hợp đồng và ký. Link có thời hạn + có thể thu hồi.
/// </summary>
public class SignController(IContractService svc) : Controller
{
    // Mở link ký: kiểm tra token còn hiệu lực rồi hiển thị trang ký.
    [HttpGet]
    public async Task<IActionResult> PublicSign(string token)
    {
        var link = await svc.ResolveSignLinkAsync(token);
        if (link == null) return View("Invalid", "Link ký không tồn tại.");
        if (link.Revoked) return View("Invalid", "Link ký đã bị thu hồi.");
        if (DateTime.Now > link.EndDate) return View("Invalid", "Link ký đã hết hạn.");
        if (link.UsedAt != null) return View("Invalid", "Link ký đã được sử dụng.");
        return View(link);
    }

    // Xác nhận ký qua link.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicSign(string token, string? signerName)
    {
        var (ok, msg) = await svc.SignViaLinkAsync(token, signerName);
        if (!ok)
        {
            var link = await svc.ResolveSignLinkAsync(token);
            if (link == null) return View("Invalid", msg);
            ViewBag.Error = msg;
            return View(link);
        }
        ViewBag.Success = msg;
        return View("Done", msg);
    }
}
