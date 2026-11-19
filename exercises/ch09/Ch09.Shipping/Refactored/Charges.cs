namespace Ch09.Shipping.Refactored;

// Step 4: Extract Class. The rules that adjust a price, as small pure functions:
// the same inputs always give the same output, so each one is easy to test on its own.
public static class Charges
{
    public const double HeavyParcelKg = 20;
    public const double HeavySurcharge = 10;
    public const double MemberBigOrder = 50;
    public const double FreeShippingLimit = 30;
    public const int BulkItems = 10;
    public const double HandlingPerItem = 0.25;

    public static double Heavy(double total, double weightKg) =>
        weightKg > HeavyParcelKg ? total + HeavySurcharge : total;

    public static double Member(double total, bool isMember) =>
        !isMember ? total : total * (total > MemberBigOrder ? 0.85 : 0.9);

    public static double Coupon(double total, string? coupon, Speed speed) => coupon switch
    {
        "FREESHIP" when speed == Speed.Standard && total < FreeShippingLimit => 0,
        "HALF" => total / 2,
        _ => total,
    };

    public static double Handling(double total, int items) =>
        items > BulkItems ? total + items * HandlingPerItem : total;
}
