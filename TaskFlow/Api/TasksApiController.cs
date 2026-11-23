using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Common;
using TaskFlow.Models;
using TaskFlow.Notifications;
using TaskFlow.Services;
using TaskFlow.UseCases;

namespace TaskFlow.Api;

// TaskFlow's tasks, as JSON, for programs rather than people (Chapter 26).
// [ApiController]: invalid input is answered with 400 automatically, and [FromBody] is assumed.
[ApiController]
[Route("api/tasks")]
[AutoValidateAntiforgeryToken] // POST, PUT and DELETE need the X-CSRF-TOKEN header
[EnableRateLimiting("api")]    // Chapter 27: 20 requests per 10 seconds per user
public class TasksApiController : ControllerBase
{
    private readonly ITaskRepository _tasks;
    private readonly ProjectExistsRule _projectRule;        // Course 2, Chapter 15 (was a private copy: TD-1)
    private readonly NewTaskChecker _newTaskRules;          // Course 2, Chapter 8
    private readonly CompleteTask _completeTask;            // Course 2, Chapter 12: a use case

    public TasksApiController(ITaskRepository tasks, ProjectExistsRule projectRule, NewTaskChecker newTaskRules,
                              CompleteTask completeTask)
    {
        _completeTask = completeTask;
        _tasks = tasks;
        _projectRule = projectRule;
        _newTaskRules = newTaskRules;
    }

    /// <summary>Lists the signed-in user's tasks.</summary>
    /// <remarks>
    /// Optional filters and paging, e.g. <c>?done=false</c>, <c>?page=2</c>, <c>?pageSize=20</c>; without them, every task.
    /// The total is in the <c>X-Total-Count</c> header; a <c>Link</c> header points to the next page, if there is one.
    /// (Course 2, Chapter 15; XML documentation added in Chapter 23.)
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IEnumerable<TaskDto>> GetAll([FromQuery] TaskListRequest request)
    {
        bool paging = request.Page is not null || request.PageSize is not null;
        int page = request.Page ?? 1;
        int pageSize = request.PageSize ?? (paging ? TaskListRequest.DefaultPageSize : int.MaxValue);

        var result = await _tasks.GetPageAsync(new TaskFilter(request.Done, request.ProjectId), page, pageSize);
        Response.Headers["X-Total-Count"] = result.Total.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (paging && (long)page * pageSize < result.Total)
        {
            string? next = Url.Action(nameof(GetAll), new { request.Done, request.ProjectId, page = page + 1, pageSize });
            Response.Headers.Link = $"<{next}>; rel=\"next\"";
        }
        return result.Items.Select(TaskDto.From);
    }

    /// <summary>Gets one of the signed-in user's tasks.</summary>
    /// <param name="id">The task's id.</param>
    /// <response code="404">No such task, or it isn't yours (TaskFlow never says which).</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(int id)
    {
        var task = await _tasks.FindAsync(id);
        if (task is null)
        {
            return NotFound();
        }
        return TaskDto.From(task);
    }

    /// <summary>Creates a task.</summary>
    /// <remarks>
    /// Answers 201 Created, with the new task and its address in <c>Location</c>.
    /// Send an <c>Idempotency-Key</c> header, and repeating the same request is safe (Course 2, Chapter 15).
    /// </remarks>
    /// <response code="400">Invalid input; <c>errors</c> lists the reasons per field.</response>
    /// <response code="422">The <c>Idempotency-Key</c> was already used for a different request.</response>
    [HttpPost]
    [ServiceFilter<IdempotencyFilter>]
    [ProducesResponseType(StatusCodes.Status201Created)]   // tells /openapi/v1.json what really happens
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskDto>> Create(TaskInput input, [FromServices] TaskFlowMetrics metrics)
    {
        // Course 2, Chapter 8: the same rules as the form. Warnings (a probable duplicate)
        // are for people at a form; a program gets only the errors.
        var errors = (await _newTaskRules.CheckAsync(input, alreadyInvalid: false)).Where(p => !p.Warning).ToList();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }
            return ValidationProblem(ModelState);
        }
        var task = await _tasks.AddAsync(input);
        metrics.TaskCreated("api");                           // Course 2, Chapter 29
        var saved = await _tasks.FindAsync(task.Id);          // with its project's name
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, TaskDto.From(saved!));
    }

    /// <summary>Replaces a task's editable fields.</summary>
    /// <remarks>The body must include the <c>version</c> it was based on (optimistic concurrency).</remarks>
    /// <param name="id">The task's id.</param>
    /// <param name="input">The new title, priority, due date, notes and project, and the <c>version</c>.</param>
    /// <response code="409">Someone changed the task since that version: fetch it again, and retry.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskDto>> Update(int id, TaskInput input)
    {
        if (await _projectRule.CheckAsync(input) is RuleProblem problem)    // Course 2, Chapter 15: one copy of the rule (TD-1)
        {
            ModelState.AddModelError(problem.Field, problem.Message);
            return ValidationProblem(ModelState);
        }
        var result = await _tasks.UpdateAsync(id, input);
        if (result == SaveResult.NotFound)
        {
            return NotFound();
        }
        if (result == SaveResult.Conflict)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                           title: "The task was changed by someone else.",
                           detail: "Fetch it again, apply your change, and send it with the new Version.");
        }
        var saved = await _tasks.FindAsync(id);
        return TaskDto.From(saved!);
    }

    /// <summary>Marks a task as done.</summary>
    /// <remarks>Idempotent: marking a done task done again changes nothing. Tasks that were waiting only for this one
    /// produce a "Ready to start" notification for their owner.</remarks>
    /// <param name="id">The task's id.</param>
    [HttpPost("{id:int}/done")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkDone(int id)
    {
        if (!await _completeTask.RunAsync(id))               // Course 2, Chapter 12: the same use case as the form
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>Deletes a task and its steps.</summary>
    /// <param name="id">The task's id.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        return await _tasks.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
