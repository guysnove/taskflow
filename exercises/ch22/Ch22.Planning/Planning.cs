using System.Globalization;

namespace Ch22.Planning;

// Chapter 22: small, honest planning tools. Every number they print comes from a CSV you can read.

public enum MoSCoW { Must, Should, Could, Wont }

public record Story(string Id, string Title, int Points, int Value, MoSCoW Priority)
{
    // "Value per point": a simple, visible way to put cheap, valuable work first (a light version of WSJF).
    public double ValuePerPoint => Points == 0 ? Value : (double)Value / Points;
}

public record Sprint(int Number, List<Story> Stories)
{
    public int Points => Stories.Sum(s => s.Points);
}

public static class Backlog
{
    // id,title,points,value,priority  (a header line; # starts a comment)
    public static List<Story> Read(string path) =>
        Csv(path).Select(f => new Story(f[0], f[1], Int(f[2]), Int(f[3]), Enum.Parse<MoSCoW>(f[4], ignoreCase: true))).ToList();

    // Must before Should before Could; inside each, the best value per point first; Won't is left out.
    public static List<Story> Prioritise(IEnumerable<Story> stories) =>
        stories.Where(s => s.Priority != MoSCoW.Wont)
               .OrderBy(s => s.Priority).ThenByDescending(s => s.ValuePerPoint).ThenBy(s => s.Id, StringComparer.Ordinal)
               .ToList();

    // The product owner's ranking: the order of the file, as it is (Won't still left out). A real backlog is
    // an ordered list: dependencies, risk and learning matter as well as value per point.
    public static List<Story> Ordered(IEnumerable<Story> stories) => stories.Where(s => s.Priority != MoSCoW.Wont).ToList();

    // Fill sprints in priority order, never above capacity. A story that doesn't fit waits for the next sprint
    // (smaller stories behind it may still fit: that's how real teams fill the last few points).
    public static List<Sprint> Plan(IEnumerable<Story> prioritised, int capacity, int maxSprints)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        var waiting = prioritised.ToList();
        var big = waiting.FirstOrDefault(s => s.Points > capacity);
        if (big is not null) throw new InvalidOperationException($"{big.Id} ({big.Points} points) is bigger than a whole sprint ({capacity}): split it.");
        var sprints = new List<Sprint>();
        for (int n = 1; n <= maxSprints && waiting.Count > 0; n++)
        {
            var sprint = new Sprint(n, []);
            foreach (var story in waiting.ToList())
            {
                if (sprint.Points + story.Points <= capacity)
                {
                    sprint.Stories.Add(story);
                    waiting.Remove(story);
                }
            }
            sprints.Add(sprint);
        }
        return sprints;
    }

    internal static IEnumerable<string[]> Csv(string path) =>
        File.ReadLines(path).Where(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith('#')).Skip(1)
            .Select(l => l.Split(',').Select(f => f.Trim()).ToArray());
    internal static int Int(string s) => int.Parse(s, CultureInfo.InvariantCulture);
}

public static class Velocity
{
    // sprint,committed,done
    public static List<int> Read(string path) => Backlog.Csv(path).Select(f => Backlog.Int(f[2])).ToList();

    // How many sprints to finish `remaining` points? Replay the team's real past sprints in random order,
    // many times, and see how often each answer comes up. Seeded, so the same data gives the same answer.
    public static (int P50, int P85) Forecast(IReadOnlyList<int> history, int remaining, int runs = 10_000, int seed = 22)
    {
        if (history.Count == 0 || history.All(v => v <= 0)) throw new ArgumentException("Need at least one sprint with work done.", nameof(history));
        var random = new Random(seed);
        var results = new int[runs];
        for (int r = 0; r < runs; r++)
        {
            int left = remaining, sprints = 0;
            while (left > 0) { left -= history[random.Next(history.Count)]; sprints++; }
            results[r] = sprints;
        }
        Array.Sort(results);
        return (results[(int)(runs * 0.50)], results[(int)(runs * 0.85)]);
    }
}

public record Card(string Id, DateOnly Started, DateOnly? Done)
{
    public int? CycleDays => Done is { } d ? d.DayNumber - Started.DayNumber + 1 : null;   // started and done the same day = 1 day
}

public static class Flow
{
    // id,started,done   (done empty = still in progress)
    public static List<Card> Read(string path) =>
        Backlog.Csv(path).Select(f => new Card(f[0], DateOnly.Parse(f[1], CultureInfo.InvariantCulture),
            f.Length > 2 && f[2].Length > 0 ? DateOnly.Parse(f[2], CultureInfo.InvariantCulture) : null)).ToList();

    public static int Percentile(IEnumerable<int> values, double p)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0) throw new ArgumentException("No values.", nameof(values));
        return sorted[(int)Math.Ceiling(p * sorted.Length) - 1];
    }

    // Average number of cards in progress per day, over [from, to].
    public static double AverageWip(IEnumerable<Card> cards, DateOnly from, DateOnly to)
    {
        int days = to.DayNumber - from.DayNumber + 1;
        int total = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            total += cards.Count(c => c.Started <= d && (c.Done is null || c.Done >= d));
        return (double)total / days;
    }
}
