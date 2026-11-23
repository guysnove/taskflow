using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Notifications;

// Settings, from the "Email" section. In Development, PickupDirectory writes each email as an
// .eml file instead of sending it: real messages you can open, and no mail server needed.
public class EmailOptions
{
    public const string Section = "Email";
    [Required] public string From { get; set; } = "taskflow@example.com";
    [Required] public string SiteUrl { get; set; } = "https://localhost:5001";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    // Course 2, Chapter 20 (F4): most mail servers need a sign-in. These are SECRETS: never in
    // appsettings*.json or git. Development: dotnet user-secrets set "Email:Password" "…".
    // Production: an environment variable (Email__Password) or a secret store. See docs/security.
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? PickupDirectory { get; set; }
    [Range(1, 10)] public int Attempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    // Course 2, Chapter 16: the longest one email may take (all attempts). Then it is given up, and logged.
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(30);

    // Course 2, Chapter 28: the outbox. An email that fails is tried again later, waiting twice as long each time
    // (OutboxFirstRetry, 2×, 4×… up to OutboxMaxDelay), up to OutboxMaxAttempts times; then it is set aside (dead) and logged.
    [Range(1, 20)] public int OutboxMaxAttempts { get; set; } = 6;
    public TimeSpan OutboxFirstRetry { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan OutboxMaxDelay { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan OutboxPollInterval { get; set; } = TimeSpan.FromSeconds(2);
}

// What the rest of TaskFlow needs from email: one method. TaskFlow's own interface.
public interface IEmailSender
{
    // Course 2, Chapter 16: a CancellationToken, so a slow send can be stopped (a timeout, or shutdown).
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}

// Course 2, Chapter 16: one email waiting to be sent.
public record EmailMessage(string To, string Subject, string Body);

// Course 2, Chapter 16: emails are queued, and sent in the background — a request never waits for a mail server.
// Course 2, Chapter 28: the queue is a table (an OUTBOX), so a restart or a crash loses nothing (TD-13). The key makes
// enqueueing idempotent: the same key twice is one email, however often the caller retries.
public interface IOutgoingEmails
{
    Task<bool> EnqueueAsync(EmailMessage email, string key);   // false: already queued with this key (a duplicate, ignored)
}
