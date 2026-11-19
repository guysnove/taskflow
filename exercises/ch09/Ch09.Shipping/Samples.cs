using System.Globalization;

namespace Ch09.Shipping;

// Every combination of these inputs: 8 × 12 × 2 × 2 × 6 × 5 = 11,520 quotes.
// Chosen to cross every boundary in the code: 20 kg, 30 and 50 in money, 10 items,
// each country group, each coupon (and wrong capitals), and some odd values (0, negative).
public static class Samples
{
    public static readonly string[] Countries = ["US", "CA", "UK", "DE", "FR", "JP", "BR", ""];
    public static readonly double[] Weights = [-1, 0, 0.1, 0.5, 1, 2.675, 7.3, 12.5, 19.99, 20, 20.01, 35];
    public static readonly string?[] Coupons = [null, "", "FREESHIP", "HALF", "half", "SAVE10"];
    public static readonly int[] Items = [0, 1, 10, 11, 40];

    public sealed record Input(string Country, double Weight, bool Express, bool Member, string? Coupon, int Items)
    {
        public override string ToString() => string.Create(CultureInfo.InvariantCulture,
            $"{(Country == "" ? "(none)" : Country)} {Weight}kg {(Express ? "express" : "standard")}{(Member ? " member" : "")} coupon={Coupon ?? "null"} items={Items}");
    }

    public static IEnumerable<Input> All()
    {
        foreach (var c in Countries)
            foreach (var w in Weights)
                foreach (var e in new[] { false, true })
                    foreach (var m in new[] { false, true })
                        foreach (var k in Coupons)
                            foreach (var i in Items)
                                yield return new Input(c, w, e, m, k, i);
    }
}
