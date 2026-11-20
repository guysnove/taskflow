namespace Ch13.DI.Coupling.Loose;

// LOOSE COUPLING: the class says what it needs; someone else decides what to give it.
public interface IMailer
{
    void Send(string to, string text);
}

public sealed class ConsoleMailer : IMailer
{
    public void Send(string to, string text) => Console.WriteLine($"  (mail to {to}: {text})");
}

public sealed class ReminderService(IMailer mailer, TimeProvider time)   // CONSTRUCTOR INJECTION
{
    public bool SendDailyReminder(string to)
    {
        var today = time.GetLocalNow().DayOfWeek;
        if (today is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        mailer.Send(to, "Your tasks for today");
        return true;
    }
}
