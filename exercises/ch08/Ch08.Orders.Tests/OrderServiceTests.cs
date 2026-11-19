using Ch08.Orders.After;
using Microsoft.Extensions.Time.Testing;

namespace Ch08.Orders.Tests;

// The AFTER version: every test builds its own shop from fakes. No waiting, no shared state.
public class OrderServiceTests
{
    private static readonly Customer Ana = new(1, "Ana", "ana@example.com", CustomerLevel.Regular);

    private readonly InMemoryStock _stock = new(new() { ["MUG"] = 10, ["TEE"] = 5, ["CAP"] = 0 });
    private readonly FakeGateway _gateway = new();
    private readonly InMemoryOrderStore _orders = new();
    private readonly RecordingNotifier _notifier = new();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        // A normal Wednesday: no Black Friday.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero));
        IDiscountRule[] rules = [new LevelDiscount(), new CouponDiscount(), new BlackFridayDiscount()];
        _service = new OrderService(new OrderValidator(_stock), new PriceCalculator(rules, clock),
                                    _gateway, _stock, _orders, _notifier);
    }

    [Fact]
    public void TwoMugs_AreCharged_Saved_AndConfirmed()
    {
        var result = _service.Place(new Order(Ana, [new("MUG", 2, 8.50m)]));

        Assert.True(result.Ok);
        Assert.Equal(17.00m, result.Total);
        Assert.Equal(("4242424242424242", 17.00m), Assert.Single(_gateway.Charges));
        Assert.Equal(8, _stock.Available("MUG"));
        Assert.Single(_orders.Saved);
        Assert.Equal(("Ana", 17.00m), Assert.Single(_notifier.Sent));
    }

    [Fact]
    public void DeclinedCard_TakesNoStock_SavesNothing_SendsNothing()
    {
        _gateway.Decline = true;

        var result = _service.Place(new Order(Ana, [new("MUG", 2, 8.50m)]));

        Assert.False(result.Ok);
        Assert.Equal(["the card was declined"], result.Problems);
        Assert.Equal(10, _stock.Available("MUG"));
        Assert.Empty(_orders.Saved);
        Assert.Empty(_notifier.Sent);
    }

    [Fact]
    public void OutOfStock_IsRefused_BeforeAnyCharge()
    {
        var result = _service.Place(new Order(Ana, [new("CAP", 1, 12m)]));

        Assert.False(result.Ok);
        Assert.Equal(["only 0 CAP left"], result.Problems);
        Assert.Empty(_gateway.Charges);                    // the card was never touched
    }

    [Fact]
    public void EveryProblem_IsReported_NotJustTheFirst()
    {
        var result = _service.Place(new Order(Ana, [new("HAT", 1, 5m), new("TEE", 0, 15m), new("TEE", 9, 15m)]));

        Assert.Equal(["there is no product HAT", "the quantity of TEE must be at least 1", "only 5 TEE left"],
                     result.Problems);
    }

    [Fact]
    public void Orders_AreNumbered_OneTwoThree()
    {
        var first = _service.Place(new Order(Ana, [new("MUG", 1, 8.50m)]));
        var second = _service.Place(new Order(Ana, [new("TEE", 1, 15m)]));

        Assert.Equal(1, first.OrderNumber);
        Assert.Equal(2, second.OrderNumber);
    }
}
