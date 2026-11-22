namespace TaskFlow.Models;

// One task in TaskFlow.
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Priority { get; set; } = "Medium";   // "High", "Medium" or "Low"
    public DateOnly? Due { get; set; }                  // null means "no due date"
    public bool Done { get; set; }
    public string? Notes { get; set; }                 // optional, added in Chapter 19
    public DateOnly? CreatedOn { get; set; }            // Chapter 23; null for older tasks

    // Chapter 24: the project this task belongs to (optional).
    public int? ProjectId { get; set; }                 // foreign key: null = no project
    public Project? Project { get; set; }               // navigation property

    // Chapter 25: who owns this task. Null only for tasks from before users existed.
    public int? UserId { get; set; }
    public User? User { get; set; }

    // Chapter 24: changes on every save; detects two people editing at once.
    public int Version { get; set; }

    // Chapter 30: the task's checklist. Empty until steps are added.
    public List<TaskStep> Steps { get; set; } = new();

    // Overdue means: not done yet, and the due date is before today.
    public bool IsOverdue(DateOnly today)
    {
        return !Done && Due is not null && Due < today;
    }

    // Chapter 28 (written test-first): not done, and due between today and today + days, inclusive.
    public bool IsDueSoon(DateOnly today, int days)
    {
        return !Done && Due >= today && Due <= today.AddDays(days);
    }

    // Chapter 30 (written test-first): "2 of 5 steps done", or null when the task has no steps.
    public string? StepProgress() => DescribeSteps(Steps.Count(s => s.Done), Steps.Count);

    // Course 2, Chapter 26: the same words when only the counts were loaded (the task list).
    public static string? DescribeSteps(int done, int total) => total == 0 ? null : $"{done} of {total} steps done";
}
