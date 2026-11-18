using TaskFlow.Models;

namespace TaskFlow.Services;

// What TaskFlow needs to manage a task's checklist (Chapter 30).
// Every method takes the task's id: a step is always reached through its task,
// and only through a task the signed-in user owns.
public interface IStepRepository
{
    Task<List<TaskStep>?> GetForTaskAsync(int taskId);           // null: no such task (of yours)
    Task<TaskStep?> FindAsync(int taskId, int stepId);
    Task<TaskStep?> AddAsync(int taskId, string text);             // null: no such task (of yours)
    Task<TaskStep?> ToggleAsync(int taskId, int stepId);           // done <-> not done
    Task<bool> DeleteAsync(int taskId, int stepId);
}
