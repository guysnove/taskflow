using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 9: a golden master for the task list, before refactoring TasksController.
// 240 addresses (every mix of show, sort, project and search, including nonsense values),
// and what each one shows: the rows in order, the chosen filter links, and the search box.
// The first run writes TaskList.received.txt; a person reads it and renames it to
// TaskList.approved.txt. From then on, any difference fails the test.
public class TaskListGoldenMasterTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public TaskListGoldenMasterTests(TaskFlowFactory factory) => _factory = factory;

    private sealed record Dto(int Id, string Title, int? ProjectId);

    [Fact]
    [Trait("Req", "FR-LIST-1")]
    public async Task TaskList_ShowsWhatItShowedBefore_ForEveryAddress()
    {
        // The first user adopts the four sample tasks and two projects (Development).
        var ana = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(ana, "ana@example.com", "Ana");
        string token = await Browser.TokenFromAsync(ana, "/tasks/quick");
        var tasks = await ana.GetFromJsonAsync<List<Dto>>("/api/tasks", JsonSerializerOptions.Web);
        int reportProject = tasks!.Single(t => t.Title == "Write report").ProjectId!.Value;

        // Three more tasks, and one done, so every sort and filter has something to do.
        await AddAsync(ana, token, new { title = "Review report draft", priority = "Medium", due = Today(3), projectId = reportProject });
        await AddAsync(ana, token, new { title = "Book train to Lyon", priority = "Low", due = Today(10) });
        await AddAsync(ana, token, new { title = "archive old reports", priority = "High" });
        int supplier = tasks!.Single(t => t.Title == "Call supplier").Id;
        var done = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{supplier}/done");
        done.Headers.Add("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.NoContent, (await ana.SendAsync(done)).StatusCode);

        var text = new StringBuilder();
        foreach (string? show in new[] { null, "open", "all", "nonsense" })
            foreach (string? sort in new[] { null, "due", "priority", "title", "nonsense" })
                foreach (string? project in new[] { null, "REPORT", "999" })
                    foreach (string? q in new[] { null, "report", "  REPORT  ", "zzz" })
                    {
                        var query = new List<string>();
                        if (show is not null) query.Add($"show={show}");
                        if (sort is not null) query.Add($"sort={sort}");
                        if (project is not null) query.Add($"project={(project == "REPORT" ? reportProject : 999)}");
                        if (q is not null) query.Add($"q={Uri.EscapeDataString(q)}");
                        string path = "/tasks" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
                        text.AppendLine(path.Replace($"project={reportProject}", "project={report}", StringComparison.Ordinal));
                        text.AppendLine("  " + Describe(await ana.GetStringAsync(path)));
                    }

        Approve(text.ToString());
    }

    private static string Today(int days) => DateOnly.FromDateTime(DateTime.Today.AddDays(days)).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static async Task AddAsync(HttpClient client, string token, object task)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(task) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(request)).StatusCode);
    }

    // What a person sees: the chosen links, the search box, the count, and the rows in order.
    private static string Describe(string html)
    {
        var chosen = Regex.Matches(html, "<a [^>]*aria-current=\"true\"[^>]*>([^<]+)</a>").Select(m => m.Groups[1].Value);
        string search = Regex.Match(html, "id=\"q\" name=\"q\" value=\"([^\"]*)\"").Groups[1].Value;
        string count = Regex.Match(html, @"(\d+ of \d+) tasks still to do").Groups[1].Value;
        var rows = Regex.Matches(html, "<tr class=\"(task-done)?\">\\s*<td data-label=\"Task\">\\s*<a [^>]*>([^<]+)</a>")
                        .Select(m => m.Groups[2].Value + (m.Groups[1].Success ? " (done)" : ""));
        return $"[{string.Join(", ", chosen)}] search=\"{search}\" {count}: {string.Join(" | ", rows)}";
    }

    private static void Approve(string received, [CallerFilePath] string here = "")
    {
        string folder = Path.Combine(Path.GetDirectoryName(here)!, "Approved");
        Directory.CreateDirectory(folder);
        string approvedFile = Path.Combine(folder, "TaskList.approved.txt");
        string receivedFile = Path.Combine(folder, "TaskList.received.txt");
        string approved = File.Exists(approvedFile) ? File.ReadAllText(approvedFile) : "";
        if (received == approved)
        {
            File.Delete(receivedFile);
            return;
        }
        File.WriteAllText(receivedFile, received);
        Assert.Fail($"The task list changed (or was never approved). Compare {Path.GetFileName(receivedFile)} with {Path.GetFileName(approvedFile)}.");
    }
}
