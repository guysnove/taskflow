using TaskFlow.Models;

namespace TaskFlow.Notifications;

// REPOSITORY: notifications as a collection; the rest of the app never writes EF Core queries for them.
public interface INotificationRepository
{
    Task<bool> ExistsAsync(int userId, string key);
    Task<bool> AddAsync(NotificationMessage message);          // false: already stored (same key)
    Task<int> UnreadCountAsync(int userId);
    Task<List<Notification>> LatestAsync(int userId, int count);
    Task<int> MarkAllReadAsync(int userId);
}
