using System.Security.Claims;
using System.Text.Encodings.Web;
using Ch18.Notes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Ch18.Notes.Tests;

// Starts the REAL app in memory: real routing, real authentication, real EF Core, real SQLite.
// Only three things change, and each one on purpose:
//   1. the database: a fresh SQLite file for this factory (never notes.db)
//   2. the clock:    a FakeTimeProvider the tests control
//   3. (optional)    sign-in: a test scheme that trusts an X-Test-User header
public class NotesFactory : WebApplicationFactory<Program>
{
    // A new, empty database file for every factory, deleted afterwards.
    protected virtual string DbFile { get; } = Path.Combine(Path.GetTempPath(), $"notes-test-{Guid.NewGuid():N}.db");
    protected virtual bool KeepDatabase => false;
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero));
    protected virtual bool TestSignIn => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Notes", $"Data Source={DbFile}");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);                 // the last registration wins
            if (TestSignIn)
            {
                services.AddAuthentication(TestAuth.Scheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuth>(TestAuth.Scheme, _ => { });
            }
        });
    }

    // Look straight into the database, the way the app would: a scope, a NotesDb.
    public async Task<T> DbAsync<T>(Func<NotesDb, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<NotesDb>());
    }

    // A client that is signed in as `name` (test sign-in only).
    public HttpClient ClientFor(string name)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuth.Header, name);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (!KeepDatabase && File.Exists(DbFile)) File.Delete(DbFile);
    }
}

public class TestSignInFactory : NotesFactory
{
    protected override bool TestSignIn => true;
}

// "Who is this?" answered from a header. ONLY for tests: anyone could send that header.
public class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "Test";
    public const string Header = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Header, out var name) || string.IsNullOrWhiteSpace(name))
            return Task.FromResult(AuthenticateResult.NoResult());       // not signed in
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, name.ToString())], Scheme);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
    }
}
