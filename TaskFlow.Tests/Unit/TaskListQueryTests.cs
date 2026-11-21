using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 9: after Extract Class, the list's rules can be tested without a web request.
[Trait("Req", "FR-LIST-1")]
public class TaskListQueryTests
{
    [Theory]
    [InlineData(null, null, "all", "due")]
    [InlineData("open", "priority", "open", "priority")]
    [InlineData("OPEN", "Title", "all", "due")]            // case matters today: unknown → defaults
    [InlineData("nonsense", "nonsense", "all", "due")]
    public void From_FallsBackToTheDefaults(string? show, string? sort, string expectedShow, string expectedSort)
    {
        var query = TaskListQuery.From(show, sort, project: null, q: null);
        Assert.Equal((expectedShow, expectedSort), (query.Show, query.Sort));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  login bug ", "login bug")]
    public void From_TrimsTheSearch(string? q, string? expected) =>
        Assert.Equal(expected, TaskListQuery.From(null, null, null, q).Q);

    private static readonly List<TaskItem> Tasks =
    [
        new() { Id = 1, Title = "b low", Priority = "Low" },
        new() { Id = 2, Title = "a done high", Priority = "High", Done = true },
        new() { Id = 3, Title = "c high", Priority = "High", ProjectId = 7 },
        new() { Id = 4, Title = "d medium", Priority = "Medium", ProjectId = 7 },
    ];

    [Fact]
    public void Priority_UnfinishedFirst_ThenHighMediumLow() =>
        Assert.Equal([3, 4, 1, 2], TaskListQuery.From(null, "priority", null, null).Apply(Tasks, new DateOnly(2026, 11, 23)).Select(t => t.Id));

    [Fact]
    public void OpenTasks_OfOneProject() =>
        Assert.Equal([3, 4], TaskListQuery.From("open", null, 7, null).Apply(Tasks, new DateOnly(2026, 11, 23)).Select(t => t.Id));

    // Course 2, Chapter 19 (test-first): sorting by priority uses the EFFECTIVE priority (FR-PRIO-1).
    [Fact]
    [Trait("Req", "FR-PRIO-1")]
    public void Priority_UsesTheEffectivePriority()
    {
        var today = new DateOnly(2026, 11, 23);
        List<TaskItem> tasks =
        [
            new() { Id = 1, Title = "medium, no date", Priority = "Medium" },
            new() { Id = 2, Title = "low, due today", Priority = "Low", Due = today },                // -> Medium
            new() { Id = 3, Title = "low, overdue", Priority = "Low", Due = today.AddDays(-2) },     // -> High
            new() { Id = 4, Title = "high", Priority = "High" },
        ];

        var sorted = TaskListQuery.From(null, "priority", null, null).Apply(tasks, today);

        Assert.Equal([3, 4, 1, 2], sorted.Select(t => t.Id));          // High: 3, 4 · Medium: 1, 2 (same level: as before)
    }
}
