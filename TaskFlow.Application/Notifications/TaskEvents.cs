namespace TaskFlow.Notifications;

// OBSERVER: "a task was completed" is announced once; any number of observers react to it.
// The controllers don't know who is listening, or what they do.
public interface ITaskCompletedObserver
{
    Task OnTaskCompletedAsync(int taskId);
}

// Course 2, Chapter 17 (found by a unit test): the task is already saved when observers run, so one
// failing observer must neither turn a finished task into an error page, nor stop the others.
// Failures are logged — the same rule Notifier follows for its channels.
public class TaskEvents(IEnumerable<ITaskCompletedObserver> observers, ILogger<TaskEvents> logger)
{
    public async Task TaskCompletedAsync(int taskId)
    {
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnTaskCompletedAsync(taskId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Observer {Observer} failed for completed task {TaskId}", observer.GetType().Name, taskId);
            }
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
