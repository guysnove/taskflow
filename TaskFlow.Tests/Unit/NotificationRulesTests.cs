using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using TaskFlow.Models;
using TaskFlow.Notifications;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 17: the notification rules that the coverage report showed untested —
// the "unblocked" observer (0%), the email channel (0%), and two branches of the factory and the notifier.
public class NotificationRulesTests
{
    private static readonly User Sam = new() { Id = 3, Email = "sam@example.com", DisplayName = "Sam", EmailNotifications = true };

    // A SPY channel: remembers every message it was given.
    private sealed class SpyChannel : INotificationChannel
    {
        public List<NotificationMessage> Sent { get; } = [];
        public string Name => "spy";
        public bool IsWantedBy(User user) => true;
        public Task SendAsync(NotificationMessage message, User user)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private readonly INotificationQueries _queries = Substitute.For<INotificationQueries>();
    private readonly INotificationRepository _stored = Substitute.For<INotificationRepository>();
    private readonly SpyChannel _channel = new();

    public NotificationRulesTests() => _queries.UserAsync(Sam.Id).Returns(Sam);

    private Notifier Notifier() => new([_channel], _stored, _queries, NullLogger<Notifier>.Instance);

    [Fact]
    [Trait("Req", "FR-NOTIF-6")]
    public async Task FinishingATask_NotifiesTheOwnerOfEveryTaskThatIsNowReady()
    {
        // Arrange: "Get logo" is finished; two of Sam's tasks were waiting only for it.
        var logo = new TaskItem { Id = 1, Title = "Get logo", UserId = Sam.Id };
        _queries.TaskAsync(1).Returns(logo);
        _queries.ReadyAfterAsync(1).Returns([new TaskItem { Id = 2, Title = "Write copy", UserId = Sam.Id },
                                             new TaskItem { Id = 5, Title = "Print flyers", UserId = Sam.Id }]);

        // Act
        await new UnblockedTasksObserver(_queries, Notifier()).OnTaskCompletedAsync(1);

        // Assert
        Assert.Equal(["Ready to start: Write copy (\"Get logo\" is done)", "Ready to start: Print flyers (\"Get logo\" is done)"],
                     _channel.Sent.Select(m => m.Text));
    }

    [Fact]
    [Trait("Req", "FR-NOTIF-6")]
    public async Task AnUnknownFinishedTask_NotifiesNobody()
    {
        await new UnblockedTasksObserver(_queries, Notifier()).OnTaskCompletedAsync(99);   // TaskAsync(99): null

        Assert.Empty(_channel.Sent);
        await _queries.DidNotReceive().ReadyAfterAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task ANotificationForAnUnknownUser_IsNotSent()
    {
        bool sent = await Notifier().NotifyAsync(new NotificationMessage(42, "DueSoon", "due:1:2026-11-22", "Due tomorrow: X", "/tasks/1"));

        Assert.False(sent);
        Assert.Empty(_channel.Sent);
    }

    [Theory]
    [Trait("Req", "FR-NOTIF-2")]
    [InlineData("2026-11-21", "Due today: Pay rent")]
    [InlineData("2026-11-22", "Due tomorrow: Pay rent")]
    [InlineData("2026-11-23", "Due Monday 23 November: Pay rent")]
    public void DueSoon_SaysWhenInWords(string due, string expected)
    {
        var task = new TaskItem { Id = 8, Title = "Pay rent", UserId = Sam.Id, Due = DateOnly.Parse(due, System.Globalization.CultureInfo.InvariantCulture) };

        var message = NotificationFactory.DueSoon(task, today: new DateOnly(2026, 11, 21));

        Assert.Equal(expected, message.Text);
        Assert.Equal($"due:8:{due}", message.Key);       // the date is part of the key (US-2)
    }

    [Fact]
    public void DueSoon_WithoutADueDate_IsABug()
    {
        var task = new TaskItem { Id = 8, Title = "Pay rent", UserId = Sam.Id, Due = null };
        Assert.Throws<ArgumentException>(() => NotificationFactory.DueSoon(task, new DateOnly(2026, 11, 21)));
    }

    // The email channel only queues (Chapter 16). A spy queue shows what it queued, and with which key;
    // FakeLogger (Microsoft.Extensions.Diagnostics.Testing) shows what it logged.
    // Course 2, Chapter 28: the queue is idempotent: the same key twice is one email.
    private sealed class SpyQueue : IOutgoingEmails
    {
        public List<(EmailMessage Email, string Key)> Queued { get; } = [];
        public Task<bool> EnqueueAsync(EmailMessage email, string key)
        {
            if (Queued.Any(q => q.Key == key)) return Task.FromResult(false);
            Queued.Add((email, key));
            return Task.FromResult(true);
        }
    }

    private static readonly NotificationMessage Ready = new(Sam.Id, "Unblocked", "unblocked:2:1", "Ready to start: Write copy", "/tasks/2");

    [Fact]
    [Trait("Req", "NFR-PRIV-1")]
    public async Task TheEmail_HasTheTitleAndALink_AndNothingElse()
    {
        var queue = new SpyQueue();
        var channel = new EmailChannel(queue, Options.Create(new EmailOptions { SiteUrl = "https://taskflow.example/" }), new FakeLogger<EmailChannel>());

        await channel.SendAsync(Ready, Sam);

        var (email, _) = Assert.Single(queue.Queued);
        Assert.Equal("sam@example.com", email.To);
        Assert.Equal("TaskFlow: Ready to start: Write copy", email.Subject);
        Assert.StartsWith("Ready to start: Write copy\n\nhttps://taskflow.example/tasks/2\n\n", email.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheSameNotificationTwice_IsOneEmail_AndTheDuplicateIsLogged()
    {
        var queue = new SpyQueue();
        var logger = new FakeLogger<EmailChannel>();
        var channel = new EmailChannel(queue, Options.Create(new EmailOptions()), logger);

        await channel.SendAsync(Ready, Sam);
        await channel.SendAsync(Ready, Sam);                            // e.g. the observer ran again after a retry

        var (_, key) = Assert.Single(queue.Queued);
        Assert.Equal($"{Sam.Id}:unblocked:2:1", key);
        Assert.Equal("Email for notification unblocked:2:1 was already queued: duplicate ignored", logger.LatestRecord.Message);
    }
}
