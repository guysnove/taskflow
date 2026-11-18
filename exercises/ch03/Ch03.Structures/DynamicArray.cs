namespace Ch03.Structures;

// Chapter 3: a growable array, built from scratch. This is the idea inside List<T>.
// The items live in one fixed-size array; when it's full, we allocate a bigger one and copy.
public class DynamicArray<T>
{
    private T[] _items = new T[4];   // the backing array: contiguous, fixed size
    public int Count { get; private set; }
    public int Capacity => _items.Length;

    public T this[int index]
    {
        get { CheckIndex(index); return _items[index]; }        // O(1): one calculation, one read
        set { CheckIndex(index); _items[index] = value; }
    }

    public void Add(T item)                                       // O(1) on average
    {
        if (Count == _items.Length) Grow();
        _items[Count] = item;
        Count++;
    }

    public void Insert(int index, T item)                         // O(n): shifts everything after index
    {
        if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (Count == _items.Length) Grow();
        for (int i = Count; i > index; i--) _items[i] = _items[i - 1];
        _items[index] = item;
        Count++;
    }

    public void RemoveAt(int index)                               // O(n): shifts everything after index
    {
        CheckIndex(index);
        for (int i = index; i < Count - 1; i++) _items[i] = _items[i + 1];
        Count--;
        _items[Count] = default!;                                 // let the garbage collector reclaim it
    }

    private void Grow()
    {
        var bigger = new T[_items.Length * 2];                    // double: copies stay rare
        Array.Copy(_items, bigger, Count);
        _items = bigger;
    }

    private void CheckIndex(int index)
    {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
    }
}
