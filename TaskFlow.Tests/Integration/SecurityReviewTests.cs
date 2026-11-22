using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 20: the defensive security review, as tests. Each test is an attacker's input
// and the result we want. Some passed on the first run (they guard what Course 1 got right);
// the ones that failed are the review's findings (docs/security/threat-model.md).
// The factory also collects every log entry, so we can check what TaskFlow writes to its logs.
public class SecurityFactory : WorkflowFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureLogging(logging => logging.AddFakeLogging());
    }

    public IReadOnlyList<FakeLogRecord> Logs => Services.GetRequiredService<FakeLogCollector>().GetSnapshot();
}

[Trait("Category", "Security")]
public class SecurityReviewTests(SecurityFactory app) : IClassFixture<SecurityFactory>
{
    private static async Task<HttpResponseMessage> PostTaskAsync(HttpClient client, object task)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(task) };
        request.Headers.Add("X-CSRF-TOKEN", await Browser.TokenFromAsync(client, "/tasks/quick"));
        return await client.SendAsync(request);
    }

    // ---- Input handling --------------------------------------------------------------------------
    // A title with a line break can forge a log line ("Task 5 created: Pay rent" + "\n[ERR] …"),
    // and breaks email subjects. The form can't type one; the API (or a hand-made POST) can.
    [Theory]
    [InlineData("Pay rent\nfail: TaskFlow[0] Admin password changed")]
    [InlineData("Pay rent\r\nBcc: everyone@example.com")]
    [InlineData("Bell\u0007 and backspace\u0008")]
    public async Task ATitleWithControlCharacters_IsRefused(string title)
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");

        var response = await PostTaskAsync(ana, new { title, priority = "Low" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Secure logging --------------------------------------------------------------------------
    // Task titles are the user's private data ("Book appointment with divorce lawyer").
    // Logs are read by more people, kept longer and copied to more places than the database.
    [Fact]
    public async Task CreatingATask_DoesNotWriteItsTitleToTheLogs()
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");
        const string secret = "Private: call the lawyer about the custody case";

        var form = await ana.PostAsync("/tasks/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Title"] = secret, ["Priority"] = "High",
            ["__RequestVerificationToken"] = await Browser.TokenFromAsync(ana, "/tasks/new"),
        }));
        Assert.Equal(HttpStatusCode.Redirect, form.StatusCode);              // created

        var leaks = app.Logs.Where(log => log.Message.Contains("lawyer", StringComparison.Ordinal))
                            .Select(log => $"{log.Category}: {log.Message}");
        Assert.Empty(leaks);
    }

    // ---- Guards for what is already right (these passed on the first run) --------------------------
    [Fact]
    public async Task StoredXss_ATitleIsShownAsText()
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");
        await PostTaskAsync(ana, new { title = "<script>alert(1)</script>", priority = "Low" });

        string html = await ana.GetStringAsync("/tasks");

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example")]
    public async Task OpenRedirect_SignInStaysOnTaskFlow(string returnUrl)
    {
        var client = app.CreateClient(new() { AllowAutoRedirect = false });
        string email = $"red-{Guid.NewGuid():N}@example.com";
        await Browser.RegisterAsync(client, email, "Red");
        await client.PostAsync("/account/signout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await Browser.TokenFromAsync(client, "/tasks"),
        }));

        string path = "/account/signin?returnUrl=" + Uri.EscapeDataString(returnUrl);
        var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email, ["Password"] = "long-enough-1",
            ["__RequestVerificationToken"] = await Browser.TokenFromAsync(client, path),
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("evil.example", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SqlInjection_TheSearchIsJustText()
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");
        var (ben, _) = await app.SignedInClientAsync("Ben");
        await PostTaskAsync(ben, new { title = "Ben's private plan", priority = "Low" });

        string html = await ana.GetStringAsync("/tasks?q=" + Uri.EscapeDataString("' OR 1=1 --"));

        Assert.DoesNotContain("Ben&#x27;s private plan", html);
        Assert.Contains("0 of 0 tasks still to do", html);
    }

    [Fact]
    public async Task BrokenAccessControl_StepsAndDependenciesOfSomeoneElsesTask()
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");
        var (ben, _) = await app.SignedInClientAsync("Ben");
        var created = await PostTaskAsync(ana, new { title = "Ana's task", priority = "Low" });
        string id = created.Headers.Location!.Segments[^1];

        var steps = await ben.GetAsync($"/api/tasks/{id}/steps");
        var token = await Browser.TokenFromAsync(ben, "/tasks/quick");
        var dependency = await ben.PostAsync($"/tasks/{id}/dependencies", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["blockedById"] = id, ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.NotFound, steps.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, dependency.StatusCode);
    }

    [Fact]
    public async Task TheAdminPage_IsForAdminsOnly()
    {
        var first = app.CreateClient(new() { AllowAutoRedirect = false });
        await Browser.RegisterAsync(first, $"first-{Guid.NewGuid():N}@example.com", "First");   // may or may not be first
        var (ben, _) = await app.SignedInClientAsync("Ben");                                     // certainly not first

        var response = await ben.GetAsync("/admin/users");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/denied", response.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TheSignInCookie_IsHttpOnly_AndSameSite()
    {
        var client = app.CreateClient(new() { AllowAutoRedirect = false });
        var response = await Browser.RegisterAsync(client, $"c-{Guid.NewGuid():N}@example.com", "Cleo");

        string cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("TaskFlow.SignIn=", StringComparison.Ordinal));

        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
