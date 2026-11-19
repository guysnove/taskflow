namespace Ch11.Kwic.Hiding;

// Alphabetizer hides the sorting method. It offers "the i-th shift in order".
public sealed class Alphabetizer
{
    private readonly List<int> _order;

    public Alphabetizer(CircularShifter shifts)
    {
        string Text(int s) => string.Join(' ', Enumerable.Range(0, shifts.WordCount(s)).Select(w => shifts.Word(s, w)));
        _order = Enumerable.Range(0, shifts.Count).ToList();
        _order.Sort((a, b) => string.Compare(Text(a), Text(b), StringComparison.OrdinalIgnoreCase));
    }

    public int Ith(int i) => _order[i];
}
