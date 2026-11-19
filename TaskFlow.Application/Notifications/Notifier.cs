namespace TaskFlow.Notifications;

// Sends one message through every channel the user wants. It knows channels only through the
// INotificationChannel interface (Strategy), and receives them all from DI.
public class Notifier(IEnumerable<INotificationChannel> channels, INotificationRepository notifications,
                      INotificationQueries queries, ILogger<Notifier> logger)
{
    private readonly List<INotificationChannel> _channels = channels.ToList();

    // Returns false when this news was already sent (same user and key): nothing is sent twice.
    public async Task<bool> NotifyAsync(NotificationMessage message)
    {
        if (await notifications.ExistsAsync(message.UserId, message.Key))
        {
            return false;
        }
        var user = await queries.UserAsync(message.UserId);
        if (user is null)
        {
            return false;
        }
        foreach (var channel in _channels.Where(c => c.IsWantedBy(user)))
        {
            try
            {
                await channel.SendAsync(message, user);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One broken channel (say, the mail server) must not stop the others.
                logger.LogError(ex, "Channel {Channel} failed for notification {Key}", channel.Name, message.Key);
            }
        }
        return true;
    }
}
