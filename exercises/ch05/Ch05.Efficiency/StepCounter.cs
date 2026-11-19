namespace Ch05.Efficiency;

// Counting steps instead of timing them: the count depends only on n, not on the machine.
// Each method returns how many times its innermost line ran.
public static class StepCounter
{
    public static long Constant(int n) => 1;                     // e.g. items[0], or a dictionary look-up

    public static long Logarithmic(int n)                        // e.g. binary search: halve until one is left
    {
        long steps = 0;
        for (int size = n; size > 1; size /= 2) steps++;
        return steps;
    }

    public static long Linear(int n)                             // e.g. find the largest item
    {
        long steps = 0;
        for (int i = 0; i < n; i++) steps++;
        return steps;
    }

    public static long Linearithmic(int n)                       // e.g. merge sort: log n levels, n work per level
    {
        long steps = 0;
        for (int size = n; size > 1; size /= 2)
            for (int i = 0; i < n; i++) steps++;
        return steps;
    }

    public static long Quadratic(int n)                          // e.g. compare every item with every item
    {
        long steps = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++) steps++;
        return steps;
    }

    public static long Triangular(int n)                         // e.g. compare each PAIR once: j starts after i
    {
        long steps = 0;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++) steps++;
        return steps;                                            // n(n − 1)/2: still O(n²)
    }

    public static long Exponential(int n)                        // e.g. try every subset of n items
    {
        long steps = 0;
        for (long subset = 0; subset < (1L << n); subset++) steps++;
        return steps;
    }
}
