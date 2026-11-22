using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskFlow.Data;
using TaskFlow.Services;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 14: a performance fitness function. Run the REAL repository query, capture the SQL
// EF Core sends, and ask SQLite how it will execute it (EXPLAIN QUERY PLAN).
public sealed class QueryPlanTests : IDisposable
{
    private sealed class SignedInUser(int id) : ICurrentUser
    {
        public bool IsSignedIn => true;
        public int Id => id;
    }

    // Remembers every command EF Core runs, with its parameters.
    private sealed class Capture : DbCommandInterceptor
    {
        public List<(string Sql, List<(string Name, object? Value)> Parameters)> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add((command.CommandText, command.Parameters.Cast<DbParameter>().Select(p => (p.ParameterName, p.Value)).ToList()));
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Capture _capture = new();
    private readonly TaskFlowDbContext _db;

    public QueryPlanTests()
    {
        _connection.Open();
        _db = new TaskFlowDbContext(new DbContextOptionsBuilder<TaskFlowDbContext>().UseSqlite(_connection).AddInterceptors(_capture).Options);
        _db.Database.Migrate();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private List<string> Plan(string sql, List<(string Name, object? Value)> parameters)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "EXPLAIN QUERY PLAN " + sql;
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        using var r = cmd.ExecuteReader();
        var steps = new List<string>();
        while (r.Read()) steps.Add(r.GetString(3));
        return steps;
    }

    [Fact]
    [Trait("Req", "NFR-PERF-2")]
    public async Task TheOverdueCount_UsesAnIndexOnUserAndDue()
    {
        var repository = new EfTaskRepository(_db, TimeProvider.System, new SignedInUser(7));
        await repository.CountDueAsync(new DateOnly(2026, 11, 19), 2);

        var (sql, parameters) = _capture.Commands.First(c => c.Sql.Contains("COUNT(*)", StringComparison.Ordinal));
        var plan = Plan(sql, parameters);
        Console.WriteLine(string.Join(Environment.NewLine, plan.Prepend(sql)));
        Assert.Contains(plan, step => step.Contains("IX_Tasks_UserId_Due", StringComparison.Ordinal));
    }

    // Course 2, Chapter 26: the task list asks for the step counts of a whole page in ONE query, however many tasks
    // are on it. One query per task (an "N+1") would be 51 round trips for a page of 50.
    [Fact]
    [Trait("Req", "NFR-PERF-5")]
    public async Task StepCounts_ForAPageOfTasks_AreOneQuery()
    {
        var repository = new EfTaskRepository(_db, TimeProvider.System, new SignedInUser(7));
        _capture.Commands.Clear();

        await repository.CountStepsAsync(Enumerable.Range(1, 50).ToList());

        Assert.Single(_capture.Commands);
    }
}
