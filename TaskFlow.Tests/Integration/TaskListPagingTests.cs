using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 26: the task list shows 50 tasks a page (it used to render every task: 2.1 MB for 2,000),
// and the step counts come from one query for the tasks on the page.
[Trait("Req", "NFR-PERF-4")]
public class TaskListPagingTests(TaskFlowFactory app) : IClassFixture<TaskFlowFactory>
{
    private async Task<string> AnaWith60TasksAsync(HttpClient client, string email)
    {
        await app.DbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Email == email);
            for (int i = 1; i <= 60; i++)
            {
                var task = new TaskItem { Title = $"Paging task {i:00}", UserId = user.Id, Due = new DateOnly(2027, 1, 1).AddDays(i) };
                if (i == 1) task.Steps.AddRange([new TaskStep { Text = "a", Position = 1, Done = true }, new TaskStep { Text = "b", Position = 2 }]);
                db.Tasks.Add(task);
            }
            return await db.SaveChangesAsync();
        });
        return await client.GetStringAsync("/tasks?sort=due");
    }

    private static int Rows(string html) => html.Split("data-label=\"Task\"").Length - 1;     // table rows
    private static int Mine(string html) => html.Split("Paging task ").Length - 1;

    [Fact]
    public async Task TheFirstPage_Shows50_AndCountsEveryTask()
    {
        var (ana, email) = await app.SignedInClientAsync("Ana");
        string html = await AnaWith60TasksAsync(ana, email);

        // Development adds a few sample tasks for the first account in a new database: count rows, not titles.
        int total = int.Parse(System.Text.RegularExpressions.Regex.Match(html, @"Page 1 of 2 \((\d+) tasks\)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(50, Rows(html));
        Assert.InRange(total, 60, 99);
        Assert.Contains("1 of 2 steps done", html);          // counted by the database, for a task on this page
    }

    [Fact]
    public async Task TheSecondPage_ShowsTheRest()
    {
        var (ana, email) = await app.SignedInClientAsync("Ana");
        await AnaWith60TasksAsync(ana, email);

        string html = await ana.GetStringAsync("/tasks?sort=due&page=2");

        Assert.Equal(60, Mine(await ana.GetStringAsync("/tasks?sort=due")) + Mine(html));   // every task, on one page or the other
        Assert.Contains("Paging task 60", html);
        Assert.Contains("← Previous", html);
    }
}
