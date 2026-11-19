using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 8: a new task can be added two ways, the form and the JSON API.
// The same rules must apply to both.
public class NewTaskRulesTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public NewTaskRulesTests(TaskFlowFactory factory) => _factory = factory;

    private async Task<HttpClient> SignedInAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(client, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        return client;
    }

    public static TheoryData<string> BadDueDates => new() { "2020-01-01", "2062-11-18" };   // the past; a typo

    [Theory]
    [Trait("Req", "FR-TASK-2")]
    [Trait("Req", "FR-TASK-3")]
    [MemberData(nameof(BadDueDates))]
    public async Task Form_RefusesTheDueDate(string due)
    {
        var ana = await SignedInAsync();
        string token = await Browser.TokenFromAsync(ana, "/tasks/new");

        var response = await ana.PostAsync("/tasks/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Title"] = "Renew passport", ["Priority"] = "Medium", ["Due"] = due, ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);             // the form again, with a message
    }

    [Theory]
    [Trait("Req", "FR-TASK-2")]
    [Trait("Req", "FR-TASK-3")]
    [MemberData(nameof(BadDueDates))]
    public async Task Api_RefusesTheDueDate(string due)
    {
        var ana = await SignedInAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(new { title = "Renew passport", priority = "Medium", due }),
        };
        request.Headers.Add("X-CSRF-TOKEN", await Browser.TokenFromAsync(ana, "/tasks/quick"));

        var response = await ana.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
