using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Attribute routing: every address of this controller starts with /tasks.
[Route("tasks")]
public class TasksController : Controller
{
    private readonly ITaskRepository _repository;
    private readonly IProjectRepository _projects;
    private readonly TimeProvider _time;
    private readonly ILogger<TasksController> _logger;

    // Dependency injection passes in all four: see Program.cs.
    public TasksController(ITaskRepository repository, IProjectRepository projects,
                           TimeProvider time, ILogger<TasksController> logger)
    {
        _repository = repository;
        _projects = projects;
        _time = time;
        _logger = logger;
    }

    // GET /tasks            all tasks
    // GET /tasks?show=open  only unfinished tasks
    // GET /tasks?sort=priority
    // GET /tasks?project=2  only one project's tasks (Chapter 24)
    // GET /tasks?q=login   only tasks whose title has all these words (Course 2, Chapter 3)
    [HttpGet("")]
    public async Task<IActionResult> Index(string? show, string? sort, int? project, string? q)
    {
        var tasks = await _repository.GetAllAsync();
        tasks = TaskSearch.Filter(tasks, q);

        if (project is not null)
        {
            tasks = tasks.Where(t => t.ProjectId == project).ToList();
        }

        if (show == "open")
        {
            tasks = tasks.Where(t => !t.Done).ToList();
        }

        if (sort == "priority")
        {
            tasks = tasks.OrderBy(t => t.Done)
                         .ThenBy(t => PriorityRank(t.Priority))
                         .ToList();
        }
        else if (sort == "title")
        {
            tasks = tasks.OrderBy(t => t.Done).ThenBy(t => t.Title).ToList();
        }
        // anything else: the repository's order (by due date)

        ViewData["Show"] = show == "open" ? "open" : "all";
        ViewData["Sort"] = sort is "priority" or "title" ? sort : "due";
        ViewData["Project"] = project;
        ViewData["Q"] = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        return View(tasks);
    }

    // GET /tasks/3
    [HttpGet("{id:int:min(1)}", Name = "TaskDetails")]
    public async Task<IActionResult> Details(int id)
    {
        var task = await _repository.FindAsync(id);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} was requested but doesn't exist", id);
            return NotFound();
        }
        return View(task);
    }

    // GET /tasks/new  shows the empty form
    // GET /tasks/new?project=2  the project is chosen already
    // GET /tasks/quick  a page that works through the JSON API, with JavaScript (Chapter 26)
    [HttpGet("quick")]
    public IActionResult Quick() => View();

    [HttpGet("new")]
    public async Task<IActionResult> Create(int? project)
    {
        await LoadProjectChoicesAsync();
        return View(new TaskInput { ProjectId = project });
    }

    // POST /tasks/new  model binding fills a TaskInput from the form
    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskInput input)
    {
        // A rule for new tasks only: no due dates in the past.
        var today = _time.Today();
        if (input.Due is not null && input.Due < today)
        {
            ModelState.AddModelError(nameof(input.Due), "A new task can't be due in the past.");
        }
        await CheckProjectExistsAsync(input);

        // Course 2, Chapter 2: warn about a probable duplicate, unless the user confirmed it.
        if (ModelState.IsValid && !input.AllowDuplicate)
        {
            var existing = TitleMatcher.FindOpenDuplicate(input.Title, await _repository.GetAllAsync());
            if (existing is not null)
            {
                ModelState.AddModelError(nameof(input.Title),
                    $"You already have an open task called \"{existing.Title}\". Tick \"Add it anyway\" to add another.");
                ViewData["PossibleDuplicate"] = true;
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadProjectChoicesAsync();
            return View(input);   // the same form, with the values and the messages
        }
        var task = await _repository.AddAsync(input);
        _logger.LogInformation("Task {TaskId} created: {Title}", task.Id, task.Title);
        TempData["Message"] = $"Added \"{task.Title}\".";
        return RedirectToAction(nameof(Index));
    }

    // GET /tasks/3/edit
    [HttpGet("{id:int:min(1)}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var task = await _repository.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }
        ViewData["Id"] = id;
        await LoadProjectChoicesAsync();
        return View(TaskInput.From(task));
    }

    // POST /tasks/3/edit  [FromRoute]: the id comes from the address, never from the form
    [HttpPost("{id:int:min(1)}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, TaskInput input)
    {
        await CheckProjectExistsAsync(input);
        if (!ModelState.IsValid)
        {
            ViewData["Id"] = id;
            await LoadProjectChoicesAsync();
            return View(input);
        }

        var result = await _repository.UpdateAsync(id, input);
        if (result == SaveResult.NotFound)
        {
            return NotFound();
        }
        if (result == SaveResult.Conflict)
        {
            // Someone saved this task after the form was shown. Keep the visitor's values,
            // show what is saved now, and let them decide.
            var current = await _repository.FindAsync(id);
            if (current is null)
            {
                return NotFound();
            }
            _logger.LogWarning("Edit conflict on task {TaskId}", id);
            ModelState.AddModelError("", "Someone else saved this task while you were editing it. "
                + "Your changes were not saved. Check the saved values below, then save again "
                + "to replace them with yours.");
            ModelState.Remove(nameof(input.Version));    // so the form shows the new version
            input.Version = current.Version;
            ViewData["Current"] = current;
            ViewData["Id"] = id;
            await LoadProjectChoicesAsync();
            return View(input);
        }
        TempData["Message"] = $"Saved \"{input.Title!.Trim()}\".";
        return RedirectToRoute("TaskDetails", new { id });
    }

    // POST /tasks/3/done  marks a task as done
    [HttpPost("{id:int:min(1)}/done")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Done([FromRoute] int id)
    {
        if (!await _repository.MarkDoneAsync(id))
        {
            return NotFound();
        }
        _logger.LogInformation("Task {TaskId} marked done", id);
        return RedirectToAction(nameof(Index));
    }

    // GET /tasks/3/delete  asks "are you sure?" (deleting never happens on a GET)
    [HttpGet("{id:int:min(1)}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _repository.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }
        return View(task);
    }

    // POST /tasks/3/delete  really deletes
    [HttpPost("{id:int:min(1)}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var task = await _repository.FindAsync(id);
        if (task is null || !await _repository.DeleteAsync(id))
        {
            return NotFound();
        }
        _logger.LogInformation("Task {TaskId} deleted: {Title}", id, task.Title);
        TempData["Message"] = $"Deleted \"{task.Title}\".";
        return RedirectToAction(nameof(Index));
    }

    // The project drop-down's choices, for the add and edit forms.
    private async Task LoadProjectChoicesAsync()
    {
        var projects = await _projects.GetChoicesAsync();
        ViewData["Projects"] = new SelectList(projects, nameof(Project.Id), nameof(Project.Name));
    }

    // A rule that needs data: the chosen project must exist.
    private async Task CheckProjectExistsAsync(TaskInput input)
    {
        if (input.ProjectId is int projectId && await _projects.FindAsync(projectId) is null)
        {
            ModelState.AddModelError(nameof(input.ProjectId), "Please choose a project from the list.");
        }
    }

    // High first, then Medium, then Low.
    private static int PriorityRank(string priority) => priority switch
    {
        "High" => 0,
        "Medium" => 1,
        _ => 2,
    };
}
