using Ch04.Algorithms;

namespace Ch04.Algorithms.Tests;

public class SearchSortTests
{
    [Theory]
    [InlineData(new int[0], 5, -1)]
    [InlineData(new[] { 5 }, 5, 0)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 1, 0)]         // first
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 9, 4)]         // last
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 4, -1)]        // between
    public void BinarySearch_EdgeCases(int[] sorted, int target, int expected)
    {
        Assert.Equal(expected, SearchSort.BinarySearch(sorted, target));
        Assert.Equal(expected, SearchSort.LinearSearch(sorted, target));
    }

    [Fact]
    public void Sorts_AgreeWithArraySort()
    {
        var rnd = new Random(11);
        foreach (int n in new[] { 0, 1, 2, 10, 500 })
        {
            int[] data = Enumerable.Range(0, n).Select(_ => rnd.Next(-100, 100)).ToArray();
            int[] expected = (int[])data.Clone(); Array.Sort(expected);
            int[] insertion = (int[])data.Clone(); SearchSort.InsertionSort(insertion);
            Assert.Equal(expected, insertion);
            Assert.Equal(expected, SearchSort.MergeSort(data));
        }
    }

    [Theory]
    [InlineData(new[] { 1, 3, 4 }, 6, 2)]      // greedy would say 3 (4 + 1 + 1)
    [InlineData(new[] { 1, 5, 10, 25 }, 63, 6)]
    [InlineData(new[] { 2 }, 3, null)]          // impossible
    [InlineData(new[] { 1 }, 0, 0)]
    public void FewestCoins(int[] coins, int amount, int? expected) =>
        Assert.Equal(expected, SearchSort.FewestCoins(coins, amount));
}
