using TaskFlow.Models;

namespace TaskFlow.Tests.Unit;

// Written BEFORE TaskItem.IsDueSoon existed (Chapter 28): first the tests, then the code.
public class IsDueSoonTests
{
    private static readonly DateOnly Today = new(2026, 11, 8);

    [Theory]
    [InlineData(0, true)]      // due today: soon
    [InlineData(1, true)]      // tomorrow
    [InlineData(2, true)]      // the last day of a 2-day window
    [InlineData(3, false)]     // just outside the window
    [InlineData(-1, false)]    // yesterday: that's overdue, not "soon"
    public void IsDueSoon_TwoDayWindow(int dueInDays, bool expected)
    {
        var task = new TaskItem { Title = "Renew licence", Due = Today.AddDays(dueInDays) };
        Assert.Equal(expected, task.IsDueSoon(Today, days: 2));
    }

    [Fact]
    public void IsDueSoon_DoneTask_IsFalse()
    {
        var task = new TaskItem { Title = "Renew licence", Due = Today, Done = true };
        Assert.False(task.IsDueSoon(Today, days: 2));
    }

    [Fact]
    public void IsDueSoon_NoDueDate_IsFalse()
    {
        var task = new TaskItem { Title = "Renew licence" };
        Assert.False(task.IsDueSoon(Today, days: 2));
    }
}
