using System.Net;
using System.Net.Http.Json;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 19 (test-first): the list SHOWS when a priority was raised, and why (FR-PRIO-1).
// WorkflowFactory: the clock says Monday 23 November 2026.
[Trait("Req", "FR-PRIO-1")]
public class TaskListPriorityTests(WorkflowFactory app) : IClassFixture<WorkflowFactory>
{
    private static async Task AddAsync(HttpClient client, object task)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(task) };
        request.Headers.Add("X-CSRF-TOKEN", await Browser.TokenFromAsync(client, "/tasks/quick"));
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task ARaisedPriority_IsShown_WithTheReason()
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");
        await AddAsync(ana, new { title = "Pay the plumber", priority = "Low", due = "2026-11-23" });

        string html = await ana.GetStringAsync("/tasks");

        Assert.Contains("class=\"priority priority-medium priority-raised\" title=\"Raised from Low: due today\"", html);
    }

    [Fact]
    public async Task AnUnchangedPriority_IsShownAsBefore()
    {
        var (ana, _) = await app.SignedInClientAsync("Ana");
        await AddAsync(ana, new { title = "Sort the photos", priority = "Low", due = "2026-12-20" });

        string html = await ana.GetStringAsync("/tasks");

        Assert.Contains("<span class=\"priority priority-low\">Low</span>", html);
        Assert.DoesNotContain("priority-raised", html);
    }
}
