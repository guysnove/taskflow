using System.Text;

namespace Ch05.Efficiency;

// Small methods to analyse. Work out each one's Big O before reading the comment.
public static class Snippets
{
    // Two loops one after the other: O(n) + O(n) = O(n).
    public static (int Min, int Max) MinAndMax(int[] items)
    {
        int min = int.MaxValue, max = int.MinValue;
        foreach (int x in items) if (x < min) min = x;
        foreach (int x in items) if (x > max) max = x;
        return (min, max);
    }

    // A loop inside a loop over the same data: O(n²).
    public static bool HasDuplicatePair(int[] items)
    {
        for (int i = 0; i < items.Length; i++)
            for (int j = i + 1; j < items.Length; j++)
                if (items[i] == items[j]) return true;
        return false;
    }

    // Two DIFFERENT inputs: O(a × b), not O(n²).
    public static int CommonCount(string[] a, string[] b)
    {
        int count = 0;
        foreach (string x in a)
            foreach (string y in b)
                if (x == y) count++;
        return count;
    }

    // Doubling i each time: the loop runs about log₂ n times. O(log n).
    public static int CountDoublings(int n)
    {
        int count = 0;
        for (int i = 1; i < n; i *= 2) count++;
        return count;
    }

    // A hidden loop: each += copies the whole string built so far. O(n²) characters copied.
    public static string JoinSlow(string[] words)
    {
        string result = "";
        foreach (string w in words) result += w + ",";
        return result;
    }

    // StringBuilder appends into a growing buffer (like List<T>): O(n) overall.
    public static string JoinFast(string[] words)
    {
        var sb = new StringBuilder();
        foreach (string w in words) sb.Append(w).Append(',');
        return sb.ToString();
    }

    // A constant-size inner loop is NOT a loop over n: 7 days × n tasks = O(n).
    public static int[] CountByWeekday(DateOnly[] dates)
    {
        var counts = new int[7];
        foreach (DateOnly d in dates)
            for (int day = 0; day < 7; day++)
                if ((int)d.DayOfWeek == day) counts[day]++;
        return counts;
    }
}
