using System.Globalization;
using Ch09.Shipping.Refactored;

namespace Ch09.Shipping.Experiments;

// An "improvement" that is NOT a refactoring: money as decimal instead of double.
// Decimal is right for money. But it rounds some values differently, so it changes
// what customers see. The golden master tells us exactly which quotes change.
public class DecimalQuoter
{
    public string Quote(string country, double weight, bool express, bool member, string coupon, int items)
    {
        var zone = Zones.For(country);
        var rate = express ? zone.Express : zone.Standard;
        if (rate is null)
        {
            return "Express is not available for " + country;
        }
        decimal w = (decimal)weight;
        decimal total = (decimal)rate.Base + w * (decimal)rate.PerKg;
        total = w > 20 ? total + 10 : total;
        total = !member ? total : total * (total > 50 ? 0.85m : 0.9m);
        total = coupon switch
        {
            "FREESHIP" when !express && total < 30 => 0,
            "HALF" => total / 2,
            _ => total,
        };
        total = items > 10 ? total + items * 0.25m : total;
        total = Math.Round(total, 2);
        return total == 0
            ? "Free shipping (" + rate.Days + ")"
            : "Total: " + total.ToString("0.00", CultureInfo.InvariantCulture) + " (" + rate.Days + ")";
    }
}
