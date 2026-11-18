namespace Ch03.Structures;

// Chapter 3: a queue (first in, first out) as a circular buffer, built from scratch.
// _head is where we take from; _tail is where we add. Both wrap around to 0 at the end.
public class RingQueue<T>
{
    private T[] _items;
    private int _head, _tail;
    public int Count { get; private set; }

    public RingQueue(int capacity = 4) => _items = new T[Math.Max(1, capacity)];
    public int Capacity => _items.Length;

    public void Enqueue(T item)                       // O(1) on average
    {
        if (Count == _items.Length) Grow();
        _items[_tail] = item;
        _tail = (_tail + 1) % _items.Length;          // wrap around
        Count++;
    }

    public T Dequeue()                                // O(1): no shifting
    {
        if (Count == 0) throw new InvalidOperationException("The queue is empty.");
        T item = _items[_head];
        _items[_head] = default!;
        _head = (_head + 1) % _items.Length;          // wrap around
        Count--;
        return item;
    }

    private void Grow()
    {
        var bigger = new T[_items.Length * 2];
        for (int i = 0; i < Count; i++) bigger[i] = _items[(_head + i) % _items.Length];  // unwrap, in order
        _items = bigger; _head = 0; _tail = Count;
    }
}
