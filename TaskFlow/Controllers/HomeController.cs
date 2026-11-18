using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Models;

namespace TaskFlow.Controllers;

// The public pages: everyone may see them, signed in or not (Chapter 25).
[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // GET /Home/About
    public IActionResult About()
    {
        return View();
    }

    // Shown for error status codes, like 404, by UseStatusCodePagesWithReExecute.
    // GET /status/404
    [Route("status/{code:int}")]
    public IActionResult Status(int code)
    {
        ViewData["Code"] = code;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
