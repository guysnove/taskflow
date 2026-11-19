namespace Ch06.Requirements;

// FR-NOTIF-2 / US-2: "A user gets a notification when a task assigned to them is due within N days."
// Each rule below comes from one acceptance criterion or one answered ambiguity.
public record ReminderTask(int Id, DateOnly? Due, bool Done, int? AssigneeId);

public static class ReminderPolicy
{
    public static bool ShouldRemind(ReminderTask task, DateOnly today, int days, DateOnly? alreadyRemindedFor)
    {
        if (task.Done || task.Due is null || task.AssigneeId is null) return false;   // nothing to remind, nobody to remind
        if (task.Due < today) return false;                                          // passed: that's overdue, not "soon"
        if (alreadyRemindedFor == task.Due) return false;                            // once per due date (AC 2)
        return task.Due <= today.AddDays(days);                                      // calendar days, like FR-DUE-1
    }
}
