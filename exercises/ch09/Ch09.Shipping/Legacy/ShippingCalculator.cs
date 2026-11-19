using System.Globalization;

namespace Ch09.Shipping.Legacy;

// Chapter 9: the code we inherited. It has shipped parcels for years, and it works.
// Nobody wants to touch it: every change has broken something before.
public class ShippingCalculator
{
    public string Quote(string country, double weight, bool express, bool member, string coupon, int items)
    {
        double total = 0;
        string days = "";
        if (country == "US" || country == "CA")
        {
            if (express)
            {
                total = 15 + weight * 2.5;
                days = "1-2 days";
            }
            else
            {
                total = 5 + weight * 1.2;
                days = "3-5 days";
            }
        }
        else if (country == "UK" || country == "DE" || country == "FR")
        {
            if (express)
            {
                total = 25 + weight * 4.0;
                days = "2-3 days";
            }
            else
            {
                total = 9 + weight * 2.0;
                days = "5-8 days";
            }
        }
        else
        {
            if (express)
            {
                return "Express is not available for " + country;
            }
            else
            {
                total = 20 + weight * 3.5;
                days = "10-15 days";
            }
        }
        if (weight > 20)
        {
            total = total + 10;
        }
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
        if (items > 10)
        {
            total = total + items * 0.25;
        }
        total = Math.Round(total, 2);
        if (total == 0)
        {
            return "Free shipping (" + days + ")";
        }
        return "Total: " + total.ToString("0.00", CultureInfo.InvariantCulture) + " (" + days + ")";
    }
}
