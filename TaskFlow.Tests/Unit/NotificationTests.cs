using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Notifications;
using TaskFlow.Settings;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 10: the notification system's parts, each tested on its own.
public class NotificationFactoryTests
{
    private static readonly DateOnly Tuesday = new(2026, 11, 17);

    [Theory]
    [Trait("Req", "FR-NOTIF-2")]
    [InlineData(0, "Due today: Logo for Acme")]
    [InlineData(1, "Due tomorrow: Logo for Acme")]
    [InlineData(2, "Due Thursday 19 November: Logo for Acme")]
    public void DueSoon_SaysWhen(int days, string expected)
    {
        var task = new TaskItem { Id = 7, Title = "Logo for Acme", UserId = 3, Due = Tuesday.AddDays(days) };
        var message = NotificationFactory.DueSoon(task, Tuesday);
        Assert.Equal(expected, message.Text);
        Assert.Equal($"due:7:{Tuesday.AddDays(days):yyyy-MM-dd}", message.Key);     // a new date, a new reminder
        Assert.Equal("/tasks/7", message.Link);
    }

    [Fact]
    [Trait("Req", "NFR-PRIV-1")]
    public void Messages_HoldTheTitleAndALink_NothingElse()
    {
        var task = new TaskItem { Id = 7, Title = "Logo for Acme", UserId = 3, Due = Tuesday, Notes = "client budget: 4,000" };
        var message = NotificationFactory.DueSoon(task, Tuesday);
        Assert.DoesNotContain("budget", message.Text, StringComparison.OrdinalIgnoreCase);
    }
}

// A fake channel: records what it was asked to send, and can be told to fail.
internal sealed class RecordingChannel(string name, bool wanted = true, bool fails = false) : INotificationChannel
{
    public List<string> Sent { get; } = new();
    public string Name => name;
    public bool IsWantedBy(User user) => wanted;

    public Task SendAsync(NotificationMessage message, User user)
    {
        if (fails)
        {
            throw new InvalidOperationException($"{name} is down");
        }
        Sent.Add(message.Text);
        return Task.CompletedTask;
    }
}

public class NotifierTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TaskFlowDbContext _db;
    private readonly EfNotificationRepository _repository;
    private readonly int _userId;

    public NotifierTests()
    {
        _connection.Open();
        _db = new TaskFlowDbContext(new DbContextOptionsBuilder<TaskFlowDbContext>().UseSqlite(_connection).Options);
        _db.Database.Migrate();
        _db.Users.Add(new User { Email = "sam@example.com", DisplayName = "Sam", PasswordHash = "x" });
        _db.SaveChanges();
        _userId = _db.Users.Single().Id;
        _repository = new EfNotificationRepository(_db, new FakeTimeProvider());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private Notifier With(params INotificationChannel[] channels) =>
        new(channels, _repository, _db, NullLogger<Notifier>.Instance);

    private NotificationMessage Message(string key = "k1") => new(_userId, "DueSoon", key, "Due today: Logo", "/tasks/1");

    [Fact]
    public async Task OnlyWantedChannels_AreUsed()
    {
        var inApp = new RecordingChannel("in-app");
        var email = new RecordingChannel("email", wanted: false);

        await With(inApp, email).NotifyAsync(Message());

        Assert.Single(inApp.Sent);
        Assert.Empty(email.Sent);
    }

    [Fact]
    [Trait("Req", "FR-NOTIF-2")]
    public async Task TheSameNews_IsNeverSentTwice()
    {
        var notifier = With(new InAppChannel(_repository));

        Assert.True(await notifier.NotifyAsync(Message()));
        Assert.False(await notifier.NotifyAsync(Message()));          // same key: skipped
        Assert.Equal(1, await _repository.UnreadCountAsync(_userId));
    }

    [Fact]
    [Trait("Req", "NFR-NOTIF-2")]
    public async Task ABrokenChannel_DoesNotStopTheOthers()
    {
        var broken = new RecordingChannel("email", fails: true);
        var inApp = new RecordingChannel("in-app");

        await With(broken, inApp).NotifyAsync(Message());

        Assert.Single(inApp.Sent);
    }

    [Fact]
    [Trait("Req", "FR-NOTIF-3")]
    public async Task MarkAllRead_ClearsTheUnreadCount()
    {
        await _repository.AddAsync(Message("a"));
        await _repository.AddAsync(Message("b"));

        Assert.Equal(2, await _repository.MarkAllReadAsync(_userId));
        Assert.Equal(0, await _repository.UnreadCountAsync(_userId));
    }
}

public class DueRemindersTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TaskFlowDbContext _db;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero));   // a Tuesday

    public DueRemindersTests()
    {
        _connection.Open();
        _db = new TaskFlowDbContext(new DbContextOptionsBuilder<TaskFlowDbContext>().UseSqlite(_connection).Options);
        _db.Database.Migrate();
        _clock.SetLocalTimeZone(TimeZoneInfo.Utc);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    [Trait("Req", "FR-NOTIF-2")]
    public async Task OpenTasksDueWithinTwoDays_GetOneReminder_Once()
    {
        var sam = new User { Email = "sam@example.com", DisplayName = "Sam", PasswordHash = "x" };
        _db.Users.Add(sam);
        await _db.SaveChangesAsync();
        var today = new DateOnly(2026, 11, 17);
        _db.Tasks.AddRange(
            new TaskItem { Title = "Due Thursday", Due = today.AddDays(2), UserId = sam.Id },
            new TaskItem { Title = "Due today", Due = today, UserId = sam.Id },
            new TaskItem { Title = "Due next week", Due = today.AddDays(7), UserId = sam.Id },
            new TaskItem { Title = "Done already", Due = today, UserId = sam.Id, Done = true },
            new TaskItem { Title = "Overdue", Due = today.AddDays(-1), UserId = sam.Id });
        await _db.SaveChangesAsync();
        var repository = new EfNotificationRepository(_db, _clock);
        var notifier = new Notifier([new InAppChannel(repository)], repository, _db, NullLogger<Notifier>.Instance);
        var reminders = new DueReminders(_db, notifier, _clock, Options.Create(new TaskFlowOptions { DueSoonDays = 2 }));

        Assert.Equal(2, await reminders.RunAsync());
        Assert.Equal(0, await reminders.RunAsync());                 // the next run: nothing new
        Assert.Equal(["Due Thursday 19 November: Due Thursday", "Due today: Due today"],
                     (await repository.LatestAsync(sam.Id, 10)).Select(n => n.Text).Order());
    }
}

public class EmailTests
{
    // A fake inner sender that fails a given number of times first.
    private sealed class FlakySender(int failures) : IEmailSender
    {
        public int Calls { get; private set; }

        public Task SendAsync(string recipient, string subject, string body)
        {
            Calls++;
            return Calls <= failures ? throw new System.Net.Mail.SmtpException("busy") : Task.CompletedTask;
        }
    }

    private static RetryingEmailSender Retrying(IEmailSender inner) =>
        new(inner, Options.Create(new EmailOptions { Attempts = 3, RetryDelay = TimeSpan.Zero }),
            NullLogger<RetryingEmailSender>.Instance, TimeProvider.System);

    [Fact]
    [Trait("Req", "NFR-NOTIF-2")]
    public async Task Decorator_Retries_UntilItWorks()
    {
        var flaky = new FlakySender(failures: 2);
        await Retrying(flaky).SendAsync("sam@example.com", "s", "b");
        Assert.Equal(3, flaky.Calls);
    }

    [Fact]
    [Trait("Req", "NFR-NOTIF-2")]
    public async Task Decorator_GivesUp_AfterTheLastAttempt()
    {
        var broken = new FlakySender(failures: 99);
        await Assert.ThrowsAsync<System.Net.Mail.SmtpException>(() => Retrying(broken).SendAsync("sam@example.com", "s", "b"));
        Assert.Equal(3, broken.Calls);
    }

    [Fact]
    public async Task Adapter_WritesARealEmail_ToThePickupDirectory()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"mail-{Guid.NewGuid():N}");
        try
        {
            var sender = new SmtpEmailSender(Options.Create(new EmailOptions { PickupDirectory = folder }));
            await sender.SendAsync("sam@example.com", "TaskFlow: Due today: Logo", "Due today: Logo");

            string eml = File.ReadAllText(Assert.Single(Directory.GetFiles(folder, "*.eml")));
            Assert.Contains("To: sam@example.com", eml);
            Assert.Contains("Subject: TaskFlow: Due today: Logo", eml);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }
}
