namespace Ch11.Kwic.SharedData;

// Module 1: Input. Packs each line's characters into Store.Chars (single spaces between words).
public static class Input
{
    public static void Read(IEnumerable<string> lines)
    {
        foreach (string raw in lines)
        {
            string line = string.Join(' ', raw.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (line.Length == 0) continue;
            Store.LineStart.Add(Store.Chars.Count);
            Store.Chars.AddRange(line);
        }
    }
}
