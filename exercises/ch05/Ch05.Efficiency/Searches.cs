namespace Ch05.Efficiency;

// The practical: six ways to answer "is this id in the collection?" Same answers, different costs.
public static class Searches
{
    // 1. A plain loop. O(n): checks items one by one until it finds a match.
    public static bool Loop(int[] items, int target)
    {
        for (int i = 0; i < items.Length; i++)
            if (items[i] == target) return true;
        return false;
    }

    // 2. LINQ Any. Still O(n): a loop, hidden behind a lambda.
    public static bool LinqAny(int[] items, int target) => items.Any(x => x == target);

    // 3. List.Contains. O(n) too; the library's loop is well optimised (vectorised).
    public static bool ListContains(List<int> items, int target) => items.Contains(target);

    // 4. Binary search on a SORTED array. O(log n) — but only if the array is already sorted.
    public static bool Binary(int[] sorted, int target) => Array.BinarySearch(sorted, target) >= 0;

    // 5. Sort first, then binary search: O(n log n) for ONE question. Worse than a loop!
    public static bool SortThenBinary(int[] items, int target)
    {
        int[] copy = (int[])items.Clone();
        Array.Sort(copy);
        return Array.BinarySearch(copy, target) >= 0;
    }

    // 6. A HashSet. O(1) on average per question, after O(n) to build it.
    public static bool Set(HashSet<int> set, int target) => set.Contains(target);
}
