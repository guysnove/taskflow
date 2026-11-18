using Ch03.Structures;

namespace Ch03.Structures.Tests;

public class ChainedHashMapTests
{
    [Fact]
    public void SetAndGet_ReplaceAndMissing()
    {
        var map = new ChainedHashMap<string, int>();
        map.Set("ana", 1); map.Set("ben", 2); map.Set("ana", 3);   // replace, not add
        Assert.Equal(2, map.Count);
        Assert.True(map.TryGet("ana", out int a)); Assert.Equal(3, a);
        Assert.False(map.TryGet("chloe", out _));
    }

    [Fact]
    public void Resizes_AndKeepsEveryKey()
    {
        var map = new ChainedHashMap<int, string>();
        for (int i = 0; i < 1000; i++) map.Set(i, $"v{i}");
        Assert.True(map.BucketCount >= 1000 * 4 / 3);                // load factor <= 0.75
        for (int i = 0; i < 1000; i++) { Assert.True(map.TryGet(i, out var v)); Assert.Equal($"v{i}", v); }
    }

    // A key type whose hash code is always the same: legal, but terrible.
    private record BadKey(int Value) { public override int GetHashCode() => 42; }

    [Fact]
    public void ABadHashCode_StillWorks_ButPutsEverythingInOneBucket()
    {
        var map = new ChainedHashMap<BadKey, int>();
        for (int i = 0; i < 200; i++) map.Set(new BadKey(i), i);
        Assert.True(map.TryGet(new BadKey(150), out int v)); Assert.Equal(150, v);   // still correct
        Assert.Equal(200, map.LongestBucket);                                          // but one long chain
    }
}
