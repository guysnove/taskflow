using System.Globalization;
using Ch22.Planning;

// Chapter 22: dotnet run --project Ch22.Planning -- <command> <file> [options]
//   prioritise <backlog.csv>                       the backlog in order
//   plan <backlog.csv> --capacity 20 [--sprints 3] [--as-ordered]
//                                                  fill sprints up to the team's capacity; --as-ordered keeps the
//                                                  file's order (a product owner's ranking) instead of sorting
//   velocity <sprints.csv>                         what the team actually finished
//   forecast <sprints.csv> --remaining 60          how many sprints, with 50% and 85% confidence
//   burndown <burndown.csv>                        a sprint's burndown, as text
//   flow <cards.csv> --from 2026-11-02 --to 2026-11-20   Kanban: cycle time, throughput, WIP
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
if (args.Length < 2) { Console.Error.WriteLine("usage: <prioritise|plan|velocity|forecast|burndown|flow> <file> [options]"); return 2; }
string command = args[0], file = args[1];
string? Opt(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

switch (command)
{
    case "prioritise":
        Console.WriteLine($"{"#",-3}{"Id",-7}{"Pri",-8}{"Pts",4}{"Value",7}{"V/pt",7}  Title");
        int rank = 1;
        foreach (var s in Backlog.Prioritise(Backlog.Read(file)))
            Console.WriteLine($"{rank++,-3}{s.Id,-7}{s.Priority,-8}{s.Points,4}{s.Value,7}{s.ValuePerPoint,7:0.00}  {s.Title}");
        var wont = Backlog.Read(file).Where(s => s.Priority == MoSCoW.Wont).ToList();
        if (wont.Count > 0) Console.WriteLine($"Not this release (Won't): {string.Join(", ", wont.Select(s => s.Id))}");
        return 0;

    case "plan":
        int capacity = int.Parse(Opt("--capacity") ?? "20");
        var stories = args.Contains("--as-ordered") ? Backlog.Ordered(Backlog.Read(file)) : Backlog.Prioritise(Backlog.Read(file));
        var sprints = Backlog.Plan(stories, capacity, int.Parse(Opt("--sprints") ?? "10"));
        foreach (var sp in sprints)
        {
            Console.WriteLine($"Sprint {sp.Number}: {sp.Points}/{capacity} points");
            foreach (var s in sp.Stories) Console.WriteLine($"   {s.Id,-7}{s.Points,3}  {s.Title}");
        }
        var left = stories.Except(sprints.SelectMany(sp => sp.Stories)).ToList();
        Console.WriteLine(left.Count == 0 ? "Everything fits." : $"Does not fit: {string.Join(", ", left.Select(s => s.Id))} ({left.Sum(s => s.Points)} points)");
        return 0;

    case "velocity":
        var rows = Backlog.Csv(file).ToList();
        foreach (var r in rows) Console.WriteLine($"Sprint {r[0],-3} committed {r[1],3}  done {r[2],3}  {new string('#', int.Parse(r[2]))}");
        var done = Velocity.Read(file);
        Console.WriteLine($"Average done: {done.Average():0.0}   last 3: {done.TakeLast(3).Average():0.0}   range: {done.Min()}–{done.Max()}");
        return 0;

    case "forecast":
        int remaining = int.Parse(Opt("--remaining") ?? throw new ArgumentException("--remaining is required"));
        var history = Velocity.Read(file);
        var (p50, p85) = Velocity.Forecast(history, remaining);
        Console.WriteLine($"{remaining} points, past sprints done: {string.Join(", ", history)}");
        Console.WriteLine($"Average velocity says: {remaining / history.Average():0.0} sprints");
        Console.WriteLine($"Monte Carlo (10,000 runs): 50% chance within {p50} sprints, 85% chance within {p85} sprints");
        return 0;

    case "burndown":
        var points = Backlog.Csv(file).Select(f => (Day: f[0], Left: int.Parse(f[1]))).ToList();
        int start = points[0].Left, days = points.Count - 1;
        for (int d = 0; d < points.Count; d++)
        {
            double ideal = start - (double)start * d / days;
            string bar = new string('#', points[d].Left).PadRight(start);
            int idealCol = (int)Math.Round(ideal);
            char[] line = bar.ToCharArray();
            if (idealCol > 0 && idealCol <= line.Length) line[idealCol - 1] = line[idealCol - 1] == '#' ? '|' : '.';
            Console.WriteLine($"{points[d].Day,-6}{points[d].Left,3} {new string(line)}");
        }
        Console.WriteLine("# = points left   . or | = the ideal line");
        return 0;

    case "flow":
        var cards = Flow.Read(file);
        var from = DateOnly.Parse(Opt("--from")!); var to = DateOnly.Parse(Opt("--to")!);
        var finished = cards.Where(c => c.Done is { } d && d >= from && d <= to).ToList();
        double weeks = (to.DayNumber - from.DayNumber + 1) / 7.0;
        double throughputPerDay = finished.Count / (double)(to.DayNumber - from.DayNumber + 1);
        double wip = Flow.AverageWip(cards, from, to);
        var cycle = finished.Select(c => c.CycleDays!.Value).ToList();
        Console.WriteLine($"Finished {finished.Count} cards in {weeks:0.#} weeks: throughput {finished.Count / weeks:0.0} per week");
        Console.WriteLine($"Cycle time (days): 50% within {Flow.Percentile(cycle, 0.50)}, 85% within {Flow.Percentile(cycle, 0.85)}, longest {cycle.Max()}");
        Console.WriteLine($"Average WIP: {wip:0.0} cards in progress per day; still open: {cards.Count(c => c.Done is null)}");
        Console.WriteLine($"Little's Law: average cycle time ≈ WIP / throughput = {wip:0.0} / {throughputPerDay:0.00} per day = {wip / throughputPerDay:0.0} days (measured average: {cycle.Average():0.0})");
        return 0;

    default:
        Console.Error.WriteLine($"unknown command: {command}");
        return 2;
}
