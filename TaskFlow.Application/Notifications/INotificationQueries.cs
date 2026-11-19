using TaskFlow.Models;

namespace TaskFlow.Notifications;

// Course 2, Chapter 12: a PORT. The questions the notification use cases ask about stored data,
// across all users. The Application layer owns this interface; Infrastructure implements it
// with EF Core. Before, Notifier, the observer and DueReminders queried the DbContext directly.
public interface INotificationQueries
{
    Task<User?> UserAsync(int userId);
    Task<TaskItem?> TaskAsync(int taskId);

    // Open tasks that waited for the finished task, and whose blockers are now ALL done.
    Task<List<TaskItem>> ReadyAfterAsync(int finishedTaskId);

    // Open tasks of any user, due between first and last (inclusive).
    Task<List<TaskItem>> OpenDueBetweenAsync(DateOnly first, DateOnly last);
}
