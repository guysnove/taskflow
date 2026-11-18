namespace Ch04.Algorithms;

// Chapter 4: recursion — a method that solves a problem by calling itself on a smaller one.
public static class Recursion
{
    public static long Factorial(int n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
        if (n <= 1) return 1;                  // base case: stops the recursion
        return n * Factorial(n - 1);           // recursive case: a smaller problem
    }

    public static long FactorialLoop(int n)    // the same, with a loop
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
        long result = 1;
        for (int i = 2; i <= n; i++) result *= i;
        return result;
    }

    // Recursion shines on recursive data: a folder contains files and folders.
    public static long TotalSize(Folder folder) =>
        folder.Files.Sum(f => f.Bytes) + folder.Folders.Sum(TotalSize);

    // Fibonacci: 0, 1, 1, 2, 3, 5, 8… each number is the sum of the two before it.
    // Naive recursion recomputes the same values again and again: exponential time.
    public static long FibNaive(int n) => n < 2 ? n : FibNaive(n - 1) + FibNaive(n - 2);

    // Memoisation: remember each answer the first time; reuse it afterwards. O(n).
    public static long FibMemo(int n, Dictionary<int, long>? memo = null)
    {
        if (n < 2) return n;
        memo ??= new Dictionary<int, long>();
        if (memo.TryGetValue(n, out long known)) return known;
        long result = FibMemo(n - 1, memo) + FibMemo(n - 2, memo);
        memo[n] = result;
        return result;
    }

    // Deliberately unsafe: one stack frame per level. Used by the demo to show a stack overflow.
    public static int Depth(int n) => n == 0 ? 0 : 1 + Depth(n - 1);
}

public record FileItem(string Name, long Bytes);
public record Folder(string Name, List<FileItem> Files, List<Folder> Folders);
