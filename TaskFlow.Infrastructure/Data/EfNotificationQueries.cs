using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;
using TaskFlow.Notifications;

namespace TaskFlow.Data;

// Course 2, Chapter 12: the EF Core ADAPTER for INotificationQueries. The queries that used to
// live in Notifier, UnblockedTasksObserver and DueReminders, moved here unchanged.
public class EfNotificationQueries(TaskFlowDbContext db) : INotificationQueries
{
    public async Task<User?> UserAsync(int userId) => await db.Users.FindAsync(userId);

    public Task<TaskItem?> TaskAsync(int taskId) => db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);

    public Task<List<TaskItem>> ReadyAfterAsync(int finishedTaskId) =>
        db.Dependencies
          .Where(d => d.BlockedById == finishedTaskId && !d.Task!.Done)
          .Select(d => d.Task!)
          .Where(t => db.Dependencies.Where(o => o.TaskId == t.Id).All(o => o.BlockedBy!.Done))
          .AsNoTracking()
          .ToListAsync();

    public Task<List<TaskItem>> OpenDueBetweenAsync(DateOnly first, DateOnly last) =>
        db.Tasks.AsNoTracking()
          .Where(t => !t.Done && t.UserId != null && t.Due != null && t.Due >= first && t.Due <= last)
          .ToListAsync();
}
