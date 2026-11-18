// Chapter 2 demos.
//   dotnet run -c Release -- duplicates   time two duplicate-finding algorithms
//   dotnet run -- schedule                compare three scheduling strategies
using System.Diagnostics;
using Ch02.Algorithms;

if (args.FirstOrDefault() == "schedule") { Schedule(); return; }

// Rough timings with a stopwatch, on one machine: good enough to see the trend.
// (Chapter 5 measures properly, with BenchmarkDotNet.)
int[] sizes = args.Skip(1).Select(int.Parse).ToArray() is { Length: > 0 } s ? s : [1_000, 5_000, 20_000];

// Warm up: the first call of any method includes compiling it (JIT).
DuplicateFinder.FindPairs(MakeCustomers(200));
DuplicateFinder.FindGroups(MakeCustomers(200));

Console.WriteLine($"{"records",8} {"pairs: ms",10} {"groups: ms",11} {"comparisons",14} {"dupes found",12}");
foreach (int n in sizes)
{
    var customers = MakeCustomers(n);

    var sw = Stopwatch.StartNew();
    var pairs = DuplicateFinder.FindPairs(customers);
    double pairsMs = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    var groups = DuplicateFinder.FindGroups(customers);
    double groupsMs = sw.Elapsed.TotalMilliseconds;

    long comparisons = (long)n * (n - 1) / 2;
    Console.WriteLine($"{n,8:N0} {pairsMs,10:F1} {groupsMs,11:F2} {comparisons,14:N0} {pairs.Count,5} / {groups.Count,-5}");
}

// n customers; every 100th one re-uses an earlier customer's email, typed differently.
static List<Customer> MakeCustomers(int n)
{
    var list = new List<Customer>(n);
    for (int i = 0; i < n; i++)
    {
        string email = i % 100 == 99 ? $" Customer{i - 50}@Example.com" : $"customer{i}@example.com";
        list.Add(new Customer(i, $"Customer {i}", email));
    }
    return list;
}

static void Schedule()
{
    List<Job> jobs =
    [
        new("Write report", 4, 8),
        new("Fix login bug", 2, 3),
        new("Reply to client", 1, 6),
        new("Prepare demo", 3, 7),
    ];
    Show("As given", Scheduler.AsGiven(jobs));
    Show("Shortest first", Scheduler.ShortestFirst(jobs));
    Show("Earliest deadline first", Scheduler.EarliestDeadlineFirst(jobs));
}

static void Show(string title, List<Slot> plan)
{
    Console.WriteLine($"{title}: {Scheduler.LateCount(plan)} late, worst lateness {Scheduler.MaxLateness(plan)} h");
    foreach (var s in plan)
        Console.WriteLine($"  {s.Start,2}-{s.End,-2}  {s.Job.Name,-16} due {s.Job.Deadline,2}  {(s.Late ? "LATE" : "ok")}");
}
