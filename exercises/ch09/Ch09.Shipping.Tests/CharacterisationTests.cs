using Ch09.Shipping.Legacy;

namespace Ch09.Shipping.Tests;

// Readable examples of what the legacy code does today, including the strange parts.
// They document behaviour; they don't judge it. Strange answers go on the debt list.
public class CharacterisationTests
{
    private static string Quote(string country, double kg, bool express = false, bool member = false, string coupon = "", int items = 1) =>
        new ShippingCalculator().Quote(country, kg, express, member, coupon, items);

    [Theory]
    [InlineData("US", 2.5, false, "Total: 8.00 (3-5 days)")]
    [InlineData("UK", 2.5, true, "Total: 35.00 (2-3 days)")]
    [InlineData("JP", 2.5, false, "Total: 28.75 (10-15 days)")]
    [InlineData("JP", 2.5, true, "Express is not available for JP")]
    public void BasicPrices(string country, double kg, bool express, string expected) =>
        Assert.Equal(expected, Quote(country, kg, express));

    [Fact]
    public void MemberDiscount_Is15Percent_Above50_Else10() =>
        Assert.Equal(["Total: 9.00 (5-8 days)", "Total: 99.88 (10-15 days)"],
                     [Quote("DE", 0.5, member: true), Quote("BR", 25, member: true)]);   // 10 → 9;  117.5 → 99.875 → 99.88

    [Fact]
    public void Coupons_AreCaseSensitive()          // "half" does nothing: today's behaviour
    {
        Assert.Equal("Total: 4.00 (3-5 days)", Quote("US", 2.5, coupon: "HALF"));
        Assert.Equal("Total: 8.00 (3-5 days)", Quote("US", 2.5, coupon: "half"));
    }

    [Fact]
    public void NegativeWeight_IsAccepted()          // a bug? It's what the code does today.
    {
        Assert.Equal("Total: 3.80 (3-5 days)", Quote("US", -1));
    }

    [Fact]
    public void UnknownCountry_IsPricedAsRestOfWorld()
    {
        Assert.Equal("Total: 20.00 (10-15 days)", Quote("", 0));
    }
}
