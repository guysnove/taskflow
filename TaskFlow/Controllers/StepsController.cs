using Microsoft.AspNetCore.Mvc;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Chapter 30: the checklist forms on a task's page. Every address starts with the task:
//   POST /tasks/3/steps              add a step
//   POST /tasks/3/steps/7/toggle     tick or untick step 7
//   POST /tasks/3/steps/7/delete     remove step 7
// After each one, back to the task's page (Post/Redirect/Get, Chapter 17), at the checklist.
[Route("tasks/{taskId:int:min(1)}/steps")]
public class StepsController : Controller
{
    private readonly IStepRepository _steps;
    private readonly ITaskRepository _tasks;
    private readonly ILogger<StepsController> _logger;

    public StepsController(IStepRepository steps, ITaskRepository tasks, ILogger<StepsController> logger)
    {
        _steps = steps;
        _tasks = tasks;
        _logger = logger;
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromRoute] int taskId, StepInput input)
    {
        if (!ModelState.IsValid)
        {
            // Show the task's page again, with the typed text and the message (Chapter 19).
            var task = await _tasks.FindAsync(taskId);
            if (task is null)
            {
                return NotFound();
            }
            ViewData["StepInput"] = input;
            return View("~/Views/Tasks/Details.cshtml", task);
        }

        var step = await _steps.AddAsync(taskId, input.Text!);
        if (step is null)
        {
            return NotFound();                 // no such task, or not yours: the same answer
        }
        _logger.LogInformation("Step {StepId} added to task {TaskId}", step.Id, taskId);
        return BackToChecklist(taskId);
    }

    [HttpPost("{stepId:int:min(1)}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromRoute] int taskId, [FromRoute] int stepId)
    {
        return await _steps.ToggleAsync(taskId, stepId) is null ? NotFound() : BackToChecklist(taskId);
    }

    [HttpPost("{stepId:int:min(1)}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromRoute] int taskId, [FromRoute] int stepId)
    {
        return await _steps.DeleteAsync(taskId, stepId) ? BackToChecklist(taskId) : NotFound();
    }

    // 302 to /tasks/3#steps: the browser scrolls to the checklist.
    private RedirectToRouteResult BackToChecklist(int taskId) =>
        RedirectToRoute("TaskDetails", new { id = taskId }, "steps");
}
