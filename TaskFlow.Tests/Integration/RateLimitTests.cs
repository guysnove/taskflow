using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Chapter 30: found while testing the practice exercises. The API's limit (20 requests per 10 seconds)
// must belong to each user — not to everyone who happens to have the same display name.
[Trait("Req", "NFR-SEC-3")]
public class RateLimitTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;
    public RateLimitTests(TaskFlowFactory factory) => _factory = factory;

    [Fact]
    public async Task TwoUsersWithTheSameName_HaveSeparateApiLimits()
    {
        var first = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var second = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(first, $"sam-{Guid.NewGuid():N}@example.com", "Sam");
        await Browser.RegisterAsync(second, $"sam-{Guid.NewGuid():N}@example.com", "Sam");

        // The first Sam uses up his own limit…
        var codes = new List<HttpStatusCode>();
        for (int i = 0; i < 25; i++)
        {
            codes.Add((await first.GetAsync("/api/tasks")).StatusCode);
        }
        Assert.Contains(HttpStatusCode.TooManyRequests, codes);

        // …and the other Sam, a different person, is not affected.
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/api/tasks")).StatusCode);
    }
}
