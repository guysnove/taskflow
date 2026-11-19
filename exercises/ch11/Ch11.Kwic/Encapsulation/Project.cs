namespace Ch11.Kwic.Encapsulation;

// ENCAPSULATED: the data is private; the only way in is through methods that keep the rules.
public class Project
{
    private readonly List<string> _tasks = new();
    private readonly HashSet<string> _done = new();

    public IReadOnlyList<string> Tasks => _tasks;        // read-only view: callers can look, not change
    public int OpenCount => _tasks.Count - _done.Count;  // computed, so it can never be wrong

    public void Add(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A task needs a title.", nameof(title));
        if (_tasks.Contains(title)) throw new InvalidOperationException($"\"{title}\" is already in the project.");
        _tasks.Add(title);
    }

    public void Complete(string title)
    {
        if (!_tasks.Contains(title)) throw new InvalidOperationException($"\"{title}\" is not in the project.");
        _done.Add(title);
    }
}
