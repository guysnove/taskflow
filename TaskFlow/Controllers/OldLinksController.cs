using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers;

// Chapter 17's addresses, kept working for old bookmarks and links.
// Each one sends a permanent redirect (301) to the new address.
public class OldLinksController : Controller
{
    [HttpGet("Tasks/Details/{id:int}")]
    public IActionResult Details(int id)
    {
        return RedirectToRoutePermanent("TaskDetails", new { id });
    }

    [HttpGet("Tasks/Create")]
    public IActionResult Create()
    {
        return RedirectToActionPermanent("Create", "Tasks");
    }
}
