namespace Ch02.Algorithms;

// Chapter 2: BrightCart's discount rules, as confirmed by the (fictitious) shop owner:
//   VIP customers: 10%.  Orders of 100.00 or more: 5%.  Coupon WELCOME10: 10%.
//   Discounts don't add up: the customer gets the single largest one.
//   The discount is rounded to cents; halves round away from zero.
public static class DiscountCalculator
{
    public const decimal BigOrder = 100.00m;

    public static decimal Rate(decimal subtotal, bool isVip, string? coupon)
    {
        decimal best = 0m;
        if (isVip) best = Math.Max(best, 0.10m);
        if (subtotal >= BigOrder) best = Math.Max(best, 0.05m);
        if (string.Equals(coupon?.Trim(), "WELCOME10", StringComparison.OrdinalIgnoreCase))
            best = Math.Max(best, 0.10m);
        return best;
    }

    public static decimal Discount(decimal subtotal, bool isVip, string? coupon)
    {
        if (subtotal < 0) throw new ArgumentOutOfRangeException(nameof(subtotal));
        decimal raw = subtotal * Rate(subtotal, isVip, coupon);
        return Math.Round(raw, 2, MidpointRounding.AwayFromZero);
    }
}
