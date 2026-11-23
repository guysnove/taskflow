using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskFlow.Common;
using TaskFlow.Models;
using TaskFlow.Notifications;
using TaskFlow.Services;
using TaskFlow.UseCases;

namespace TaskFlow.Controllers;

// Attribute routing: every address of this controller starts with /tasks.
[Route("tasks")]
public class TasksController : Controller
{
    private readonly ITaskRepository _repository;
    private readonly IProjectRepository _projects;
    private readonly ILogger<TasksController> _logger;
    private readonly IDependencyRepository _dependencies;   // Course 2, Chapter 4
    private readonly NewTaskChecker _newTaskRules;          // Course 2, Chapter 8
    private readonly ProjectExistsRule _projectRule;        // Course 2, Chapter 9
    private readonly CompleteTask _completeTask;            // Course 2, Chapter 12: a use case
    private readonly TimeProvider _time;                    // Course 2, Chapter 19: "today", for priorities

    // Dependency injection passes them all in: see Program.cs.
    public TasksController(ITaskRepository repository, IProjectRepository projects,
                           ILogger<TasksController> logger,
                           IDependencyRepository dependencies, NewTaskChecker newTaskRules,
                           ProjectExistsRule projectRule, CompleteTask completeTask, TimeProvider time)
    {
        _time = time;
        _completeTask = completeTask;
        _projectRule = projectRule;
        _dependencies = dependencies;
        _newTaskRules = newTaskRules;
        _repository = repository;
        _projects = projects;
        _logger = logger;
    }

    // GET /tasks            all tasks
    // GET /tasks?show=open  only unfinished tasks
    // GET /tasks?sort=priority
    // GET /tasks?project=2  only one project's tasks (Chapter 24)
    // GET /tasks?q=login   only tasks whose title has all these words (Course 2, Chapter 3)
    [HttpGet("")]
    public async Task<IActionResult> Index(string? show, string? sort, int? project, string? q, int page = 1)
    {
        // Course 2, Chapter 9: the filtering and sorting moved to TaskListQuery (Extract Class).
        var query = TaskListQuery.From(show, sort, project, q);
        var tasks = query.Apply(await _repository.GetAllAsync(), _time.Today());   // Course 2, Chapter 19: effective priority

        // Course 2, Chapter 26: show one page of 50, not every task. Measured with 2,000 tasks: rendering them all
        // (2.1 MB of HTML) cost about half of the page's time, for rows nobody scrolls to.
        int pages = Math.Max(1, (tasks.Count + PageSize - 1) / PageSize);
        page = Math.Clamp(page, 1, pages);
        var shown = tasks.Skip((page - 1) * PageSize).Take(PageSize).ToList();
        ViewData["StepCounts"] = await _repository.CountStepsAsync(shown.Select(t => t.Id).ToList());
        ViewData["Total"] = tasks.Count;
        ViewData["Left"] = tasks.Count(t => !t.Done);
        ViewData["Page"] = page;
        ViewData["Pages"] = pages;
        ViewData["Today"] = _time.Today();

        ViewData["Show"] = query.Show;
        ViewData["Sort"] = query.Sort;
        ViewData["Project"] = query.Project;
        ViewData["Q"] = query.Q;
        return View(shown);
    }

    public const int PageSize = 50;

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
        // Course 2, Chapter 4: what it waits for, and the tasks it could wait for.
        var blockers = await _dependencies.BlockersOfAsync(id);
        ViewData["Blockers"] = blockers;
        ViewData["DependencyChoices"] = await DependencyChoicesAsync(id, blockers);
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
    public async Task<IActionResult> Create(TaskInput input, [FromServices] TaskFlowMetrics metrics)
    {
        // Course 2, Chapter 8: the rules live in their own classes (Services/NewTaskRules.cs).
        // The controller only turns their answers into messages on the form.
        foreach (var problem in await _newTaskRules.CheckAsync(input, alreadyInvalid: !ModelState.IsValid))
        {
            ModelState.AddModelError(problem.Field, problem.Message);
            if (problem.Warning)
            {
                ViewData["PossibleDuplicate"] = true;
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadProjectChoicesAsync();
            return View(input);   // the same form, with the values and the messages
        }
        var task = await _repository.AddAsync(input);
        _logger.LogInformation("Task {TaskId} created", task.Id);   // Course 2, Ch. 20 (F2): ids, never the user's text
        metrics.TaskCreated("web");                                  // Course 2, Chapter 29: a counter, for dashboards
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
        return await EditFormAsync(id, TaskInput.From(task));
    }

    // POST /tasks/3/edit  [FromRoute]: the id comes from the address, never from the form
    [HttpPost("{id:int:min(1)}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, TaskInput input)
    {
        if (await _projectRule.CheckAsync(input) is RuleProblem problem)     // Chapter 9: one copy of the rule
        {
            ModelState.AddModelError(problem.Field, problem.Message);
        }
        if (!ModelState.IsValid)
        {
            return await EditFormAsync(id, input);
        }

        return await _repository.UpdateAsync(id, input) switch
        {
            SaveResult.NotFound => NotFound(),
            SaveResult.Conflict => await ShowConflictAsync(id, input),
            _ => SavedAndBackToTheTask(id, input),
        };
    }

    // Someone saved this task after the form was shown. Keep the visitor's values,
    // show what is saved now, and let them decide.
    private async Task<IActionResult> ShowConflictAsync(int id, TaskInput input)
    {
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
        return await EditFormAsync(id, input);
    }

    private RedirectToRouteResult SavedAndBackToTheTask(int id, TaskInput input)
    {
        TempData["Message"] = $"Saved \"{input.Title!.Trim()}\".";
        return RedirectToRoute("TaskDetails", new { id });
    }

    // The edit form, with these values (and any messages): used four times.
    private async Task<ViewResult> EditFormAsync(int id, TaskInput input)
    {
        ViewData["Id"] = id;
        await LoadProjectChoicesAsync();
        return View(input);
    }

    // POST /tasks/3/done  marks a task as done
    [HttpPost("{id:int:min(1)}/done")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Done([FromRoute] int id)
    {
        if (!await _completeTask.RunAsync(id))               // Course 2, Chapter 12: the use case
        {
            return NotFound();
        }
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
        _logger.LogInformation("Task {TaskId} deleted", id);
        TempData["Message"] = $"Deleted \"{task.Title}\".";
        return RedirectToAction(nameof(Index));
    }

    // The project drop-down's choices, for the add and edit forms.
    private async Task LoadProjectChoicesAsync()
    {
        var projects = await _projects.GetChoicesAsync();
        ViewData["Projects"] = new SelectList(projects, nameof(Project.Id), nameof(Project.Name));
    }

    // The tasks this task could wait for: not itself, and not one it already waits for.
    // Course 2, Chapter 26: ids and titles only (it loaded every task, with its project, for this list).
    private async Task<List<TaskChoice>> DependencyChoicesAsync(int id, List<TaskItem> blockers) =>
        (await _repository.GetChoicesAsync())
            .Where(t => t.Id != id && blockers.All(b => b.Id != t.Id))
            .OrderBy(t => t.Title).ToList();
}
