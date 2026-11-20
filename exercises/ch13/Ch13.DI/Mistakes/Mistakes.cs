namespace Ch13.DI.Mistakes;

// MISTAKE 1: a CAPTIVE DEPENDENCY. A singleton keeps the first scoped object it was given — forever.
public sealed class ShoppingBasket                          // meant to be scoped: one per request/user
{
    public List<string> Items { get; } = new();
}

public sealed class CheckoutService(ShoppingBasket basket)  // registered as a singleton by mistake
{
    public void Add(string item) => basket.Items.Add(item);
    public IReadOnlyList<string> Items => basket.Items;
}

// MISTAKE 2: state in a singleton. "Who is the current user?" stored in a field shared by everyone.
public sealed class CurrentUserHolder                      // registered as a singleton by mistake
{
    public string? Name { get; set; }
}

public sealed class GreetingService(CurrentUserHolder user)
{
    public async Task<string> GreetAsync(string signedInAs, TimeSpan work)
    {
        user.Name = signedInAs;                            // "remember who this request is for"
        await Task.Delay(work);                            // …do some work (a database call, say)
        return $"Hello, {user.Name}";                      // …and read it back
    }
}

// MISTAKE 3: a disposable TRANSIENT resolved from the ROOT provider is kept until the app ends.
public sealed class ReportFile : IDisposable
{
    public static int Open;
    public ReportFile() => Interlocked.Increment(ref Open);
    public void Dispose() => Interlocked.Decrement(ref Open);
}
