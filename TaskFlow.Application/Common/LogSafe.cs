namespace TaskFlow.Common;

// Course 2, Chapter 20 (security review, finding F2): what may go into a log.
// Logs are kept longer, copied to more places and read by more people than the database.
// Rule: ids and counts, yes; the user's own text (titles, notes, names), never; email addresses masked.
public static class LogSafe
{
    // "ana.lopez@example.com" -> "a***@example.com": enough to tell which domain is failing, no more.
    public static string Email(string? address)
    {
        if (string.IsNullOrEmpty(address)) return "(none)";
        int at = address.IndexOf('@', StringComparison.Ordinal);
        return at <= 0 ? "***" : $"{address[0]}***{address[at..]}";
    }
}
