using System.Diagnostics.Metrics;
using Ch29.Orders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Ch29.Tests;

// Metrics are code: test them like code. MetricCollector records what an instrument measured.
public class OrderMetricsTests
{
    private static readonly IMeterFactory Meters = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();

    [Fact]
    public void APlacedOrder_IsCounted_WithItsItem_AndTimed()
    {
        var metrics = new OrderMetrics(Meters);
        using var placed = new MetricCollector<long>(Meters, OrderMetrics.Name, "orders.placed");
        using var duration = new MetricCollector<double>(Meters, OrderMetrics.Name, "orders.duration");

        metrics.Placed("chair", TimeSpan.FromMilliseconds(250));

        var m = Assert.Single(placed.GetMeasurementSnapshot());
        Assert.Equal((1L, "chair"), (m.Value, (string)m.Tags["order.item"]!));
        Assert.Equal(0.25, Assert.Single(duration.GetMeasurementSnapshot()).Value);
    }

    [Fact]
    public void ARejectedOrder_IsCountedSeparately()
    {
        var metrics = new OrderMetrics(Meters);
        using var placed = new MetricCollector<long>(Meters, OrderMetrics.Name, "orders.placed");
        using var rejected = new MetricCollector<long>(Meters, OrderMetrics.Name, "orders.rejected");

        metrics.Rejected("desk");

        Assert.Empty(placed.GetMeasurementSnapshot());
        Assert.Equal(1, rejected.GetMeasurementSnapshot().EvaluateAsCounter());
    }
}
