// Chapter 10 demo: one problem per pattern, and alternative implementations compared.
//   dotnet run -- strategy | observer | factory | decorator | adapter | repository
using Ch10.Patterns;
using Ch10.Patterns.Adapter;
using Ch10.Patterns.Decorator;
using Ch10.Patterns.Factory;
using Ch10.Patterns.Observer;
using Ch10.Patterns.Repository;
using Ch10.Patterns.Strategy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

Item[] items =
[
    new(1, "Book train", "Low", new DateOnly(2026, 11, 20)),
    new(2, "Archive files", "High", null),
    new(3, "Call supplier", "Medium", new DateOnly(2026, 11, 19), Done: true),
    new(4, "Write report", "High", new DateOnly(2026, 11, 18)),
];
string Titles(IEnumerable<Item> list) => string.Join(" | ", list.Select(i => i.Title + (i.Done ? " (done)" : "")));

switch (args.FirstOrDefault() ?? "strategy")
{
    case "strategy":
        foreach (IOrdering o in new IOrdering[] { new ByDue(), new ByPriority() })
            Console.WriteLine($"{o.Name,-9} {Titles(o.Apply(items))}");
        Console.WriteLine($"{"title",-9} {Titles(Orderings.ByName["title"](items))}   (a delegate)");
        break;

    case "observer":
        var list = new TaskList();
        var stats = new Statistics();
        list.Completed += stats.Handle;
        list.Completed += (_, item) => Console.WriteLine($"  [audit] completed: {item.Title}");
        list.Complete(items[0]);
        list.Complete(items[1]);
        Console.WriteLine($"Statistics saw {stats.CompletedCount} completions");
        break;

    case "factory":
        Console.WriteLine(SenderFactory.Create("email").Send("sam@example.com", "Due today: Write report"));
        using (var services = KeyedSenders.Build())
            Console.WriteLine(services.GetRequiredKeyedService<ISender>("sms").Send("+44 7700 900123", "Due today: Write report"));
        try { SenderFactory.Create("pigeon"); }
        catch (ArgumentException ex) { Console.WriteLine($"Unknown name: {ex.Message}"); }
        break;

    case "decorator":
        var slow = new SlowRates();
        IExchangeRates rates = new CachingRates(slow);
        decimal total = 0;
        for (int i = 0; i < 1000; i++) total += 10m * rates.Rate("EUR", "USD");
        Console.WriteLine($"1,000 conversions = {total:0.00} USD; calls to the slow service: {slow.Calls}");
        break;

    case "adapter":
        ITemperature t = new VendorThermometerAdapter(new VendorThermometer());
        Console.WriteLine($"Vendor says 71.6 °F; our code reads {t.Celsius()} °C");
        break;

    case "repository":
        using (var connection = new SqliteConnection("Data Source=:memory:"))
        {
            connection.Open();
            var sql = new List<string>();
            var options = new DbContextOptionsBuilder<Db>().UseSqlite(connection)
                .LogTo(m => sql.Add(m), [RelationalEventId.CommandExecuted]).Options;
            using var db = new Db(options);
            db.Database.EnsureCreated();
            db.Rows.AddRange(Enumerable.Range(1, 1000).Select(i => new Row { Title = $"Task {i}", Done = i % 2 == 0 }));
            db.SaveChanges();
            sql.Clear();

            var a = new GenericRepository<Row>(db).GetAll().Where(r => !r.Done).Take(5).ToList();
            Console.WriteLine($"Generic repository: {a.Count} rows wanted. The SQL it ran:");
            Console.WriteLine("  " + Select(sql.Last()));
            var b = new RowRepository(db).Open(5);
            Console.WriteLine($"RowRepository.Open(5): {b.Count} rows wanted. The SQL it ran:");
            Console.WriteLine("  " + Select(sql.Last()));
        }
        break;
}

// Just the SELECT … part of EF Core's log message.
static string Select(string logLine) =>
    string.Join(" ", logLine[logLine.IndexOf("SELECT", StringComparison.Ordinal)..].Split('\n').Select(l => l.Trim()));
