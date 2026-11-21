using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Api;

// Chapter 30: a task's checklist, as JSON. The same rules as TasksApiController (Chapter 26).
//   GET    /api/tasks/3/steps             the steps, in order
//   GET    /api/tasks/3/steps/7           one step
//   POST   /api/tasks/3/steps             add one: { "text": "Buy boxes" }  → 201 Created
//   POST   /api/tasks/3/steps/7/toggle    tick or untick                    → 200 with the step
//   DELETE /api/tasks/3/steps/7                                             → 204 No Content
[ApiController]
[Route("api/tasks/{taskId:int}/steps")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting("api")]
public class StepsApiController : ControllerBase
{
    private readonly IStepRepository _steps;

    public StepsApiController(IStepRepository steps)
    {
        _steps = steps;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<StepDto>>> GetAll(int taskId)
    {
        var steps = await _steps.GetForTaskAsync(taskId);
        if (steps is null)
        {
            return NotFound();
        }
        return Ok(steps.Select(StepDto.From));
    }

    [HttpGet("{stepId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StepDto>> GetById(int taskId, int stepId)
    {
        var step = await _steps.FindAsync(taskId, stepId);
        return step is null ? NotFound() : StepDto.From(step);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StepDto>> Add(int taskId, StepInput input)
    {
        var step = await _steps.AddAsync(taskId, input.Text!);
        if (step is null)
        {
            return NotFound();
        }
        return CreatedAtAction(nameof(GetById), new { taskId, stepId = step.Id }, StepDto.From(step));
    }

    [HttpPost("{stepId:int}/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StepDto>> Toggle(int taskId, int stepId)
    {
        var step = await _steps.ToggleAsync(taskId, stepId);
        return step is null ? NotFound() : StepDto.From(step);
    }

    [HttpDelete("{stepId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int taskId, int stepId)
    {
        return await _steps.DeleteAsync(taskId, stepId) ? NoContent() : NotFound();
    }
}
