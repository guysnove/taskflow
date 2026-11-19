using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 5: the database's counts must agree with the C# rules (TaskItem.IsOverdue / IsDueSoon),
// especially at the edges: yesterday, today, the last "soon" day, the day after it, done tasks, no due date.
[Trait("Req", "FR-DUE-1")]
public class DueCountsTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;
    public DueCountsTests(TaskFlowFactory factory) => _factory = factory;

    private class FixedUser(int id) : ICurrentUser
    {
        public bool IsSignedIn => true;
        public int Id => id;
    }

    [Fact]
    public async Task DatabaseCounts_AgreeWithTheRules_AtEveryEdge()
    {
        var client = _factory.CreateClient();
        string email = $"ana-{Guid.NewGuid():N}@example.com";
        await Browser.RegisterAsync(client, email, "Ana");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        int userId = db.Users.Single(u => u.Email == email).Id;
        var today = new DateOnly(2026, 11, 18);
        const int days = 2;
        int[] offsets = [-30, -1, 0, 1, 2, 3, 40];               // relative to today
        var tasks = new List<TaskItem>();
        foreach (int d in offsets)
        {
            tasks.Add(new TaskItem { Title = $"open {d}", UserId = userId, Due = today.AddDays(d) });
            tasks.Add(new TaskItem { Title = $"done {d}", UserId = userId, Due = today.AddDays(d), Done = true });
        }
        tasks.Add(new TaskItem { Title = "no due date", UserId = userId });
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();

        var repository = new EfTaskRepository(db, TimeProvider.System, new FixedUser(userId));
        var counts = await repository.CountDueAsync(today, days);

        // Compare with ALL of this user's tasks: in Development, the first user to register
        // adopts the sample tasks, so the user may own more tasks than the ones added above.
        var mine = db.Tasks.Where(t => t.UserId == userId).ToList();
        Assert.Equal(mine.Count(t => t.IsOverdue(today)), counts.Overdue);
        Assert.Equal(mine.Count(t => t.IsDueSoon(today, days)), counts.DueSoon);
        Assert.True(counts.Overdue >= 2 && counts.DueSoon >= 3);     // at least -30, -1 / 0, 1, 2 from above
    }
}
