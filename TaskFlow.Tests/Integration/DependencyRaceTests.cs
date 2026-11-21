using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 14: two requests add opposite dependencies AT THE SAME MOMENT:
// "Write copy waits for Get logo" and "Get logo waits for Write copy".
// Each request checks for a loop, finds none, and saves. Together, they make a loop.
// An interceptor makes the timing certain: each request pauses right after reading the graph,
// until the other one has read it too (or 3 seconds have passed).
public sealed class DependencyRaceTests : IDisposable
{
    private sealed class SignedInUser(int id) : ICurrentUser
    {
        public bool IsSignedIn => true;
        public int Id => id;
    }

    private sealed class PauseAfterReadingTheGraph(Barrier barrier) : DbCommandInterceptor
    {
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("SELECT \"d\".\"BlockedById\", \"d\".\"TaskId\"", StringComparison.Ordinal))
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(3), cancellationToken);   // wait for the other request
            }
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
    }

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"race-{Guid.NewGuid():N}.db");
    private readonly Barrier _barrier = new(2);
    private readonly int _userId, _copy, _logo;

    public DependencyRaceTests()
    {
        using var db = Context(intercept: false);
        db.Database.Migrate();
        var user = new User { Email = "ana@example.com", DisplayName = "Ana", PasswordHash = "x", CreatedOn = new DateOnly(2026, 11, 19) };
        db.Users.Add(user);
        db.SaveChanges();
        var copy = new TaskItem { Title = "Write copy", UserId = user.Id, CreatedOn = user.CreatedOn };
        var logo = new TaskItem { Title = "Get logo", UserId = user.Id, CreatedOn = user.CreatedOn };
        db.Tasks.AddRange(copy, logo);
        db.SaveChanges();
        (_userId, _copy, _logo) = (user.Id, copy.Id, logo.Id);
    }

    private TaskFlowDbContext Context(bool intercept = true)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContext>().UseSqlite($"Data Source={_path}");
        if (intercept) options.AddInterceptors(new PauseAfterReadingTheGraph(_barrier));
        return new TaskFlowDbContext(options.Options);
    }

    public void Dispose()
    {
        _barrier.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }

    private async Task<DependencyResult> AddAsync(int taskId, int blockedById)
    {
        await using var db = Context();
        return await new EfDependencyRepository(db, new SignedInUser(_userId)).AddAsync(taskId, blockedById);
    }

    [Fact]
    [Trait("Req", "NFR-CONC-2")]
    public async Task TwoOppositeDependencies_AtTheSameMoment_NeverMakeALoop()
    {
        var results = await Task.WhenAll(
            Task.Run(() => AddAsync(_copy, _logo)),      // Write copy waits for Get logo
            Task.Run(() => AddAsync(_logo, _copy)));     // Get logo waits for Write copy

        await using var db = Context(intercept: false);
        int saved = await db.Dependencies.CountAsync();
        Console.WriteLine($"Succeeded: {results.Count(r => r.Ok)} of 2. Dependencies saved: {saved}. " +
                          string.Join(" ", results.Where(r => !r.Ok).Select(r => r.Error)));
        Assert.Equal(1, results.Count(r => r.Ok));
        Assert.Equal(1, saved);
    }
}
