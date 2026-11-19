using BenchmarkDotNet.Attributes;
using Ch05.Efficiency;

// The practical: one question ("is this id here?") answered six ways, at three sizes.
[MemoryDiagnoser]
public class SearchBenchmarks
{
    [Params(10, 1_000, 100_000)]
    public int N;

    private int[] _items = [], _sorted = [];
    private List<int> _list = [];
    private HashSet<int> _set = [];
    private int _target;

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(42);
        _items = Enumerable.Range(0, N).Select(_ => rnd.Next()).ToArray();
        _sorted = _items.Order().ToArray();
        _list = [.. _items];
        _set = [.. _items];
        _target = _items[N * 3 / 4];          // present, three quarters of the way along
    }

    [Benchmark(Baseline = true)] public bool Loop() => Searches.Loop(_items, _target);
    [Benchmark] public bool LinqAny() => Searches.LinqAny(_items, _target);
    [Benchmark] public bool ListContains() => Searches.ListContains(_list, _target);
    [Benchmark] public bool BinarySearch() => Searches.Binary(_sorted, _target);
    [Benchmark] public bool SortThenBinary() => Searches.SortThenBinary(_items, _target);
    [Benchmark] public bool HashSet() => Searches.Set(_set, _target);
}
