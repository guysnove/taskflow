using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Data;
using TaskFlow.Notifications;
using TaskFlow.UseCases;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 12: a use case, tested with no web server and no database.
public class CompleteTaskTests
{
    private sealed class RecordingObserver : ITaskCompletedObserver
    {
        public List<int> Completed { get; } = [];
        public Task OnTaskCompletedAsync(int taskId)
        {
            Completed.Add(taskId);
            return Task.CompletedTask;
        }
    }

    private readonly InMemoryTaskRepository _tasks = new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero)));
    private readonly RecordingObserver _observer = new();

    private CompleteTask UseCase() => new(_tasks, new TaskEvents([_observer]), NullLogger<CompleteTask>.Instance);

    [Fact]
    public async Task CompletingATask_MarksItDone_AndAnnouncesIt()
    {
        var first = (await _tasks.GetAllAsync()).First(t => !t.Done);

        Assert.True(await UseCase().RunAsync(first.Id));

        Assert.True((await _tasks.FindAsync(first.Id))!.Done);
        Assert.Equal([first.Id], _observer.Completed);
    }

    [Fact]
    public async Task AnUnknownTask_IsNotFound_AndNothingIsAnnounced()
    {
        Assert.False(await UseCase().RunAsync(9999));
        Assert.Empty(_observer.Completed);
    }
}
