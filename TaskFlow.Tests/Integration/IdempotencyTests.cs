using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 15: a program sends POST /api/tasks, the network fails before the answer arrives,
// and the program sends it again. Did the first one work? With an Idempotency-Key header, it doesn't
// matter: the same key means "the same request", so the task is created once, and the repeat gets
// the same answer.
[Trait("Req", "FR-API-2")]
public class IdempotencyTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public IdempotencyTests(TaskFlowFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, string Token)> SignedInAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(client, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        return (client, await Browser.TokenFromAsync(client, "/tasks/quick"));
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string token, string title, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(new { title, priority = "High" }) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }

    private static async Task<int> CountAsync(HttpClient client, string title) =>
        (await client.GetFromJsonAsync<List<TaskDtoTitle>>("/api/tasks"))!.Count(t => t.Title == title);

    private sealed record TaskDtoTitle(string Title);

    private static string Describe(HttpResponseMessage response) =>
        $"{(int)response.StatusCode} {response.Headers.Location?.OriginalString ?? ""} " +
        (response.Headers.TryGetValues("Idempotent-Replayed", out var replayed) ? "Idempotent-Replayed: " + string.Join("", replayed) : "");

    [Fact]
    public async Task SameKeyTwice_CreatesOneTask_AndRepeatsTheAnswer()
    {
        var (client, token) = await SignedInAsync();
        string key = Guid.NewGuid().ToString();

        var first = await PostAsync(client, token, "Pay invoice 1042", key);
        var again = await PostAsync(client, token, "Pay invoice 1042", key);       // the retry
        Console.WriteLine($"first: {Describe(first)}\nagain: {Describe(again)}\ntasks called \"Pay invoice 1042\": {await CountAsync(client, "Pay invoice 1042")}");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        Assert.Equal(first.Headers.Location, again.Headers.Location);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await again.Content.ReadAsStringAsync());
        Assert.Equal(1, await CountAsync(client, "Pay invoice 1042"));
    }

    [Fact]
    public async Task SameKey_DifferentRequest_IsRefused()
    {
        var (client, token) = await SignedInAsync();
        string key = Guid.NewGuid().ToString();

        await PostAsync(client, token, "Pay invoice 1042", key);
        var different = await PostAsync(client, token, "Pay invoice 2077", key);
        Console.WriteLine($"different body, same key: {Describe(different)} {await different.Content.ReadAsStringAsync()}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, different.StatusCode);
        Assert.Equal(0, await CountAsync(client, "Pay invoice 2077"));
    }

    [Fact]
    public async Task SameKey_AtTheSameMoment_StillCreatesOneTask()           // Chapter 14: they take turns
    {
        var (client, token) = await SignedInAsync();
        string key = Guid.NewGuid().ToString();

        var both = await Task.WhenAll(PostAsync(client, token, "Pay invoice 1042", key), PostAsync(client, token, "Pay invoice 1042", key));

        Assert.All(both, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(1, await CountAsync(client, "Pay invoice 1042"));
    }

    [Fact]
    public async Task WithoutAKey_EveryPostCreatesATask()                     // the old behaviour, still there
    {
        var (client, token) = await SignedInAsync();

        await PostAsync(client, token, "Pay invoice 1042", key: null);
        await PostAsync(client, token, "Pay invoice 1042", key: null);

        Assert.Equal(2, await CountAsync(client, "Pay invoice 1042"));
    }
}
