using System.Globalization;
using Ch02.Algorithms;

namespace Ch02.Algorithms.Tests;

public class DiscountCalculatorTests
{
    // Each row: subtotal, VIP?, coupon, expected discount. The boundaries are the point.
    [Theory]
    [InlineData("50.00",  false, null,        "0.00")]   // no rule applies
    [InlineData("99.99",  false, null,        "0.00")]   // just below the big-order line
    [InlineData("100.00", false, null,        "5.00")]   // exactly on the line: "or more"
    [InlineData("100.00", true,  null,        "10.00")]  // VIP and big order: the larger, not both
    [InlineData("34.48",  true,  null,        "3.45")]   // 3.448 rounds to 3.45
    [InlineData("0.05",   false, "WELCOME10", "0.01")]   // 0.005 is a half: away from zero
    [InlineData("80.00",  false, " welcome10 ", "8.00")] // coupon: spaces and case ignored
    [InlineData("80.00",  false, "WELCOME20", "0.00")]   // unknown coupon: no discount
    [InlineData("0.00",   true,  null,        "0.00")]   // empty basket
    public void Discount_FollowsTheRules(string subtotal, bool vip, string? coupon, string expected)
    {
        Assert.Equal(D(expected), DiscountCalculator.Discount(D(subtotal), vip, coupon));
    }

    // Attributes can't hold decimals, so the numbers arrive as text.
    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    [Fact]
    public void NegativeSubtotal_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DiscountCalculator.Discount(-1m, false, null));
    }
}
