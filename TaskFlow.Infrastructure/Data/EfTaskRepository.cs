using Microsoft.EntityFrameworkCore;
using TaskFlow.Common;
using TaskFlow.Data;
using TaskFlow.Models;

using TaskFlow.Services;

namespace TaskFlow.Data;

// TaskFlow's tasks, kept in the database through Entity Framework Core.
// Since Chapter 25, every query only sees the signed-in user's tasks.
public class EfTaskRepository : ITaskRepository
{
    private readonly TaskFlowDbContext _db;
    private readonly TimeProvider _time;
    private readonly ICurrentUser _user;

    public EfTaskRepository(TaskFlowDbContext db, TimeProvider time, ICurrentUser user)
    {
        _db = db;
        _time = time;
        _user = user;
    }

    // The starting point of every query: this user's tasks, and nobody else's.
    private IQueryable<TaskItem> MyTasks()
    {
        int userId = _user.Id;
        return _db.Tasks.Where(t => t.UserId == userId);
    }

    // Unfinished first, then by due date, tasks without a date last (as in Chapter 22's SQL).
    // Include: each task's project comes in the same query (a LEFT JOIN), not one query per task.
    // Chapter 30: and its steps, for "2 of 5 steps done" in the list.
    public Task<List<TaskItem>> GetAllAsync()
    {
        return MyTasks()
                  .AsNoTracking()
                  .Include(t => t.Project)
                  .Include(t => t.Steps)
                  .OrderBy(t => t.Done)
                  .ThenBy(t => t.Due == null)
                  .ThenBy(t => t.Due)
                  .ToListAsync();
    }

    // Course 2, Chapter 15: filter, count and page IN THE DATABASE. The order must be total (Id last):
    // with ties, a database may return them in a different order each time, and a task could
    // appear on two pages, or on none.
    public async Task<TaskPage> GetPageAsync(TaskFilter filter, int page, int pageSize)
    {
        var query = MyTasks().AsNoTracking();
        if (filter.Done is bool done) query = query.Where(t => t.Done == done);
        if (filter.ProjectId is int projectId) query = query.Where(t => t.ProjectId == projectId);

        int total = await query.CountAsync();
        var items = await query.Include(t => t.Project)
                               .OrderBy(t => t.Done)
                               .ThenBy(t => t.Due == null)
                               .ThenBy(t => t.Due)
                               .ThenBy(t => t.Id)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        return new TaskPage(items, total);
    }

    // Course 2, Chapter 5: the database counts; no task is loaded. The same rules as
    // TaskItem.IsOverdue and IsDueSoon (a test checks they agree), written as queries.
    public async Task<DueCounts> CountDueAsync(DateOnly today, int days)
    {
        DateOnly last = today.AddDays(days);
        var open = MyTasks().Where(t => !t.Done && t.Due != null);
        int overdue = await open.CountAsync(t => t.Due < today);
        int dueSoon = await open.CountAsync(t => t.Due >= today && t.Due <= last);
        return new DueCounts(overdue, dueSoon);
    }

    public Task<TaskItem?> FindAsync(int id)
    {
        return MyTasks().AsNoTracking()
                        .Include(t => t.Project)
                        .Include(t => t.Steps.OrderBy(s => s.Position))   // Chapter 30: in order
                        .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TaskItem> AddAsync(TaskInput input)
    {
        var task = new TaskItem
        {
            Title = input.Title!.Trim(),
            Priority = input.Priority,
            Due = input.Due,
            Notes = input.Notes,
            ProjectId = input.ProjectId,
            CreatedOn = _time.Today(),
            UserId = _user.Id,                      // Chapter 25: the new task is yours
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();               // INSERT; the database fills in task.Id
        return task;
    }

    public async Task<SaveResult> UpdateAsync(int id, TaskInput input)
    {
        // Tracked: EF notices the changes below. (Not FindAsync: it can't add "and it's mine".)
        var task = await MyTasks().FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
        {
            return SaveResult.NotFound;
        }

        // The version the form was shown with becomes the "original" value, so the UPDATE
        // only succeeds if nobody has saved this task since: ... WHERE Id = @id AND Version = @v
        _db.Entry(task).Property(t => t.Version).OriginalValue = input.Version;

        task.Title = input.Title!.Trim();
        task.Priority = input.Priority;
        task.Due = input.Due;
        task.Notes = input.Notes;
        task.ProjectId = input.ProjectId;
        task.Version = input.Version + 1;           // the next version
        try
        {
            await _db.SaveChangesAsync();           // UPDATE, only for the columns that changed
            return SaveResult.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SaveResult.Conflict;             // 0 rows matched: someone else saved first
        }
    }

    public async Task<bool> MarkDoneAsync(int id)
    {
        int rows = await MyTasks()
                            .Where(t => t.Id == id)
                            .ExecuteUpdateAsync(set => set
                                .SetProperty(t => t.Done, true)
                                .SetProperty(t => t.Version, t => t.Version + 1));
        return rows == 1;                            // 0 rows: no task with that id
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await MyTasks().FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
        {
            return false;
        }
        _db.Tasks.Remove(task);                     // marked Deleted...
        await _db.SaveChangesAsync();               // ...DELETE sent here
        return true;
    }
}
