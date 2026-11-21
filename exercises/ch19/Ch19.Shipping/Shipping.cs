namespace Ch19.Shipping;

public record Order(decimal Subtotal, bool Express = false, decimal WeightKg = 0m, string? Postcode = null);

// REFACTOR: the cost is a base price plus a list of extras; each rule is one small, named method.
// The tests didn't change, and stayed green, so the behaviour didn't either.
public static class ShippingCalculator
{
    public const decimal Standard = 4.99m;
    public const decimal FreeFrom = 50m;
    public const decimal ExpressExtra = 10m;
    public const decimal HeavyFromKg = 20m;
    public const decimal PerExtraKg = 1m;
    public const decimal RemoteExtra = 5m;
    private static readonly string[] RemotePrefixes = ["HS", "ZE"];

    public static decimal Cost(Order order) =>
        BasePrice(order) + Express(order) + Heavy(order) + Remote(order);

    private static decimal BasePrice(Order o) => o.Subtotal >= FreeFrom ? 0m : Standard;

    private static decimal Express(Order o) => o.Express ? ExpressExtra : 0m;

    private static decimal Heavy(Order o) =>
        o.WeightKg > HeavyFromKg ? Math.Ceiling(o.WeightKg - HeavyFromKg) * PerExtraKg : 0m;

    private static decimal Remote(Order o) =>
        o.Postcode is string p && RemotePrefixes.Any(r => p.StartsWith(r, StringComparison.OrdinalIgnoreCase))
            ? RemoteExtra : 0m;
}
