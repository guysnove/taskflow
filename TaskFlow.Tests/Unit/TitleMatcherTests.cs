using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 2: the duplicate-title algorithm, edge cases first.
public class TitleMatcherTests
{
    [Theory]
    [InlineData("Buy milk", "buy milk")]
    [InlineData("  Buy   MILK!  ", "buy milk")]      // spaces, case, punctuation at the end
    [InlineData("Call Ana?", "call ana")]
    [InlineData("Buy milk !", "buy milk")]             // punctuation after a space
    [InlineData("v1.0 release", "v1.0 release")]     // a dot inside the title is kept
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Key_IgnoresWhatDoesNotMatter(string? title, string expected)
    {
        Assert.Equal(expected, TitleMatcher.Key(title));
    }

    private static readonly List<TaskItem> Tasks =
    [
        new() { Id = 1, Title = "Buy milk", Done = true },          // finished: not a duplicate
        new() { Id = 2, Title = "Call the bank" },
        new() { Id = 3, Title = "Write report" },
    ];

    [Fact]
    public void FindsAnOpenTaskWithTheSameKey()
    {
        Assert.Equal(2, TitleMatcher.FindOpenDuplicate("call THE bank.", Tasks)?.Id);
    }

    [Fact]
    public void IgnoresDoneTasks()
    {
        Assert.Null(TitleMatcher.FindOpenDuplicate("Buy milk", Tasks));
    }

    [Fact]
    public void DifferentWords_AreNotDuplicates()
    {
        Assert.Null(TitleMatcher.FindOpenDuplicate("Write the report", Tasks));
    }

    [Fact]
    public void EmptyTitleOrNoTasks_FindsNothing()
    {
        Assert.Null(TitleMatcher.FindOpenDuplicate("", Tasks));
        Assert.Null(TitleMatcher.FindOpenDuplicate("Buy milk", []));
    }
}
