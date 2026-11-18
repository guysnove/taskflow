using Ch03.Structures;

namespace Ch03.Structures.Tests;

public class StackQueueTests
{
    [Fact]
    public void Stack_IsLastInFirstOut()
    {
        var s = new ArrayStack<string>();
        s.Push("open"); s.Push("type"); s.Push("bold");
        Assert.Equal("bold", s.Peek());
        Assert.Equal("bold", s.Pop());
        Assert.Equal("type", s.Pop());
        Assert.Equal(1, s.Count);
    }

    [Fact]
    public void Stack_PopWhenEmpty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new ArrayStack<int>().Pop());
    }

    [Fact]
    public void Queue_IsFirstInFirstOut_AcrossTheWrapAround()
    {
        var q = new RingQueue<int>(capacity: 3);
        q.Enqueue(1); q.Enqueue(2); q.Enqueue(3);       // full
        Assert.Equal(1, q.Dequeue());                   // head moves to index 1
        q.Enqueue(4);                                   // tail wraps to index 0
        Assert.Equal(3, q.Capacity);                    // no growth needed
        Assert.Equal([2, 3, 4], new[] { q.Dequeue(), q.Dequeue(), q.Dequeue() });
    }

    [Fact]
    public void Queue_GrowsWhileWrapped_AndKeepsOrder()
    {
        var q = new RingQueue<int>(capacity: 3);
        q.Enqueue(1); q.Enqueue(2); q.Enqueue(3);
        q.Dequeue(); q.Enqueue(4);                      // wrapped: [4, 2, 3], head at 1
        q.Enqueue(5);                                   // full: grow and unwrap
        Assert.Equal(6, q.Capacity);
        Assert.Equal([2, 3, 4, 5], Enumerable.Range(0, 4).Select(_ => q.Dequeue()));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("(a[b]{c})", true)]
    [InlineData("(]", false)]          // wrong kind
    [InlineData("((", false)]          // left open
    [InlineData(")(", false)]          // closed before opened
    public void Brackets(string text, bool balanced) =>
        Assert.Equal(balanced, BracketChecker.IsBalanced(text));
}
