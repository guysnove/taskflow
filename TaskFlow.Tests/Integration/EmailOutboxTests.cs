using System.Diagnostics.Metrics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Common;
using TaskFlow.Data;
using TaskFlow.Notifications;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 28: the email outbox, against a real SQLite database. Safe retries (backoff, then dead), duplicate
// handling (idempotent enqueue; one sender per email), and nothing lost when stopping. TD-13 is closed by these.
[Trait("Req", "NFR-NOTIF-2")]
public sealed class EmailOutboxTests : IDisposable
{
    // A mail server we control: fails the first `failures` sends, hangs for addresses starting with "hang".
    private sealed class MailServer(int failures = 0) : IEmailSender
    {
        private int _failuresLeft = failures;
        public List<string> Sent { get; } = [];
        public int Calls;
        public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            if (recipient.StartsWith("hang", StringComparison.Ordinal)) await Task.Delay(Timeout.Infinite, cancellationToken);
            if (Interlocked.Decrement(ref _failuresLeft) >= 0) throw new InvalidOperationException("421 Service not available");
            await Task.Delay(20, cancellationToken);           // a real send takes a moment
            lock (Sent) Sent.Add(recipient);
        }
    }
    private sealed class NoJitter : Random { public override double NextDouble() => 0; }

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"outbox-{Guid.NewGuid():N}.db");
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero));
    private static readonly EmailOptions Settings = new() { OutboxMaxAttempts = 4, OutboxFirstRetry = TimeSpan.FromSeconds(30), OutboxMaxDelay = TimeSpan.FromMinutes(1), SendTimeout = TimeSpan.FromMilliseconds(300) };

    public EmailOutboxTests() { using var db = Db(); db.Database.Migrate(); }
    public void Dispose() { SqliteConnection.ClearAllPools(); File.Delete(_file); }

    private TaskFlowDbContext Db() => new(new DbContextOptionsBuilder<TaskFlowDbContext>().UseSqlite($"Data Source={_file}").Options);
    private EmailOutboxProcessor Processor(TaskFlowDbContext db, IEmailSender server) =>
        new(db, server, Options.Create(Settings), _clock, NullLogger<EmailOutboxProcessor>.Instance, new NoJitter());
    private async Task QueueAsync(string to, string key = "k1")
    {
        await using var db = Db();
        await new EfOutgoingEmails(db, _clock).EnqueueAsync(new EmailMessage(to, "Subject", "Body"), key);
    }
    private OutboxEmail Row() { using var db = Db(); return db.OutboxEmails.AsNoTracking().Single(); }

    [Fact]
    public async Task TheSameKeyTwice_IsOneEmail()
    {
        await using var db = Db();
        var outbox = new EfOutgoingEmails(db, _clock);
        Assert.True(await outbox.EnqueueAsync(new EmailMessage("ana@example.com", "s", "b"), "7:unblocked:2:1"));
        Assert.False(await outbox.EnqueueAsync(new EmailMessage("ana@example.com", "s", "b"), "7:unblocked:2:1"));
        Assert.Equal(1, await db.OutboxEmails.CountAsync());
    }

    [Fact]
    public async Task ASentEmail_IsNeverSentAgain()
    {
        await QueueAsync("ana@example.com");
        var server = new MailServer();
        await using (var db = Db()) await Processor(db, server).ProcessDueAsync(CancellationToken.None);
        await using (var db = Db()) await Processor(db, server).ProcessDueAsync(CancellationToken.None);
        Assert.Equal(["ana@example.com"], server.Sent);
        Assert.NotNull(Row().SentAt);
    }

    [Fact]
    public async Task AFailure_IsRetriedLater_WithExponentialBackoff()
    {
        await QueueAsync("ana@example.com");
        var server = new MailServer(failures: 2);
        var start = _clock.GetUtcNow().UtcDateTime;

        await using (var db = Db()) await Processor(db, server).ProcessDueAsync(CancellationToken.None);
        Assert.Equal((1, start.AddSeconds(30)), (Row().Attempts, Row().NextAttemptAt));       // 30 s

        await using (var db = Db()) Assert.Equal(0, await Processor(db, server).ProcessDueAsync(CancellationToken.None));   // not due yet

        _clock.Advance(TimeSpan.FromSeconds(30));
        await using (var db = Db()) await Processor(db, server).ProcessDueAsync(CancellationToken.None);
        Assert.Equal((2, start.AddSeconds(30 + 60)), (Row().Attempts, Row().NextAttemptAt));  // then 60 s

        _clock.Advance(TimeSpan.FromSeconds(60));
        await using (var db = Db()) await Processor(db, server).ProcessDueAsync(CancellationToken.None);
        Assert.Equal(["ana@example.com"], server.Sent);                                        // third time lucky
        Assert.Equal(3, Row().Attempts);
    }

    [Fact]
    public async Task TooManyFailures_SetsTheEmailAside()
    {
        await QueueAsync("ana@example.com");
        var server = new MailServer(failures: 99);
        for (int i = 0; i < 6; i++)
        {
            await using (var db = Db()) await Processor(db, server).ProcessDueAsync(CancellationToken.None);
            _clock.Advance(TimeSpan.FromMinutes(5));
        }
        var row = Row();
        Assert.Equal(4, server.Calls);                 // OutboxMaxAttempts, then never again
        Assert.NotNull(row.DeadAt);
        Assert.Equal("InvalidOperationException: 421 Service not available", row.LastError);
    }

    [Fact]
    [Trait("Req", "NFR-NOTIF-5")]
    public async Task AHangingMailServer_IsGivenUp_AndTriedAgainLater()
    {
        await QueueAsync("hang@example.com");
        await using (var db = Db()) await Processor(db, new MailServer()).ProcessDueAsync(CancellationToken.None);
        Assert.Equal((1, "timed out after 0.3 s"), (Row().Attempts, Row().LastError));
        Assert.Null(Row().SentAt);
    }

    [Fact]
    public async Task TwoSendersAtOnce_SendEachEmailOnce()
    {
        for (int i = 1; i <= 10; i++) await QueueAsync($"user{i}@example.com", $"k{i}");
        var server = new MailServer();
        await using var a = Db();
        await using var b = Db();
        await Task.WhenAll(Processor(a, server).ProcessDueAsync(CancellationToken.None), Processor(b, server).ProcessDueAsync(CancellationToken.None));
        Assert.Equal(10, server.Sent.Count);
        Assert.Equal(10, server.Sent.Distinct().Count());   // no email twice
    }

    [Fact]
    [Trait("Req", "NFR-NOTIF-5")]
    public async Task Stopping_DuringASend_LosesNothing()
    {
        await QueueAsync("hang@example.com");
        using var stopping = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var bigTimeout = new EmailOptions { SendTimeout = TimeSpan.FromMinutes(5) };
        await using (var db = Db())
        {
            var processor = new EmailOutboxProcessor(db, new MailServer(), Options.Create(bigTimeout), _clock, NullLogger<EmailOutboxProcessor>.Instance);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processor.ProcessDueAsync(stopping.Token));
        }
        var row = Row();
        Assert.Null(row.SentAt);
        Assert.Null(row.ClaimedUntil);                 // free for the next start: it will be sent then
    }

    // Course 2, Chapter 29: the signals. Every attempt is counted by outcome, and the gauge shows what's still waiting.
    private static readonly IMeterFactory Meters = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();

    [Fact]
    [Trait("Req", "NFR-OPS-3")]
    public async Task EveryAttempt_IsCounted_ByOutcome()
    {
        var metrics = new TaskFlowMetrics(Meters);
        using var emails = new MetricCollector<long>(Meters, TaskFlowTelemetry.Name, "taskflow.emails");
        using var waiting = new MetricCollector<int>(Meters, TaskFlowTelemetry.Name, "taskflow.email.outbox.waiting");
        await QueueAsync("ana@example.com", "k1");
        await QueueAsync("sam@example.com", "k2");
        var server = new MailServer(failures: 1);                 // the first send fails, the second succeeds
        await using (var db = Db())
        {
            await new EmailOutboxProcessor(db, server, Options.Create(Settings), _clock, NullLogger<EmailOutboxProcessor>.Instance, new NoJitter(), metrics)
                .ProcessDueAsync(CancellationToken.None);
        }
        Assert.Equal(["failed", "sent"], emails.GetMeasurementSnapshot().Select(m => (string)m.Tags["taskflow.outcome"]!));
        waiting.RecordObservableInstruments();
        Assert.Equal(1, waiting.LastMeasurement!.Value);          // ana's email waits for its retry
    }

    [Fact]
    [Trait("Req", "NFR-OPS-2")]
    public async Task TheHealthCheck_IsDegraded_WhenAnEmailWasGivenUp_OrIsStuck()
    {
        await using var db = Db();
        var check = new OutboxHealthCheck(db, _clock);
        Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new HealthCheckContext())).Status);

        await QueueAsync("ana@example.com", "k1");
        _clock.Advance(TimeSpan.FromMinutes(11));                 // due for 11 minutes, and no worker sent it
        var stuck = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal((HealthStatus.Degraded, 1), (stuck.Status, (int)stuck.Data["overdue_10min"]));

        await db.OutboxEmails.ExecuteUpdateAsync(s => s.SetProperty(e => e.DeadAt, _clock.GetUtcNow().UtcDateTime));
        var dead = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal((HealthStatus.Degraded, 1, 0), (dead.Status, (int)dead.Data["dead_last_24h"], (int)dead.Data["overdue_10min"]));
        _clock.Advance(TimeSpan.FromHours(25));
        Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new HealthCheckContext())).Status);   // a day later
    }

    // Course 2, Chapter 30: the drill showed "SmtpException: Failure sending mail." and nothing else. Now the cause is kept.
    [Fact]
    public void AFailure_KeepsItsInnerCause()
    {
        var ex = new InvalidOperationException("Failure sending mail.", new System.Net.Sockets.SocketException(111));
        string text = EmailOutboxProcessor.Describe(ex);
        Assert.StartsWith("InvalidOperationException: Failure sending mail. → SocketException: ", text);
        Assert.Contains("refused", text, StringComparison.OrdinalIgnoreCase);
    }
}
