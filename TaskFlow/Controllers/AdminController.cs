using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Chapter 25: a page only administrators may open.
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly AccountService _accounts;               // Course 2, Chapter 11: not the DbContext

    public AdminController(AccountService accounts)
    {
        _accounts = accounts;
    }

    // GET /admin/users  every account, with its number of tasks
    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        return View(await _accounts.SummariesAsync());
    }
}
