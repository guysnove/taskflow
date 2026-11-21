using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Data;

namespace TaskFlow.Tests.Integration;

// Starts the real TaskFlow in memory, for integration tests: real middleware, real controllers, real database.
// Each factory gets its own SQLite file, so tests never touch taskflow.db.
public class TaskFlowFactory : WebApplicationFactory<Program>
{
    public string MailFolder { get; } = Path.Combine(Path.GetTempPath(), $"taskflow-mail-{Guid.NewGuid():N}");
    private readonly string _dbFile = Path.Combine(Path.GetTempPath(), $"taskflow-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:TaskFlow", $"Data Source={_dbFile}");
        // Chapter 30: every test registers new users, all from the same address.
        // The real limit (5 a minute) would answer 429 after five; the tests aren't attackers.
        builder.UseSetting("RateLimits:SignInPerMinute", "1000");
        // Course 2, Chapter 10: no reminder timer in tests (they call DueReminders directly),
        // and emails go to a folder of this test run.
        builder.UseSetting("Notifications:ReminderInterval", "00:00:00");
        builder.UseSetting("Email:PickupDirectory", MailFolder);
        builder.UseEnvironment("Development");
    }

    // Course 2, Chapter 18: look straight into the database, the way the app does: a scope, a DbContext.
    // For checking what a request really stored — things the HTTP answer doesn't show.
    public async Task<T> DbAsync<T>(Func<TaskFlowDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>());
    }

    // Course 2, Chapter 18: a new account with a unique email (tests share the database, never their data),
    // signed in through the real register form. The client keeps the cookie; redirects are not followed.
    public async Task<(HttpClient Client, string Email)> SignedInClientAsync(string name = "Ana")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var response = await Browser.RegisterAsync(client, email, name);
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);       // registered and signed in
        return (client, email);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbFile)) File.Delete(_dbFile);
        if (Directory.Exists(MailFolder)) Directory.Delete(MailFolder, recursive: true);
    }
}
