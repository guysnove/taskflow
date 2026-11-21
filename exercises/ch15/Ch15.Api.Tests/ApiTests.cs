using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ch15.Api.Tests;

// The Library API's contract, as tests. Each test starts the real app in memory (WebApplicationFactory).
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task UnknownBook_Is404_WithAProblemDocument()
    {
        var response = await _client.GetAsync("/v1/books/99");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_Is201_AndTheLocationWorks()
    {
        var response = await _client.PostAsJsonAsync("/v1/books", new { title = "Team Topologies", authors = new[] { "Matthew Skelton", "Manuel Pais" }, year = 2019 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(response.Headers.Location)).StatusCode);
    }

    [Fact]
    public async Task InvalidBook_Is400_AndNamesEveryProblem()
    {
        var response = await _client.PostAsJsonAsync("/v1/books", new { title = "", authors = Array.Empty<string>(), year = 3000 });
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var fields = problem.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["title", "authors", "year"], fields);
    }

    [Fact]
    public async Task Version1_StillHasOneAuthorString_Version2_HasAList()
    {
        using var v1 = JsonDocument.Parse(await _client.GetStringAsync("/v1/books/5"));
        using var v2 = JsonDocument.Parse(await _client.GetStringAsync("/v2/books/5"));
        Assert.Equal("David Thomas, Andrew Hunt", v1.RootElement.GetProperty("author").GetString());
        Assert.Equal(2, v2.RootElement.GetProperty("authors").GetArrayLength());
    }

    [Fact]
    public async Task Pages_CoverEveryBookOnce()
    {
        var ids = new List<int>();
        string? next = "/v2/books?pageSize=3";
        while (next is not null)
        {
            using var page = JsonDocument.Parse(await _client.GetStringAsync(next));
            ids.AddRange(page.RootElement.GetProperty("items").EnumerateArray().Select(b => b.GetProperty("id").GetInt32()));
            next = page.RootElement.GetProperty("next").GetString();
        }
        Assert.Equal(ids.Distinct().Count(), ids.Count);
        Assert.Contains(1, ids);
        Assert.Contains(8, ids);
    }

    [Fact]
    public async Task SameIdempotencyKey_BorrowsOnce()
    {
        async Task<HttpResponseMessage> Borrow()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/loans") { Content = JsonContent.Create(new { bookId = 1, member = "ana" }) };
            request.Headers.Add("Idempotency-Key", "test-key-1");
            return await _client.SendAsync(request);
        }
        var first = await Borrow();
        var again = await Borrow();
        Assert.Equal(first.Headers.Location, again.Headers.Location);
        Assert.True(again.Headers.Contains("Idempotent-Replayed"));
    }
}
