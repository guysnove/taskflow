namespace Ch11.Kwic.Hiding;

// Parnas's SECOND decomposition: each module hides one design decision.
// This one hides HOW LINES ARE STORED. Nobody else knows the characters are packed.
public interface ILineStorage
{
    void AddLine(IReadOnlyList<string> words);
    int LineCount { get; }
    int WordCount(int line);
    string Word(int line, int word);
}

public sealed class PackedLineStorage : ILineStorage
{
    private readonly List<char> _chars = new();
    private readonly List<int> _lineStart = new();
    private readonly List<List<int>> _wordStart = new();

    public void AddLine(IReadOnlyList<string> words)
    {
        _lineStart.Add(_chars.Count);
        var starts = new List<int>();
        foreach (string w in words)
        {
            starts.Add(_chars.Count);
            _chars.AddRange(w);
        }
        _wordStart.Add(starts);
    }

    public int LineCount => _lineStart.Count;
    public int WordCount(int line) => _wordStart[line].Count;

    public string Word(int line, int word)
    {
        int start = _wordStart[line][word];
        int end = word + 1 < _wordStart[line].Count ? _wordStart[line][word + 1]
                : line + 1 < _lineStart.Count ? _lineStart[line + 1] : _chars.Count;
        return new string(_chars.GetRange(start, end - start).ToArray());
    }
}
