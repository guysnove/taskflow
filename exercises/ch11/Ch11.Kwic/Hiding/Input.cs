namespace Ch11.Kwic.Hiding;

// Input hides the input format (here: words separated by spaces).
public static class Input
{
    public static void Read(IEnumerable<string> lines, ILineStorage storage)
    {
        foreach (string line in lines)
        {
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0) storage.AddLine(words);
        }
    }
}
