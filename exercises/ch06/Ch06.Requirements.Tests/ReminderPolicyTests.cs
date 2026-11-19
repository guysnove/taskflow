using Ch06.Requirements;

namespace Ch06.Requirements.Tests;

// One test per acceptance criterion of US-2 (and per answered ambiguity). The names say which.
public class ReminderPolicyTests
{
    private static readonly DateOnly Tuesday = new(2026, 11, 17), Thursday = new(2026, 11, 19);
    private static ReminderTask Task(DateOnly? due = null, bool done = false, int? assignee = 7) => new(1, due ?? Thursday, done, assignee);

    [Fact] // AC 1: due Thursday, N = 2, today is Tuesday → remind
    public void DueInTwoDays_IsReminded() => Assert.True(ReminderPolicy.ShouldRemind(Task(), Tuesday, 2, null));

    [Fact] // AC 2: only once per due date
    public void AlreadyReminded_ForThisDueDate_IsNotRemindedAgain() => Assert.False(ReminderPolicy.ShouldRemind(Task(), Tuesday, 2, Thursday));

    [Fact] // AC 2, continued: a NEW due date gets a new reminder
    public void DueDateMoved_IsRemindedAgain() => Assert.True(ReminderPolicy.ShouldRemind(Task(Thursday), Tuesday, 2, Thursday.AddDays(-7)));

    [Fact] // AC 3: done tasks get no reminder
    public void DoneTask_IsNotReminded() => Assert.False(ReminderPolicy.ShouldRemind(Task(done: true), Tuesday, 2, null));

    [Fact] // AC 3: a passed due date is "overdue", not "due soon"
    public void PassedDueDate_IsNotReminded() => Assert.False(ReminderPolicy.ShouldRemind(Task(Tuesday.AddDays(-1)), Tuesday, 2, null));

    [Fact] // the ambiguity "how long before?": 2 calendar days, so 3 days away is too early
    public void ThreeDaysAway_IsTooEarly() => Assert.False(ReminderPolicy.ShouldRemind(Task(Tuesday.AddDays(3)), Tuesday, 2, null));

    [Fact] // due today is still "soon" (the same rule as FR-DUE-1)
    public void DueToday_IsReminded() => Assert.True(ReminderPolicy.ShouldRemind(Task(Tuesday), Tuesday, 2, null));

    [Fact] // nobody to remind
    public void UnassignedTask_IsNotReminded() => Assert.False(ReminderPolicy.ShouldRemind(Task(assignee: null), Tuesday, 2, null));
}
