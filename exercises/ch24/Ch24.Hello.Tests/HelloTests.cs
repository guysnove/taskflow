using Microsoft.AspNetCore.Mvc.Testing;

namespace Ch24.Hello.Tests;

public class HelloTests(WebApplicationFactory<Program> app) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Home_Greets() =>
        Assert.StartsWith("Hello from Ch24.Hello", await app.CreateClient().GetStringAsync("/"));

    [Fact]
    public async Task Health_IsHealthy() =>
        Assert.Equal("Healthy", await app.CreateClient().GetStringAsync("/health"));
}
