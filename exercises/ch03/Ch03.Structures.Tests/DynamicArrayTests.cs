using Ch03.Structures;

namespace Ch03.Structures.Tests;

public class DynamicArrayTests
{
    [Fact]
    public void Add_GrowsByDoubling_AndKeepsOrder()
    {
        var a = new DynamicArray<int>();
        Assert.Equal(4, a.Capacity);
        for (int i = 1; i <= 5; i++) a.Add(i * 10);    // the 5th Add doesn't fit: grow

        Assert.Equal(5, a.Count);
        Assert.Equal(8, a.Capacity);
        Assert.Equal([10, 20, 30, 40, 50], Enumerable.Range(0, a.Count).Select(i => a[i]));
    }

    [Fact]
    public void Insert_ShiftsLaterItemsRight()
    {
        var a = new DynamicArray<string>();
        a.Add("A"); a.Add("C");
        a.Insert(1, "B");
        Assert.Equal(["A", "B", "C"], Enumerable.Range(0, a.Count).Select(i => a[i]));
    }

    [Fact]
    public void RemoveAt_ShiftsLaterItemsLeft()
    {
        var a = new DynamicArray<string>();
        a.Add("A"); a.Add("B"); a.Add("C");
        a.RemoveAt(0);
        Assert.Equal(["B", "C"], Enumerable.Range(0, a.Count).Select(i => a[i]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]      // == Count: one past the end
    public void Index_OutsideCount_Throws(int index)
    {
        var a = new DynamicArray<int>();
        a.Add(1); a.Add(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => a[index]);
    }
}
