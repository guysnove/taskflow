using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Starts the real TaskFlow in memory, for integration tests: real middleware, real controllers, real database.
// Each factory gets its own SQLite file, so tests never touch taskflow.db.
public class TaskFlowFactory : WebApplicationFactory<Program>
{
    private readonly string _dbFile = Path.Combine(Path.GetTempPath(), $"taskflow-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:TaskFlow", $"Data Source={_dbFile}");
        // Chapter 30: every test registers new users, all from the same address.
        // The real limit (5 a minute) would answer 429 after five; the tests aren't attackers.
        builder.UseSetting("RateLimits:SignInPerMinute", "1000");
        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbFile)) File.Delete(_dbFile);
    }
}
