using Microsoft.AspNetCore.Mvc;
using MiniContract.Services;

namespace MiniContract.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => Redirect("/index.html");   // SPA React ở "/"
}

public class LegacyController(IContractService svc) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Dash = await svc.DashboardAsync();
        ViewBag.Recent = await svc.ListAsync(null, null);
        return View("~/Views/Home/Index.cshtml");
    }
}
