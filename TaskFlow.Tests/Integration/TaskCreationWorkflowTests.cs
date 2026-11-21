using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Models;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 18: the whole "create a task" workflow, from an HTTP request to a row in the database.
// The real app, the real middleware (sign-in, anti-forgery, validation), real EF Core and SQLite.
// Only the clock is fake, so "created today" has a known answer, and the environment is "Testing".
public class WorkflowFactory : TaskFlowFactory
{
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // "Testing", not "Development": no sample tasks (in Development the FIRST account to register
        // adopts them, so a test's result depended on which test registered first), and errors are
        // handled the way they are in production.
        builder.UseEnvironment("Testing");
        // Outside Development, ASP.NET Core only finds CSS and JavaScript after `dotnet publish`.
        // The end-to-end test's screenshot showed a page with no styles; this serves them from the build.
        builder.UseStaticWebAssets();
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }
}

[Trait("Req", "FR-TASK-1")]
public class TaskCreationWorkflowTests(WorkflowFactory app) : IClassFixture<WorkflowFactory>
{
    private static readonly DateOnly Today = new(2026, 11, 23);

    private Task<TaskItem[]> TasksOfAsync(string email) =>
        app.DbAsync(db => db.Tasks.IgnoreQueryFilters().Where(t => t.User!.Email == email).ToArrayAsync());

    // The API trusts the sign-in cookie, so (like the form) it wants the anti-forgery token: in a header.
    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await Browser.TokenFromAsync(client, "/tasks/quick"));
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await Browser.TokenFromAsync(client, "/tasks/new");
        return await client.PostAsync("/tasks/new", new FormUrlEncodedContent(fields));
    }

    [Fact]
    public async Task TheForm_StoresTheTask_ForTheSignedInUser()
    {
        // Arrange: a new account, signed in through the real register form
        var (ana, email) = await app.SignedInClientAsync("Ana");

        // Act: what the browser sends when Ana fills in the form
        var response = await PostFormAsync(ana, new()
        {
            ["Title"] = "  Pay rent  ", ["Priority"] = "High", ["Due"] = "2026-12-01", ["Notes"] = "", ["ProjectId"] = "",
        });

        // Assert 1: the HTTP answer — back to the list, which says what happened
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/tasks", response.Headers.Location!.OriginalString);
        Assert.Contains("Added &quot;Pay rent&quot;.", await ana.GetStringAsync("/tasks"));

        // Assert 2: what the database really holds
        var task = Assert.Single(await TasksOfAsync(email));
        Assert.Equal(("Pay rent", "High", new DateOnly(2026, 12, 1), Today, false),
                     (task.Title, task.Priority, task.Due, task.CreatedOn, task.Done));
        Assert.Null(task.Notes);
        Assert.Null(task.ProjectId);
    }

    [Fact]
    public async Task TheApi_StoresTheTask_AndSaysWhereItIs()
    {
        var (ana, email) = await app.SignedInClientAsync("Ana");

        var response = await PostJsonAsync(ana, new { title = "  Pay rent  ", priority = "High", due = "2026-12-01", notes = "" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = Assert.Single(await TasksOfAsync(email));
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(response.Headers.Location)).StatusCode);
        Assert.Equal(("Pay rent", "High", new DateOnly(2026, 12, 1), Today, false),
                     (task.Title, task.Priority, task.Due, task.CreatedOn, task.Done));
        Assert.Null(task.Notes);
    }

    [Fact]
    public async Task AnInvalidTask_IsRefused_ByBothDoors_AndNothingIsStored()
    {
        var (ana, email) = await app.SignedInClientAsync("Ana");

        var form = await PostFormAsync(ana, new() { ["Title"] = "ab", ["Priority"] = "Medium" });
        var api = await PostJsonAsync(ana, new { title = "ab", priority = "Medium" });

        Assert.Equal(HttpStatusCode.OK, form.StatusCode);                                   // the form, again
        Assert.Contains("A title needs 3 to 80 characters.", await form.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, api.StatusCode);
        Assert.Contains("A title needs 3 to 80 characters.", await api.Content.ReadAsStringAsync());
        Assert.Empty(await TasksOfAsync(email));
    }

    [Fact]
    public async Task ATaskWithoutAToken_IsRefused_AndNothingIsStored()
    {
        var (ana, email) = await app.SignedInClientAsync("Ana");

        var response = await ana.PostAsync("/tasks/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Title"] = "Sneaky task", ["Priority"] = "Medium",                 // no __RequestVerificationToken
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await TasksOfAsync(email));
    }

    [Fact]
    [Trait("Req", "FR-OWN-1")]
    public async Task SomeoneElse_CannotSeeTheNewTask()
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");
        var (ben, _) = await app.SignedInClientAsync("Ben");
        var created = await PostJsonAsync(ana, new { title = "Ana's secret plan", priority = "Low" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);       // if the Arrange step fails, say so here

        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync(created.Headers.Location)).StatusCode);
        Assert.DoesNotContain("Ana&#x27;s secret plan", await ben.GetStringAsync("/tasks"));
    }

    [Fact]
    public async Task NotSignedIn_NothingCanBeCreated()
    {
        var stranger = app.CreateClient(new() { AllowAutoRedirect = false });

        var page = await stranger.GetAsync("/tasks/new");
        var api = await stranger.PostAsJsonAsync("/api/tasks", new { title = "Hello there", priority = "Low" });

        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.StartsWith("/account/signin", page.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
    }
}
