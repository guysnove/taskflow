using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 4: "Blocked by", tested through real HTTP requests.
[Trait("Req", "FR-DEP-1")]
public class DependenciesTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;
    public DependenciesTests(TaskFlowFactory factory) => _factory = factory;

    private HttpClient NewBrowser() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private record TaskInfo(int Id);

    private static async Task<int> AddTaskAsync(HttpClient browser, string token, string title)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(new { title, priority = "Medium" }) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await browser.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskInfo>())!.Id;
    }

    private static async Task<HttpResponseMessage> WaitForAsync(HttpClient browser, int taskId, int blockedById)
    {
        string token = await Browser.TokenFromAsync(browser, $"/tasks/{taskId}");
        return await browser.PostAsync($"/tasks/{taskId}/dependencies", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["blockedById"] = blockedById.ToString(), ["__RequestVerificationToken"] = token,
        }));
    }

    [Fact]
    public async Task Dependencies_AreShown_AndALoopIsRefused()
    {
        var ana = NewBrowser();
        await Browser.RegisterAsync(ana, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        string token = await Browser.TokenFromAsync(ana, "/tasks/quick");
        int build = await AddTaskAsync(ana, token, "Build");
        int test = await AddTaskAsync(ana, token, "Test");
        int deploy = await AddTaskAsync(ana, token, "Deploy");

        var r1 = await WaitForAsync(ana, test, build);                  // Test waits for Build
        Assert.Equal(HttpStatusCode.Redirect, r1.StatusCode);
        Assert.Equal($"/tasks/{test}#dependencies", r1.Headers.Location!.ToString());
        await WaitForAsync(ana, deploy, test);                          // Deploy waits for Test

        string page = await ana.GetStringAsync($"/tasks/{deploy}");
        Assert.Contains("Blocked by", page);
        Assert.Contains("Waiting for 1 of 1.", page);

        await WaitForAsync(ana, build, deploy);                         // Build waits for Deploy: a loop!
        // HtmlDecode: Razor writes "→" as "&#x2192;" in the HTML; the browser shows the arrow.
        string refused = WebUtility.HtmlDecode(await ana.GetStringAsync($"/tasks/{build}"));
        Assert.Contains("That would create a loop where nothing can start: Deploy → Build → Test → Deploy.", refused);
        Assert.Contains("Nothing: this task can start any time.", refused);   // the loop was not saved
    }

    [Fact]
    [Trait("Req", "FR-OWN-1")]
    public async Task SomeoneElsesTask_Is404()
    {
        var ana = NewBrowser();
        await Browser.RegisterAsync(ana, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        int anas = await AddTaskAsync(ana, await Browser.TokenFromAsync(ana, "/tasks/quick"), "Ana's task");

        var ben = NewBrowser();
        await Browser.RegisterAsync(ben, $"ben-{Guid.NewGuid():N}@example.com", "Ben");
        int bens = await AddTaskAsync(ben, await Browser.TokenFromAsync(ben, "/tasks/quick"), "Ben's task");

        var response = await WaitForAsync(ben, bens, anas);              // Ben tries to depend on Ana's task
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
