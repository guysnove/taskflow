namespace Ch17.Legacy;

// Guided exercise 2: a "legacy" class that is hard to unit-test.
// A library charges late fees: 0.20 per day late, at most 5.00 per book.
// Weekends (Saturday, Sunday) don't count as late days.
// Borrowed books are read from a file, one per line: title;due-date (yyyy-MM-dd)
//
// Try to write a test for Total() and notice what gets in the way:
//   1. DateTime.Now — the answer changes every day
//   2. File.ReadAllText — every test needs a real file on disk
//   3. Console.WriteLine — the result is printed, not returned
// Your job: make it testable WITHOUT changing what it calculates, then test it.
public class LateFees
{
    public decimal Total(string path)
    {
        decimal total = 0;
        foreach (var line in File.ReadAllText(path).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(';');
            var due = DateTime.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture).Date;
            int lateDays = 0;
            for (var day = due.AddDays(1); day <= DateTime.Now.Date; day = day.AddDays(1))
            {
                if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) lateDays++;
            }
            decimal fee = Math.Min(lateDays * 0.20m, 5.00m);
            if (fee > 0) Console.WriteLine($"{parts[0]}: {lateDays} day(s) late, {fee:0.00}");
            total += fee;
        }
        Console.WriteLine($"Total: {total:0.00}");
        return total;
    }
}
