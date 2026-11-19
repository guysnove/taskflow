using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

using TaskFlow.Services;

namespace TaskFlow.Data;

// The checklist steps, in the database (Chapter 30).
// The same rule as Chapter 25's MyTasks(): every query starts from "this user's" data.
public class EfStepRepository : IStepRepository
{
    private readonly TaskFlowDbContext _db;
    private readonly ICurrentUser _user;

    public EfStepRepository(TaskFlowDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    // Steps of tasks that belong to the signed-in user. EF turns s.TaskItem!.UserId into a JOIN.
    private IQueryable<TaskStep> MySteps()
    {
        int userId = _user.Id;
        return _db.Steps.Where(s => s.TaskItem!.UserId == userId);
    }

    private Task<bool> TaskIsMineAsync(int taskId)
    {
        int userId = _user.Id;
        return _db.Tasks.AnyAsync(t => t.Id == taskId && t.UserId == userId);
    }

    public async Task<List<TaskStep>?> GetForTaskAsync(int taskId)
    {
        if (!await TaskIsMineAsync(taskId))
        {
            return null;
        }
        return await MySteps().AsNoTracking()
                              .Where(s => s.TaskItemId == taskId)
                              .OrderBy(s => s.Position)
                              .ToListAsync();
    }

    public Task<TaskStep?> FindAsync(int taskId, int stepId)
    {
        return MySteps().AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == stepId && s.TaskItemId == taskId);
    }

    public async Task<TaskStep?> AddAsync(int taskId, string text)
    {
        if (!await TaskIsMineAsync(taskId))
        {
            return null;
        }
        // The new step goes last: one more than the highest position so far (0 if none).
        int last = await _db.Steps.Where(s => s.TaskItemId == taskId)
                                  .MaxAsync(s => (int?)s.Position) ?? 0;
        var step = new TaskStep { TaskItemId = taskId, Text = text.Trim(), Position = last + 1 };
        _db.Steps.Add(step);
        await _db.SaveChangesAsync();
        return step;
    }

    public async Task<TaskStep?> ToggleAsync(int taskId, int stepId)
    {
        var step = await MySteps().FirstOrDefaultAsync(s => s.Id == stepId && s.TaskItemId == taskId);
        if (step is null)
        {
            return null;
        }
        step.Done = !step.Done;
        await _db.SaveChangesAsync();
        return step;
    }

    public async Task<bool> DeleteAsync(int taskId, int stepId)
    {
        int rows = await MySteps()
                            .Where(s => s.Id == stepId && s.TaskItemId == taskId)
                            .ExecuteDeleteAsync();          // one DELETE, no loading first
        return rows == 1;
    }
}
