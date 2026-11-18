namespace Ch04.Algorithms;

// Chapter 4: a binary min-heap stored in an array. The smallest item is always at index 0.
// For the item at index i: parent (i - 1) / 2, children 2i + 1 and 2i + 2.
public class MinHeap
{
    private readonly List<int> _items = new();
    public int Count => _items.Count;

    public int Peek() => _items.Count > 0 ? _items[0] : throw new InvalidOperationException("The heap is empty.");

    public void Push(int value)                    // O(log n): bubble up
    {
        _items.Add(value);
        int i = _items.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_items[parent] <= _items[i]) break;          // in order: stop
            (_items[parent], _items[i]) = (_items[i], _items[parent]);
            i = parent;
        }
    }

    public int Pop()                               // O(log n): move the last to the top, sink it
    {
        int top = Peek();
        int last = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        if (_items.Count == 0) return top;
        _items[0] = last;
        int i = 0;
        while (true)
        {
            int left = 2 * i + 1, right = left + 1, smallest = i;
            if (left < _items.Count && _items[left] < _items[smallest]) smallest = left;
            if (right < _items.Count && _items[right] < _items[smallest]) smallest = right;
            if (smallest == i) break;
            (_items[smallest], _items[i]) = (_items[i], _items[smallest]);
            i = smallest;
        }
        return top;
    }
}
