namespace Ch04.Algorithms;

// Chapter 4: searching and sorting.
public static class SearchSort
{
    public static int LinearSearch(int[] items, int target)       // O(n), any order
    {
        for (int i = 0; i < items.Length; i++)
            if (items[i] == target) return i;
        return -1;
    }

    public static int BinarySearch(int[] sorted, int target)      // O(log n), sorted input only
    {
        int lo = 0, hi = sorted.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;                          // avoids int overflow of (lo + hi)
            if (sorted[mid] == target) return mid;
            if (sorted[mid] < target) lo = mid + 1; else hi = mid - 1;
        }
        return -1;
    }

    public static void InsertionSort(int[] a)                      // O(n²); fast for small or nearly sorted
    {
        for (int i = 1; i < a.Length; i++)
        {
            int value = a[i], j = i - 1;
            while (j >= 0 && a[j] > value) { a[j + 1] = a[j]; j--; }  // shift larger items right
            a[j + 1] = value;
        }
    }

    public static int[] MergeSort(int[] a)                         // O(n log n): divide and conquer
    {
        if (a.Length <= 1) return a;
        int mid = a.Length / 2;
        int[] left = MergeSort(a[..mid]), right = MergeSort(a[mid..]);
        var merged = new int[a.Length];
        int i = 0, j = 0, k = 0;
        while (i < left.Length && j < right.Length)
            merged[k++] = left[i] <= right[j] ? left[i++] : right[j++];   // <= keeps it stable
        while (i < left.Length) merged[k++] = left[i++];
        while (j < right.Length) merged[k++] = right[j++];
        return merged;
    }

    // Dynamic programming: the fewest coins for an amount (Chapter 2's greedy counterexample).
    public static int? FewestCoins(int[] coins, int amount)
    {
        var best = new int?[amount + 1];
        best[0] = 0;                                               // 0 coins make 0
        for (int a = 1; a <= amount; a++)
            foreach (int c in coins)
                if (c <= a && best[a - c] is int prev && (best[a] is null || prev + 1 < best[a]))
                    best[a] = prev + 1;                            // reuse the answers to smaller amounts
        return best[amount];
    }
}
