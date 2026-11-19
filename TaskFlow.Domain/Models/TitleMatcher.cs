
namespace TaskFlow.Models;

// Course 2, Chapter 2: spotting a task the user has probably added already.
// The algorithm: reduce each title to a "key" (the abstraction), then compare keys.
//   "  Buy MILK!" and "buy   milk" both become "buy milk".
public static class TitleMatcher
{
    public static string Key(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        string cleaned = title.ToLowerInvariant().Trim().TrimEnd('.', '!', '?');   // punctuation first...
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);      // ...then collapse spaces
        return string.Join(' ', words);
    }

    // The first open task whose title has the same key, or null. One pass: O(n).
    public static TaskItem? FindOpenDuplicate(string? title, IEnumerable<TaskItem> tasks)
    {
        string key = Key(title);
        if (key == "") return null;
        return tasks.FirstOrDefault(t => !t.Done && Key(t.Title) == key);
    }
}
