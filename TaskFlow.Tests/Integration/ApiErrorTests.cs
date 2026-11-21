using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 15: every error from the API has the same shape — a "problem details" document
// (RFC 9457), with the media type application/problem+json — whatever went wrong, and wherever.
// A program then needs ONE way to read errors, not one per endpoint.
[Trait("Req", "NFR-API-2")]
public class ApiErrorTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public ApiErrorTests(TaskFlowFactory factory) => _factory = factory;

    [Theory]
    [InlineData("not signed in", 401)]
    [InlineData("no such task", 404)]
    [InlineData("id is not a number", 404)]
    [InlineData("no such endpoint", 404)]
    [InlineData("invalid task", 400)]
    [InlineData("too many requests", 429)]
    public async Task EveryApiError_IsAProblemDocument(string situation, int expectedStatus)
    {
        var response = await ProvokeAsync(situation);
        string body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"{situation,-20} {(int)response.StatusCode} {response.Content.Headers.ContentType?.MediaType ?? "(no body)"}");

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(expectedStatus, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(problem.RootElement.GetProperty("title").GetString()));
    }

    private async Task<HttpResponseMessage> ProvokeAsync(string situation)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (situation == "not signed in")
        {
            return await client.GetAsync("/api/tasks");
        }
        await Browser.RegisterAsync(client, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        switch (situation)
        {
            case "no such task": return await client.GetAsync("/api/tasks/999999");
            case "id is not a number": return await client.GetAsync("/api/tasks/abc");
            case "no such endpoint": return await client.GetAsync("/api/nope");
            case "invalid task":
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(new { title = "", priority = "Medium" }) };
                request.Headers.Add("X-CSRF-TOKEN", await Browser.TokenFromAsync(client, "/tasks/quick"));
                return await client.SendAsync(request);
            default:                                                  // too many requests: the limit is 20 per 10 seconds
                HttpResponseMessage last = null!;
                for (int i = 0; i < 25; i++) last = await client.GetAsync("/api/tasks");
                return last;
        }
    }
}
