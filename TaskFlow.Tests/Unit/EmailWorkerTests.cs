using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Notifications;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 16: the background worker, on its own. What happens when a mail server hangs,
// and when the app is stopped while emails are still waiting?
[Trait("Req", "NFR-NOTIF-5")]
public class EmailWorkerTests
{
    // Hangs (until cancelled) when the recipient's address starts with "hang"; otherwise "sends" at once.
    private sealed class MailServer : IEmailSender
    {
        public List<string> Sent { get; } = [];

        public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (recipient.StartsWith("hang", StringComparison.Ordinal))
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            lock (Sent) Sent.Add(recipient);
        }
    }

    // Keeps every log message, so the test can read what the worker said.
    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (Lines) Lines.Add($"{logLevel}: {formatter(state, exception)}");
        }
    }

    private static (EmailWorker Worker, OutgoingEmails Queue, MailServer Server, ListLogger<EmailWorker> Log) Create(TimeSpan sendTimeout)
    {
        var queue = new OutgoingEmails();
        var server = new MailServer();
        var log = new ListLogger<EmailWorker>();
        var worker = new EmailWorker(queue, server, Options.Create(new EmailOptions { SendTimeout = sendTimeout }), log);
        return (worker, queue, server, log);
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        var deadline = Stopwatch.StartNew();
        while (!condition() && deadline.Elapsed < TimeSpan.FromSeconds(5)) await Task.Delay(20);
    }

    [Fact]
    public async Task AHangingMailServer_IsGivenUp_AndTheNextEmailStillGoes()
    {
        var (worker, queue, server, log) = Create(sendTimeout: TimeSpan.FromMilliseconds(300));
        await worker.StartAsync(CancellationToken.None);

        queue.TryEnqueue(new EmailMessage("hang@example.com", "s", "b"));
        queue.TryEnqueue(new EmailMessage("ana@example.com", "s", "b"));
        await UntilAsync(() => server.Sent.Count == 1);
        await worker.StopAsync(CancellationToken.None);

        Console.WriteLine(string.Join("\n", log.Lines));
        Assert.Equal(["ana@example.com"], server.Sent);
        Assert.Contains(log.Lines, l => l.StartsWith("Error: Email to h***@example.com timed out", StringComparison.Ordinal));   // Course 2, Ch. 20: masked (F2)
    }

    [Fact]
    public async Task Stopping_CancelsTheCurrentSend_AndSaysWhatWasNotSent()
    {
        var (worker, queue, _, log) = Create(sendTimeout: TimeSpan.FromMinutes(5));
        await worker.StartAsync(CancellationToken.None);
        queue.TryEnqueue(new EmailMessage("hang@example.com", "s", "b"));
        queue.TryEnqueue(new EmailMessage("ana@example.com", "s", "b"));
        queue.TryEnqueue(new EmailMessage("ben@example.com", "s", "b"));
        await UntilAsync(() => queue.Waiting == 2);                  // the first one is being "sent"

        var clock = Stopwatch.StartNew();
        await worker.StopAsync(CancellationToken.None);
        clock.Stop();

        Console.WriteLine($"Stopped after {clock.ElapsedMilliseconds} ms\n" + string.Join("\n", log.Lines));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(2));         // not 5 minutes
        Assert.Contains("Warning: Stopping: 2 queued email(s) were not sent", log.Lines);
    }
}
