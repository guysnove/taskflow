using Microsoft.Playwright;

namespace Ch18.Notes.Tests;

// END-TO-END: a real browser (Chromium, driven by Playwright) uses the app through a real
// network port, like a person would: click, type, read the page.
// Slow and fragile compared with the tests above, so there are only a few, marked Category=E2E.
// The browser: CH18_BROWSER (a path to chrome), or the one `playwright install chromium` downloaded.
[Trait("Category", "E2E")]
public class BrowserTests : IAsyncLifetime
{
    private readonly NotesFactory _app = new();
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private string _url = "";

    public async Task InitializeAsync()
    {
        _app.UseKestrel(0);                     // .NET 10: a real server on a free port, not the in-memory one
        _app.StartServer();
        _url = _app.ClientOptions.BaseAddress.ToString().TrimEnd('/');
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            ExecutablePath = Environment.GetEnvironmentVariable("CH18_BROWSER"),   // null: Playwright's own
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task APerson_SignsIn_AddsANote_AndSeesIt()
    {
        var page = await _browser.NewPageAsync();

        await page.GotoAsync(_url);
        await page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
        await page.GetByLabel("Your name").FillAsync("Dana");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await page.GetByLabel("New note").FillAsync("Book the dentist");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        // Playwright's assertions WAIT (up to 5 s) for the page to get there: no Thread.Sleep.
        await Assertions.Expect(page.Locator("#notes li")).ToHaveTextAsync(["Book the dentist"]);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading)).ToHaveTextAsync("Dana's notes");
        await page.ScreenshotAsync(new() { Path = Path.Combine(AppContext.BaseDirectory, "e2e-notes.png") });
    }

    [Fact]
    public async Task AnEmptyNote_ShowsAMessage_AndAddsNothing()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync(_url + "/login");
        await page.GetByLabel("Your name").FillAsync("Eli");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Please write something.");
        await Assertions.Expect(page.Locator("#notes li")).ToHaveCountAsync(0);
    }
}
