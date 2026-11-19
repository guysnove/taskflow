using TaskFlow.Models;

namespace TaskFlow.Notifications;

// STRATEGY: one way of delivering a message. Notifier uses every channel that the user wants.
// A future channel (SMS, a chat webhook…) is one more class and one line in Program.cs.
public interface INotificationChannel
{
    string Name { get; }
    bool IsWantedBy(User user);
    Task SendAsync(NotificationMessage message, User user);
}

// In-app: always on (FR-NOTIF-3). Stores the notification; the page and the badge read it.
public class InAppChannel(INotificationRepository notifications) : INotificationChannel
{
    public string Name => "in-app";
    public bool IsWantedBy(User user) => true;
    public Task SendAsync(NotificationMessage message, User user) => notifications.AddAsync(message);
}

// Email: only for users who turned it on (FR-NOTIF-4).
public class EmailChannel(IEmailSender email, Microsoft.Extensions.Options.IOptions<EmailOptions> options) : INotificationChannel
{
    public string Name => "email";
    public bool IsWantedBy(User user) => user.EmailNotifications;

    // NFR-PRIV-1: the title and a link, nothing else (no notes, no project names).
    public Task SendAsync(NotificationMessage message, User user) =>
        email.SendAsync(user.Email, $"TaskFlow: {message.Text}",
                        $"{message.Text}\n\n{options.Value.SiteUrl.TrimEnd('/')}{message.Link}\n\n"
                        + "You can turn these emails off in TaskFlow: Notifications > Settings.");
}
