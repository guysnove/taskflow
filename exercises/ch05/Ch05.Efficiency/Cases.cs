namespace Ch05.Efficiency;

// Best, average and worst case: the same algorithm, different inputs.
public static class Cases
{
    // Linear search that reports how many comparisons it made.
    public static int ComparisonsToFind(int[] items, int target)
    {
        for (int i = 0; i < items.Length; i++)
            if (items[i] == target) return i + 1;
        return items.Length;                               // not found: compared with everything
    }

    // Insertion sort that counts how many items it shifted.
    public static long InsertionSortShifts(int[] items)
    {
        long shifts = 0;
        for (int i = 1; i < items.Length; i++)
        {
            int current = items[i], j = i - 1;
            while (j >= 0 && items[j] > current) { items[j + 1] = items[j]; j--; shifts++; }
            items[j + 1] = current;
        }
        return shifts;
    }
}
