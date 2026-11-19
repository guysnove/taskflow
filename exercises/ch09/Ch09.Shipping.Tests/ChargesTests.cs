using Ch09.Shipping.Refactored;

namespace Ch09.Shipping.Tests;

// After Extract Class, each rule can be tested directly, with no country or parcel needed.
public class ChargesTests
{
    [Theory]
    [InlineData(20.00, 30.0)]   // exactly 20 kg: no surcharge
    [InlineData(20.01, 40.0)]
    public void Heavy(double kg, double expected) => Assert.Equal(expected, Charges.Heavy(30, kg));

    [Theory]
    [InlineData(50, 45.0)]      // exactly 50: still the 10% rate
    [InlineData(60, 51.0)]      // above 50: 15%
    public void Member(double total, double expected) => Assert.Equal(expected, Charges.Member(total, isMember: true), 10);

    [Fact]
    public void FreeShip_OnlyForStandard_AndUnder30()
    {
        Assert.Equal(0, Charges.Coupon(29.99, "FREESHIP", Speed.Standard));
        Assert.Equal(30, Charges.Coupon(30, "FREESHIP", Speed.Standard));
        Assert.Equal(20, Charges.Coupon(20, "FREESHIP", Speed.Express));
    }

    [Fact]
    public void Handling_From11Items() =>
        Assert.Equal([10.0, 12.75], [Charges.Handling(10, 10), Charges.Handling(10, 11)]);

    [Fact]
    public void NewInterface_ReadsLikeTheBusiness()
    {
        var quote = new ShippingQuoter().Quote(new ShippingRequest("UK", 2.5, Speed.Express));
        Assert.Equal((35.0, "2-3 days", true), (quote.Total, quote.Days, quote.Available));
    }
}
