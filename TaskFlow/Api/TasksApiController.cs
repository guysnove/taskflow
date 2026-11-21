using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    // GET /api/tasks  the signed-in user's tasks
    // Course 2, Chapter 15: optional filters and paging, e.g. /api/tasks?done=false&page=2&pageSize=20
    // Without them: every task, as before — programs written for the old API keep working.
    // The total is in X-Total-Count; a Link header points to the next page, if there is one.
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

    // GET /api/tasks/3
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

    // POST /api/tasks  with a JSON body; answers 201 Created, and where to find the new task
    // Course 2, Chapter 15: send an Idempotency-Key header, and repeating the request is safe.
    [HttpPost]
    [ServiceFilter<IdempotencyFilter>]
    [ProducesResponseType(StatusCodes.Status201Created)]   // tells /openapi/v1.json what really happens
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskDto>> Create(TaskInput input)
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
        var saved = await _tasks.FindAsync(task.Id);          // with its project's name
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, TaskDto.From(saved!));
    }

    // PUT /api/tasks/3  replaces the editable fields; the body must include the Version it was based on
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

    // POST /api/tasks/3/done
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

    // DELETE /api/tasks/3
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        return await _tasks.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
