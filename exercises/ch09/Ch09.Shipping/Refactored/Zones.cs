namespace Ch09.Shipping.Refactored;

// Step 4: Extract Class. The price list is its own small class: data that changes when the
// carrier changes its prices, and for no other reason.
public sealed record Rate(double Base, double PerKg, string Days)
{
    public double For(double weightKg) => Base + weightKg * PerKg;
}

public sealed record Zone(string Name, string[] Countries, Rate Standard, Rate? Express);

public static class Zones
{
    public static readonly Zone NorthAmerica = new("North America", ["US", "CA"], new(5, 1.2, "3-5 days"), new(15, 2.5, "1-2 days"));
    public static readonly Zone Europe = new("Europe", ["UK", "DE", "FR"], new(9, 2.0, "5-8 days"), new(25, 4.0, "2-3 days"));
    public static readonly Zone RestOfWorld = new("Rest of world", [], new(20, 3.5, "10-15 days"), Express: null);

    private static readonly Zone[] Named = [NorthAmerica, Europe];

    public static Zone For(string country) => Named.FirstOrDefault(z => z.Countries.Contains(country)) ?? RestOfWorld;
}
