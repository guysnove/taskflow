using System.Diagnostics;
using Ch16.Async;

string mode = args.Length > 0 ? args[0] : "exceptions";
var clock = Stopwatch.StartNew();
void Say(string text) => Console.WriteLine($"[{clock.Elapsed.TotalSeconds,4:0.0} s] {text}");

switch (mode)
{
    case "exceptions":
    {
        var store = new FlakyStore(failEvery: 3);
        var importer = new Importer(store, Say);
        string[] lines = ["1001,Desk lamp,2", "1002,Chair,x", "1003,Notebook,5", "oops", "1004,Pen,10", "1005,Stapler,1"];
        int imported = await importer.ImportAsync(lines);
        Say($"imported {imported} of {lines.Length} lines");

        Say("now a bug: a null where the code never expected one");
        try
        {
            string? product = null;
            Say($"length: {product!.Length}");                               // a BUG, not bad input
        }
        catch (NullReferenceException ex)
        {
            // Shown here only for the demo. In real code: don't catch it — fix it. Let it reach the top-level handler.
            Say($"{ex.GetType().Name}: {ex.Message}");
        }
        break;
    }
    case "blocking":
    {
        ThreadPool.GetMinThreads(out int minWorkers, out _);
        Say($"{Environment.ProcessorCount} CPU cores; the thread pool starts with {minWorkers} worker threads");
        int peak = 0;
        void Watch() => peak = Math.Max(peak, ThreadPool.ThreadCount);

        clock.Restart(); peak = 0;
        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => { Watch(); Thread.Sleep(200); })));
        Say($"100 waits with Thread.Sleep (blocking): {clock.Elapsed.TotalSeconds:0.0} s, up to {peak} threads");

        clock.Restart(); peak = 0;
        await Task.WhenAll(Enumerable.Range(0, 100).Select(async _ => { Watch(); await Task.Delay(200); }));
        Say($"100 waits with await Task.Delay:          {clock.Elapsed.TotalSeconds:0.0} s, up to {peak} threads");
        break;
    }
    case "whenall":
    {
        clock.Restart();
        foreach (int id in new[] { 1, 2, 3, 4, 5 }) await Jobs.SlowServiceAsync(TimeSpan.FromMilliseconds(400), _ => { });
        Say("five calls, one after another (await each)");
        clock.Restart();
        await Task.WhenAll(new[] { 1, 2, 3, 4, 5 }.Select(_ => Jobs.SlowServiceAsync(TimeSpan.FromMilliseconds(400), _ => { })));
        Say("five calls, at the same time (Task.WhenAll)");
        break;
    }
    case "cancel":
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(700));                     // e.g. the user closed the page
        try
        {
            await Jobs.LongJobAsync(10, Say, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Say("OperationCanceledException: the job was cancelled — not an error, nothing to log as one");
        }
        break;
    }
    case "timeout":
    {
        Say("A: WaitAsync(1 s) — stop WAITING after 1 second");
        try
        {
            await Jobs.SlowServiceAsync(TimeSpan.FromSeconds(3), Say).WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (TimeoutException) { Say("A: TimeoutException — but did the slow work stop?"); }
        await Task.Delay(TimeSpan.FromSeconds(2.5));                         // watch what happens in the background

        clock.Restart();
        Say("B: a token that cancels after 1 s — stop the WORK after 1 second");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await Jobs.SlowServiceAsync(TimeSpan.FromSeconds(3), Say, cts.Token);
        }
        catch (OperationCanceledException) { Say("B: cancelled — the slow work itself stopped"); }
        await Task.Delay(TimeSpan.FromSeconds(2.5));
        Say("B: (nothing else happened)");
        break;
    }
    case "race":
    {
        Say("100 items in stock; 100 orders arrive at the same moment; each takes one. Right answer: 0 left");
        for (int run = 1; run <= 3; run++)
        {
            clock.Restart();
            Say($"read, await, write   (run {run}): {await Stock.UnsafeAsync(100, 100),3} left");
        }
        clock.Restart();
        Say($"with SemaphoreSlim(1)         : {await Stock.WithSemaphoreAsync(100, 100),3} left");
        clock.Restart();
        Say($"with Interlocked.Decrement    : {await Stock.WithInterlockedAsync(100, 100),3} left");
        break;
    }
    case "propagation":
    {
        static async Task FailAsync(string message, int ms) { await Task.Delay(ms); throw new InvalidOperationException(message); }

        try { await FailAsync("disk full", 10); }
        catch (InvalidOperationException ex) { Say($"await:  caught {ex.GetType().Name}: {ex.Message}"); }

        try { FailAsync("disk full", 10).Wait(); }
        catch (AggregateException ex) { Say($".Wait(): caught {ex.GetType().Name} around {ex.InnerException!.GetType().Name}"); }

        var both = Task.WhenAll(FailAsync("first failure", 20), FailAsync("second failure", 10));
        try { await both; }
        catch (InvalidOperationException ex) { Say($"WhenAll: await shows ONE: \"{ex.Message}\"; the task holds {both.Exception!.InnerExceptions.Count}: {string.Join(", ", both.Exception.InnerExceptions.Select(e => e.Message))}"); }

        _ = FailAsync("nobody is listening", 10);                            // fire and forget
        await Task.Delay(100);
        Say("fire-and-forget: its exception happened — and nothing was printed. It's simply lost.");
        break;
    }
    case "worker":
    {
        var worker = new ResizeWorker(Say);
        using var stopping = new CancellationTokenSource();
        var running = worker.RunAsync(stopping.Token);
        foreach (string photo in new[] { "beach.jpg", "cat.jpg", "scan.bad", "party.jpg", "map.jpg", "dog.jpg", "sky.jpg", "tree.jpg" })
        {
            worker.Enqueue(photo);
        }
        Say($"8 photos queued; the requests returned at once. Waiting: {worker.Waiting}");
        await Task.Delay(700);
        Say("the app is stopping: cancel the worker");
        await stopping.CancelAsync();
        await running;
        Say("worker stopped cleanly");
        break;
    }
    default:
        Console.WriteLine("modes: exceptions | blocking | whenall | cancel | timeout | race | propagation | worker");
        break;
}
