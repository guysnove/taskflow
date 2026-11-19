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
public class UnblockedTasksObserver(INotificationQueries queries, Notifier notifier) : ITaskCompletedObserver
{
    public async Task OnTaskCompletedAsync(int taskId)
    {
        var finished = await queries.TaskAsync(taskId);
        if (finished is null)
        {
            return;
        }
        foreach (var task in await queries.ReadyAfterAsync(taskId))     // the query moved to Infrastructure
        {
            await notifier.NotifyAsync(NotificationFactory.Unblocked(task, finished));
        }
    }
}
