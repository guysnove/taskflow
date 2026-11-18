using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Services;
using TaskFlow.Settings;
using TaskFlow.ViewComponents;

namespace TaskFlow.Tests.Unit;

// A test with a stand-in ("fake") for the database: the in-memory repository from Chapter 21.
// The view component only knows ITaskRepository, so it can't tell the difference.
public class OverdueSummaryTests
{
    private class SignedInUser : ICurrentUser          // a tiny fake: always "user 1, signed in"
    {
        public bool IsSignedIn => true;
        public int Id => 1;
    }

    [Fact]
    public async Task Summary_CountsOverdueAndDueSoon()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 8, 9, 0, 0, TimeSpan.Zero));
        var repository = new InMemoryTaskRepository(clock);   // its sample tasks: due dates relative to "today"
        var component = new OverdueSummaryViewComponent(repository, clock,
                            Options.Create(new TaskFlowOptions { DueSoonDays = 2 }), new SignedInUser());

        var result = (ViewViewComponentResult)await component.InvokeAsync();
        var summary = (OverdueSummary)result.ViewData!.Model!;

        Assert.Equal(1, summary.Overdue);     // "Write report", due 2 days ago
        Assert.Equal(1, summary.DueSoon);     // "Call supplier", due tomorrow
        Assert.Equal(2, summary.Days);
    }
}
