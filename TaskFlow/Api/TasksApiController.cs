using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Api;

// TaskFlow's tasks, as JSON, for programs rather than people (Chapter 26).
// [ApiController]: invalid input is answered with 400 automatically, and [FromBody] is assumed.
[ApiController]
[Route("api/tasks")]
[SkipStatusCodePages]          // errors stay JSON: no HTML "Something went wrong" page
[AutoValidateAntiforgeryToken] // POST, PUT and DELETE need the X-CSRF-TOKEN header
[Produces("application/json")] // every answer is JSON
[EnableRateLimiting("api")]    // Chapter 27: 20 requests per 10 seconds per user
public class TasksApiController : ControllerBase
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;

    public TasksApiController(ITaskRepository tasks, IProjectRepository projects)
    {
        _tasks = tasks;
        _projects = projects;
    }

    // GET /api/tasks  the signed-in user's tasks
    [HttpGet]
    public async Task<IEnumerable<TaskDto>> GetAll()
    {
        var tasks = await _tasks.GetAllAsync();
        return tasks.Select(TaskDto.From);
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
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]   // tells /openapi/v1.json what really happens
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(TaskInput input)
    {
        if (!await ProjectIsMineAsync(input))
        {
            ModelState.AddModelError(nameof(input.ProjectId), "There is no such project.");
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
        if (!await ProjectIsMineAsync(input))
        {
            ModelState.AddModelError(nameof(input.ProjectId), "There is no such project.");
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
        return await _tasks.MarkDoneAsync(id) ? NoContent() : NotFound();
    }

    // DELETE /api/tasks/3
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        return await _tasks.DeleteAsync(id) ? NoContent() : NotFound();
    }

    // A project id in the body must be one of this user's projects.
    private async Task<bool> ProjectIsMineAsync(TaskInput input) =>
        input.ProjectId is not int projectId || await _projects.FindAsync(projectId) is not null;
}
