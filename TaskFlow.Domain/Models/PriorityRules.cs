namespace TaskFlow.Models;

// Course 2, Chapter 19 (written test-first): the priority a task is treated as today.
// Level: "High", "Medium" or "Low". Raised: why it is higher than the person chose, or null.
public sealed record EffectivePriority(string Level, string? Raised);

// FR-PRIO-1. The rules, in order:
//   - a done task keeps the priority the person chose
//   - an overdue task is High
//   - a task due today or tomorrow goes up one level (Low -> Medium -> High)
//   - otherwise: the priority the person chose
// REFACTORED after the tests were green: the same behaviour, using TaskItem's own IsOverdue and
// IsDueSoon (Course 1, Chapter 28) instead of comparing dates again, and one place for "one level up".
public static class PriorityRules
{
    private static readonly string[] Levels = ["Low", "Medium", "High"];

    public static EffectivePriority For(TaskItem task, DateOnly today)
    {
        if (task.IsOverdue(today))
        {
            return new("High", task.Priority == "High" ? null : "overdue");     // already High: nothing raised
        }
        if (task.IsDueSoon(today, days: 1) && OneUp(task.Priority) is string up)
        {
            return new(up, task.Due == today ? "due today" : "due tomorrow");
        }
        return new(task.Priority, null);
    }

    // The next level, or null at the top (or for a value we don't know).
    private static string? OneUp(string level)
    {
        int i = Array.IndexOf(Levels, level);
        return i >= 0 && i < Levels.Length - 1 ? Levels[i + 1] : null;
    }
}
