using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TaskFlow.Tests.Integration;

namespace TaskFlow.Tests.EndToEnd;

// Course 2, Chapter 18: END-TO-END. A real browser (Chromium, driven by Playwright) uses TaskFlow through
// a real network port, the way a person does: it reads labels, types, clicks, and waits for pages.
// It checks what the integration tests can't: the HTML, the redirects and the cookies working together
// in a browser. Slower, so only the most important journeys get one. Not part of `test: unit`.
// The browser: TASKFLOW_BROWSER (a path to Chrome or Chromium), or Playwright's own
// (`pwsh TaskFlow.Tests/bin/Debug/net10.0/playwright.ps1 install chromium`).
[Trait("Category", "E2E")]
public sealed class CreateTaskInABrowserTests : IAsyncLifetime, IDisposable
{
    private readonly WorkflowFactory _app = new();
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private string _url = "";

    public async Task InitializeAsync()
    {
        _app.UseKestrel(0);                     // .NET 10: a real server on a free port (the in-memory one has no port)
        _app.StartServer();
        _url = _app.ClientOptions.BaseAddress.ToString().TrimEnd('/');
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { ExecutablePath = Environment.GetEnvironmentVariable("TASKFLOW_BROWSER") });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    public void Dispose() => _app.Dispose();      // the server, and its database file

    [Fact]
    [Trait("Req", "FR-TASK-1")]
    public async Task APerson_Registers_AddsATask_AndSeesIt()
    {
        var page = await _browser.NewPageAsync();
        var email = $"dana-{Guid.NewGuid():N}@example.com";

        // Register (the app signs you in and shows your task list)
        await page.GotoAsync(_url + "/account/register");
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Your name").FillAsync("Dana");
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync("long-enough-1");
        await page.GetByLabel("Password again").FillAsync("long-enough-1");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

        // Add a task through the form
        await page.GotoAsync(_url + "/tasks/new");
        await page.GetByLabel("Title").FillAsync("Book the venue");
        await page.GetByLabel("Priority").SelectOptionAsync("High");
        await page.GetByLabel("Due date (optional)").FillAsync("2026-12-04");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add task" }).ClickAsync();

        // What Dana sees. Playwright's assertions wait (up to 5 s) for the page: no sleeps.
        await Assertions.Expect(page).ToHaveURLAsync(_url + "/tasks");
        await Assertions.Expect(page.GetByText("Added \"Book the venue\".")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Book the venue" })).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(AppContext.BaseDirectory, "e2e-task-list.png"), FullPage = true });

        // And what was stored
        var task = await _app.DbAsync(db => db.Tasks.IgnoreQueryFilters().SingleAsync(t => t.User!.Email == email));
        Assert.Equal(("Book the venue", "High", new DateOnly(2026, 12, 4)), (task.Title, task.Priority, task.Due));
    }
}
