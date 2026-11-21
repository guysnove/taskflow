using Xunit.Abstractions;

namespace Ch17.Pricing.Tests;

// Isolation: xUnit creates a NEW instance of the test class for EVERY test,
// so a field set in one test can never leak into another. A static field can.
public class IsolationTests(ITestOutputHelper output)
{
    private static int s_instances;                  // shared by every instance: avoid in real tests
    private readonly List<string> _cart = [];        // fresh for every test

    private void Report(string test)
    {
        int n = Interlocked.Increment(ref s_instances);
        output.WriteLine($"{test}: instance #{n}, cart has {_cart.Count} item(s) before I start");
    }

    [Fact] public void First() { Report(nameof(First)); _cart.Add("lamp"); Assert.Single(_cart); }
    [Fact] public void Second() { Report(nameof(Second)); _cart.Add("pen"); Assert.Single(_cart); }
    [Fact] public void Third() { Report(nameof(Third)); _cart.Add("ink"); Assert.Single(_cart); }
}
