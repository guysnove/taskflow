namespace Ch03.Structures;

// Chapter 3: a hash table with separate chaining, built from scratch.
// The hash code picks a bucket; each bucket is a small list of (key, value) entries.
public class ChainedHashMap<TKey, TValue> where TKey : notnull
{
    private List<KeyValuePair<TKey, TValue>>[] _buckets = NewBuckets(8);
    public int Count { get; private set; }
    public int BucketCount => _buckets.Length;

    private int BucketOf(TKey key, int bucketCount) =>
        (key.GetHashCode() & 0x7FFFFFFF) % bucketCount;   // non-negative, then fold into range

    public void Set(TKey key, TValue value)               // O(1) on average
    {
        var bucket = _buckets[BucketOf(key, _buckets.Length)];
        for (int i = 0; i < bucket.Count; i++)
        {
            if (bucket[i].Key.Equals(key)) { bucket[i] = new(key, value); return; }   // replace
        }
        bucket.Add(new(key, value));
        Count++;
        if (Count > _buckets.Length * 3 / 4) Resize();     // keep buckets short: load factor 0.75
    }

    public bool TryGet(TKey key, out TValue value)         // O(1) on average
    {
        foreach (var entry in _buckets[BucketOf(key, _buckets.Length)])
        {
            if (entry.Key.Equals(key)) { value = entry.Value; return true; }
        }
        value = default!;
        return false;
    }

    // The longest chain: 1 or 2 with a good hash; Count with a terrible one.
    public int LongestBucket => _buckets.Max(b => b.Count);

    private void Resize()
    {
        var bigger = NewBuckets(_buckets.Length * 2);
        foreach (var bucket in _buckets)
            foreach (var entry in bucket)
                bigger[BucketOf(entry.Key, bigger.Length)].Add(entry);   // every key moves
        _buckets = bigger;
    }

    private static List<KeyValuePair<TKey, TValue>>[] NewBuckets(int n) =>
        Enumerable.Range(0, n).Select(_ => new List<KeyValuePair<TKey, TValue>>()).ToArray();
}
