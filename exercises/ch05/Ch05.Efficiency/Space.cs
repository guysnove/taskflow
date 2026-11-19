namespace Ch05.Efficiency;

// Space complexity: how much EXTRA memory an algorithm needs as n grows.
public static class Space
{
    // O(1) extra space: swaps in place, two indexes.
    public static void ReverseInPlace(int[] items)
    {
        for (int i = 0, j = items.Length - 1; i < j; i++, j--)
            (items[i], items[j]) = (items[j], items[i]);
    }

    // O(n) extra space: a whole new array.
    public static int[] ReversedCopy(int[] items)
    {
        var copy = new int[items.Length];
        for (int i = 0; i < items.Length; i++) copy[i] = items[items.Length - 1 - i];
        return copy;
    }

    // O(n) extra space you can't see: n stack frames.
    public static long SumRecursive(int[] items, int i = 0) =>
        i == items.Length ? 0 : items[i] + SumRecursive(items, i + 1);
}
