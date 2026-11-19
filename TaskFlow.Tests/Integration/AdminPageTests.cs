using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 11: written before moving AdminController's query into AccountService
// (the page had no test). The first account is the administrator (Chapter 25).
public class AdminPageTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public AdminPageTests(TaskFlowFactory factory) => _factory = factory;

    [Fact]
    [Trait("Req", "FR-ACC-1")]
    public async Task Admin_SeesEveryAccount_OthersAreRefused()
    {
        var options = new WebApplicationFactoryClientOptions { AllowAutoRedirect = false };
        var ana = _factory.CreateClient(options);
        await Browser.RegisterAsync(ana, "admin-ana@example.com", "Ana");          // first: admin
        var ben = _factory.CreateClient(options);
        await Browser.RegisterAsync(ben, "ben@example.com", "Ben");

        string page = await ana.GetStringAsync("/admin/users");
        Assert.Contains("admin-ana@example.com", page);
        Assert.Contains("ben@example.com", page);

        var refused = await ben.GetAsync("/admin/users");
        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.StartsWith("/account/denied", refused.Headers.Location!.PathAndQuery);
    }
}
