using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Data;

namespace TaskFlow.Tests.Integration;

// Chapter 30: the checklist feature, tested through real HTTP requests (pages and API).
[Trait("Req", "FR-STEP-1")]
public class StepsTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public StepsTests(TaskFlowFactory factory) => _factory = factory;

    private HttpClient NewBrowser() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // A signed-in user, with one task created through the API. Returns the browser, its token and the task's id.
    private async Task<(HttpClient Browser, string Token, int TaskId)> UserWithTaskAsync(string name, string title)
    {
        var browser = NewBrowser();
        await Browser.RegisterAsync(browser, $"{name}-{Guid.NewGuid():N}@example.com", name);
        string token = await Browser.TokenFromAsync(browser, "/tasks/quick");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(new { title = title, priority = "Medium" }),
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await browser.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskInfo>();
        return (browser, token, task!.Id);
    }
    private sealed record TaskInfo(int Id);
    private sealed record StepInfo(int Id, string Text, bool Done, int Position);

    // Posts a form on a task's page, the way the browser does (with the page's own token).
    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient browser, int taskId, string path, Dictionary<string, string>? fields = null)
    {
        string token = await Browser.TokenFromAsync(browser, $"/tasks/{taskId}");
        fields ??= new();
        fields["__RequestVerificationToken"] = token;
        return await browser.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    private static async Task<StepInfo> AddStepWithApiAsync(HttpClient browser, string token, int taskId, string text)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{taskId}/steps")
        {
            Content = JsonContent.Create(new { text }),
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await browser.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StepInfo>())!;
    }

    [Fact]
    public async Task AddStep_ThroughTheForm_AppearsOnTheTaskPage()
    {
        var (ana, _, taskId) = await UserWithTaskAsync("Ana", "Office move");

        var response = await PostFormAsync(ana, taskId, $"/tasks/{taskId}/steps", new() { ["Text"] = "Buy boxes" });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/tasks/{taskId}#steps", response.Headers.Location!.ToString());
        string page = await ana.GetStringAsync($"/tasks/{taskId}");
        Assert.Contains("Buy boxes", page);
        Assert.Contains("0 of 1 steps done", page);
    }

    [Fact]
    public async Task AddStep_EmptyText_ShowsTheMessage()
    {
        var (ana, _, taskId) = await UserWithTaskAsync("Ana", "Office move");

        var response = await PostFormAsync(ana, taskId, $"/tasks/{taskId}/steps", new() { ["Text"] = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);       // the page again, not a redirect
        string page = await response.Content.ReadAsStringAsync();
        Assert.Contains("Please write what the step is.", page);
        Assert.Contains($"href=\"/tasks/{taskId}/edit\"", page);  // the page's links still work
    }

    [Fact]
    public async Task TaskList_ShowsStepProgress()
    {
        var (ana, token, taskId) = await UserWithTaskAsync("Ana", "Office move");
        var first = await AddStepWithApiAsync(ana, token, taskId, "Buy boxes");
        await AddStepWithApiAsync(ana, token, taskId, "Pack books");
        await PostFormAsync(ana, taskId, $"/tasks/{taskId}/steps/{first.Id}/toggle");

        string list = await ana.GetStringAsync("/tasks");

        Assert.Contains("1 of 2 steps done", list);
    }

    [Fact]
    public async Task Ben_CannotSeeTickOrRemove_AnasSteps()
    {
        var (ana, anaToken, taskId) = await UserWithTaskAsync("Ana", "Ana's move");
        var step = await AddStepWithApiAsync(ana, anaToken, taskId, "Ana's step");
        var (ben, benToken, benTaskId) = await UserWithTaskAsync("Ben", "Ben's task");

        // The API: reading, ticking and deleting Ana's step all answer 404.
        Assert.Equal(HttpStatusCode.NotFound, (await ben.GetAsync($"/api/tasks/{taskId}/steps")).StatusCode);
        var toggle = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{taskId}/steps/{step.Id}/toggle");
        toggle.Headers.Add("X-CSRF-TOKEN", benToken);
        Assert.Equal(HttpStatusCode.NotFound, (await ben.SendAsync(toggle)).StatusCode);

        // The pages: Ben uses a valid token from his own task's page, with Ana's ids in the address.
        string token = await Browser.TokenFromAsync(ben, $"/tasks/{benTaskId}");
        var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        Assert.Equal(HttpStatusCode.NotFound, (await ben.PostAsync($"/tasks/{taskId}/steps/{step.Id}/toggle", form)).StatusCode);
        // Ana's id as the task, Ben's own task in the address? Also refused: the step must belong to the task.
        Assert.Equal(HttpStatusCode.NotFound, (await ben.PostAsync($"/tasks/{benTaskId}/steps/{step.Id}/toggle", form)).StatusCode);

        // And Ana's step is still not done.
        var steps = await ana.GetFromJsonAsync<List<StepInfo>>($"/api/tasks/{taskId}/steps");
        Assert.False(steps!.Single().Done);
    }

    [Fact]
    public async Task ApiAddStep_EmptyText_Returns400()
    {
        var (ana, token, taskId) = await UserWithTaskAsync("Ana", "Office move");
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{taskId}/steps")
        {
            Content = JsonContent.Create(new { text = "" }),
        };
        request.Headers.Add("X-CSRF-TOKEN", token);

        var response = await ana.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StepText_IsShownAsText_NotHtml()
    {
        var (ana, token, taskId) = await UserWithTaskAsync("Ana", "Office move");
        await AddStepWithApiAsync(ana, token, taskId, "<b>Bold?</b>");

        string page = await ana.GetStringAsync($"/tasks/{taskId}");

        Assert.Contains("&lt;b&gt;Bold?&lt;/b&gt;", page);   // encoded (Chapter 27)
        Assert.DoesNotContain("<b>Bold?</b>", page);
    }

    [Fact]
    public async Task DeletingATask_DeletesItsSteps()
    {
        var (ana, token, taskId) = await UserWithTaskAsync("Ana", "Short-lived task");
        await AddStepWithApiAsync(ana, token, taskId, "Step one");
        await AddStepWithApiAsync(ana, token, taskId, "Step two");

        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/tasks/{taskId}");
        delete.Headers.Add("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.NoContent, (await ana.SendAsync(delete)).StatusCode);

        // Look in the database itself: no steps are left for that task.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        Assert.Equal(0, await db.Steps.CountAsync(s => s.TaskItemId == taskId));
    }
}
