using Ch27.Caching;
using Microsoft.Extensions.Caching.Memory;

public class CachingTests
{
    private readonly SlowPriceSource _source = new();
    private readonly CachedPrices _prices;
    public CachingTests() => _prices = new CachedPrices(new MemoryCache(new MemoryCacheOptions()), _source, TimeSpan.FromMinutes(5));

    [Fact]
    public async Task SecondRead_IsAHit()
    {
        await _prices.PriceAsync(1);
        await _prices.PriceAsync(1);
        Assert.Equal(1, _source.Calls);
        Assert.Equal(0.5, _prices.HitRate);
    }

    [Fact]
    public async Task AChangeBehindTheCache_IsStale()
    {
        await _prices.PriceAsync(1);
        _source.SetPrice(1, 99m);
        Assert.Equal(10m, await _prices.PriceAsync(1));          // the cache doesn't know
    }

    [Fact]
    public async Task AChangeThroughTheCache_IsFresh()
    {
        await _prices.PriceAsync(2);
        await _prices.UpdatePriceAsync(2, 42m);
        Assert.Equal(42m, await _prices.PriceAsync(2));
    }

    [Theory]
    [InlineData(2_000, 4, 3, 127.8, 0.6, 400.0, 6, 7)]        // TaskFlow's example
    [InlineData(10, 1, 1, 100, 0.5, 0.1666, 1, 2)]            // tiny: still one, plus a spare
    public void Capacity_Plan(int users, double perMinute, double peak, double perInstance, double target, double expectedPeak, int forPeak, int withSpare)
    {
        var plan = Capacity.Plan(users, perMinute, peak, perInstance, target);
        Assert.Equal(expectedPeak, plan.PeakRps, 3);
        Assert.Equal((forPeak, withSpare), (plan.ForPeak, plan.WithSpare));
    }
}
