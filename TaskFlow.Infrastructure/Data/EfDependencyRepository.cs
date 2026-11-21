using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

using TaskFlow.Services;

namespace TaskFlow.Data;

// Course 2, Chapter 4: dependencies in the database. Every query starts from this user's tasks.
public class EfDependencyRepository : IDependencyRepository
{
    private readonly TaskFlowDbContext _db;
    private readonly ICurrentUser _user;

    public EfDependencyRepository(TaskFlowDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    private IQueryable<TaskDependency> MyDependencies()
    {
        int userId = _user.Id;
        return _db.Dependencies.Where(d => d.Task!.UserId == userId);
    }

    public Task<List<TaskItem>> BlockersOfAsync(int taskId) =>
        MyDependencies().Where(d => d.TaskId == taskId)
                        .Select(d => d.BlockedBy!)
                        .AsNoTracking()
                        .OrderBy(t => t.Title)
                        .ToListAsync();

    public async Task<DependencyResult> AddAsync(int taskId, int blockedById)
    {
        // Course 2, Chapter 14: check-then-insert must happen as ONE unit. Two requests adding opposite
        // dependencies at the same moment could each find "no loop" and both save — a loop.
        // On SQLite, BeginTransaction starts with BEGIN IMMEDIATE: it takes the write lock first, so a
        // second request waits here until the first has committed, and then sees its dependency.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        int userId = _user.Id;
        var titles = await _db.Tasks.Where(t => t.UserId == userId && (t.Id == taskId || t.Id == blockedById))
                                    .ToDictionaryAsync(t => t.Id, t => t.Title);
        if (!titles.TryGetValue(taskId, out string? taskTitle) || !titles.TryGetValue(blockedById, out string? blockerTitle))
            return new DependencyResult(false, NotFound: true);              // not yours, or no such task

        if (await MyDependencies().AnyAsync(d => d.TaskId == taskId && d.BlockedById == blockedById))
            return new DependencyResult(false, $"\"{taskTitle}\" already waits for \"{blockerTitle}\".");

        // Load this user's whole graph (a few hundred edges at most) and check it in memory.
        var edges = await MyDependencies().Select(d => new { d.BlockedById, d.TaskId }).ToListAsync();
        var cycle = DependencyGraph.CycleIfAdded(edges.Select(e => (e.BlockedById, e.TaskId)), taskId, blockedById);
        if (cycle is not null)
        {
            var names = await _db.Tasks.Where(t => t.UserId == userId && cycle.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Title);
            string loop = string.Join(" → ", cycle.Select(id => names[id]));
            return new DependencyResult(false, $"That would create a loop where nothing can start: {loop}.");
        }

        _db.Dependencies.Add(new TaskDependency { TaskId = taskId, BlockedById = blockedById });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();                 // any early return above rolls back (nothing was written)
        return new DependencyResult(true);
    }

    public async Task<bool> RemoveAsync(int taskId, int blockedById) =>
        await MyDependencies().Where(d => d.TaskId == taskId && d.BlockedById == blockedById)
                              .ExecuteDeleteAsync() == 1;
}
