namespace TaskFlow.Notifications;

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
                int sent = await scope.ServiceProvider.GetRequiredService<DueReminders>().RunAsync(stoppingToken);   // Chapter 16
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
