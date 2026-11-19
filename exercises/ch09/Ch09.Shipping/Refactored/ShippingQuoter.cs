using System.Globalization;

namespace Ch09.Shipping.Refactored;

// Step 4, the result: the whole calculation reads as the business describes it.
public class ShippingQuoter
{
    public ShippingQuote Quote(ShippingRequest request)
    {
        var rate = request.Speed == Speed.Express ? Zones.For(request.Country).Express
                                                  : Zones.For(request.Country).Standard;
        if (rate is null)
        {
            return new ShippingQuote(0, "", Problem: "Express is not available for " + request.Country);
        }
        double total = rate.For(request.WeightKg);
        total = Charges.Heavy(total, request.WeightKg);
        total = Charges.Member(total, request.IsMember);
        total = Charges.Coupon(total, request.Coupon, request.Speed);
        total = Charges.Handling(total, request.Items);
        return new ShippingQuote(Math.Round(total, 2), rate.Days);
    }

    public static string ToText(ShippingQuote quote) =>
        !quote.Available ? quote.Problem!
        : quote.Total == 0 ? $"Free shipping ({quote.Days})"
        : $"Total: {quote.Total.ToString("0.00", CultureInfo.InvariantCulture)} ({quote.Days})";

    // The old interface, kept for the callers that still use it ("parallel change"):
    // they move over one at a time, and then this method is deleted.
    public string Quote(string country, double weight, bool express, bool member, string coupon, int items) =>
        ToText(Quote(new ShippingRequest(country, weight, express ? Speed.Express : Speed.Standard, member, coupon, items)));
}
