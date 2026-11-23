using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Caching;

// Course 2, Chapter 27: a DECORATOR (Chapter 10) that caches the one read every page makes: the overdue / due-soon counts
// in the header. Measured with 2,000 tasks (docs/scalability.md): removing those two COUNT queries raised throughput on
// light pages. Everything else passes straight through to the real repository.
//
// Rules for a cache that never shows a wrong count to the person who changed something:
//  - one entry per user, valid for one day and one "due soon" window (the key says which), for at most `ttl`;
//  - every write that can change the counts REMOVES the entry first (invalidation), so the next page reads fresh numbers;
//  - writes that don't go through this class (other copies of the app, a script, the database directly) are NOT seen
//    until the entry expires. That's the price of an in-memory cache; `ttl` bounds it.
public sealed class CachingTaskRepository(ITaskRepository inner, IMemoryCache cache, ICurrentUser user, TimeSpan ttl) : ITaskRepository
{
    private string Key => $"due-counts:{user.Id}";
    private sealed record Entry(DateOnly Today, int Days, DueCounts Counts);

    public async Task<DueCounts> CountDueAsync(DateOnly today, int days)
    {
        if (cache.TryGetValue(Key, out Entry? hit) && hit!.Today == today && hit.Days == days)
        {
            return hit.Counts;
        }
        var counts = await inner.CountDueAsync(today, days);
        cache.Set(Key, new Entry(today, days, counts), ttl);
        return counts;
    }

    // Writes: invalidate, then write (and again after, in case a read filled the entry in between).
    public async Task<TaskItem> AddAsync(TaskInput input) { Forget(); try { return await inner.AddAsync(input); } finally { Forget(); } }
    public async Task<SaveResult> UpdateAsync(int id, TaskInput input) { Forget(); try { return await inner.UpdateAsync(id, input); } finally { Forget(); } }
    public async Task<bool> MarkDoneAsync(int id) { Forget(); try { return await inner.MarkDoneAsync(id); } finally { Forget(); } }
    public async Task<bool> DeleteAsync(int id) { Forget(); try { return await inner.DeleteAsync(id); } finally { Forget(); } }
    private void Forget() => cache.Remove(Key);

    // Reads that aren't cached.
    public Task<List<TaskItem>> GetAllAsync() => inner.GetAllAsync();
    public Task<Dictionary<int, StepCount>> CountStepsAsync(IReadOnlyCollection<int> taskIds) => inner.CountStepsAsync(taskIds);
    public Task<TaskPage> GetPageAsync(TaskFilter filter, int page, int pageSize) => inner.GetPageAsync(filter, page, pageSize);
    public Task<TaskItem?> FindAsync(int id) => inner.FindAsync(id);
    public Task<List<TaskChoice>> GetChoicesAsync() => inner.GetChoicesAsync();
}
