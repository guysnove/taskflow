using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Integration tests: real HTTP requests to the whole app, running in memory.
// IClassFixture: one factory (one app, one database) shared by the tests in this class.
public class SiteTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public SiteTests(TaskFlowFactory factory) => _factory = factory;

    // AllowAutoRedirect = false: we want to see the 302 itself, not the page it points to.
    private HttpClient NewBrowser() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    [Trait("Req", "NFR-OPS-1")]
    public async Task Health_ReturnsHealthy()
    {
        var response = await NewBrowser().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // Course 2, Chapter 29: a dead email makes /health/details Degraded, but NOT /health: deploys (Chapter 24) and
    // container health checks (Chapter 25) restart or roll back on /health, and a mail problem isn't fixed by that.
    [Fact]
    [Trait("Req", "NFR-OPS-2")]
    public async Task ADeadEmail_ShowsInHealthDetails_ButNotInHealth()
    {
        using var factory = new TaskFlowFactory();
        var client = factory.CreateClient();
        await factory.DbAsync(async db =>
        {
            db.OutboxEmails.Add(new Data.OutboxEmail
            {
                Key = "test:dead", To = "ana@example.com", Subject = "s", Body = "b",
                CreatedAt = DateTime.UtcNow, NextAttemptAt = DateTime.UtcNow, DeadAt = DateTime.UtcNow, Attempts = 6
            });
            return await db.SaveChangesAsync();
        });

        Assert.Equal("Healthy", await client.GetStringAsync("/health"));
        var details = await client.GetAsync("/health/details");
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await details.Content.ReadAsStringAsync());
        Assert.Equal("Degraded", json.RootElement.GetProperty("status").GetString());
        var outbox = json.RootElement.GetProperty("checks").GetProperty("email-outbox");
        Assert.Equal(1, outbox.GetProperty("data").GetProperty("dead_last_24h").GetInt32());
    }

    [Fact]
    [Trait("Req", "FR-ACC-1")]
    public async Task TasksPage_WhenSignedOut_RedirectsToSignIn()
    {
        var response = await NewBrowser().GetAsync("/tasks");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/account/signin", response.Headers.Location!.ToString());
    }

    [Fact]
    [Trait("Req", "FR-ACC-1")]
    public async Task Api_WhenSignedOut_Returns401()
    {
        var response = await NewBrowser().GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Req", "NFR-SEC-1")]
    public async Task EveryPage_HasTheSecurityHeaders()
    {
        var response = await NewBrowser().GetAsync("/");
        Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    [Trait("Req", "FR-OWN-1")]
    public async Task Ben_CannotReadOrDelete_AnasTask()
    {
        var ana = NewBrowser();
        var ben = NewBrowser();
        await Browser.RegisterAsync(ana, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        await Browser.RegisterAsync(ben, $"ben-{Guid.NewGuid():N}@example.com", "Ben");

        // Ana creates a task through the API, with her token.
        string anaToken = await Browser.TokenFromAsync(ana, "/tasks/quick");
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(new { title = "Ana's private task", priority = "High" }),
        };
        create.Headers.Add("X-CSRF-TOKEN", anaToken);
        var created = await ana.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        string location = created.Headers.Location!.AbsolutePath;      // e.g. /api/tasks/5

        // Ana can read it; Ben gets 404, for reading and for deleting.
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync(location)).StatusCode);

        string benToken = await Browser.TokenFromAsync(ben, "/tasks/quick");
        var delete = new HttpRequestMessage(HttpMethod.Delete, location);
        delete.Headers.Add("X-CSRF-TOKEN", benToken);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.SendAsync(delete)).StatusCode);
    }

    // Course 2, Chapter 2: the add form warns about a probable duplicate, and allows it once confirmed.
    [Fact]
    [Trait("Req", "FR-DUP-1")]
    public async Task AddForm_WarnsAboutDuplicateTitle_UntilConfirmed()
    {
        var ana = NewBrowser();
        await Browser.RegisterAsync(ana, $"ana-{Guid.NewGuid():N}@example.com", "Ana");

        async Task<HttpResponseMessage> Add(string title, bool allow)
        {
            string token = await Browser.TokenFromAsync(ana, "/tasks/new");
            var form = new Dictionary<string, string>
            {
                ["Title"] = title, ["Priority"] = "Medium", ["__RequestVerificationToken"] = token,
            };
            if (allow) form["AllowDuplicate"] = "true";
            return await ana.PostAsync("/tasks/new", new FormUrlEncodedContent(form));
        }

        Assert.Equal(HttpStatusCode.Redirect, (await Add("Renew passport", false)).StatusCode);   // added

        var again = await Add("  renew PASSPORT ", false);                                          // probable duplicate
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);                                          // the form again...
        Assert.Contains("You already have an open task called", await again.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Redirect, (await Add("Renew passport", true)).StatusCode);    // confirmed: added
    }

    // Course 2, Chapter 3: searching the task list.
    [Fact]
    [Trait("Req", "FR-SRCH-1")]
    public async Task TaskList_Search_ShowsOnlyMatchingTasks()
    {
        var ana = NewBrowser();
        await Browser.RegisterAsync(ana, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        foreach (var title in new[] { "Fix login bug", "Plan the budget" })
        {
            string token = await Browser.TokenFromAsync(ana, "/tasks/new");
            await ana.PostAsync("/tasks/new", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Title"] = title, ["Priority"] = "Medium", ["__RequestVerificationToken"] = token,
            }));
        }

        await ana.GetStringAsync("/tasks");          // shows (and uses up) the "Added …" message first
        string html = await ana.GetStringAsync("/tasks?q=LOGIN");
        Assert.Contains("Fix login bug", html);
        Assert.DoesNotContain("Plan the budget", html);

        string none = await ana.GetStringAsync("/tasks?q=holiday");
        Assert.Contains("No tasks match", none);
    }

    [Fact]
    [Trait("Req", "NFR-SEC-2")]
    public async Task ApiPost_WithoutToken_Returns400()
    {
        var ana = NewBrowser();
        await Browser.RegisterAsync(ana, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        var response = await ana.PostAsync("/api/tasks", JsonContent.Create(new { title = "No token", priority = "Low" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
