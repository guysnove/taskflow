using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 10: the whole path, through the real app. Finishing a task that another task
// waits for → an event → the observer → the notifier → in-app (and email, when turned on).
public class NotificationFlowTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public NotificationFlowTests(TaskFlowFactory factory) => _factory = factory;

    private sealed record Dto(int Id);

    private static async Task<int> AddAsync(HttpClient client, string token, string title)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(new { title, priority = "Medium" }) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<Dto>(JsonSerializerOptions.Web))!.Id;
    }

    private async Task<(HttpClient Client, string Token, string Email)> SignedInAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        string email = $"sam-{Guid.NewGuid():N}@example.com";
        await Browser.RegisterAsync(client, email, "Sam");
        return (client, await Browser.TokenFromAsync(client, "/tasks/quick"), email);
    }

    // "Write copy" waits for "Get logo"; then "Get logo" is marked done through the API.
    private static async Task FinishABlockerAsync(HttpClient sam, string token)
    {
        int logo = await AddAsync(sam, token, "Get logo");
        int copy = await AddAsync(sam, token, "Write copy");
        string formToken = await Browser.TokenFromAsync(sam, $"/tasks/{copy}");
        var wait = await sam.PostAsync($"/tasks/{copy}/dependencies", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["blockedById"] = logo.ToString(System.Globalization.CultureInfo.InvariantCulture), ["__RequestVerificationToken"] = formToken,
        }));
        Assert.Equal(HttpStatusCode.Redirect, wait.StatusCode);
        var done = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{logo}/done");
        done.Headers.Add("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.NoContent, (await sam.SendAsync(done)).StatusCode);
    }

    [Fact]
    [Trait("Req", "FR-NOTIF-6")]
    [Trait("Req", "FR-NOTIF-3")]
    public async Task FinishingABlocker_NotifiesInApp_AndShowsTheCount()
    {
        var (sam, token, _) = await SignedInAsync();

        await FinishABlockerAsync(sam, token);

        string page = await sam.GetStringAsync("/notifications");
        Assert.Contains("Ready to start: Write copy (&quot;Get logo&quot; is done)", page);
        Assert.Contains("Notifications (1)", page);                       // the badge in the menu
    }

    [Fact]
    [Trait("Req", "FR-NOTIF-4")]
    public async Task WithEmailTurnedOn_AnEmailIsWritten()
    {
        var (sam, token, email) = await SignedInAsync();
        string settingsToken = await Browser.TokenFromAsync(sam, "/notifications/settings");
        await sam.PostAsync("/notifications/settings", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["emailNotifications"] = "true", ["__RequestVerificationToken"] = settingsToken,
        }));

        await FinishABlockerAsync(sam, token);

        var mine = Directory.GetFiles(_factory.MailFolder, "*.eml").Select(File.ReadAllText).Where(m => m.Contains($"To: {email}")).ToList();
        Assert.Contains("Subject: TaskFlow: Ready to start: Write copy", Assert.Single(mine));
    }
}
