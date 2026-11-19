using System.Globalization;

namespace Ch09.Shipping.Steps;

// Step 1: Extract Method. The same code, cut into named pieces. Nothing else changed:
// the conditions, the numbers and the order of the arithmetic are exactly as before.
public class Step1_ExtractMethod
{
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
        if (country == "US" || country == "CA")
        {
            if (express)
            {
                return (15 + weight * 2.5, "1-2 days", true);
            }
            else
            {
                return (5 + weight * 1.2, "3-5 days", true);
            }
        }
        else if (country == "UK" || country == "DE" || country == "FR")
        {
            if (express)
            {
                return (25 + weight * 4.0, "2-3 days", true);
            }
            else
            {
                return (9 + weight * 2.0, "5-8 days", true);
            }
        }
        else
        {
            if (express)
            {
                return (0, "", false);
            }
            else
            {
                return (20 + weight * 3.5, "10-15 days", true);
            }
        }
    }

    private static double AddHeavySurcharge(double total, double weight)
    {
        if (weight > 20)
        {
            total = total + 10;
        }
        return total;
    }

    private static double ApplyMemberDiscount(double total, bool member)
    {
        if (member == true)
        {
            if (total > 50)
            {
                total = total * 0.85;
            }
            else
            {
                total = total * 0.9;
            }
        }
        return total;
    }

    private static double ApplyCoupon(double total, string coupon, bool express)
    {
        if (coupon != null && coupon != "")
        {
            if (coupon == "FREESHIP" && express == false && total < 30)
            {
                total = 0;
            }
            if (coupon == "HALF")
            {
                total = total / 2;
            }
        }
        return total;
    }

    private static double AddHandling(double total, int items)
    {
        if (items > 10)
        {
            total = total + items * 0.25;
        }
        return total;
    }

    private static string Format(double total, string days)
    {
        total = Math.Round(total, 2);
        if (total == 0)
        {
            return "Free shipping (" + days + ")";
        }
        return "Total: " + total.ToString("0.00", CultureInfo.InvariantCulture) + " (" + days + ")";
    }
}
