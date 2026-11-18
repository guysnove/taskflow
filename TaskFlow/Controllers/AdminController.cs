using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;

namespace TaskFlow.Controllers;

public record UserSummary(int Id, string Email, string DisplayName, DateOnly CreatedOn,
                          bool IsAdmin, int Tasks);

// Chapter 25: a page only administrators may open.
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly TaskFlowDbContext _db;

    public AdminController(TaskFlowDbContext db)
    {
        _db = db;
    }

    // GET /admin/users  every account, with its number of tasks
    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .OrderBy(u => u.Id)
            .Select(u => new UserSummary(u.Id, u.Email, u.DisplayName, u.CreatedOn,
                                         u.IsAdmin, u.Tasks.Count()))
            .ToListAsync();
        return View(users);
    }
}
