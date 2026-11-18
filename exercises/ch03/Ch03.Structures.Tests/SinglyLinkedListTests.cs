using Ch03.Structures;

namespace Ch03.Structures.Tests;

public class SinglyLinkedListTests
{
    [Fact]
    public void AddFirst_PutsNewestAtTheFront()
    {
        var list = new SinglyLinkedList<string>();
        list.AddFirst("C"); list.AddFirst("B"); list.AddFirst("A");
        Assert.Equal(["A", "B", "C"], list.Items());
        Assert.Equal("B", list[1]);
    }

    [Theory]
    [InlineData("A", new[] { "B", "C" })]   // the head
    [InlineData("B", new[] { "A", "C" })]   // the middle
    [InlineData("C", new[] { "A", "B" })]   // the tail
    public void Remove_UnlinksTheNode(string remove, string[] expected)
    {
        var list = new SinglyLinkedList<string>();
        list.AddFirst("C"); list.AddFirst("B"); list.AddFirst("A");
        Assert.True(list.Remove(remove));
        Assert.Equal(expected, list.Items());
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Remove_Missing_ReturnsFalse_AndEmptyListIsSafe()
    {
        var list = new SinglyLinkedList<int>();
        Assert.False(list.Remove(42));
        list.AddFirst(1);
        Assert.False(list.Remove(42));
        Assert.Equal(1, list.Count);
    }
}
