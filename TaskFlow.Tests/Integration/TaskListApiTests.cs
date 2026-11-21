using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 15: GET /api/tasks can filter and page — without breaking programs that already call it.
// No query string: exactly the old answer (every task). page / pageSize / done / projectId are all optional.
[Trait("Req", "FR-API-1")]
public class TaskListApiTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public TaskListApiTests(TaskFlowFactory factory) => _factory = factory;

    private sealed record Dto(int Id, string Title, bool Done);

    private async Task<(HttpClient Client, List<int> Ids)> FiveTasksAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(client, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        string token = await Browser.TokenFromAsync(client, "/tasks/quick");
        var ids = new List<int>();
        foreach (string title in new[] { "Pack books", "Pack kitchen", "Book van", "Change address", "Clean flat" })
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(new { title, priority = "Medium" }) };
            request.Headers.Add("X-CSRF-TOKEN", token);
            var response = await client.SendAsync(request);
            ids.Add((await response.Content.ReadFromJsonAsync<Dto>(JsonSerializerOptions.Web))!.Id);
        }
        var done = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{ids[2]}/done");
        done.Headers.Add("X-CSRF-TOKEN", token);
        await client.SendAsync(done);
        return (client, ids);
    }

    private static async Task<List<Dto>> ReadAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<List<Dto>>(JsonSerializerOptions.Web))!;

    [Fact]
    public async Task Pages_CoverEveryTaskOnce_AndSayWhatComesNext()
    {
        var (client, ids) = await FiveTasksAsync();

        var first = await client.GetAsync("/api/tasks?pageSize=2");
        var second = await client.GetAsync("/api/tasks?page=2&pageSize=2");
        var third = await client.GetAsync("/api/tasks?page=3&pageSize=2");
        var pages = new List<List<Dto>>();
        foreach (var response in new[] { first, second, third })
        {
            pages.Add(await ReadAsync(response));
            Console.WriteLine($"{response.RequestMessage!.RequestUri!.PathAndQuery,-30} {(int)response.StatusCode} " +
                $"items={pages[^1].Count} total={string.Join("", response.Headers.GetValues("X-Total-Count"))} " +
                $"link={(response.Headers.TryGetValues("Link", out var link) ? string.Join("", link) : "(none)")}");
        }

        var seen = pages.SelectMany(p => p).Select(t => t.Id).ToList();
        Assert.Equal(ids.Order(), seen.Order());                                   // every task, exactly once
        Assert.Equal("5", first.Headers.GetValues("X-Total-Count").Single());
        Assert.Contains("page=2", first.Headers.GetValues("Link").Single(), StringComparison.Ordinal);
        Assert.False(third.Headers.Contains("Link"));                              // nothing after the last page
    }

    [Fact]
    public async Task Filters_ByDone()
    {
        var (client, ids) = await FiveTasksAsync();

        var done = await ReadAsync(await client.GetAsync("/api/tasks?done=true"));
        var open = await ReadAsync(await client.GetAsync("/api/tasks?done=false"));

        Assert.Equal([ids[2]], done.Select(t => t.Id));
        Assert.Equal(4, open.Count);
    }

    [Fact]
    public async Task NoQueryString_StillReturnsEveryTask()                         // backward compatible
    {
        var (client, _) = await FiveTasksAsync();
        Assert.Equal(5, (await ReadAsync(await client.GetAsync("/api/tasks"))).Count);
    }

    [Fact]
    public async Task APageThatIsTooBig_IsAProblem()
    {
        var (client, _) = await FiveTasksAsync();

        var response = await client.GetAsync("/api/tasks?pageSize=500");
        string body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"pageSize=500 → {(int)response.StatusCode} {body}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("pageSize", body, StringComparison.Ordinal);
    }
}
