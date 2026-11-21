using Ch19.Shipping;

namespace Ch19.Shipping.Tests;

// Written test-first, one small step at a time (Chapter 19). Each test was red before its code existed.
public class ShippingTests
{
    // 1. The simplest case first.
    [Fact]
    public void ASmallOrder_Costs4_99()
    {
        var cost = ShippingCalculator.Cost(new Order(Subtotal: 20m));
        Assert.Equal(4.99m, cost);
    }

    // 2. Free shipping from 50: the boundary, both sides.
    [Theory]
    [InlineData(49.99, 4.99)]
    [InlineData(50.00, 0.00)]
    [InlineData(120.00, 0.00)]
    public void FreeShipping_StartsAt50(decimal subtotal, decimal expected)
    {
        Assert.Equal(expected, ShippingCalculator.Cost(new Order(subtotal)));
    }

    // 3. Express always costs 10 more, even when shipping is free.
    [Theory]
    [InlineData(20, 14.99)]
    [InlineData(80, 10.00)]
    public void Express_Adds10(decimal subtotal, decimal expected)
    {
        Assert.Equal(expected, ShippingCalculator.Cost(new Order(subtotal, Express: true)));
    }

    // 4. Heavy parcels: 1.00 for every started kg over 20 kg.
    [Theory]
    [InlineData(20.0, 0.00)]      // exactly 20 kg: nothing extra
    [InlineData(20.1, 1.00)]      // a started kg counts
    [InlineData(23.0, 3.00)]
    public void HeavyParcels_PayPerStartedKgOver20(double kg, decimal extra)
    {
        var free = new Order(Subtotal: 100m, WeightKg: (decimal)kg);
        Assert.Equal(extra, ShippingCalculator.Cost(free));
    }

    // 5. Remote islands (postcodes starting HS or ZE): 5.00 more. Not case-sensitive.
    [Theory]
    [InlineData("HS1 2AB", 5.00)]
    [InlineData("ze2 9xx", 5.00)]
    [InlineData("EH1 1AA", 0.00)]
    [InlineData(null, 0.00)]
    public void RemotePostcodes_Add5(string? postcode, decimal extra)
    {
        Assert.Equal(extra, ShippingCalculator.Cost(new Order(100m, Postcode: postcode)));
    }
}
