namespace Ch05.Efficiency;

// Guided exercise 1: what is the Big O of each method? Decide first, then verify by counting steps.
public static class Practice
{
    public static long A(int[] items)
    {
        long total = 0;
        foreach (int x in items) total += x;
        foreach (int x in items) total -= x / 2;
        return total;
    }

    public static int B(int n)
    {
        int count = 0;
        while (n > 1) { n /= 3; count++; }
        return count;
    }

    public static long C(int[] items)
    {
        long total = 0;
        for (int i = 0; i < items.Length; i++)
            for (int k = 1; k < items.Length; k *= 2)
                total += items[i] % k;
        return total;
    }

    public static int D(int[] items)
    {
        int count = 0;
        for (int i = 0; i < items.Length; i++)
            for (int j = 0; j < i; j++)
                if (items[i] < items[j]) count++;
        return count;
    }

    public static int E(int[] items)
    {
        int count = 0;
        foreach (int x in items)
            for (int bit = 0; bit < 32; bit++)
                if ((x & (1 << bit)) != 0) count++;
        return count;
    }

    public static long F(int n) => n <= 0 ? 1 : F(n - 1) + F(n - 1);
}
