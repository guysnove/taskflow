namespace Ch03.Structures;

// Chapter 3: a stack (last in, first out), built on an array.
public class ArrayStack<T>
{
    private T[] _items = new T[4];
    public int Count { get; private set; }

    public void Push(T item)                          // O(1) on average
    {
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        _items[Count++] = item;
    }

    public T Pop()                                    // O(1)
    {
        if (Count == 0) throw new InvalidOperationException("The stack is empty.");
        T item = _items[--Count];
        _items[Count] = default!;
        return item;
    }

    public T Peek() =>
        Count == 0 ? throw new InvalidOperationException("The stack is empty.") : _items[Count - 1];
}
