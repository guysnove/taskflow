using Ch02.Algorithms;

namespace Ch02.Algorithms.Tests;

public class SchedulerTests
{
    // A morning's work: (name, hours, deadline in hours from now).
    private static readonly List<Job> Jobs =
    [
        new("Write report", 4, 8),
        new("Fix login bug", 2, 3),
        new("Reply to client", 1, 6),
        new("Prepare demo", 3, 7),
    ];

    [Fact]
    public void Plan_RunsJobsBackToBack()
    {
        var plan = Scheduler.AsGiven(Jobs);

        Assert.Equal(0, plan[0].Start);
        Assert.Equal(4, plan[0].End);
        Assert.Equal(4, plan[1].Start);              // starts when the previous one ends
        Assert.Equal(10, plan[^1].End);              // 4 + 2 + 1 + 3 hours in total
    }

    [Fact]
    public void EarliestDeadlineFirst_IsNeverLaterThanTheOthers()
    {
        int asGiven = Scheduler.MaxLateness(Scheduler.AsGiven(Jobs));
        int shortest = Scheduler.MaxLateness(Scheduler.ShortestFirst(Jobs));
        int edf = Scheduler.MaxLateness(Scheduler.EarliestDeadlineFirst(Jobs));

        Assert.True(edf <= asGiven);
        Assert.True(edf <= shortest);
    }

    [Fact]
    public void ShortestFirst_CanMissADeadlineThatEdfMeets()
    {
        List<Job> jobs = [new("Long, urgent", 3, 3), new("Short", 1, 10)];

        Assert.Equal(1, Scheduler.LateCount(Scheduler.ShortestFirst(jobs)));
        Assert.Equal(0, Scheduler.LateCount(Scheduler.EarliestDeadlineFirst(jobs)));
    }

    [Fact]
    public void EmptyList_GivesAnEmptyPlan()
    {
        var plan = Scheduler.EarliestDeadlineFirst([]);

        Assert.Empty(plan);
        Assert.Equal(0, Scheduler.LateCount(plan));
        Assert.Equal(0, Scheduler.MaxLateness(plan));
    }
}
