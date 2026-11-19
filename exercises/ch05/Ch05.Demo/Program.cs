// Chapter 5 demos. Stopwatch timings: rough, one machine. Ch05.Benchmarks measures properly.
//   dotnet run -c Release -- dedupe     the opening example: List.Contains vs HashSet, as n grows
//   dotnet run -- steps                 counted steps for each complexity class
//   dotnet run -c Release -- doubling   double n: how does the time change?
//   dotnet run -- cases                 best, average and worst case
//   dotnet run -c Release -- concat     string += vs StringBuilder
//   dotnet run -c Release -- space      extra memory: in place vs a copy
//   dotnet run -- jit                   the first call is slower (warm-up)
//   dotnet run -c Release -- setup      the cost of preparing: a sorted copy, a HashSet (time and memory)
using System.Diagnostics;
using Ch05.Efficiency;

switch (args.FirstOrDefault())
{
    case "dedupe": DedupeDemo(); break;
    case "steps": Steps(); break;
    case "doubling": Doubling(); break;
    case "cases": CasesDemo(); break;
    case "concat": Concat(); break;
    case "space": SpaceDemo(); break;
    case "jit": Jit(); break;
    case "setup": Setup(); break;
    default: Console.WriteLine("Usage: dotnet run -- dedupe | steps | doubling | cases | concat | space | jit | setup"); break;
}

static string[] Emails(int n)
{
    var rnd = new Random(n);
    return Enumerable.Range(0, n).Select(_ => $"user{rnd.Next(n * 4 / 5)}@example.com").ToArray();  // ~20% repeats
}

static void DedupeDemo()
{
    Dedupe.WithList(Emails(100)); Dedupe.WithSet(Emails(100));       // warm up
    foreach (int n in new[] { 1_000, 10_000, 20_000, 40_000 })
    {
        string[] emails = Emails(n);
        var sw = Stopwatch.StartNew(); int a = Dedupe.WithList(emails).Count; double list = sw.Elapsed.TotalMilliseconds;
        sw.Restart(); int b = Dedupe.WithSet(emails).Count; double set = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"{n,7:N0} emails: List.Contains {list,9:F1} ms   HashSet {set,6:F2} ms   ({a:N0} unique, same: {a == b})");
    }
}

static void Steps()
{
    Console.WriteLine($"{"n",9} {"O(1)",5} {"O(log n)",9} {"O(n)",10} {"O(n log n)",12} {"O(n²)",16}");
    foreach (int n in new[] { 10, 100, 1_000, 10_000, 100_000 })
        Console.WriteLine($"{n,9:N0} {StepCounter.Constant(n),5} {StepCounter.Logarithmic(n),9} {StepCounter.Linear(n),10:N0} {StepCounter.Linearithmic(n),12:N0} {StepCounter.Quadratic(n),16:N0}");
    Console.WriteLine($"O(2ⁿ): n = 10 → {StepCounter.Exponential(10):N0}   n = 20 → {StepCounter.Exponential(20):N0}   n = 30 → {StepCounter.Exponential(30):N0}");
}

static void Doubling()
{
    var rnd = new Random(1);
    int[] Data(int n) => Enumerable.Range(0, n).Select(_ => rnd.Next()).ToArray();
    Snippets.HasDuplicatePair(Data(1000)); Array.Sort(Data(1000));                   // warm up
    double prevQ = 0, prevS = 0;
    foreach (int n in new[] { 5_000, 10_000, 20_000, 40_000 })
    {
        int[] d = Data(n);
        var sw = Stopwatch.StartNew(); Snippets.HasDuplicatePair(d.Select(x => x & int.MaxValue).Distinct().ToArray()); double q = sw.Elapsed.TotalMilliseconds;
        int[] s = Data(n * 100); sw.Restart(); Array.Sort(s); double srt = sw.Elapsed.TotalMilliseconds;
        string r1 = prevQ == 0 ? "" : $"(×{q / prevQ:F1})", r2 = prevS == 0 ? "" : $"(×{srt / prevS:F1})";
        Console.WriteLine($"n = {n,6:N0}: all pairs O(n²) {q,8:F1} ms {r1,-7}   Array.Sort of {n * 100:N0} O(n log n) {srt,7:F1} ms {r2}");
        prevQ = q; prevS = srt;
    }
}

static void CasesDemo()
{
    int[] items = Enumerable.Range(1, 1_000).ToArray();
    var rnd = new Random(4);
    double avg = Enumerable.Range(0, 10_000).Average(_ => Cases.ComparisonsToFind(items, rnd.Next(1, 1_001)));
    Console.WriteLine($"Linear search in 1,000 items: best {Cases.ComparisonsToFind(items, 1)}, average {avg:F1}, worst {Cases.ComparisonsToFind(items, -1):N0} comparisons");
    int n = 1_000;
    long sorted = Cases.InsertionSortShifts(Enumerable.Range(0, n).ToArray());
    long random = Cases.InsertionSortShifts(Enumerable.Range(0, n).Select(_ => rnd.Next()).ToArray());
    long reversed = Cases.InsertionSortShifts(Enumerable.Range(0, n).Reverse().ToArray());
    Console.WriteLine($"Insertion sort of 1,000 items: sorted {sorted:N0}, random {random:N0}, reversed {reversed:N0} shifts");
}

static void Concat()
{
    Snippets.JoinSlow(["a", "b"]); Snippets.JoinFast(["a", "b"]);         // warm up
    foreach (int n in new[] { 5_000, 10_000, 20_000, 40_000 })
    {
        string[] words = Enumerable.Range(0, n).Select(i => $"task{i}").ToArray();
        var sw = Stopwatch.StartNew(); int a = Snippets.JoinSlow(words).Length; double slow = sw.Elapsed.TotalMilliseconds;
        sw.Restart(); int b = Snippets.JoinFast(words).Length; double fast = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"{n,6:N0} words: += {slow,8:F1} ms   StringBuilder {fast,5:F2} ms   (same length: {a == b})");
    }
}

static void SpaceDemo()
{
    int[] items = Enumerable.Range(0, 1_000_000).ToArray();
    Space.ReverseInPlace(items); Space.ReversedCopy(items);               // warm up
    long before = GC.GetAllocatedBytesForCurrentThread(); Space.ReverseInPlace(items);
    long inPlace = GC.GetAllocatedBytesForCurrentThread() - before;
    before = GC.GetAllocatedBytesForCurrentThread(); var copy = Space.ReversedCopy(items);
    long copied = GC.GetAllocatedBytesForCurrentThread() - before;
    Console.WriteLine($"Reverse 1,000,000 ints: in place allocated {inPlace:N0} bytes; a reversed copy allocated {copied:N0} bytes");
    GC.KeepAlive(copy);
}

static void Jit()
{
    int[] items = Enumerable.Range(0, 100).ToArray();
    for (int call = 1; call <= 4; call++)
    {
        var sw = Stopwatch.StartNew(); bool found = Searches.LinqAny(items, 99); double us = sw.Elapsed.TotalMicroseconds;
        Console.WriteLine($"call {call}: {us,9:F1} µs  (found: {found})");
    }
}

static void Setup()
{
    var rnd = new Random(42);
    int[] items = Enumerable.Range(0, 100_000).Select(_ => rnd.Next()).ToArray();
    for (int w = 0; w < 3; w++) { _ = items.Order().ToArray(); _ = new HashSet<int>(items); }      // warm up
    const int reps = 20;
    long before = GC.GetAllocatedBytesForCurrentThread(); var sw = Stopwatch.StartNew();
    for (int r = 0; r < reps; r++) { int[] sorted = (int[])items.Clone(); Array.Sort(sorted); }
    double sortMs = sw.Elapsed.TotalMilliseconds / reps; long sortBytes = (GC.GetAllocatedBytesForCurrentThread() - before) / reps;
    before = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
    for (int r = 0; r < reps; r++) { var set = new HashSet<int>(items); }
    double setMs = sw.Elapsed.TotalMilliseconds / reps; long setBytes = (GC.GetAllocatedBytesForCurrentThread() - before) / reps;
    Console.WriteLine($"100,000 ints (the array itself: {items.Length * sizeof(int):N0} bytes)");
    Console.WriteLine($"  sorted copy: {sortMs,6:F2} ms to build, {sortBytes,9:N0} bytes");
    Console.WriteLine($"  HashSet:     {setMs,6:F2} ms to build, {setBytes,9:N0} bytes");
}
