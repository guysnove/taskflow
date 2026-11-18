namespace Ch03.Structures;

// Chapter 3: a singly linked list, built from scratch.
// Each node holds a value and a reference to the next node. No index, no shifting.
public class SinglyLinkedList<T>
{
    private sealed class Node(T value)
    {
        public T Value = value;
        public Node? Next;
    }

    private Node? _head;
    public int Count { get; private set; }

    public void AddFirst(T value)                     // O(1): no shifting, just two references
    {
        var node = new Node(value) { Next = _head };
        _head = node;
        Count++;
    }

    public T this[int index]                         // O(n): must walk from the head
    {
        get
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            var current = _head!;
            for (int i = 0; i < index; i++) current = current.Next!;
            return current.Value;
        }
    }

    public bool Remove(T value)                       // O(n) to find; O(1) to unlink
    {
        Node? previous = null, current = _head;
        while (current is not null)
        {
            if (EqualityComparer<T>.Default.Equals(current.Value, value))
            {
                if (previous is null) _head = current.Next;      // removing the head
                else previous.Next = current.Next;               // skip over it
                Count--;
                return true;
            }
            previous = current;
            current = current.Next;
        }
        return false;
    }

    public IEnumerable<T> Items()
    {
        for (var n = _head; n is not null; n = n.Next) yield return n.Value;
    }
}
