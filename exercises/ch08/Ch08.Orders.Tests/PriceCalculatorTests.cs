using Ch08.Orders.After;
using Microsoft.Extensions.Time.Testing;

namespace Ch08.Orders.Tests;

public class PriceCalculatorTests
{
    private static readonly IDiscountRule[] AllRules = [new LevelDiscount(), new CouponDiscount(), new BlackFridayDiscount()];

    private static Price PriceOn(DateOnly day, Order order, params IDiscountRule[] rules)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(day.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        return new PriceCalculator(rules.Length > 0 ? rules : AllRules, clock).PriceOf(order);
    }

    private static Order Mugs(CustomerLevel level, int count, string? coupon = null) =>
        new(new Customer(1, "Ana", "ana@example.com", level), [new("MUG", count, 10m)], Coupon: coupon);

    private static readonly DateOnly Wednesday = new(2026, 11, 18);
    private static readonly DateOnly BlackFriday2026 = new(2026, 11, 27);

    [Theory]
    [InlineData(CustomerLevel.Regular, 30.00)]
    [InlineData(CustomerLevel.Gold, 27.00)]
    [InlineData(CustomerLevel.Staff, 21.00)]
    public void Level_Discounts(CustomerLevel level, decimal expected)
    {
        Assert.Equal(expected, PriceOn(Wednesday, Mugs(level, 3)).Total);
    }

    [Fact]
    public void Coupon_TakesFiveOff_From20Up_AfterTheLevelDiscount()
    {
        Assert.Equal(25.00m, PriceOn(Wednesday, Mugs(CustomerLevel.Regular, 3, "SAVE5")).Total);   // 30 - 5
        Assert.Equal(10.00m, PriceOn(Wednesday, Mugs(CustomerLevel.Regular, 1, "SAVE5")).Total);   // under 20: no coupon
    }

    // The test the Before code could never have: choose the day.
    [Fact]
    public void BlackFriday_TakesTwentyPercentOff()
    {
        var price = PriceOn(BlackFriday2026, Mugs(CustomerLevel.Gold, 3));

        Assert.Equal(21.60m, price.Total);                                 // 30 → 27 (Gold) → 21.60
        Assert.Equal([new Saving("Customer level", 3.00m), new Saving("Black Friday", 5.40m)], price.Savings);
    }

    [Theory]
    [InlineData("2024-11-29", true)]      // Thanksgiving was the 28th: Black Friday the 29th (a fifth Friday)
    [InlineData("2025-11-28", true)]
    [InlineData("2026-11-27", true)]
    [InlineData("2026-11-20", false)]     // a Friday in November, but a week early
    [InlineData("2026-11-26", false)]     // Thanksgiving itself
    public void IsBlackFriday(string day, bool expected)
    {
        Assert.Equal(expected, BlackFridayDiscount.IsBlackFriday(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture)));
    }

    // Open/Closed in one test: a brand-new rule, and PriceCalculator didn't change.
    private sealed class BulkDiscount : IDiscountRule
    {
        public string Name => "Bulk: 10 or more items";
        public decimal Apply(Order order, decimal total, DateOnly today) =>
            order.Lines.Sum(l => l.Quantity) >= 10 ? total * 0.95m : total;
    }

    [Fact]
    public void A_NewRule_PlugsIn_WithoutChangingTheCalculator()
    {
        var price = PriceOn(Wednesday, Mugs(CustomerLevel.Regular, 10), new LevelDiscount(), new BulkDiscount());

        Assert.Equal(95.00m, price.Total);
        Assert.Equal("Bulk: 10 or more items", Assert.Single(price.Savings).Rule);
    }
}
