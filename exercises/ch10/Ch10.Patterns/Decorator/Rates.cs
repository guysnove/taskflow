namespace Ch10.Patterns.Decorator;

// Problem: a slow lookup (say, exchange rates from a web service) is called far too often.
public interface IExchangeRates
{
    decimal Rate(string from, string to);
}

public sealed class SlowRates : IExchangeRates
{
    public int Calls { get; private set; }

    public decimal Rate(string from, string to)
    {
        Calls++;                                   // imagine a 300 ms web call here
        return (from, to) switch { ("EUR", "USD") => 1.08m, ("USD", "EUR") => 0.93m, _ => 1m };
    }
}

// DECORATOR: implements the same interface, and wraps another implementation to add caching.
// The callers, and SlowRates itself, don't change.
public sealed class CachingRates(IExchangeRates inner) : IExchangeRates
{
    private readonly Dictionary<(string, string), decimal> _cache = new();

    public decimal Rate(string from, string to)
    {
        if (!_cache.TryGetValue((from, to), out decimal rate))
        {
            rate = inner.Rate(from, to);
            _cache[(from, to)] = rate;
        }
        return rate;
    }
}
