using Ch08.Orders.Before;

namespace Ch08.Orders.Tests;

// Characterisation tests: they record what the OLD code does today, before we change it.
// They're painful to write, and that pain is the lesson:
//   - every test really "charges a card" (1.5 seconds each),
//   - the stock is static, so the tests change each other's starting point,
//   - the Black Friday rule reads the real clock, so it can't be tested at all.
[Trait("Category", "Before")]
public class BeforeTests
{
    private static readonly Customer Ana = new(1, "Ana", "ana@example.com", CustomerLevel.Regular);

    // The workaround: put the static stock back before every test. It works here, but only
    // because xUnit runs the tests of one class one at a time. Two classes doing this in
    // parallel would trample each other again.
    public BeforeTests()
    {
        OrderProcessor.Stock = new() { ["MUG"] = 10, ["TEE"] = 5, ["CAP"] = 0 };
        OrderProcessor.SavedOrders.Clear();
    }

    [Fact]
    public void TwoMugs_AreCharged_AndLeaveEightInStock()
    {
        var result = new OrderProcessor().Process(new Order(Ana, [new("MUG", 2, 8.50m)]));

        Assert.Equal("OK: charged 17.00", result);
        Assert.Equal(8, OrderProcessor.Stock["MUG"]);       // assumes nobody else bought mugs
    }

    [Fact]
    public void ThreeMugs_AreCharged_AndLeaveSevenInStock()
    {
        var result = new OrderProcessor().Process(new Order(Ana, [new("MUG", 3, 8.50m)]));

        Assert.Equal("OK: charged 25.50", result);
        Assert.Equal(7, OrderProcessor.Stock["MUG"]);       // the same assumption
    }

    [Fact]
    public void EmptyOrder_IsRefused()
    {
        Assert.Equal("ERROR: the order is empty", new OrderProcessor().Process(new Order(Ana, [])));
    }
}
