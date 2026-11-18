// Chapter 3 demos. Rough stopwatch timings on one machine (Chapter 5 measures properly).
//   dotnet run -- capacity              how List<T> grows
//   dotnet run -c Release -- locality   summing a List vs a LinkedList
//   dotnet run -c Release -- search     the task search: List vs Dictionary
//   dotnet run -c Release -- hash       a good vs a terrible GetHashCode
//   dotnet run -- hashcode              string hash codes change from run to run
//   dotnet run -- modify                changing a List while looping over it
using System.Diagnostics;
using Ch03.Structures;
using Ch03.Structures.Search;

switch (args.FirstOrDefault())
{
    case "capacity": Capacity(); break;
    case "locality": Locality(); break;
    case "search": Search(); break;
    case "hash": Hash(); break;
    case "modify": Modify(); break;
    case "hashcode": Console.WriteLine($"\"login\".GetHashCode() = {"login".GetHashCode()}   42.GetHashCode() = {42.GetHashCode()}"); break;
    default: Console.WriteLine("Usage: dotnet run -- capacity | locality | search | hash | hashcode | modify"); break;
}

static void Modify()
{
    var tasks = new List<string> { "write report", "DONE: call Ana", "fix bug", "DONE: plan demo" };
    try
    {
        foreach (var t in tasks)
            if (t.StartsWith("DONE")) tasks.Remove(t);          // changing the list we're looping over
    }
    catch (InvalidOperationException e) { Console.WriteLine($"{e.GetType().Name}: {e.Message}"); }

    tasks.RemoveAll(t => t.StartsWith("DONE"));               // the safe way: one call, one pass
    Console.WriteLine("after RemoveAll: " + string.Join(", ", tasks));
}

static void Capacity()
{
    var list = new List<int>();
    int last = -1;
    Console.WriteLine($"empty list: Count {list.Count}, Capacity {list.Capacity}");
    for (int i = 1; i <= 40; i++)
    {
        list.Add(i);
        if (list.Capacity != last) Console.WriteLine($"after Add #{i,2}: Count {list.Count,2}, Capacity {list.Capacity,2}");
        last = list.Capacity;
    }
}

static void Locality()
{
    const int n = 5_000_000;
    var list = new List<int>(Enumerable.Range(0, n));
    var linked = new LinkedList<int>(Enumerable.Range(0, n));
    Sum(list); Sum(linked);                                     // warm up
    for (int round = 1; round <= 3; round++)
    {
        var sw = Stopwatch.StartNew(); long a = Sum(list); double ms1 = sw.Elapsed.TotalMilliseconds;
        sw.Restart(); long b = Sum(linked); double ms2 = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"round {round}: List {ms1,6:F1} ms   LinkedList {ms2,6:F1} ms   (same sum: {a == b})");
    }
    static long Sum(IEnumerable<int> items) { long s = 0; foreach (int x in items) s += x; return s; }
}

static void Search()
{
    string[] words = ["fix", "write", "report", "login", "bug", "call", "plan", "review", "update", "invoice",
                      "client", "demo", "budget", "email", "server", "backup", "design", "test", "deploy", "meeting"];
    var rnd = new Random(1);
    const int n = 100_000;
    var tasks = Enumerable.Range(1, n).Select(i => new TaskRecord(i,
        $"{words[rnd.Next(words.Length)]} {words[rnd.Next(words.Length)]} #{i}", rnd.Next(4) == 0)).ToList();

    long before = GC.GetTotalMemory(true);
    var sw = Stopwatch.StartNew();
    var indexed = new IndexedTaskStore(tasks);
    double buildMs = sw.Elapsed.TotalMilliseconds;
    long indexBytes = GC.GetTotalMemory(true) - before;
    var list = new ListTaskStore(tasks);

    var ids = Enumerable.Range(0, 1_000).Select(_ => rnd.Next(1, n + 1)).ToArray();
    Run("warm-up", list, indexed, ids[..10], words[..2]);
    Console.WriteLine($"{n:N0} tasks. Building the indexed store: {buildMs:F0} ms, about {indexBytes / 1_048_576.0:F1} MB extra memory");
    Run("1,000 look-ups by id + 20 word searches", list, indexed, ids, words);

    static void Run(string title, ITaskStore list, ITaskStore indexed, int[] ids, string[] words)
    {
        foreach (var (name, store) in new[] { ("List      ", list), ("Dictionary", indexed) })
        {
            var sw = Stopwatch.StartNew(); int found = 0;
            foreach (int id in ids) if (store.FindById(id) is not null) found++;
            double idMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart(); int hits = 0;
            foreach (string w in words) hits += store.SearchWord(w).Count;
            double wordMs = sw.Elapsed.TotalMilliseconds;
            if (title != "warm-up")
                Console.WriteLine($"  {name}: by id {idMs,8:F2} ms ({found} found)   by word {wordMs,8:F2} ms ({hits:N0} hits)");
        }
    }
}

static void Hash()
{
    const int n = 20_000;
    foreach (var (name, make) in new (string, Func<int, object>)[] { ("good GetHashCode", i => new GoodKey(i)), ("GetHashCode = 42", i => new BadKey(i)) })
    {
        var sw = Stopwatch.StartNew();
        var set = new HashSet<object>();
        for (int i = 0; i < n; i++) set.Add(make(i));
        bool found = set.Contains(make(n / 2));
        Console.WriteLine($"{name}: {n:N0} adds + 1 lookup = {sw.Elapsed.TotalMilliseconds,8:F1} ms (found: {found})");
    }
}

record GoodKey(int Value);
record BadKey(int Value) { public override int GetHashCode() => 42; }
