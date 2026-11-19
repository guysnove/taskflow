using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;
using TaskFlow.Notifications;

namespace TaskFlow.Data;

public class EfNotificationRepository(TaskFlowDbContext db, TimeProvider time) : INotificationRepository
{
    public Task<bool> ExistsAsync(int userId, string key) =>
        db.Notifications.AnyAsync(n => n.UserId == userId && n.Key == key);

    public async Task<bool> AddAsync(NotificationMessage message)
    {
        db.Notifications.Add(new Notification
        {
            UserId = message.UserId, Kind = message.Kind, Key = message.Key, Text = message.Text, Link = message.Link, CreatedAt = time.GetUtcNow(),
        });
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            db.ChangeTracker.Clear();
            return false;                                       // the unique (UserId, Key) index said: already there
        }
    }

    public Task<int> UnreadCountAsync(int userId) =>
        db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null);

    public Task<List<Notification>> LatestAsync(int userId, int count) =>
        db.Notifications.AsNoTracking().Where(n => n.UserId == userId)
                        .OrderByDescending(n => n.Id).Take(count).ToListAsync();

    public Task<int> MarkAllReadAsync(int userId)
    {
        var now = time.GetUtcNow();
        return db.Notifications.Where(n => n.UserId == userId && n.ReadAt == null)
                               .ExecuteUpdateAsync(set => set.SetProperty(n => n.ReadAt, now));
    }
}
