using Ch17.Pricing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Ch17.Pricing.Tests;

// The five kinds of test double, each in its natural use. (Names from Gerard Meszaros, "xUnit Test Patterns".)
public class TestDoublesTests
{
    private static readonly Order Order = new("ana", [new OrderLine("PEN", 2m, 5)]);
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.Zero));

    // DUMMY: passed only because a parameter needs something; never used by this test's path.
    private sealed class DummyCoupons : ICouponStore
    {
        public Task<Coupon?> FindAsync(string code) => throw new InvalidOperationException("a dummy must not be used");
    }

    // STUB: gives a fixed answer.
    private sealed class StubMembers(bool isMember) : IMemberDirectory
    {
        public Task<bool> IsMemberAsync(string customerId) => Task.FromResult(isMember);
    }

    // FAKE: a real, working implementation — just simpler (in memory) than the production one.
    private sealed class InMemoryCoupons : ICouponStore
    {
        private readonly Dictionary<string, Coupon> _coupons = new(StringComparer.OrdinalIgnoreCase);
        public void Add(Coupon c) => _coupons[c.Code] = c;
        public Task<Coupon?> FindAsync(string code) => Task.FromResult(_coupons.GetValueOrDefault(code));
    }

    // SPY: records what happened, so the test can look afterwards.
    private sealed class SpyReceipts : IReceiptSender
    {
        public List<(string Customer, decimal Total)> Sent { get; } = [];
        public Task SendAsync(string customerId, decimal total) { Sent.Add((customerId, total)); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Dummy_AndStub()
    {
        var calculator = new PriceCalculator(new DummyCoupons(), new StubMembers(isMember: true), _clock);
        var quote = await calculator.QuoteAsync(Order);           // no coupon: the dummy is never asked
        Assert.Equal(9.50m, quote.Total);                          // 10 − 5%
    }

    [Fact]
    public async Task Fake()
    {
        var coupons = new InMemoryCoupons();
        coupons.Add(new Coupon("WELCOME", 20, new DateOnly(2026, 12, 31)));
        var calculator = new PriceCalculator(coupons, new StubMembers(false), _clock);

        var quote = await calculator.QuoteAsync(Order with { Coupon = "welcome" });   // case doesn't matter

        Assert.Equal(8m, quote.Total);
    }

    [Fact]
    public async Task Spy()
    {
        var receipts = new SpyReceipts();
        var checkout = new Checkout(new PriceCalculator(new DummyCoupons(), new StubMembers(false), _clock), receipts);

        await checkout.PayAsync(Order);

        Assert.Equal([("ana", 10m)], receipts.Sent);
    }

    [Fact]
    public async Task Mock_WithNSubstitute()
    {
        var receipts = Substitute.For<IReceiptSender>();           // a mock: we'll check the CALL itself
        var checkout = new Checkout(new PriceCalculator(new DummyCoupons(), new StubMembers(false), _clock), receipts);

        await checkout.PayAsync(Order);

        await receipts.Received(1).SendAsync("ana", 10m);
        await receipts.DidNotReceive().SendAsync("ben", Arg.Any<decimal>());
    }
}
