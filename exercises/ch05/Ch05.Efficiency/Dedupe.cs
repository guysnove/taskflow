namespace Ch05.Efficiency;

// Chapter 5's opening example: remove duplicate email addresses from an import, keeping the first of each.
public static class Dedupe
{
    // Looks innocent: one loop. But List.Contains is a loop too, hidden inside. O(n²).
    public static List<string> WithList(IEnumerable<string> emails)
    {
        var unique = new List<string>();
        foreach (string email in emails)
        {
            if (!unique.Contains(email))       // scans everything kept so far
                unique.Add(email);
        }
        return unique;
    }

    // The same result, with a set to answer "seen it?" in O(1) on average. O(n) overall.
    public static List<string> WithSet(IEnumerable<string> emails)
    {
        var seen = new HashSet<string>();
        var unique = new List<string>();
        foreach (string email in emails)
        {
            if (seen.Add(email))               // Add returns false if it was already there
                unique.Add(email);
        }
        return unique;
    }
}
