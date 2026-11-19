// Chapter 7 demo: print the digest for some sample tasks.
//   dotnet run -- before        the original DigestService
//   dotnet run -- after         the refactored DailyDigest
//   dotnet run -- clockbug      both, for a digest dated a week ago (the old code reads the real clock)
using Ch07.Digest;

var today = new DateOnly(2026, 11, 18);
List<TaskInfo> tasks =
[
    new(1, 7, "Send invoices", "High", today.AddDays(-2), false, 1, 3),
    new(2, 7, "Logo for Acme", "Medium", today, false, 0, 0),
    new(3, 7, "Brochure draft", "High", today.AddDays(1), false, 2, 5),
    new(4, 7, "Book the venue", "Low", today.AddDays(-1), false, 0, 0),
    new(5, 7, "Renew domain", "Low", today.AddDays(30), false, 0, 0),
    new(6, 7, "Client call notes", "Medium", null, false, 0, 0),
    new(7, 7, "Old report", "Medium", today.AddDays(-5), true, 2, 2),
    new(8, 9, "Someone else's task", "High", today, false, 0, 0),
];
var at = today.ToDateTime(new TimeOnly(8, 0));
string mode = args.FirstOrDefault() ?? "before";
if (mode == "clockbug")
{
    var realToday = DateOnly.FromDateTime(DateTime.Now);
    var weekAgo = realToday.AddDays(-7);
    List<TaskInfo> later = [new(1, 7, "Due in 5 days", "High", weekAgo.AddDays(5), false, 0, 0)];
    Console.WriteLine($"The machine's clock says {realToday:d MMM yyyy}. A digest for {weekAgo:d MMM yyyy}, one task due {weekAgo.AddDays(5):d MMM}:");
    Console.WriteLine("--- before ---");
    Console.Write(new Ch07.Digest.Before.DigestService().Make(later, 7, weekAgo.ToDateTime(new TimeOnly(8, 0)), false));
    Console.WriteLine("--- after ---");
    Console.Write(Ch07.Digest.After.DailyDigest.Write(later, weekAgo, includeDone: false));
    return;
}
Console.Write(mode == "after"
    ? Ch07.Digest.After.DailyDigest.Write(tasks.Where(t => t.UserId == 7), today, includeDone: true)
    : new Ch07.Digest.Before.DigestService().Make(tasks, 7, at, true));
