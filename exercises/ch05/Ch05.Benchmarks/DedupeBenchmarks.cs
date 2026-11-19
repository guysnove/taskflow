using BenchmarkDotNet.Attributes;
using Ch05.Efficiency;

// "Benchmarks alone aren't enough": at small n, the O(n²) version looks perfectly fine.
[MemoryDiagnoser]
public class DedupeBenchmarks
{
    [Params(10, 100, 1_000)]
    public int N;
    private string[] _emails = [];

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(N);
        _emails = Enumerable.Range(0, N).Select(_ => $"user{rnd.Next(N * 4 / 5 + 1)}@example.com").ToArray();
    }

    [Benchmark(Baseline = true)] public int WithList() => Dedupe.WithList(_emails).Count;
    [Benchmark] public int WithSet() => Dedupe.WithSet(_emails).Count;
}
