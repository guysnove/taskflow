using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ch29.Orders;

// Chapter 29: this service's own signals. A Meter (metrics) and an ActivitySource (traces), both named "Ch29.Orders".
public sealed class OrderMetrics
{
    public const string Name = "Ch29.Orders";
    public static readonly ActivitySource Traces = new(Name);

    private readonly Counter<long> _placed;
    private readonly Counter<long> _rejected;
    private readonly Histogram<double> _duration;

    public OrderMetrics(IMeterFactory meters)
    {
        var meter = meters.Create(Name);
        _placed = meter.CreateCounter<long>("orders.placed", "{order}", "Orders placed");
        _rejected = meter.CreateCounter<long>("orders.rejected", "{order}", "Orders refused: not enough stock");
        _duration = meter.CreateHistogram<double>("orders.duration", "s", "How long placing an order took");
    }

    public void Placed(string item, TimeSpan took)
    {
        _placed.Add(1, new KeyValuePair<string, object?>("order.item", item));
        _duration.Record(took.TotalSeconds);
    }

    public void Rejected(string item) => _rejected.Add(1, new KeyValuePair<string, object?>("order.item", item));
}

public record OrderRequest(string Item, int Quantity);
public record StockLevel(string Item, int Available);
