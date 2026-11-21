using Ch17.Pricing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Ch17.Pricing.Tests;

// Unit tests for the pricing rules. Each test: Arrange (set up), Act (one call), Assert (check).
public class PriceCalculatorTests
{
    // Shared setup: a fresh calculator for every test (xUnit creates a new instance of this class per test).
    private readonly ICouponStore _coupons = Substitute.For<ICouponStore>();
    private readonly IMemberDirectory _members = Substitute.For<IMemberDirectory>();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.Zero));
    private PriceCalculator Calculator() => new(_coupons, _members, _clock);

    private static Order OneLine(decimal price, int quantity, string? coupon = null) =>
        new("ana", [new OrderLine("LAMP", price, quantity)], coupon);

    [Fact]
    public async Task NoDiscounts_TotalIsPriceTimesQuantity()
    {
        // Arrange
        var order = OneLine(price: 20m, quantity: 3);

        // Act
        var quote = await Calculator().QuoteAsync(order);

        // Assert
        Assert.Equal(60m, quote.Total);
        Assert.Equal(0m, quote.DiscountPercent);
    }

    // A parameterised test: one rule, its boundary, several cases.
    [Theory]
    [InlineData(9, 90.00)]     // just below the bulk threshold: no discount
    [InlineData(10, 90.00)]    // exactly 10: 10% off  (10 × 10 × 0.9)
    [InlineData(11, 99.00)]
    public async Task BulkDiscount_StartsAtTen(int quantity, decimal expected)
    {
        var quote = await Calculator().QuoteAsync(OneLine(10m, quantity));
        Assert.Equal(expected, quote.Total);
    }

    [Theory]
    [InlineData("2026-11-21", 90.00)]   // expires today: still valid
    [InlineData("2026-11-20", 100.00)]  // expired yesterday: ignored
    public async Task ACoupon_IsValidUpToAndIncludingItsExpiryDate(string expires, decimal expected)
    {
        _coupons.FindAsync("SAVE10").Returns(new Coupon("SAVE10", 10, DateOnly.Parse(expires, System.Globalization.CultureInfo.InvariantCulture)));

        var quote = await Calculator().QuoteAsync(OneLine(100m, 1, coupon: "SAVE10"));

        Assert.Equal(expected, quote.Total);
    }

    [Fact]
    public async Task CouponAndMembership_AreCappedAt25Percent()
    {
        _coupons.FindAsync("BIG").Returns(new Coupon("BIG", 30, new DateOnly(2027, 1, 1)));
        _members.IsMemberAsync("ana").Returns(true);

        var quote = await Calculator().QuoteAsync(OneLine(100m, 1, coupon: "BIG"));

        Assert.Equal(25m, quote.DiscountPercent);       // 30 + 5 = 35, capped
        Assert.Equal(75m, quote.Total);
    }

    [Fact]
    public async Task Money_IsRoundedToCents_HalvesAwayFromZero()
    {
        _members.IsMemberAsync("ana").Returns(true);   // 5% of 0.10 = 0.005 → 0.095 → 0.10 (away from zero)

        var quote = await Calculator().QuoteAsync(OneLine(0.10m, 1));

        Assert.Equal(0.10m, quote.Total);
    }

    [Fact]
    public async Task AnEmptyOrder_IsRefused()
    {
        var empty = new Order("ana", []);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Calculator().QuoteAsync(empty));
        Assert.Equal("order", ex.ParamName);
    }

    [Fact]
    public async Task AnUnknownCoupon_IsIgnored()
    {
        // NSubstitute returns null for FindAsync unless told otherwise.
        var quote = await Calculator().QuoteAsync(OneLine(50m, 2, coupon: "NOPE"));
        Assert.Equal(100m, quote.Total);
    }
}
