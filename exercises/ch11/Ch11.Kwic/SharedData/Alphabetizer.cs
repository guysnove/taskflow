using System.Text;

namespace Ch11.Kwic.SharedData;

// Module 3: Alphabetizer. Needs the text of each shift, so it walks the packed characters too.
public static class Alphabetizer
{
    public static void Sort()
    {
        Store.Order.AddRange(Enumerable.Range(0, Store.Shifts.Count));
        Store.Order.Sort((a, b) => string.Compare(Text(a), Text(b), StringComparison.OrdinalIgnoreCase));
    }

    private static string Text(int shift)
    {
        var (line, start) = Store.Shifts[shift];
        var text = new StringBuilder();
        for (int i = start; i < Store.LineEnd(line); i++) text.Append(Store.Chars[i]);
        if (start > Store.LineStart[line]) text.Append(' ');
        for (int i = Store.LineStart[line]; i < start - 1; i++) text.Append(Store.Chars[i]);
        return text.ToString();
    }
}
