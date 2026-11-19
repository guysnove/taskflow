using System.Text;

namespace Ch07.Digest.After;

// The daily summary email (FR-NOTIF-5), refactored from Before/DigestService.cs.
// Same output for the same input — except one bug, fixed and tested (see DigestTests).
public static class DailyDigest
{
    public const string NothingToDo = "Nothing to do. Enjoy your day!";
    private const int DueSoonDays = 2;                       // the same window as FR-DUE-1

    // tasks: one user's tasks (the caller chooses whose). today: the user's date.
    public static string Write(IEnumerable<TaskInfo> tasks, DateOnly today, bool includeDone)
    {
        var shown = tasks.Where(t => includeDone || !t.Done)
                         .OrderBy(t => PriorityRank(t.Priority))
                         .ThenBy(t => t.Title, StringComparer.CurrentCulture)
                         .ToList();
        if (shown.Count == 0) return NothingToDo;

        var open = shown.Where(t => !t.Done).ToList();
        var overdue = open.Where(t => t.Due < today).ToList();
        var dueSoon = open.Where(t => t.Due >= today && t.Due <= today.AddDays(DueSoonDays)).ToList();
        int otherOpen = open.Count - overdue.Count - dueSoon.Count;
        int done = shown.Count - open.Count;

        var text = new StringBuilder();
        text.Append($"Good morning! Here is your TaskFlow summary for {today:dddd d MMMM}.\n");
        AppendSection(text, "OVERDUE", overdue, t => $"  ! {Describe(t, DaysLate(t, today))}");
        AppendSection(text, "DUE SOON", dueSoon, t => $"  - {Describe(t, WhenDue(t, today))}");
        if (otherOpen > 0) text.Append($"\n{otherOpen} other open {Plural(otherOpen, "task")}.\n");
        if (includeDone && done > 0) text.Append($"{done} done.\n");
        return text.ToString();
    }

    private static void AppendSection(StringBuilder text, string heading, List<TaskInfo> tasks, Func<TaskInfo, string> line)
    {
        if (tasks.Count == 0) return;
        text.Append($"\n{heading} ({tasks.Count})\n");
        foreach (var task in tasks) text.Append(line(task)).Append('\n');
    }

    // "Send invoices (high, 2 days late) [1/3]" — the checklist part only when there is one.
    private static string Describe(TaskInfo task, string when) =>
        $"{task.Title} ({task.Priority.ToLower()}, {when})" + (task.StepsTotal > 0 ? $" [{task.StepsDone}/{task.StepsTotal}]" : "");

    private static string DaysLate(TaskInfo task, DateOnly today)
    {
        int days = today.DayNumber - task.Due!.Value.DayNumber;
        return $"{days} {Plural(days, "day")} late";
    }

    private static string WhenDue(TaskInfo task, DateOnly today) => (task.Due!.Value.DayNumber - today.DayNumber) switch
    {
        0 => "today",
        1 => "tomorrow",
        int days => $"in {days} days",
    };

    private static int PriorityRank(string priority) => priority switch { "High" => 0, "Medium" => 1, _ => 2 };

    private static string Plural(int count, string word) => count == 1 ? word : word + "s";
}
