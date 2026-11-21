using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 8: each rule is a small class, so each can be tested on its own:
// no web server, no database, a clock we choose.
public class NewTaskRulesTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));

    private static TaskInput Due(string? day) => new()
    {
        Title = "Write report",
        Due = day is null ? null : DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture),
    };

    [Theory]
    [Trait("Req", "FR-TASK-2")]
    [InlineData("2026-11-17", true)]       // yesterday
    [InlineData("2026-11-18", false)]      // today is fine
    [InlineData(null, false)]              // no due date is fine
    public async Task DueNotInPast(string? day, bool refused)
    {
        var problem = await new DueNotInPastRule(Clock).CheckAsync(Due(day));
        Assert.Equal(refused, problem is not null);
    }

    [Theory]
    [Trait("Req", "FR-TASK-3")]
    [InlineData("2031-11-18", false)]      // exactly five years: allowed
    [InlineData("2031-11-19", true)]
    [InlineData("2062-11-18", true)]       // the typo this rule is for
    public async Task DueWithinFiveYears(string day, bool refused)
    {
        var problem = await new DueWithinFiveYearsRule(Clock).CheckAsync(Due(day));
        Assert.Equal(refused, problem is not null);
        if (refused)
        {
            Assert.Equal("Due", problem!.Field);
        }
    }

    [Fact]
    [Trait("Req", "FR-DUP-1")]
    public async Task Duplicate_IsAWarning_AndAllowDuplicateSilencesIt()
    {
        var rule = new DuplicateTitleRule(new InMemoryTaskRepository(Clock));   // has an open "Write report"

        var problem = await rule.CheckAsync(new TaskInput { Title = "write  REPORT" });
        Assert.True(problem!.Warning);

        Assert.Null(await rule.CheckAsync(new TaskInput { Title = "write report", AllowDuplicate = true }));
    }

    // A rule made up for the test: the checker works with any rule.
    private sealed class Always(bool warning, string message) : INewTaskRule
    {
        public int Runs { get; private set; }
        public bool IsWarning => warning;

        public Task<RuleProblem?> CheckAsync(TaskInput input)
        {
            Runs++;
            return Task.FromResult<RuleProblem?>(new RuleProblem("Title", message, warning));
        }
    }

    [Fact]
    public async Task Checker_SkipsWarnings_WhenThereAreErrors()
    {
        var error = new Always(false, "an error");
        var warning = new Always(true, "a warning");

        var problems = await new NewTaskChecker([warning, error]).CheckAsync(new TaskInput(), alreadyInvalid: false);

        Assert.Equal(["an error"], problems.Select(p => p.Message));
        Assert.Equal(0, warning.Runs);
    }

    [Fact]
    public async Task Checker_SkipsWarnings_WhenTheFormIsAlreadyInvalid()
    {
        var warning = new Always(true, "a warning");

        var problems = await new NewTaskChecker([warning]).CheckAsync(new TaskInput(), alreadyInvalid: true);

        Assert.Empty(problems);
    }

    [Fact]
    public async Task Checker_RunsWarnings_WhenAllElseIsFine()
    {
        var problems = await new NewTaskChecker([new Always(true, "a warning")]).CheckAsync(new TaskInput(), alreadyInvalid: false);

        Assert.True(Assert.Single(problems).Warning);
    }

    // Course 2, Chapter 17: ProjectExistsRule had no unit test at all (the coverage report: 0%).
    // Its only collaborator, IProjectRepository, is replaced by an NSubstitute "mock":
    // we tell it what to answer, and afterwards ask it what it was asked.
    [Fact]
    [Trait("Req", "FR-OWN-1")]
    public async Task ProjectExists_NoProjectChosen_DoesNotAskTheDatabase()
    {
        // Arrange
        var projects = Substitute.For<IProjectRepository>();
        var rule = new ProjectExistsRule(projects);

        // Act
        var problem = await rule.CheckAsync(new TaskInput { Title = "Pay rent", ProjectId = null });

        // Assert
        Assert.Null(problem);
        await projects.DidNotReceive().FindAsync(Arg.Any<int>());
    }

    [Theory]
    [Trait("Req", "FR-OWN-1")]
    [InlineData(true, false)]       // my project: fine
    [InlineData(false, true)]       // not found (or someone else's: FindAsync only sees mine): refused
    public async Task ProjectExists_AsksForTheChosenProject(bool found, bool refused)
    {
        var projects = Substitute.For<IProjectRepository>();
        projects.FindAsync(7).Returns(found ? new Project { Id = 7, Name = "Move flat" } : null);

        var problem = await new ProjectExistsRule(projects).CheckAsync(new TaskInput { Title = "Pack books", ProjectId = 7 });

        Assert.Equal(refused, problem is not null);
        await projects.Received(1).FindAsync(7);
        if (refused)
        {
            Assert.Equal(new RuleProblem("ProjectId", "Please choose a project from the list."), problem);
        }
    }
}
