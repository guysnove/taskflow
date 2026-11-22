// Chapter 26: micro-benchmarks with BenchmarkDotNet. It warms up, runs each method many times, and reports the mean,
// the spread (StdDev) and the memory allocated, so you compare numbers, not hunches. Always in Release:
//   dotnet run -c Release --project Ch26.Bench -- --filter '*'
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Text;

BenchmarkSwitcher.FromAssembly(typeof(Building).Assembly).Run(args);

/// <summary>Building one string from 1,000 task titles.</summary>
[MemoryDiagnoser]
public class Building
{
    private readonly string[] _titles = Enumerable.Range(1, 1000).Select(i => $"Task {i}").ToArray();

    [Benchmark(Baseline = true)]
    public string PlusEquals()                 // each += copies everything so far into a NEW string
    {
        string s = "";
        foreach (var t in _titles) s += t + ", ";
        return s;
    }

    [Benchmark]
    public string StringBuilder()              // appends into one growing buffer
    {
        var sb = new StringBuilder();
        foreach (var t in _titles) sb.Append(t).Append(", ");
        return sb.ToString();
    }

    [Benchmark]
    public string Join() => string.Join(", ", _titles);
}

/// <summary>Is this id one of 10,000? Asked 1,000 times.</summary>
[MemoryDiagnoser]
public class Lookup
{
    private readonly List<int> _list = Enumerable.Range(0, 10_000).ToList();
    private readonly HashSet<int> _set = Enumerable.Range(0, 10_000).ToHashSet();
    private readonly int[] _wanted = Enumerable.Range(0, 1000).Select(i => i * 10).ToArray();

    [Benchmark(Baseline = true)]
    public int ListContains()                  // looks at items one by one: O(n) per question
    {
        int found = 0;
        foreach (int id in _wanted) if (_list.Contains(id)) found++;
        return found;
    }

    [Benchmark]
    public int HashSetContains()               // jumps straight to the item's bucket: O(1) per question
    {
        int found = 0;
        foreach (int id in _wanted) if (_set.Contains(id)) found++;
        return found;
    }
}
