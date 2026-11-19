using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Tests.Integration;
using Xunit.Abstractions;

namespace TaskFlow.Tests.Growth;

// Course 2, Chapter 5: how do TaskFlow's pages behave as one user's task list grows?
// Not a pass/fail test: it prints timings. It's slow, so it only runs when asked:
//   dotnet test -c Release --filter Category=Growth --logger "console;verbosity=detailed"
[Trait("Category", "Growth")]
public class PageGrowthTests(ITestOutputHelper output)
{
    [Fact]
    public async Task PagesAsTheTaskListGrows()
    {
        output.WriteLine($"{"tasks",7}  {"/tasks/new",11}  {"/tasks/{id}",12}  {"/tasks",9}");
        foreach (int n in new[] { 100, 1_000, 2_000, 4_000, 8_000 })
        {
            using var factory = new TaskFlowFactory();
            var ana = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            string email = $"ana-{n}@example.com";
            await Browser.RegisterAsync(ana, email, "Ana");
            int firstId = await AddTasksDirectlyAsync(factory, email, n);

            double newPage = await TimeAsync(() => ana.GetStringAsync("/tasks/new"));
            double onePage = await TimeAsync(() => ana.GetStringAsync($"/tasks/{firstId}"));
            double list = await TimeAsync(() => ana.GetStringAsync("/tasks"));
            output.WriteLine($"{n,7:N0}  {newPage,8:F1} ms  {onePage,9:F1} ms  {list,6:F1} ms");
        }
    }

    // Straight into the database: n tasks with 3 steps each, a third of them done, due dates over ~13 months.
    private static async Task<int> AddTasksDirectlyAsync(TaskFlowFactory factory, string email, int n)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        int userId = db.Users.Single(u => u.Email == email).Id;
        var rnd = new Random(n);
        var tasks = new List<TaskItem>();
        for (int i = 0; i < n; i++)
        {
            var task = new TaskItem { Title = $"Task {i}", UserId = userId, Done = i % 3 == 0,
                                      Due = new DateOnly(2026, 6, 1).AddDays(rnd.Next(400)) };
            for (int s = 1; s <= 3; s++) task.Steps.Add(new TaskStep { Text = $"step {s}", Position = s });
            tasks.Add(task);
        }
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();
        return tasks[0].Id;
    }

    // Warm up once, then the average of 5 requests.
    private static async Task<double> TimeAsync(Func<Task> request)
    {
        await request();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 5; i++) await request();
        return sw.Elapsed.TotalMilliseconds / 5;
    }
}
