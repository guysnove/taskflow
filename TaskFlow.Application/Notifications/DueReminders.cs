using Microsoft.Extensions.Options;
using TaskFlow.Common;
using TaskFlow.Services;
using TaskFlow.Settings;

namespace TaskFlow.Notifications;

// FR-NOTIF-2: a reminder for every open task due within N days (TaskFlow:DueSoonDays),
// for every user. Sending twice is harmless: Notifier skips news it has already sent.
public class DueReminders(INotificationQueries queries, Notifier notifier, TimeProvider time, IOptions<TaskFlowOptions> options)
{
    public async Task<int> RunAsync()
    {
        var today = time.Today();
        var last = today.AddDays(options.Value.DueSoonDays);
        var due = await queries.OpenDueBetweenAsync(today, last);
        int sent = 0;
        foreach (var task in due)
        {
            if (await notifier.NotifyAsync(NotificationFactory.DueSoon(task, today)))
            {
                sent++;
            }
        }
        return sent;
    }
}
