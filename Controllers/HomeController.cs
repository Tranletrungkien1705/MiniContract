using Microsoft.AspNetCore.Mvc;
using MiniContract.Services;

namespace MiniContract.Controllers;

public class HomeController(IContractService svc) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Dash = await svc.DashboardAsync();
        ViewBag.Recent = await svc.ListAsync(null, null);
        return View();
    }
}
