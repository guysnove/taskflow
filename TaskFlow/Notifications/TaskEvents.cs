using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;

namespace TaskFlow.Notifications;

// OBSERVER: "a task was completed" is announced once; any number of observers react to it.
// The controllers don't know who is listening, or what they do.
public interface ITaskCompletedObserver
{
    Task OnTaskCompletedAsync(int taskId);
}

public class TaskEvents(IEnumerable<ITaskCompletedObserver> observers)
{
    public async Task TaskCompletedAsync(int taskId)
    {
        foreach (var observer in observers)
        {
            await observer.OnTaskCompletedAsync(taskId);
        }
    }
}

// The first observer: tell the owner of each task that was waiting for the finished one,
// once nothing else blocks it (FR-NOTIF-6).
public class UnblockedTasksObserver(TaskFlowDbContext db, Notifier notifier) : ITaskCompletedObserver
{
    public async Task OnTaskCompletedAsync(int taskId)
    {
        var finished = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (finished is null)
        {
            return;
        }
        // Open tasks that waited for it, and whose blockers are now ALL done.
        var ready = await db.Dependencies
            .Where(d => d.BlockedById == taskId && !d.Task!.Done)
            .Select(d => d.Task!)
            .Where(t => db.Dependencies.Where(o => o.TaskId == t.Id).All(o => o.BlockedBy!.Done))
            .AsNoTracking()
            .ToListAsync();
        foreach (var task in ready)
        {
            await notifier.NotifyAsync(NotificationFactory.Unblocked(task, finished));
        }
    }
}
