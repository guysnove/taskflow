using Microsoft.AspNetCore.Mvc;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Chapter 24: the four CRUD operations for projects — Create, Read, Update, Delete.
[Route("projects")]
public class ProjectsController : Controller
{
    private readonly IProjectRepository _projects;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectRepository projects, ILogger<ProjectsController> logger)
    {
        _projects = projects;
        _logger = logger;
    }

    // READ: GET /projects  the list, with task counts
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        return View(await _projects.GetSummariesAsync());
    }

    // READ: GET /projects/2  one project and its tasks
    [HttpGet("{id:int:min(1)}", Name = "ProjectDetails")]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projects.FindWithTasksAsync(id);
        if (project is null)
        {
            return NotFound();
        }
        return View(project);
    }

    // CREATE: GET /projects/new  the empty form
    [HttpGet("new")]
    public IActionResult Create()
    {
        return View(new ProjectInput());
    }

    // CREATE: POST /projects/new
    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }
        var project = await _projects.AddAsync(input);
        if (project is null)
        {
            ModelState.AddModelError(nameof(input.Name), NameTakenMessage(input.Name!));
            return View(input);
        }
        _logger.LogInformation("Project {ProjectId} created", project.Id);   // Course 2, Ch. 20 (F2)
        TempData["Message"] = $"Added the project \"{project.Name}\".";
        return RedirectToRoute("ProjectDetails", new { id = project.Id });
    }

    // UPDATE: GET /projects/2/edit
    [HttpGet("{id:int:min(1)}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projects.FindAsync(id);
        if (project is null)
        {
            return NotFound();
        }
        ViewData["Id"] = id;
        return View(ProjectInput.From(project));
    }

    // UPDATE: POST /projects/2/edit
    [HttpPost("{id:int:min(1)}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, ProjectInput input)
    {
        ViewData["Id"] = id;
        if (!ModelState.IsValid)
        {
            return View(input);
        }
        var result = await _projects.UpdateAsync(id, input);
        if (result == SaveResult.NotFound)
        {
            return NotFound();
        }
        if (result == SaveResult.NameTaken)
        {
            ModelState.AddModelError(nameof(input.Name), NameTakenMessage(input.Name!));
            return View(input);
        }
        TempData["Message"] = $"Saved \"{input.Name!.Trim()}\".";
        return RedirectToRoute("ProjectDetails", new { id });
    }

    // DELETE: GET /projects/2/delete  "are you sure?"
    [HttpGet("{id:int:min(1)}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _projects.FindWithTasksAsync(id);
        if (project is null)
        {
            return NotFound();
        }
        return View(project);
    }

    // DELETE: POST /projects/2/delete
    [HttpPost("{id:int:min(1)}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var project = await _projects.FindAsync(id);
        var result = await _projects.DeleteAsync(id);
        if (project is null || result == DeleteResult.NotFound)
        {
            return NotFound();
        }
        if (result == DeleteResult.HasTasks)
        {
            TempData["Error"] = $"\"{project.Name}\" still has tasks. "
                + "Move them to another project, or delete them, first.";
            return RedirectToAction(nameof(Delete), new { id });
        }
        _logger.LogInformation("Project {ProjectId} deleted", id);
        TempData["Message"] = $"Deleted the project \"{project.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    private static string NameTakenMessage(string name) =>
        $"There is already a project called \"{name.Trim()}\". Please choose another name.";
}
