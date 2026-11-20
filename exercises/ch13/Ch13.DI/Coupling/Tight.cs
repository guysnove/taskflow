namespace Ch13.DI.Coupling.Tight;

// TIGHT COUPLING: the class creates its own dependencies, and reads the real clock.
// To test "remind only on weekdays", you'd need a real mail server and to wait for a Monday.
public sealed class SmtpMailer
{
    public static List<string> Sent { get; } = new();      // stands in for a real SMTP server
    public void Send(string to, string text) => Sent.Add($"{to}: {text}");
}

public sealed class ReminderService
{
    private readonly SmtpMailer _mailer = new();            // hidden dependency #1: "new"

    public bool SendDailyReminder(string to)
    {
        var today = DateTime.Now.DayOfWeek;                 // hidden dependency #2: the real clock
        if (today is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        _mailer.Send(to, "Your tasks for today");
        return true;
    }
}
