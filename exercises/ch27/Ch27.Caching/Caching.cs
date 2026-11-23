using Microsoft.Extensions.Caching.Memory;

namespace Ch27.Caching;

/// <summary>Something slow to ask: 50 ms per call. Counts the calls.</summary>
public class SlowPriceSource
{
    private readonly Dictionary<int, decimal> _prices = Enumerable.Range(1, 10).ToDictionary(i => i, i => 10m * i);
    public int Calls;
    public virtual async Task<decimal> PriceAsync(int id) { Calls++; await Task.Delay(50); return _prices[id]; }
    public decimal Peek(int id) => _prices[id];
    public void SetPrice(int id, decimal price) => _prices[id] = price;
}

/// <summary>Cache-aside: look in the cache; on a miss, ask the source and remember the answer for a while (TTL).</summary>
public class CachedPrices(IMemoryCache cache, SlowPriceSource source, TimeSpan ttl)
{
    private int _hits, _misses;
    public double HitRate => _hits + _misses == 0 ? 0 : (double)_hits / (_hits + _misses);

    public async Task<decimal> PriceAsync(int id)
    {
        if (cache.TryGetValue(Key(id), out decimal price)) { _hits++; return price; }
        _misses++;
        price = await source.PriceAsync(id);
        cache.Set(Key(id), price, ttl);
        return price;
    }

    /// <summary>Change the truth, then invalidate: the next read is a miss, and fresh.</summary>
    public Task UpdatePriceAsync(int id, decimal price)
    {
        source.SetPrice(id, price);
        cache.Remove(Key(id));
        return Task.CompletedTask;
    }

    private static string Key(int id) => $"price:{id}";
}

/// <summary>Capacity planning: how many copies, for the peak, with room to spare, plus one for failures.</summary>
public static class Capacity
{
    public record Result(double AverageRps, double PeakRps, double UsablePerInstance, int ForPeak, int WithSpare)
    {
        public override string ToString() =>
            $"average load:        {AverageRps,8:F1} requests/s\n" +
            $"peak load:           {PeakRps,8:F1} requests/s\n" +
            $"one copy, at target: {UsablePerInstance,8:F1} requests/s\n" +
            $"copies for the peak: {ForPeak,8}\n" +
            $"plus one spare:      {WithSpare,8}   (one copy can fail, or be updated, at the peak)";
    }

    public static Result Plan(int users, double requestsPerUserPerMinute, double peakFactor, double perInstanceRps, double targetUtilisation)
    {
        double average = users * requestsPerUserPerMinute / 60.0;
        double peak = average * peakFactor;
        double usable = perInstanceRps * targetUtilisation;          // never plan to run at 100%: latency explodes near it
        int forPeak = Math.Max(1, (int)Math.Ceiling(peak / usable));
        return new Result(average, peak, usable, forPeak, forPeak + 1);
    }
}
