using System.Globalization;

namespace Ch09.Shipping.Steps;

// Step 2: simplify the conditions, and name the magic numbers.
//   - "== true", "== false" and "!= null && != \"\"" removed (the comparisons already say it)
//   - if/else that only choose a value became conditional expressions
//   - else after return removed; the numbers got names
public class Step2_SimplifyConditions
{
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
        if (country is "US" or "CA")
        {
            return express ? (15 + weight * 2.5, "1-2 days", true) : (5 + weight * 1.2, "3-5 days", true);
        }
        if (country is "UK" or "DE" or "FR")
        {
            return express ? (25 + weight * 4.0, "2-3 days", true) : (9 + weight * 2.0, "5-8 days", true);
        }
        return express ? (0, "", false) : (20 + weight * 3.5, "10-15 days", true);
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
