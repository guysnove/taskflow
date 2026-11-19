
namespace TaskFlow.Models;

// Course 2, Chapter 3: searching the task list by words.
// A task matches when EVERY word of the search appears as a word in its title,
// in any order, ignoring upper/lower case. "bug login" matches "Fix login bug".
public static class TaskSearch
{
    private static readonly char[] Separators = [' ', ',', '.', '!', '?', ':', ';', '-', '(', ')', '/'];

    // A set: each word once, and "is it in here?" is O(1) on average.
    public static HashSet<string> Words(string? text) =>
        new((text ?? "").Split(Separators, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);

    public static List<TaskItem> Filter(IEnumerable<TaskItem> tasks, string? query)
    {
        var wanted = Words(query);
        if (wanted.Count == 0) return tasks.ToList();              // no search: everything
        return tasks.Where(t => wanted.IsSubsetOf(Words(t.Title))).ToList();
    }
}
