namespace Ch03.Structures.Search;

// Chapter 3 practical: the same two questions, answered with different data structures.
public record TaskRecord(int Id, string Title, bool Done);

public interface ITaskStore
{
    TaskRecord? FindById(int id);
    List<TaskRecord> SearchWord(string word);
}

public static class TextWords
{
    private static readonly char[] Separators = [' ', ',', '.', '!', '?', ':', ';', '-', '(', ')'];

    // "Fix login bug!" -> "fix", "login", "bug"
    public static IEnumerable<string> Words(string text) =>
        text.ToLowerInvariant().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
}

// Version 1: one List. Every question walks through it: O(n).
public class ListTaskStore(IEnumerable<TaskRecord> tasks) : ITaskStore
{
    private readonly List<TaskRecord> _tasks = tasks.ToList();

    public TaskRecord? FindById(int id)
    {
        foreach (var t in _tasks)
            if (t.Id == id) return t;
        return null;
    }

    public List<TaskRecord> SearchWord(string word)
    {
        string w = word.Trim().ToLowerInvariant();
        return _tasks.Where(t => TextWords.Words(t.Title).Contains(w)).ToList();
    }
}

// Version 2: build two dictionaries once. Questions become look-ups: O(1) on average.
public class IndexedTaskStore : ITaskStore
{
    private readonly Dictionary<int, TaskRecord> _byId = new();
    private readonly Dictionary<string, List<TaskRecord>> _byWord = new();   // an "inverted index"

    public IndexedTaskStore(IEnumerable<TaskRecord> tasks)
    {
        foreach (var t in tasks)
        {
            _byId.Add(t.Id, t);                                    // throws if an id repeats
            foreach (string w in TextWords.Words(t.Title).Distinct())
            {
                if (!_byWord.TryGetValue(w, out var list)) _byWord[w] = list = new List<TaskRecord>();
                list.Add(t);
            }
        }
    }

    public TaskRecord? FindById(int id) => _byId.GetValueOrDefault(id);

    // Return a copy: handing out our own list would let callers change the index.
    public List<TaskRecord> SearchWord(string word) =>
        _byWord.TryGetValue(word.Trim().ToLowerInvariant(), out var list) ? [.. list] : [];
}
