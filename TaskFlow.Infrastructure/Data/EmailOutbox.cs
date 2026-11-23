using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Common;
using TaskFlow.Notifications;

namespace TaskFlow.Data;

// Course 2, Chapter 28: one email in the OUTBOX table. It stays until it's sent, or until it has failed too often (dead).
public class OutboxEmail
{
    public int Id { get; set; }
    public string Key { get; set; } = "";                 // unique: the same email can't be queued twice
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }               // UTC (SQLite compares DateTime, not DateTimeOffset)
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }      // when it may be tried (again)
    public DateTime? ClaimedUntil { get; set; }      // a sender is working on it until then
    public DateTime? SentAt { get; set; }
    public DateTime? DeadAt { get; set; }            // given up: kept, for a person to look at
    public string? LastError { get; set; }
}

// Course 2, Chapter 28: the producer side. Requests ADD a row; they never talk to a mail server (Chapter 16's promise),
// and nothing is lost when the app stops (TD-13: the old queue lived in memory).
public sealed class EfOutgoingEmails(TaskFlowDbContext db, TimeProvider time) : IOutgoingEmails
{
    public async Task<bool> EnqueueAsync(EmailMessage email, string key)
    {
        if (await db.OutboxEmails.AnyAsync(e => e.Key == key)) return false;       // a duplicate: ignore it
        var now = time.GetUtcNow().UtcDateTime;
        db.OutboxEmails.Add(new OutboxEmail { Key = key, To = email.To, Subject = email.Subject, Body = email.Body, CreatedAt = now, NextAttemptAt = now });
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)                         // the unique Key: another request queued it a moment ago
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}

// Course 2, Chapter 28: the consumer side. Sends what's due; on failure, tries again later with EXPONENTIAL BACKOFF plus a
// little random JITTER (so many failed emails don't all retry in the same second); after OutboxMaxAttempts it gives up (dead).
// Delivery is AT LEAST ONCE: if the app dies after the mail server accepted an email but before SentAt is saved, that email
// is sent again. The claim (ClaimedUntil) stops two copies of TaskFlow sending the same email at the same time.
public sealed class EmailOutboxProcessor(TaskFlowDbContext db, IEmailSender sender, IOptions<EmailOptions> options,
                                         TimeProvider time, ILogger<EmailOutboxProcessor> logger, Random? jitter = null,
                                         TaskFlowMetrics? metrics = null)
{
    private readonly EmailOptions _o = options.Value;
    private readonly Random _jitter = jitter ?? Random.Shared;

    // How long to wait before attempt n+1, after n failed attempts: first, 2×first, 4×first… capped; +0–20% jitter.
    public TimeSpan Backoff(int failedAttempts)
    {
        double seconds = _o.OutboxFirstRetry.TotalSeconds * Math.Pow(2, failedAttempts - 1);
        seconds = Math.Min(seconds, _o.OutboxMaxDelay.TotalSeconds);
        return TimeSpan.FromSeconds(seconds * (1 + 0.2 * _jitter.NextDouble()));
    }

    // Sends every email that's due (up to `batch`); returns how many were tried.
    public async Task<int> ProcessDueAsync(CancellationToken stoppingToken, int batch = 20)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var due = await db.OutboxEmails.AsNoTracking()
                          .Where(e => e.SentAt == null && e.DeadAt == null && e.NextAttemptAt <= now && (e.ClaimedUntil == null || e.ClaimedUntil < now))
                          .OrderBy(e => e.Id).Take(batch).Select(e => e.Id).ToListAsync(stoppingToken);
        int tried = 0;
        foreach (int id in due)
        {
            if (await ClaimAsync(id, now, stoppingToken)) { tried++; await SendOneAsync(id, stoppingToken); }
        }
        // Course 2, Chapter 29: how many emails are still waiting (a gauge on dashboards; it should stay near 0).
        metrics?.OutboxWaiting(await db.OutboxEmails.CountAsync(e => e.SentAt == null && e.DeadAt == null, CancellationToken.None));
        return tried;
    }

    // Course 2, Chapter 30 (an action from the Chapter 29 drill): the whole chain of causes, not just the outer message.
    // SmtpClient says only "Failure sending mail."; the reason ("Connection refused") is in an inner exception.
    public static string Describe(Exception ex)
    {
        var parts = new List<string>();
        for (var e = ex; e is not null && parts.Count < 4; e = e.InnerException)
        {
            parts.Add(e.GetType().Name + ": " + e.Message);
        }
        string text = string.Join(" → ", parts);
        return text.Length <= 500 ? text : text[..500];
    }

    // Only one sender wins the UPDATE: it says "1 row", the others "0 rows".
    private async Task<bool> ClaimAsync(int id, DateTime now, CancellationToken ct)
    {
        var until = now + _o.SendTimeout + TimeSpan.FromSeconds(30);
        int rows = await db.OutboxEmails.Where(e => e.Id == id && e.SentAt == null && (e.ClaimedUntil == null || e.ClaimedUntil < now))
                                        .ExecuteUpdateAsync(s => s.SetProperty(e => e.ClaimedUntil, until), ct);
        return rows == 1;
    }

    private async Task SendOneAsync(int id, CancellationToken stoppingToken)
    {
        var email = await db.OutboxEmails.AsNoTracking().SingleAsync(e => e.Id == id, stoppingToken);
        // Course 2, Chapter 29: one span per attempt; the log lines below carry its trace id.
        using var activity = TaskFlowTelemetry.Traces.StartActivity("SendEmail");
        activity?.SetTag("taskflow.email.id", id);
        activity?.SetTag("taskflow.email.attempt", email.Attempts + 1);
        var started = time.GetTimestamp();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(_o.SendTimeout);
        string? error = null;
        try
        {
            await sender.SendAsync(email.To, email.Subject, email.Body, timeout.Token);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping: leave it in the outbox, unclaimed, for the next start. Nothing is lost.
            await db.OutboxEmails.Where(e => e.Id == id).ExecuteUpdateAsync(s => s.SetProperty(e => e.ClaimedUntil, (DateTime?)null), CancellationToken.None);
            throw;
        }
        catch (OperationCanceledException) { error = $"timed out after {_o.SendTimeout.TotalSeconds} s"; }
        catch (Exception ex) { error = Describe(ex); }

        var now = time.GetUtcNow().UtcDateTime;
        int attempts = email.Attempts + 1;
        var took = time.GetElapsedTime(started);
        if (error is not null) activity?.SetStatus(ActivityStatusCode.Error, error);
        if (error is null)
        {
            await db.OutboxEmails.Where(e => e.Id == id).ExecuteUpdateAsync(s => s
                .SetProperty(e => e.SentAt, now).SetProperty(e => e.Attempts, attempts).SetProperty(e => e.ClaimedUntil, (DateTime?)null), CancellationToken.None);
            logger.LogInformation("Email {Id} sent to {To} (attempt {Attempt})", id, LogSafe.Email(email.To), attempts);
            metrics?.EmailAttempt("sent", took);
        }
        else if (attempts >= _o.OutboxMaxAttempts)
        {
            await db.OutboxEmails.Where(e => e.Id == id).ExecuteUpdateAsync(s => s
                .SetProperty(e => e.DeadAt, now).SetProperty(e => e.Attempts, attempts).SetProperty(e => e.LastError, error)
                .SetProperty(e => e.ClaimedUntil, (DateTime?)null), CancellationToken.None);
            logger.LogError("Email {Id} to {To} failed {Attempts} times; given up (dead): {Error}", id, LogSafe.Email(email.To), attempts, error);
            metrics?.EmailAttempt("dead", took);
        }
        else
        {
            var wait = Backoff(attempts);
            await db.OutboxEmails.Where(e => e.Id == id).ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Attempts, attempts).SetProperty(e => e.LastError, error).SetProperty(e => e.NextAttemptAt, now + wait)
                .SetProperty(e => e.ClaimedUntil, (DateTime?)null), CancellationToken.None);
            logger.LogWarning("Email {Id} to {To} failed (attempt {Attempt} of {Max}): {Error}; next try in {Seconds:F0} s",
                              id, LogSafe.Email(email.To), attempts, _o.OutboxMaxAttempts, error, wait.TotalSeconds);
            metrics?.EmailAttempt("failed", took);
        }
    }
}

// Course 2, Chapter 28: the background loop. Every OutboxPollInterval it asks the processor to send what's due, in a new scope
// (a new DbContext each time). One failing round never stops the loop.
public sealed class EmailOutboxWorker(IServiceScopeFactory scopes, IOptions<EmailOptions> options, ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>().ProcessDueAsync(stoppingToken);
                await Task.Delay(options.Value.OutboxPollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email outbox round failed; trying again");
                await Task.Delay(options.Value.OutboxPollInterval, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
            }
        }
    }
}
