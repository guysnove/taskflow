namespace Ch11.Kwic.Hiding;

// CircularShifter hides how shifts are represented. It offers shifts as if they were lines.
public sealed class CircularShifter
{
    private readonly ILineStorage _lines;
    private readonly List<(int Line, int FirstWord)> _shifts = new();

    public CircularShifter(ILineStorage lines)
    {
        _lines = lines;
        for (int l = 0; l < lines.LineCount; l++)
            for (int w = 0; w < lines.WordCount(l); w++)
                _shifts.Add((l, w));
    }

    public int Count => _shifts.Count;
    public int WordCount(int shift) => _lines.WordCount(_shifts[shift].Line);

    public string Word(int shift, int word)
    {
        var (line, first) = _shifts[shift];
        return _lines.Word(line, (first + word) % _lines.WordCount(line));
    }
}
