using Microsoft.Extensions.Options;
using TaskFlow.Common;
using TaskFlow.Services;
using TaskFlow.Settings;

namespace TaskFlow.Notifications;

// FR-NOTIF-2: a reminder for every open task due within N days (TaskFlow:DueSoonDays),
// for every user. Sending twice is harmless: Notifier skips news it has already sent.
public class DueReminders(INotificationQueries queries, Notifier notifier, TimeProvider time, IOptions<TaskFlowOptions> options)
{
    // Course 2, Chapter 16: cancellation is cooperative. The caller asks (cancels the token);
    // this code checks, at a safe point: between two reminders, never in the middle of one.
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var today = time.Today();
        var last = today.AddDays(options.Value.DueSoonDays);
        var due = await queries.OpenDueBetweenAsync(today, last);
        int sent = 0;
        foreach (var task in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await notifier.NotifyAsync(NotificationFactory.DueSoon(task, today)))
            {
                sent++;
            }
        }
        return sent;
    }
}
