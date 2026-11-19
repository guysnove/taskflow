using System.Globalization;
using TaskFlow.Models;

namespace TaskFlow.Notifications;

// Course 2, Chapter 10: what to tell whom. Every channel (in-app, email, …) delivers the same message.
public sealed record NotificationMessage(int UserId, string Kind, string Key, string Text, string? Link);

// FACTORY: the one place that builds each kind of message, so the wording, the key and the link
// are always consistent. NFR-PRIV-1: a message holds the task's title and a link, nothing else.
public static class NotificationFactory
{
    public static NotificationMessage DueSoon(TaskItem task, DateOnly today)
    {
        var due = task.Due ?? throw new ArgumentException("A due-soon reminder needs a due date.", nameof(task));
        string when = due == today ? "today"
                    : due == today.AddDays(1) ? "tomorrow"
                    : due.ToString("dddd d MMMM", CultureInfo.InvariantCulture);
        // The key includes the due date: if the date moves, the new date gets its own reminder (US-2).
        return new(task.UserId!.Value, "DueSoon", $"due:{task.Id}:{due:yyyy-MM-dd}",
                   $"Due {when}: {task.Title}", $"/tasks/{task.Id}");
    }

    public static NotificationMessage Unblocked(TaskItem waiting, TaskItem finished) =>
        new(waiting.UserId!.Value, "Unblocked", $"unblocked:{waiting.Id}:{finished.Id}",
            $"Ready to start: {waiting.Title} (\"{finished.Title}\" is done)", $"/tasks/{waiting.Id}");
}
