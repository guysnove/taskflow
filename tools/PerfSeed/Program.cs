// Course 2, Chapter 26: realistic test data for performance work. A performance problem that needs 2,000 tasks to show
// won't show with the 5 sample tasks, so this gives one user a heavy, REPEATABLE workload (the same seed, the same data).
//   dotnet run --project tools/PerfSeed -- --db taskflow.db --email ana@example.com --tasks 2000 --projects 20 --steps 3
// The user must exist (register first). Their existing tasks and projects are REPLACED. Never run it against production.
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

string Arg(string name, string fallback) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
string db = Arg("--db", "TaskFlow/taskflow.db"), email = Arg("--email", "");
int tasks = int.Parse(Arg("--tasks", "2000")), projects = int.Parse(Arg("--projects", "20")), steps = int.Parse(Arg("--steps", "3"));
var random = new Random(int.Parse(Arg("--seed", "26")));

var options = new DbContextOptionsBuilder<TaskFlowDbContext>().UseSqlite($"Data Source={db}").Options;
await using var context = new TaskFlowDbContext(options);
var user = await context.Users.SingleOrDefaultAsync(u => u.Email == email)
           ?? throw new InvalidOperationException($"No user {email} in {db}: register first.");

await context.Tasks.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();        // steps and dependencies go with them
await context.Projects.Where(p => p.UserId == user.Id).ExecuteDeleteAsync();

var today = DateOnly.FromDateTime(DateTime.UtcNow);
var projectList = Enumerable.Range(1, projects).Select(i => new Project { Name = $"Project {i:00}", UserId = user.Id }).ToList();
string[] priorities = ["High", "Medium", "Low"];
for (int i = 1; i <= tasks; i++)
{
    var task = new TaskItem
    {
        Title = $"Task {i:0000}: {Words(random)}",
        Priority = priorities[random.Next(3)],
        Due = random.Next(4) == 0 ? null : today.AddDays(random.Next(-30, 60)),
        Done = random.Next(3) == 0,
        CreatedOn = today.AddDays(-random.Next(0, 90)),
        Project = random.Next(5) == 0 ? null : projectList[random.Next(projects)],
        UserId = user.Id,
    };
    for (int s = 1; s <= steps; s++) task.Steps.Add(new TaskStep { Text = $"Step {s}", Position = s, Done = random.Next(2) == 0 });
    context.Tasks.Add(task);
}
await context.SaveChangesAsync();
Console.WriteLine($"{email}: {tasks} tasks, {projects} projects, {tasks * steps} steps (seed {Arg("--seed", "26")})");

static string Words(Random r)
{
    string[] w = ["call", "write", "check", "order", "plan", "review", "book", "send", "fix", "update", "supplier", "report", "invoice", "meeting", "budget", "office"];
    return string.Join(' ', Enumerable.Range(0, 3).Select(_ => w[r.Next(w.Length)]));
}
