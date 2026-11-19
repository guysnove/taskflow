using System.Globalization;

namespace Ch09.Shipping.Steps;

// Step 3: replace duplication. The zones repeated the same shape five times:
// "base + weight × per kg, and a delivery time". Now that shape is a record, and the
// zones are rows in one table. A new zone is a new row, not a new else-if.
public class Step3_ReplaceDuplication
{
    private sealed record Rate(double Base, double PerKg, string Days);
    private sealed record Zone(string[] Countries, Rate Standard, Rate? Express);

    private static readonly Zone[] Zones =
    [
        new(["US", "CA"], Standard: new(5, 1.2, "3-5 days"), Express: new(15, 2.5, "1-2 days")),
        new(["UK", "DE", "FR"], Standard: new(9, 2.0, "5-8 days"), Express: new(25, 4.0, "2-3 days")),
    ];
    private static readonly Zone RestOfWorld = new([], Standard: new(20, 3.5, "10-15 days"), Express: null);

    private const double HeavyParcelKg = 20;
    private const double HeavySurcharge = 10;
    private const double MemberBigOrder = 50;
    private const double FreeShippingLimit = 30;
    private const int BulkItems = 10;
    private const double HandlingPerItem = 0.25;

    public string Quote(string country, double weight, bool express, bool member, string coupon, int items)
    {
        var (total, days, available) = BasePrice(country, weight, express);
        if (!available)
        {
            return "Express is not available for " + country;
        }
        total = AddHeavySurcharge(total, weight);
        total = ApplyMemberDiscount(total, member);
        total = ApplyCoupon(total, coupon, express);
        total = AddHandling(total, items);
        return Format(total, days);
    }

    private static (double Total, string Days, bool Available) BasePrice(string country, double weight, bool express)
    {
        var zone = Zones.FirstOrDefault(z => z.Countries.Contains(country)) ?? RestOfWorld;
        var rate = express ? zone.Express : zone.Standard;
        return rate is null ? (0, "", false) : (rate.Base + weight * rate.PerKg, rate.Days, true);
    }

    private static double AddHeavySurcharge(double total, double weight) =>
        weight > HeavyParcelKg ? total + HeavySurcharge : total;

    private static double ApplyMemberDiscount(double total, bool member) =>
        !member ? total : total * (total > MemberBigOrder ? 0.85 : 0.9);

    private static double ApplyCoupon(double total, string coupon, bool express) => coupon switch
    {
        "FREESHIP" when !express && total < FreeShippingLimit => 0,
        "HALF" => total / 2,
        _ => total,
    };

    private static double AddHandling(double total, int items) =>
        items > BulkItems ? total + items * HandlingPerItem : total;

    private static string Format(double total, string days)
    {
        total = Math.Round(total, 2);
        return total == 0
            ? "Free shipping (" + days + ")"
            : "Total: " + total.ToString("0.00", CultureInfo.InvariantCulture) + " (" + days + ")";
    }
}
