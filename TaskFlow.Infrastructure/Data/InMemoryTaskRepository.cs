using TaskFlow.Common;
using TaskFlow.Models;

using TaskFlow.Services;

namespace TaskFlow.Data;

// Keeps TaskFlow's tasks in the server's memory. They are lost when the app stops.
// No longer used by the app since Chapter 23 (see EfTaskRepository), but kept:
// it's handy for tests (Chapter 28). Task.FromResult wraps an answer that's ready at once.
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = new();
    private readonly Lock _lock = new();   // many requests can arrive at once: they take turns

    public InMemoryTaskRepository(TimeProvider time)
    {
        // Sample tasks, with due dates relative to today, so the demo always looks the same.
        var today = time.Today();
        _tasks.Add(new() { Id = 1, Title = "Write report", Priority = "High", Due = today.AddDays(-2) });
        _tasks.Add(new() { Id = 2, Title = "Call supplier", Priority = "High", Due = today.AddDays(1) });
        _tasks.Add(new()
        {
            Id = 3, Title = "Renew licence", Priority = "Medium",
            Due = today.AddDays(5)
        });
        _tasks.Add(new() { Id = 4, Title = "Plan team lunch", Priority = "Low" });
    }

    public Task<Dictionary<int, StepCount>> CountStepsAsync(IReadOnlyCollection<int> taskIds)
    {
        lock (_lock)
        {
            return Task.FromResult(_tasks.Where(t => taskIds.Contains(t.Id) && t.Steps.Count > 0)
                                         .ToDictionary(t => t.Id, t => new StepCount(t.Steps.Count(s => s.Done), t.Steps.Count)));
        }
    }

    // All tasks: unfinished first, then by due date (no date last).
    public Task<List<TaskItem>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_tasks.OrderBy(t => t.Done)
                         .ThenBy(t => t.Due ?? DateOnly.MaxValue)
                         .ToList());
        }
    }

    public Task<TaskPage> GetPageAsync(TaskFilter filter, int page, int pageSize)
    {
        lock (_lock)
        {
            var matching = _tasks.Where(t => (filter.Done is not bool done || t.Done == done)
                                          && (filter.ProjectId is not int projectId || t.ProjectId == projectId))
                                 .OrderBy(t => t.Done).ThenBy(t => t.Due ?? DateOnly.MaxValue).ThenBy(t => t.Id)
                                 .ToList();
            return Task.FromResult(new TaskPage(matching.Skip((page - 1) * pageSize).Take(pageSize).ToList(), matching.Count));
        }
    }

    // Course 2, Chapter 5: here the C# rules themselves do the counting.
    public Task<DueCounts> CountDueAsync(DateOnly today, int days)
    {
        lock (_lock)
        {
            return Task.FromResult(new DueCounts(_tasks.Count(t => t.IsOverdue(today)),
                                                 _tasks.Count(t => t.IsDueSoon(today, days))));
        }
    }

    // One task, or null if there is no task with that id.
    public Task<TaskItem?> FindAsync(int id)
    {
        lock (_lock)
        {
            return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));
        }
    }

    public Task<TaskItem> AddAsync(TaskInput input)
    {
        lock (_lock)
        {
            int id = _tasks.Count == 0 ? 1 : _tasks.Max(t => t.Id) + 1;
            var task = new TaskItem
            {
                Id = id,
                Title = input.Title!.Trim(),
                Priority = input.Priority,
                Due = input.Due,
                Notes = input.Notes,
                ProjectId = input.ProjectId,
            };
            _tasks.Add(task);
            return Task.FromResult(task);
        }
    }

    // Copies the editable fields onto an existing task, if its version still matches.
    public Task<SaveResult> UpdateAsync(int id, TaskInput input)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task is null)
            {
                return Task.FromResult(SaveResult.NotFound);
            }
            if (task.Version != input.Version)
            {
                return Task.FromResult(SaveResult.Conflict);
            }
            task.Title = input.Title!.Trim();
            task.Priority = input.Priority;
            task.Due = input.Due;
            task.Notes = input.Notes;
            task.ProjectId = input.ProjectId;
            task.Version++;
            return Task.FromResult(SaveResult.Saved);
        }
    }

    // Returns false if there is no task with that id.
    public Task<bool> MarkDoneAsync(int id)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task is null)
            {
                return Task.FromResult(false);
            }
            task.Done = true;
            task.Version++;
            return Task.FromResult(true);
        }
    }

    // Returns false if there is no task with that id.
    public Task<bool> DeleteAsync(int id)
    {
        lock (_lock)
        {
            return Task.FromResult(_tasks.RemoveAll(t => t.Id == id) == 1);
        }
    }
}
