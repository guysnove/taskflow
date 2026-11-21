using Ch17.Pricing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Ch17.Mistakes.Tests;

// THIS PROJECT FAILS ON PURPOSE. Each test shows a common mistake, so you can read what the
// failure (or the missing failure) looks like. The story: the business has just decided that
// members get 10% off. The code still gives 5% — so a good test SHOULD fail.
public class MistakesTests
{
    private static PriceCalculator Calculator(bool member) =>
        new(Substitute.For<ICouponStore>(), new Members(member), new FakeTimeProvider(new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.Zero)));

    private sealed class Members(bool member) : IMemberDirectory
    {
        public Task<bool> IsMemberAsync(string customerId) => Task.FromResult(member);
    }

    private static readonly Order Order = new("ana", [new OrderLine("LAMP", 100m, 1)]);

    // MISTAKE 1: Assert.True hides the values. When it fails, you only learn "False".
    [Fact]
    public async Task Mistake1_AssertTrue_SaysNothingUseful()
    {
        var quote = await Calculator(member: true).QuoteAsync(Order);
        Assert.True(quote.Total == 90m);
    }

    // BETTER: Assert.Equal shows what was expected AND what actually happened.
    [Fact]
    public async Task Better1_AssertEqual_ShowsBothValues()
    {
        var quote = await Calculator(member: true).QuoteAsync(Order);
        Assert.Equal(90m, quote.Total);
    }

    // MISTAKE 2: testing the mock, not the code. This "passes" whatever PriceCalculator does —
    // it never even calls it.
    [Fact]
    public async Task Mistake2_TestingTheMock_AlwaysPasses()
    {
        var members = Substitute.For<IMemberDirectory>();
        members.IsMemberAsync("ana").Returns(true);
        Assert.True(await members.IsMemberAsync("ana"));
    }

    // MISTAKE 3: logic in a test. The test computes the answer the same (wrong?) way as the code,
    // so it can only agree with it. Write the expected value as a plain number.
    [Fact]
    public async Task Mistake3_LogicInTheTest_CopiesTheCode()
    {
        decimal expected = Math.Round(100m * (100 - 5) / 100, 2);   // the same formula as the code…
        var quote = await Calculator(member: true).QuoteAsync(Order);
        Assert.Equal(expected, quote.Total);                        // …so it passes, even if 5 is wrong
    }
}
