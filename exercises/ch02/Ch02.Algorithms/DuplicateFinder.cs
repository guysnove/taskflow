namespace Ch02.Algorithms;

// Chapter 2: finding duplicate customer records.
// Our definition: two records are the same customer when their emails match
// after normalising (ignore spaces around it, and upper/lower case).
public static class DuplicateFinder
{
    // The abstraction: reduce an email to the part that matters for comparing.
    public static string NormalizeEmail(string? email) =>
        (email ?? "").Trim().ToLowerInvariant();

    public static bool SameCustomer(Customer a, Customer b) =>
        NormalizeEmail(a.Email) != "" &&
        NormalizeEmail(a.Email) == NormalizeEmail(b.Email);

    // Algorithm 1: compare every record with every later record.
    // n records -> about n * n / 2 comparisons.
    public static List<(Customer, Customer)> FindPairs(IReadOnlyList<Customer> customers)
    {
        var pairs = new List<(Customer, Customer)>();
        for (int i = 0; i < customers.Count; i++)
        {
            for (int j = i + 1; j < customers.Count; j++)
            {
                if (SameCustomer(customers[i], customers[j]))
                {
                    pairs.Add((customers[i], customers[j]));
                }
            }
        }
        return pairs;
    }

    // Algorithm 2: put each record in a group keyed by its normalised email.
    // One pass over the data: n records -> n dictionary look-ups.
    public static List<List<Customer>> FindGroups(IEnumerable<Customer> customers)
    {
        var groups = new Dictionary<string, List<Customer>>();
        foreach (var c in customers)
        {
            string key = NormalizeEmail(c.Email);
            if (key == "") continue;                     // no email: can't match on it
            if (!groups.TryGetValue(key, out var group))
            {
                group = new List<Customer>();
                groups[key] = group;
            }
            group.Add(c);
        }
        return groups.Values.Where(g => g.Count > 1).ToList();
    }
}
