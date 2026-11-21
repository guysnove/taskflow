using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Notifications;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 16: marking a task done must not wait for the mail server.
// Two realistic mail servers, as fakes: one that takes 3 seconds to answer, and one that is down
// (every attempt fails, so the retrying decorator tries 3 times, 2 seconds apart).
[Trait("Req", "NFR-PERF-3")]
public class SlowMailTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public SlowMailTests(TaskFlowFactory factory) => _factory = factory;

    private sealed class SlowMailServer : IEmailSender
    {
        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default) =>
            Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
    }

    private sealed class MailServerDown : IEmailSender
    {
        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default) =>
            throw new IOException("Connection refused");
    }

    private HttpClient ClientWith(string mailServer) =>
        _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddSingleton<IEmailSender>(sp => new RetryingEmailSender(
                mailServer == "slow" ? new SlowMailServer() : new MailServerDown(),
                sp.GetRequiredService<IOptions<EmailOptions>>(), sp.GetRequiredService<ILogger<RetryingEmailSender>>(), TimeProvider.System))))
        .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private sealed record Dto(int Id);

    [Theory]
    [InlineData("slow")]
    [InlineData("down")]
    public async Task FinishingATask_DoesNotWaitForTheMailServer(string mailServer)
    {
        var sam = ClientWith(mailServer);
        string email = $"sam-{Guid.NewGuid():N}@example.com";
        await Browser.RegisterAsync(sam, email, "Sam");
        string settings = await Browser.TokenFromAsync(sam, "/notifications/settings");
        await sam.PostAsync("/notifications/settings", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["emailNotifications"] = "true", ["__RequestVerificationToken"] = settings,
        }));
        string token = await Browser.TokenFromAsync(sam, "/tasks/quick");
        int logo = await AddAsync(sam, token, "Get logo");
        int copy = await AddAsync(sam, token, "Write copy");
        string formToken = await Browser.TokenFromAsync(sam, $"/tasks/{copy}");
        await sam.PostAsync($"/tasks/{copy}/dependencies", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["blockedById"] = logo.ToString(System.Globalization.CultureInfo.InvariantCulture), ["__RequestVerificationToken"] = formToken,
        }));

        var done = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{logo}/done");
        done.Headers.Add("X-CSRF-TOKEN", token);
        var clock = Stopwatch.StartNew();
        var response = await sam.SendAsync(done);
        clock.Stop();
        Console.WriteLine($"Mail server {mailServer}: POST /api/tasks/{{id}}/done answered {(int)response.StatusCode} after {clock.Elapsed.TotalSeconds:0.0} s");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1), $"took {clock.Elapsed.TotalSeconds:0.0} s");
    }

    private static async Task<int> AddAsync(HttpClient client, string token, string title)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(new { title, priority = "Medium" }) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<Dto>(JsonSerializerOptions.Web))!.Id;
    }
}
