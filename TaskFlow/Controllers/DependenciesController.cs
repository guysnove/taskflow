using Microsoft.AspNetCore.Mvc;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Course 2, Chapter 4: the "Blocked by" forms on a task's page.
//   POST /tasks/3/dependencies                 task 3 now waits for the task chosen in the form
//   POST /tasks/3/dependencies/5/delete        task 3 no longer waits for task 5
// After each one, back to the task's page, at the dependencies section.
[Route("tasks/{taskId:int:min(1)}/dependencies")]
public class DependenciesController : Controller
{
    private readonly IDependencyRepository _dependencies;
    private readonly ILogger<DependenciesController> _logger;

    public DependenciesController(IDependencyRepository dependencies, ILogger<DependenciesController> logger)
    {
        _dependencies = dependencies;
        _logger = logger;
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromRoute] int taskId, [FromForm] int blockedById)
    {
        var result = await _dependencies.AddAsync(taskId, blockedById);
        if (result.NotFound) return NotFound();
        if (!result.Ok)
        {
            _logger.LogInformation("Dependency {TaskId} <- {BlockedById} refused: {Reason}", taskId, blockedById, result.Error);
            TempData["DependencyError"] = result.Error;
        }
        return Back(taskId);
    }

    [HttpPost("{blockedById:int:min(1)}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove([FromRoute] int taskId, [FromRoute] int blockedById)
    {
        return await _dependencies.RemoveAsync(taskId, blockedById) ? Back(taskId) : NotFound();
    }

    private IActionResult Back(int taskId) => RedirectToRoute("TaskDetails", new { id = taskId }, "dependencies");
}
