using TaskFlow.Models;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 19: the priority rules, written TEST-FIRST. Each test below was red before the
// code that makes it pass existed. Read them top to bottom: that's the order they were written.
[Trait("Req", "FR-PRIO-1")]
public class PriorityRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);     // a Monday

    private static TaskItem Task(string priority, int? dueInDays = null, bool done = false) => new()
    {
        Title = "Pay rent", Priority = priority, Done = done,
        Due = dueInDays is int d ? Today.AddDays(d) : null,
    };

    // 1. Without a due date, nothing changes: the priority is the one the person chose.
    [Theory]
    [InlineData("High")]
    [InlineData("Medium")]
    [InlineData("Low")]
    public void NoDueDate_KeepsItsOwnPriority(string chosen)
    {
        var effective = PriorityRules.For(Task(chosen), Today);

        Assert.Equal(new EffectivePriority(chosen, Raised: null), effective);
    }

    // 2. Overdue (not done, due before today): treated as High, and we say why.
    [Theory]
    [InlineData("Low", -1)]
    [InlineData("Medium", -30)]
    public void Overdue_IsHigh(string chosen, int dueInDays)
    {
        var effective = PriorityRules.For(Task(chosen, dueInDays), Today);

        Assert.Equal(new EffectivePriority("High", Raised: "overdue"), effective);
    }

    // 3. Due today or tomorrow: one level up. Two days away: not yet (the boundary).
    [Theory]
    [InlineData("Low", 0, "Medium", "due today")]
    [InlineData("Medium", 1, "High", "due tomorrow")]
    [InlineData("High", 0, "High", null)]           // already the top: not "raised"
    [InlineData("Low", 2, "Low", null)]             // the day after tomorrow: unchanged
    public void DueTodayOrTomorrow_GoesUpOneLevel(string chosen, int dueInDays, string level, string? raised)
    {
        var effective = PriorityRules.For(Task(chosen, dueInDays), Today);

        Assert.Equal(new EffectivePriority(level, raised), effective);
    }

    // 4. A finished task is never raised: it needs no attention, whatever its date.
    [Theory]
    [InlineData(-3)]
    [InlineData(0)]
    [InlineData(1)]
    public void ADoneTask_IsNeverRaised(int dueInDays)
    {
        var effective = PriorityRules.For(Task("Low", dueInDays, done: true), Today);

        Assert.Equal(new EffectivePriority("Low", Raised: null), effective);
    }

    // 5. Found while looking at the list: an overdue task that was ALREADY High showed "Raised from
    //    High: overdue". Its level is right, but nothing was raised. Written as a test first.
    [Fact]
    public void Overdue_AndAlreadyHigh_IsNotRaised()
    {
        var effective = PriorityRules.For(Task("High", -2), Today);

        Assert.Equal(new EffectivePriority("High", Raised: null), effective);
    }
}
