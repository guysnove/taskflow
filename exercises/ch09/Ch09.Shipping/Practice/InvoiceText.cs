using System.Globalization;
using System.Text;

namespace Ch09.Shipping.Practice;

// Independent exercise A: legacy code to refactor. Build a golden master FIRST.
// Do not change a single character of what Build returns.
public sealed record InvoiceLine(string Description, int Quantity, double UnitPrice);

public static class InvoiceText
{
    public static string Build(string customer, List<InvoiceLine> lines, bool eu, double shipping, string coupon)
    {
        var sb = new StringBuilder();
        sb.Append("INVOICE for " + customer + "\n");
        sb.Append("----------------------------------------\n");
        double sub = 0;
        foreach (var l in lines)
        {
            double t = l.Quantity * l.UnitPrice;
            sub = sub + t;
            if (eu == true)
            {
                sb.Append(l.Description.PadRight(24) + l.Quantity.ToString(CultureInfo.InvariantCulture).PadLeft(4) + " x " + "EUR " + Math.Round(t, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
            }
            else
            {
                sb.Append(l.Description.PadRight(24) + l.Quantity.ToString(CultureInfo.InvariantCulture).PadLeft(4) + " x " + "$" + Math.Round(t, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
            }
        }
        sb.Append("----------------------------------------\n");
        if (eu == true)
        {
            sb.Append("Subtotal: EUR " + Math.Round(sub, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
        }
        else
        {
            sb.Append("Subtotal: $" + Math.Round(sub, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
        }
        double discount = 0;
        if (coupon != null && coupon != "")
        {
            if (coupon == "TEN")
            {
                discount = sub * 0.1;
            }
            if (coupon == "BIG" && sub > 200)
            {
                discount = 25;
            }
        }
        if (discount > 0)
        {
            if (eu == true)
            {
                sb.Append("Discount: -EUR " + Math.Round(discount, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
            }
            else
            {
                sb.Append("Discount: -$" + Math.Round(discount, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
            }
        }
        double tax = 0;
        if (eu == true)
        {
            tax = (sub - discount) * 0.2;
            sb.Append("VAT 20%: EUR " + Math.Round(tax, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
        }
        if (shipping > 0)
        {
            if (eu == true)
            {
                sb.Append("Shipping: EUR " + Math.Round(shipping, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
            }
            else
            {
                sb.Append("Shipping: $" + Math.Round(shipping, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
            }
        }
        double total = sub - discount + tax + shipping;
        if (eu == true)
        {
            sb.Append("TOTAL: EUR " + Math.Round(total, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
        }
        else
        {
            sb.Append("TOTAL: $" + Math.Round(total, 2).ToString("0.00", CultureInfo.InvariantCulture) + "\n");
        }
        return sb.ToString();
    }
}
