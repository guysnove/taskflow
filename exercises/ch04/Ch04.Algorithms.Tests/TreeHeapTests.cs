using Ch04.Algorithms;

namespace Ch04.Algorithms.Tests;

public class TreeHeapTests
{
    [Fact]
    public void Bst_AddContains_InOrderIsSorted()
    {
        var t = new BinarySearchTree();
        foreach (int k in new[] { 50, 30, 70, 20, 40, 60, 80 }) t.Add(k);
        Assert.False(t.Add(40));                                   // duplicate refused
        Assert.True(t.Contains(60));
        Assert.False(t.Contains(65));
        Assert.Equal([20, 30, 40, 50, 60, 70, 80], t.InOrder());
        Assert.Equal(3, t.Height);                                 // perfectly balanced
    }

    [Fact]
    public void Bst_SortedInput_DegeneratesIntoALine()
    {
        var t = new BinarySearchTree();
        for (int k = 1; k <= 100; k++) t.Add(k);
        Assert.Equal(100, t.Height);                               // a linked list in disguise
    }

    [Fact]
    public void Heap_PopsInAscendingOrder()
    {
        var h = new MinHeap();
        foreach (int v in new[] { 5, 3, 8, 1, 9, 2 }) h.Push(v);
        Assert.Equal(1, h.Peek());
        Assert.Equal([1, 2, 3, 5, 8, 9], Enumerable.Range(0, 6).Select(_ => h.Pop()));
        Assert.Throws<InvalidOperationException>(() => h.Pop());
    }

    [Fact]
    public void Heap_AgreesWithSorting_OnRandomData()
    {
        var rnd = new Random(3);
        int[] data = Enumerable.Range(0, 1000).Select(_ => rnd.Next(500)).ToArray();   // with duplicates
        var h = new MinHeap();
        foreach (int v in data) h.Push(v);
        Assert.Equal(data.Order(), Enumerable.Range(0, data.Length).Select(_ => h.Pop()));
    }
}
