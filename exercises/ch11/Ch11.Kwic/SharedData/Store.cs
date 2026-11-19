namespace Ch11.Kwic.SharedData;

// Parnas's FIRST decomposition (1972): one module per processing step, all sharing these data.
// Every module below knows exactly how lines are stored: all characters of all lines packed into
// one list, with LineStart saying where each line begins. That knowledge is the problem.
public static class Store
{
    public static List<char> Chars { get; } = new();
    public static List<int> LineStart { get; } = new();
    public static List<(int Line, int CharIndex)> Shifts { get; } = new();
    public static List<int> Order { get; } = new();

    public static int LineEnd(int line) => line + 1 < LineStart.Count ? LineStart[line + 1] : Chars.Count;

    public static void Clear()
    {
        Chars.Clear(); LineStart.Clear(); Shifts.Clear(); Order.Clear();
    }
}
