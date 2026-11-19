using System.Text;

namespace Ch11.Kwic.SharedData;

// Module 4: Output. Walks the packed characters again, in sorted order.
public static class Output
{
    public static List<string> Lines()
    {
        var result = new List<string>();
        foreach (int shift in Store.Order)
        {
            var (line, start) = Store.Shifts[shift];
            var text = new StringBuilder();
            for (int i = start; i < Store.LineEnd(line); i++) text.Append(Store.Chars[i]);
            if (start > Store.LineStart[line]) text.Append(' ');
            for (int i = Store.LineStart[line]; i < start - 1; i++) text.Append(Store.Chars[i]);
            result.Add(text.ToString());
        }
        return result;
    }
}
