using TaskFlow.Models;

namespace TaskFlow.Services;

// Course 2, Chapter 4: task dependencies ("blocked by"), always within the signed-in user's tasks.
public interface IDependencyRepository
{
    Task<List<TaskItem>> BlockersOfAsync(int taskId);           // the tasks this one waits for
    Task<DependencyResult> AddAsync(int taskId, int blockedById);
    Task<bool> RemoveAsync(int taskId, int blockedById);
}

// Either it worked, or Error says why not, in words a user understands.
public record DependencyResult(bool Ok, string? Error = null, bool NotFound = false);
