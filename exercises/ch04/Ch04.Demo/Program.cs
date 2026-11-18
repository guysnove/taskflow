// Chapter 4 demos. Rough stopwatch timings on one machine (Chapter 5 measures properly).
//   dotnet run -- overflow N           recursion N levels deep (a large N crashes the process)
//   dotnet run -c Release -- bst       tree height: random vs sorted insertion
//   dotnet run -c Release -- sort      insertion sort vs merge sort vs Array.Sort
//   dotnet run -c Release -- search    linear vs binary search
//   dotnet run -- plan                 task dependencies: order, then a cycle
//   dotnet run -- queue                PriorityQueue: the most urgent task first
//   dotnet run -c Release -- fib       naive recursion vs memoisation
//   dotnet run -- stable               is the sort stable? Array.Sort vs OrderBy
//   dotnet run -- tree PATH            a real folder tree, walked recursively
//   dotnet run -c Release -- range     "due in the next 7 days": scan a List vs SortedSet.GetViewBetween
using System.Diagnostics;
using Ch04.Algorithms;

switch (args.FirstOrDefault())
{
    case "overflow":
        int n = int.Parse(args[1]);
        Console.WriteLine($"Recursing {n:N0} levels deep...");
        Console.WriteLine($"Reached depth {Recursion.Depth(n):N0}");
        break;
    case "bst": Bst(); break;
    case "sort": Sort(); break;
    case "search": Search(); break;
    case "plan": Plan(); break;
    case "queue": Urgent(); break;
    case "fib": Fib(); break;
    case "stable": Stable(); break;
    case "tree": Tree(args.Length > 1 ? args[1] : ".."); break;
    case "range": Range(); break;
    default: Console.WriteLine("Usage: dotnet run -- overflow N | bst | sort | search | plan | queue | fib | stable | tree PATH | range"); break;
}

static void Bst()
{
    const int n = 10_000;
    var rnd = new Random(5);
    var random = new BinarySearchTree(); var sorted = new BinarySearchTree();
    foreach (int k in Enumerable.Range(0, n).OrderBy(_ => rnd.Next())) random.Add(k);
    for (int k = 0; k < n; k++) sorted.Add(k);
    Console.WriteLine($"{n:N0} keys. Height, random order: {random.Height}   sorted order: {sorted.Height:N0}");
    foreach (var (name, tree) in new[] { ("random", random), ("sorted", sorted) })
    {
        var sw = Stopwatch.StartNew(); int found = 0;
        for (int k = 0; k < n; k++) if (tree.Contains(k)) found++;
        Console.WriteLine($"  {n:N0} look-ups in the {name} tree: {sw.Elapsed.TotalMilliseconds,8:F1} ms ({found:N0} found)");
    }
}

static void Sort()
{
    foreach (int n in new[] { 10_000, 40_000 })
    {
        var rnd = new Random(n);
        int[] data = Enumerable.Range(0, n).Select(_ => rnd.Next()).ToArray();
        int[] a = (int[])data.Clone(), c = (int[])data.Clone();
        var sw = Stopwatch.StartNew(); SearchSort.InsertionSort(a); double ins = sw.Elapsed.TotalMilliseconds;
        sw.Restart(); int[] b = SearchSort.MergeSort(data); double merge = sw.Elapsed.TotalMilliseconds;
        sw.Restart(); Array.Sort(c); double builtin = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"{n,6:N0} numbers: insertion {ins,8:F1} ms   merge {merge,6:F1} ms   Array.Sort {builtin,5:F1} ms   (agree: {a.SequenceEqual(b) && b.SequenceEqual(c)})");
    }
}

static void Search()
{
    const int n = 1_000_000;
    int[] sorted = Enumerable.Range(0, n).Select(i => i * 2).ToArray();     // even numbers
    var rnd = new Random(2);
    int[] targets = Enumerable.Range(0, 1_000).Select(_ => rnd.Next(0, 2 * n)).ToArray();
    SearchSort.LinearSearch(sorted, 1); SearchSort.BinarySearch(sorted, 1);  // warm up
    var sw = Stopwatch.StartNew(); int f1 = targets.Count(t => SearchSort.LinearSearch(sorted, t) >= 0); double lin = sw.Elapsed.TotalMilliseconds;
    sw.Restart(); int f2 = targets.Count(t => SearchSort.BinarySearch(sorted, t) >= 0); double bin = sw.Elapsed.TotalMilliseconds;
    Console.WriteLine($"1,000 searches in {n:N0} sorted numbers: linear {lin:F1} ms, binary {bin:F3} ms (found {f1} / {f2})");
}

static void Plan()
{
    var g = new Graph();
    g.AddEdge("design", "build"); g.AddEdge("build", "test"); g.AddEdge("test", "deploy");
    g.AddEdge("build", "docs"); g.AddEdge("docs", "deploy");
    Console.WriteLine("Order:  " + string.Join(" -> ", g.TopologicalOrder()!));
    Console.WriteLine("Cycle:  " + (g.FindCycle() is { } c1 ? string.Join(" -> ", c1) : "none"));
    g.AddEdge("deploy", "build");
    Console.WriteLine("After adding deploy -> build:");
    Console.WriteLine("Order:  " + (g.TopologicalOrder() is { } o ? string.Join(" -> ", o) : "none possible"));
    Console.WriteLine("Cycle:  " + string.Join(" -> ", g.FindCycle()!));
}

static void Urgent()
{
    var queue = new PriorityQueue<string, (int Priority, DateOnly Due)>();
    queue.Enqueue("Write report", (2, new DateOnly(2026, 11, 20)));
    queue.Enqueue("Fix login bug", (1, new DateOnly(2026, 11, 19)));
    queue.Enqueue("Plan team lunch", (3, new DateOnly(2026, 11, 30)));
    queue.Enqueue("Renew licence", (1, new DateOnly(2026, 11, 18)));
    while (queue.TryDequeue(out var task, out var p))
        Console.WriteLine($"priority {p.Priority}, due {p.Due:yyyy-MM-dd}: {task}");
}

static void Fib()
{
    foreach (int n in new[] { 30, 35, 40 })
    {
        var sw = Stopwatch.StartNew(); long a = Recursion.FibNaive(n); double naive = sw.Elapsed.TotalMilliseconds;
        sw.Restart(); long b = Recursion.FibMemo(n); double memo = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"Fib({n}) = {a,12:N0}   naive {naive,9:F1} ms   memoised {memo,6:F3} ms   (agree: {a == b})");
    }
}

static void Stable()
{
    // 40 tasks, added in order 1..40; only 3 different due days, so there are many ties.
    var tasks = Enumerable.Range(1, 40).Select(i => (Added: i, Due: (i * 7) % 3)).ToArray();
    var arraySorted = ((int Added, int Due)[])tasks.Clone();
    Array.Sort(arraySorted, (a, b) => a.Due.CompareTo(b.Due));
    var linqSorted = tasks.OrderBy(t => t.Due).ToArray();
    static bool KeepsOrder((int Added, int Due)[] x) =>
        x.Zip(x.Skip(1)).All(p => p.First.Due != p.Second.Due || p.First.Added < p.Second.Added);
    Console.WriteLine("Array.Sort, due day 0: " + string.Join(" ", arraySorted.Where(t => t.Due == 0).Select(t => t.Added)));
    Console.WriteLine("OrderBy,    due day 0: " + string.Join(" ", linqSorted.Where(t => t.Due == 0).Select(t => t.Added)));
    Console.WriteLine($"Ties kept in their original order?  Array.Sort: {KeepsOrder(arraySorted)}   OrderBy: {KeepsOrder(linqSorted)}");
}

static void Tree(string path)
{
    var root = new DirectoryInfo(path);
    int files = 0, folders = 0, deepest = 0;
    long Walk(DirectoryInfo dir, int depth)                     // recursion over a real folder tree
    {
        folders++; deepest = Math.Max(deepest, depth);
        long size = 0;
        foreach (var f in dir.EnumerateFiles()) { files++; size += f.Length; }
        foreach (var d in dir.EnumerateDirectories())
            if (d.Name is not ("bin" or "obj" or ".git")) size += Walk(d, depth + 1);
        return size;
    }
    long total = Walk(root, 0);
    Console.WriteLine($"{root.Name}: {files:N0} files in {folders:N0} folders, {total / 1024.0:N0} KB, deepest folder {deepest} levels down (bin, obj, .git skipped)");
}

static void Range()
{
    const int n = 200_000, queries = 1_000;
    var rnd = new Random(7);
    var tasks = Enumerable.Range(1, n).Select(id => (Due: rnd.Next(0, 3650), Id: id)).ToList();   // due within ~10 years
    var byDue = new SortedSet<(int Due, int Id)>(tasks);                                           // sorted by Due, then Id
    long a = 0, b = 0;
    var sw = Stopwatch.StartNew();
    for (int day = 0; day < queries; day++)
        a += tasks.Count(t => t.Due >= day && t.Due < day + 7);                                    // check every task
    double scan = sw.Elapsed.TotalMilliseconds;
    sw.Restart();
    for (int day = 0; day < queries; day++)
        b += byDue.GetViewBetween((day, int.MinValue), (day + 6, int.MaxValue)).Count;             // walk only the range
    double tree = sw.Elapsed.TotalMilliseconds;
    Console.WriteLine($"{n:N0} tasks, {queries:N0} queries \"due in the next 7 days\" ({a / queries:N0} matches on average)");
    Console.WriteLine($"  List + Where (check every task):   {scan,8:F1} ms");
    Console.WriteLine($"  SortedSet.GetViewBetween:          {tree,8:F1} ms   (same answers: {a == b})");
}
