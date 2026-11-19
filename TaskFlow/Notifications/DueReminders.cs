using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Data;
using TaskFlow.Services;
using TaskFlow.Settings;

namespace TaskFlow.Notifications;

// FR-NOTIF-2: a reminder for every open task due within N days (TaskFlow:DueSoonDays),
// for every user. Sending twice is harmless: Notifier skips news it has already sent.
public class DueReminders(TaskFlowDbContext db, Notifier notifier, TimeProvider time, IOptions<TaskFlowOptions> options)
{
    public async Task<int> RunAsync()
    {
        var today = time.Today();
        var last = today.AddDays(options.Value.DueSoonDays);
        var due = await db.Tasks.AsNoTracking()
            .Where(t => !t.Done && t.UserId != null && t.Due != null && t.Due >= today && t.Due <= last)
            .ToListAsync();
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

// Runs DueReminders every Notifications:ReminderInterval (default: hourly; 0 turns it off).
public sealed class DueReminderService(IServiceScopeFactory scopes, IConfiguration config, TimeProvider time,
                                       ILogger<DueReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = config.GetValue("Notifications:ReminderInterval", TimeSpan.FromHours(1));
        if (interval <= TimeSpan.Zero)
        {
            return;
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();          // a fresh DbContext for each run
                int sent = await scope.ServiceProvider.GetRequiredService<DueReminders>().RunAsync();
                logger.LogInformation("Due reminders: {Sent} sent", sent);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Due reminders failed; trying again next time");
            }
            await Task.Delay(interval, time, stoppingToken);
        }
    }
}
