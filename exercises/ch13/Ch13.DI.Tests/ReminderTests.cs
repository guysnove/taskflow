using Ch13.DI.Coupling.Loose;
using Microsoft.Extensions.Time.Testing;

namespace Ch13.DI.Tests;

// With constructor injection, a test chooses the clock and catches the mail. No server, no waiting for Monday.
public class ReminderTests
{
    private sealed class RecordingMailer : IMailer
    {
        public List<string> Sent { get; } = [];
        public void Send(string to, string text) => Sent.Add(to);
    }

    [Theory]
    [InlineData(2026, 11, 16, true)]     // a Monday
    [InlineData(2026, 11, 21, false)]    // a Saturday
    public void RemindsOnWeekdaysOnly(int y, int m, int d, bool expected)
    {
        var mailer = new RecordingMailer();
        var clock = new FakeTimeProvider(new DateTimeOffset(y, m, d, 9, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);

        bool sent = new ReminderService(mailer, clock).SendDailyReminder("ana@example.com");

        Assert.Equal(expected, sent);
        Assert.Equal(expected ? 1 : 0, mailer.Sent.Count);
    }
}
