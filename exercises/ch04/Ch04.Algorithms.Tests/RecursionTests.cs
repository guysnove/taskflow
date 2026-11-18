using Ch04.Algorithms;

namespace Ch04.Algorithms.Tests;

public class RecursionTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 120)]
    [InlineData(20, 2432902008176640000)]      // the largest factorial that fits in a long
    public void Factorial_RecursiveAndLoop_Agree(int n, long expected)
    {
        Assert.Equal(expected, Recursion.Factorial(n));
        Assert.Equal(expected, Recursion.FactorialLoop(n));
    }

    [Fact]
    public void Factorial_Negative_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Recursion.Factorial(-1));

    [Fact]
    public void TotalSize_AddsUpEveryLevel()
    {
        var root = new Folder("root", [new("a.txt", 100)],
        [
            new Folder("docs", [new("b.pdf", 2_000), new("c.pdf", 3_000)], [new Folder("old", [new("d.zip", 50_000)], [])]),
            new Folder("empty", [], []),
        ]);
        Assert.Equal(55_100, Recursion.TotalSize(root));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 55)]
    [InlineData(30, 832040)]
    public void Fibonacci_NaiveAndMemo_Agree(int n, long expected)
    {
        Assert.Equal(expected, Recursion.FibNaive(n));
        Assert.Equal(expected, Recursion.FibMemo(n));
    }

    [Fact]
    public void FibMemo_HandlesLargeN_Quickly() => Assert.Equal(12586269025, Recursion.FibMemo(50));
}
