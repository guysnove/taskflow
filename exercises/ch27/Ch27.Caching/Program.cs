// Chapter 27: caching, measured.
//   dotnet run --project Ch27.Caching -- cache       cache-aside over a slow source: speed, hit rate, staleness, invalidation
//   dotnet run --project Ch27.Caching -- capacity    how many copies of an app for a given load (capacity planning)
using System.Diagnostics;
using Ch27.Caching;
using Microsoft.Extensions.Caching.Memory;

if (args.FirstOrDefault() == "capacity")
{
    // TaskFlow, Chapter 26: one copy (2 CPUs) served 127.8 requests/s on the task list with its CPUs full.
    var plan = Capacity.Plan(users: 2_000, requestsPerUserPerMinute: 4, peakFactor: 3, perInstanceRps: 127.8, targetUtilisation: 0.6);
    Console.WriteLine(plan);
    return;
}

var source = new SlowPriceSource();                       // 50 ms per call, like a remote service or a heavy query
int[] lookups = Enumerable.Range(0, 200).Select(i => i % 10 + 1).ToArray();   // 200 lookups of 10 products

var sw = Stopwatch.StartNew();
foreach (int id in lookups) await source.PriceAsync(id);
Console.WriteLine($"no cache:        {sw.ElapsedMilliseconds,5} ms   source calls: {source.Calls}");

source.Calls = 0;
using var memory = new MemoryCache(new MemoryCacheOptions());
var prices = new CachedPrices(memory, source, ttl: TimeSpan.FromSeconds(1));
sw.Restart();
foreach (int id in lookups) await prices.PriceAsync(id);
Console.WriteLine($"cache-aside:     {sw.ElapsedMilliseconds,5} ms   source calls: {source.Calls}   hit rate: {prices.HitRate:P0}");

source.SetPrice(1, 99m);                                  // the truth changes, behind the cache's back
Console.WriteLine($"after a change:  cached {await prices.PriceAsync(1)}, source {source.Peek(1)}   ← STALE");
await Task.Delay(1100);                                   // the time-to-live runs out
Console.WriteLine($"after the TTL:   cached {await prices.PriceAsync(1)}   ← fresh again (but stale for up to 1 s)");

await prices.UpdatePriceAsync(2, 42m);                    // write through our own code: update, THEN remove the entry
Console.WriteLine($"with invalidation: cached {await prices.PriceAsync(2)}, source {source.Peek(2)}   ← fresh at once");
