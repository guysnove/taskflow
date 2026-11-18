namespace Ch02.Algorithms;

// Chapter 2: one person, a list of jobs. In what order should they be done?
// Hours are counted from the start of the day: Deadline 6 means "6 hours from now".
public record Job(string Name, int Hours, int Deadline);

public record Slot(Job Job, int Start, int End)
{
    public bool Late => End > Job.Deadline;
}

public static class Scheduler
{
    // Do the jobs one after another, in the order given.
    public static List<Slot> Plan(IEnumerable<Job> ordered)
    {
        var plan = new List<Slot>();
        int now = 0;
        foreach (var job in ordered)
        {
            plan.Add(new Slot(job, now, now + job.Hours));
            now += job.Hours;
        }
        return plan;
    }

    // Three strategies = three algorithms. Same input, different orders.
    public static List<Slot> AsGiven(IEnumerable<Job> jobs) => Plan(jobs);
    public static List<Slot> ShortestFirst(IEnumerable<Job> jobs) =>
        Plan(jobs.OrderBy(j => j.Hours).ThenBy(j => j.Deadline));
    public static List<Slot> EarliestDeadlineFirst(IEnumerable<Job> jobs) =>
        Plan(jobs.OrderBy(j => j.Deadline).ThenBy(j => j.Hours));

    public static int LateCount(List<Slot> plan) => plan.Count(s => s.Late);
    // Edge case: an empty plan has no lateness. (Max of nothing throws an exception.)
    public static int MaxLateness(List<Slot> plan) =>
        plan.Count == 0 ? 0 : plan.Max(s => Math.Max(0, s.End - s.Job.Deadline));
}
