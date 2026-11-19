using Ch07.Digest;

namespace Ch07.Digest.Tests;

// Characterisation tests: written BEFORE refactoring, to pin down what the old code really does.
// Then the same expectations run against the new code. If both pass, the behaviour didn't change.
public class DigestTests
{
    // DigestService reads the real clock (DateTime.Now) in one place, so these tests use the real date.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);
    private const int User = 7;

    private static List<TaskInfo> Sample(DateOnly today) =>
    [
        new(1, User, "Send invoices", "High", today.AddDays(-2), false, 1, 3),
        new(2, User, "Logo for Acme", "Medium", today, false, 0, 0),
        new(3, User, "Brochure draft", "High", today.AddDays(1), false, 2, 5),
        new(4, User, "Book the venue", "Low", today.AddDays(-1), false, 0, 0),
        new(5, User, "Renew domain", "Low", today.AddDays(30), false, 0, 0),
        new(6, User, "Client call notes", "Medium", null, false, 0, 0),
        new(7, User, "Old report", "Medium", today.AddDays(-5), true, 2, 2),
        new(8, 9, "Someone else's task", "High", today, false, 0, 0),
    ];

    private static string Expected(DateOnly today) =>
        $"Good morning! Here is your TaskFlow summary for {today:dddd d MMMM}.\n" +
        "\nOVERDUE (2)\n  ! Send invoices (high, 2 days late) [1/3]\n  ! Book the venue (low, 1 day late)\n" +
        "\nDUE SOON (2)\n  - Brochure draft (high, tomorrow) [2/5]\n  - Logo for Acme (medium, today)\n" +
        "\n2 other open tasks.\n1 done.\n";

    private static string Before(List<TaskInfo> tasks, DateOnly today, bool includeDone) =>
        new Before.DigestService().Make(tasks, User, today.ToDateTime(new TimeOnly(8, 0)), includeDone);
    private static string After(List<TaskInfo> tasks, DateOnly today, bool includeDone) =>
        Ch07.Digest.After.DailyDigest.Write(tasks.Where(t => t.UserId == User), today, includeDone);

    [Fact] public void Before_FullSample() => Assert.Equal(Expected(Today), Before(Sample(Today), Today, true));
    [Fact] public void After_FullSample() => Assert.Equal(Expected(Today), After(Sample(Today), Today, true));

    [Fact]
    public void BothAgree_WithoutDoneTasks() =>
        Assert.Equal(Before(Sample(Today), Today, false), After(Sample(Today), Today, false));

    [Fact]
    public void BothAgree_NothingToDo()
    {
        Assert.Equal("Nothing to do. Enjoy your day!", Before([], Today, false));
        Assert.Equal("Nothing to do. Enjoy your day!", After([], Today, false));
    }

    [Fact]
    public void BothAgree_OneTaskInEachSection_SingularWords()
    {
        List<TaskInfo> one = [new(1, User, "A", "High", Today.AddDays(-1), false, 0, 0), new(2, User, "B", "Low", Today.AddDays(2), false, 0, 0),
                              new(3, User, "C", "Medium", null, false, 0, 0)];
        Assert.Equal(Before(one, Today, false), After(one, Today, false));
        Assert.Contains("1 day late", After(one, Today, false));
        Assert.Contains("1 other open task.", After(one, Today, false));
    }

    // The bug the characterisation tests uncovered: for any date other than the real today,
    // the old code's "due soon" window ends 2 days after the REAL today, not after the given date.
    [Fact]
    public void Before_HasAHiddenClockBug_ForADigestAWeekAgo()
    {
        var weekAgo = Today.AddDays(-7);
        List<TaskInfo> tasks = [new(1, User, "Due in 5 days", "High", weekAgo.AddDays(5), false, 0, 0)];
        Assert.Contains("DUE SOON (1)", Before(tasks, weekAgo, false));       // wrong: 5 days isn't "soon"
        Assert.DoesNotContain("DUE SOON", After(tasks, weekAgo, false));        // fixed: the window uses the given date
    }
}
