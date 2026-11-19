using TaskFlow.Models;

namespace TaskFlow.Tests.Unit;

// Unit tests (Chapter 28): one small piece of code, on its own, with no web server and no database.
[Trait("Req", "FR-DUE-1")]
public class TaskItemTests
{
    private static readonly DateOnly Today = new(2026, 11, 8);   // a fixed "today": the result never depends on the real date

    [Fact]
    public void IsOverdue_DueYesterdayAndNotDone_IsTrue()
    {
        // Arrange: build what the test needs
        var task = new TaskItem { Title = "Call supplier", Due = Today.AddDays(-1), Done = false };

        // Act: do the one thing being tested
        bool overdue = task.IsOverdue(Today);

        // Assert: check the result
        Assert.True(overdue);
    }

    [Fact]
    public void IsOverdue_DueToday_IsFalse()
    {
        var task = new TaskItem { Title = "Call supplier", Due = Today };
        Assert.False(task.IsOverdue(Today));        // due today is not overdue yet
    }

    [Fact]
    public void IsOverdue_DoneTask_IsFalse()
    {
        var task = new TaskItem { Title = "Write report", Due = Today.AddDays(-5), Done = true };
        Assert.False(task.IsOverdue(Today));
    }

    [Fact]
    public void IsOverdue_NoDueDate_IsFalse()
    {
        var task = new TaskItem { Title = "Plan team lunch", Due = null };
        Assert.False(task.IsOverdue(Today));
    }
}
