using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Caching;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 27: the header's due counts are cached per user, and every write that can change them invalidates the entry.
[Trait("Req", "NFR-PERF-6")]
public sealed class CachingTaskRepositoryTests : IDisposable
{
    private sealed class User(int id) : ICurrentUser { public bool IsSignedIn => true; public int Id => id; }

    // Counts how often the real repository is asked for the counts.
    private sealed class Counting(ITaskRepository inner) : ITaskRepository
    {
        public int CountCalls;
        public Task<DueCounts> CountDueAsync(DateOnly today, int days) { CountCalls++; return inner.CountDueAsync(today, days); }
        public Task<TaskItem> AddAsync(TaskInput input) => inner.AddAsync(input);
        public Task<SaveResult> UpdateAsync(int id, TaskInput input) => inner.UpdateAsync(id, input);
        public Task<bool> MarkDoneAsync(int id) => inner.MarkDoneAsync(id);
        public Task<bool> DeleteAsync(int id) => inner.DeleteAsync(id);
        public Task<List<TaskItem>> GetAllAsync() => inner.GetAllAsync();
        public Task<Dictionary<int, StepCount>> CountStepsAsync(IReadOnlyCollection<int> taskIds) => inner.CountStepsAsync(taskIds);
        public Task<TaskPage> GetPageAsync(TaskFilter filter, int page, int pageSize) => inner.GetPageAsync(filter, page, pageSize);
        public Task<TaskItem?> FindAsync(int id) => inner.FindAsync(id);
        public Task<List<TaskChoice>> GetChoicesAsync() => inner.GetChoicesAsync();
    }

    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly Today = new(2026, 11, 23);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly Counting _real = new(new InMemoryTaskRepository(Clock));
    private CachingTaskRepository As(int userId) => new(_real, _cache, new User(userId), TimeSpan.FromMinutes(1));
    public void Dispose() => _cache.Dispose();      // CA1001: CI's -warnaserror build caught the missing Dispose

    [Fact]
    public async Task TheSecondPage_DoesNotAskTheDatabase()
    {
        var tasks = As(7);
        var first = await tasks.CountDueAsync(Today, 2);
        var second = await tasks.CountDueAsync(Today, 2);
        Assert.Equal(first, second);
        Assert.Equal(1, _real.CountCalls);
    }

    [Fact]
    public async Task AddingATask_ShowsInTheCountsAtOnce()
    {
        var tasks = As(7);
        var before = await tasks.CountDueAsync(Today, 2);
        await tasks.AddAsync(new TaskInput { Title = "Pay the plumber", Priority = "High", Due = Today.AddDays(-1) });
        var after = await tasks.CountDueAsync(Today, 2);
        Assert.Equal(before.Overdue + 1, after.Overdue);
        Assert.Equal(2, _real.CountCalls);
    }

    [Fact]
    public async Task FinishingATask_InvalidatesToo()
    {
        var tasks = As(7);
        await tasks.CountDueAsync(Today, 2);
        await tasks.MarkDoneAsync(1);                  // "Write report", overdue in the sample data
        await tasks.CountDueAsync(Today, 2);
        Assert.Equal(2, _real.CountCalls);
    }

    [Fact]
    public async Task EachUser_HasTheirOwnEntry()
    {
        await As(7).CountDueAsync(Today, 2);
        await As(8).CountDueAsync(Today, 2);
        Assert.Equal(2, _real.CountCalls);
    }

    [Fact]
    public async Task ANewDay_IsANewEntry()
    {
        var tasks = As(7);
        await tasks.CountDueAsync(Today, 2);
        await tasks.CountDueAsync(Today.AddDays(1), 2);   // after midnight, "overdue" means something else
        Assert.Equal(2, _real.CountCalls);
    }
}
