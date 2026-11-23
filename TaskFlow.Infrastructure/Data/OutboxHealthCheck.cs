using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskFlow.Data;

// Course 2, Chapter 29: is email delivery working? Two things a person must know about:
//  - an email was given up on (dead) in the last 24 hours: someone didn't get a notification;
//  - an email has been due for more than 10 minutes: the outbox worker isn't running, or is stuck.
// DEGRADED, not Unhealthy: TaskFlow still works, so /health (used by deploys, Chapter 24) leaves this check out.
public sealed class OutboxHealthCheck(TaskFlowDbContext db, TimeProvider time) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var dayAgo = now.AddHours(-24);
        var stuckSince = now.AddMinutes(-10);
        int dead = await db.OutboxEmails.CountAsync(e => e.DeadAt != null && e.DeadAt > dayAgo, cancellationToken);
        int stuck = await db.OutboxEmails.CountAsync(e => e.SentAt == null && e.DeadAt == null && e.NextAttemptAt < stuckSince, cancellationToken);
        var data = new Dictionary<string, object> { ["dead_last_24h"] = dead, ["overdue_10min"] = stuck };
        if (dead == 0 && stuck == 0) return HealthCheckResult.Healthy("emails are being delivered", data);
        return HealthCheckResult.Degraded($"{dead} email(s) given up in the last 24 h; {stuck} overdue by more than 10 min", data: data);
    }
}
