using TaskFlow.Models;

namespace TaskFlow.Api;

// What the API sends for a task (Chapter 26). Chosen on purpose: no UserId, no navigation properties.
public record TaskDto(
    int Id,
    string Title,
    string Priority,
    DateOnly? Due,
    bool Done,
    string? Notes,
    int? ProjectId,
    string? ProjectName,
    DateOnly? CreatedOn,
    int Version)
{
    public static TaskDto From(TaskItem task) => new(
        task.Id, task.Title, task.Priority, task.Due, task.Done, task.Notes,
        task.ProjectId, task.Project?.Name, task.CreatedOn, task.Version);
}
