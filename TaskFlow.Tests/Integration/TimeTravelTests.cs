using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 13: because every class asks for TimeProvider instead of reading the clock,
// a test can REPLACE it in the real app's container — and move time forward.
public class TimeTravelTests : IDisposable
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly WebApplicationFactory<Program> _factory;

    public TimeTravelTests()
    {
        _clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        _factory = new TaskFlowFactory().WithWebHostBuilder(b =>
            b.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(_clock)));   // the last registration wins
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ATaskDueTomorrow_IsOverdue_TwoDaysLater()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(client, "first@example.com", "First");          // the first user adopts the samples
        client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(client, "ana@example.com", "Ana");              // Ana starts with no tasks
        string token = await Browser.TokenFromAsync(client, "/tasks/quick");
        var add = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(new { title = "Send invoice", priority = "High", due = "2026-11-20" }),
        };
        add.Headers.Add("X-CSRF-TOKEN", token);
        Assert.True((await client.SendAsync(add)).IsSuccessStatusCode);

        string today = await client.GetStringAsync("/tasks");
        Assert.Contains("No overdue tasks", today);
        Assert.Contains("1 due in the next", today);

        _clock.Advance(TimeSpan.FromDays(2));                                       // two days later, instantly

        string later = await client.GetStringAsync("/tasks");
        Assert.Contains("1 overdue task", later);
    }
}
