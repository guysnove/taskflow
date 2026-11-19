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

namespace Ch10.Patterns.Tests;

public class StrategyTests
{
    private static readonly Item[] Items =
    [
        new(1, "b", "Low", new DateOnly(2026, 11, 20)),
        new(2, "a", "High", null),
        new(3, "c", "Medium", new DateOnly(2026, 11, 19), Done: true),
        new(4, "d", "High", new DateOnly(2026, 11, 18)),
    ];

    [Fact]
    public void ThreeImplementations_SameAnswers()
    {
        Assert.Equal([4, 1, 2, 3], new ByDue().Apply(Items).Select(i => i.Id));
        Assert.Equal([4, 1, 2, 3], Orderings.ByName["due"](Items).Select(i => i.Id));
        Assert.Equal([4, 1, 2, 3], OrderingSwitch.Apply("due", Items).Select(i => i.Id));

        Assert.Equal([2, 4, 1, 3], new ByPriority().Apply(Items).Select(i => i.Id));
        Assert.Equal([2, 4, 1, 3], OrderingSwitch.Apply("priority", Items).Select(i => i.Id));
    }
}

public class ObserverTests
{
    [Fact]
    public void Event_TellsEverySubscriber()
    {
        var list = new TaskList();
        var stats = new Statistics();
        list.Completed += stats.Handle;

        list.Complete(new Item(1, "a", "Low", null));

        Assert.Equal(1, stats.CompletedCount);
    }

    // The classic trap: a subscriber that is "finished with" but never unsubscribed is still called,
    // and the subject keeps it alive (a memory leak in long-running programs).
    [Fact]
    public void Event_ForgottenSubscriber_IsStillCalled_UntilUnsubscribed()
    {
        var list = new TaskList();
        var old = new Statistics();
        list.Completed += old.Handle;
        // ... the screen that used "old" is closed, but nobody wrote: list.Completed -= old.Handle;
        list.Complete(new Item(1, "a", "Low", null));
        Assert.Equal(1, old.CompletedCount);

        list.Completed -= old.Handle;
        list.Complete(new Item(2, "b", "Low", null));
        Assert.Equal(1, old.CompletedCount);                // unsubscribed: no longer called
    }

    [Fact]
    public void ObserverInterface_GivenThroughTheConstructor()
    {
        var a = new Statistics();
        var b = new Statistics();
        new TaskListWithObservers([a, b]).Complete(new Item(1, "a", "Low", null));
        Assert.Equal((1, 1), (a.CompletedCount, b.CompletedCount));
    }
}

public class FactoryTests
{
    [Fact]
    public void SimpleFactory_And_KeyedServices_MakeTheSameThing()
    {
        using var services = KeyedSenders.Build();
        Assert.Equal("sms to Sam: hi", SenderFactory.Create("sms").Send("Sam", "hi"));
        Assert.Equal("sms to Sam: hi", services.GetRequiredKeyedService<ISender>("sms").Send("Sam", "hi"));
    }

    [Fact]
    public void UnknownNames_FailClearly()
    {
        using var services = KeyedSenders.Build();
        Assert.Throws<ArgumentException>(() => SenderFactory.Create("pigeon"));
        Assert.Throws<InvalidOperationException>(() => services.GetRequiredKeyedService<ISender>("pigeon"));
    }
}

public class DecoratorAndAdapterTests
{
    [Fact]
    public void CachingDecorator_CallsTheSlowServiceOncePerPair()
    {
        var slow = new SlowRates();
        IExchangeRates rates = new CachingRates(slow);
        for (int i = 0; i < 1000; i++)
        {
            rates.Rate("EUR", "USD");
        }
        rates.Rate("USD", "EUR");
        Assert.Equal(2, slow.Calls);
    }

    [Fact]
    public void Adapter_TurnsFahrenheitTextIntoCelsius() =>
        Assert.Equal(22.0, new VendorThermometerAdapter(new VendorThermometer()).Celsius());
}

public class RepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly List<string> _sql = new();
    private readonly Db _db;

    public RepositoryTests()
    {
        _connection.Open();
        _db = new Db(new DbContextOptionsBuilder<Db>().UseSqlite(_connection)
            .LogTo(m => _sql.Add(m), [RelationalEventId.CommandExecuted]).Options);
        _db.Database.EnsureCreated();
        _db.Rows.AddRange(Enumerable.Range(1, 1000).Select(i => new Row { Title = $"Task {i}", Done = i % 2 == 0 }));
        _db.SaveChanges();
        _sql.Clear();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GenericRepository_LoadsTheWholeTable_ThenFiltersInCSharp()
    {
        var open = new GenericRepository<Row>(_db).GetAll().Where(r => !r.Done).Take(5).ToList();

        Assert.Equal(5, open.Count);
        string sql = Assert.Single(_sql);
        Assert.DoesNotContain("WHERE", sql);                 // all 1,000 rows came back
    }

    [Fact]
    public void IntentionRevealingRepository_LetsTheDatabaseFilter()
    {
        var open = new RowRepository(_db).Open(5);

        Assert.Equal(5, open.Count);
        string sql = Assert.Single(_sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("LIMIT", sql);                       // only 5 rows came back
    }
}
