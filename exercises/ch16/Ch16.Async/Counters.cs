namespace Ch16.Async;

// A race condition: 100 orders arrive at the same moment, and each takes one item from stock.
// Each order reads the stock, waits for something (a database call, here Task.Delay), then writes stock - 1.
// While one order waits, the others read the SAME old value: their writes overwrite each other.
public static class Stock
{
    public static async Task<int> UnsafeAsync(int start, int orders)
    {
        int stock = start;
        await Task.WhenAll(Enumerable.Range(0, orders).Select(async _ =>
        {
            int seen = stock;                 // read
            await Task.Delay(1);              // … an await in the middle …
            stock = seen - 1;                 // write: may overwrite another order's write
        }));
        return stock;
    }

    // An async lock. (C#'s lock statement can't contain an await, so SemaphoreSlim(1) is the usual tool.)
    public static async Task<int> WithSemaphoreAsync(int start, int orders)
    {
        int stock = start;
        using var gate = new SemaphoreSlim(1, 1);
        await Task.WhenAll(Enumerable.Range(0, orders).Select(async _ =>
        {
            await gate.WaitAsync();           // one order at a time inside
            try
            {
                int seen = stock;
                await Task.Delay(1);
                stock = seen - 1;
            }
            finally
            {
                gate.Release();               // always, even after an exception
            }
        }));
        return stock;
    }

    // No await needed in the middle? Then make the change one atomic step.
    public static async Task<int> WithInterlockedAsync(int start, int orders)
    {
        int stock = start;
        await Task.WhenAll(Enumerable.Range(0, orders).Select(async _ =>
        {
            await Task.Delay(1);              // the slow part happens first…
            Interlocked.Decrement(ref stock); // …then one indivisible step: read, subtract, write
        }));
        return stock;
    }
}
