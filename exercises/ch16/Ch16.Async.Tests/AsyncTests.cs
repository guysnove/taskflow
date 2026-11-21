using Ch16.Async;

namespace Ch16.Async.Tests;

public class AsyncTests
{
    [Fact]
    public async Task Importer_SkipsBadLines_AndRetriesTransientFaults()
    {
        var store = new FlakyStore(failEvery: 3);
        var log = new List<string>();
        int imported = await new Importer(store, log.Add).ImportAsync(["1,Lamp,2", "2,Chair,x", "3,Pen,1", "4,Ink,1"]);
        Assert.Equal(3, imported);                                       // every good line saved, despite a fault
        Assert.Contains(log, l => l.Contains("retrying", StringComparison.Ordinal));
        Assert.Contains(log, l => l.StartsWith("line 2: skipped", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ARaceLosesOrders_TheFixesDont()
    {
        Assert.NotEqual(0, await Stock.UnsafeAsync(100, 100));
        Assert.Equal(0, await Stock.WithSemaphoreAsync(100, 100));
        Assert.Equal(0, await Stock.WithInterlockedAsync(100, 100));
    }

    [Fact]
    public async Task Cancelling_StopsTheJob_AndCleanupStillRuns()
    {
        var log = new List<string>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Jobs.LongJobAsync(10, log.Add, cts.Token));
        Assert.StartsWith("cleanup:", log[^1], StringComparison.Ordinal);
        Assert.DoesNotContain("step 10 done", log);
    }

    [Fact]
    public async Task ATimeoutWithAToken_StopsTheWork()
    {
        var log = new List<string>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Jobs.SlowServiceAsync(TimeSpan.FromSeconds(1), log.Add, cts.Token));
        await Task.Delay(1200);
        Assert.Empty(log);                                               // it never "finished its work"
    }

    [Fact]
    public async Task Worker_SkipsABadJob_AndStopsCleanly()
    {
        var log = new List<string>();
        var worker = new ResizeWorker(m => { lock (log) log.Add(m); });
        using var stopping = new CancellationTokenSource();
        var running = worker.RunAsync(stopping.Token);
        worker.Enqueue("a.bad");
        worker.Enqueue("b.jpg");
        for (int i = 0; i < 250 && !log.Contains("resized b.jpg"); i++) await Task.Delay(20);   // up to 5 s; don't guess a fixed time
        await stopping.CancelAsync();
        await running;                                                   // no exception escapes
        Assert.Contains("resized b.jpg", log);
        Assert.Contains(log, l => l.StartsWith("FAILED a.bad", StringComparison.Ordinal));
    }
}
