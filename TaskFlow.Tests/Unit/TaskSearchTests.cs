using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 3: task search with a set of words.
[Trait("Req", "FR-SRCH-1")]
public class TaskSearchTests
{
    private static readonly List<TaskItem> Tasks =
    [
        new() { Id = 1, Title = "Fix login bug" },
        new() { Id = 2, Title = "Login page: new design" },
        new() { Id = 3, Title = "Write the bug report" },
        new() { Id = 4, Title = "Blog post" },
    ];

    [Theory]
    [InlineData("login", new[] { 1, 2 })]
    [InlineData("LOGIN", new[] { 1, 2 })]            // case doesn't matter
    [InlineData("bug login", new[] { 1 })]           // every word, any order
    [InlineData("  bug   ", new[] { 1, 3 })]         // extra spaces
    [InlineData("log", new int[0])]                  // whole words only: not "login", not "blog"
    [InlineData("login login", new[] { 1, 2 })]      // a repeated word counts once
    [InlineData("missing", new int[0])]
    public void Filter_MatchesTitlesContainingEveryWord(string query, int[] expected)
    {
        Assert.Equal(expected, TaskSearch.Filter(Tasks, query).Select(t => t.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" , ! ")]                            // only separators
    public void EmptySearch_ReturnsEverything(string? query)
    {
        Assert.Equal(4, TaskSearch.Filter(Tasks, query).Count);
    }

    [Fact]
    public void Words_IsASet()
    {
        var words = TaskSearch.Words("Fix the FIX, fix!");
        Assert.Equal(2, words.Count);                // "fix" and "the"
        Assert.Contains("FIX", words);               // and it ignores case
    }
}
