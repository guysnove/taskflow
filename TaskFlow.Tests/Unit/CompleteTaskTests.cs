using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Common;
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

    // Course 2, Chapter 29: real metrics, from a real IMeterFactory (each test class gets its own).
    private static readonly IMeterFactory Meters = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();
    private readonly TaskFlowMetrics _metrics = new(Meters);

    private CompleteTask UseCase() => new(_tasks, new TaskEvents([_observer], NullLogger<TaskEvents>.Instance), NullLogger<CompleteTask>.Instance, _metrics);

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

    // Course 2, Chapter 17: found while writing unit tests. The task is ALREADY saved as done when the
    // observers run. If one of them fails (a query times out, say), should the user get an error page
    // for a task that was in fact completed — and should the other observers be skipped?
    private sealed class BrokenObserver : ITaskCompletedObserver
    {
        public Task OnTaskCompletedAsync(int taskId) => throw new TimeoutException("the notifications query timed out");
    }

    [Fact]
    [Trait("Req", "NFR-NOTIF-2")]
    public async Task AFailingObserver_DoesNotFailTheUseCase_OrStopTheOthers()
    {
        var first = (await _tasks.GetAllAsync()).First(t => !t.Done);
        var useCase = new CompleteTask(_tasks, new TaskEvents([new BrokenObserver(), _observer], NullLogger<TaskEvents>.Instance), NullLogger<CompleteTask>.Instance, _metrics);

        bool completed = await useCase.RunAsync(first.Id);

        Assert.True(completed);
        Assert.True((await _tasks.FindAsync(first.Id))!.Done);
        Assert.Equal([first.Id], _observer.Completed);                // the second observer still ran
    }

    // Course 2, Chapter 29: completing a task counts, and makes a span with the task's id.
    [Fact]
    [Trait("Req", "NFR-OPS-3")]
    public async Task CompletingATask_IsCounted_AndTraced()
    {
        var first = (await _tasks.GetAllAsync()).First(t => !t.Done);
        using var completed = new MetricCollector<long>(Meters, TaskFlowTelemetry.Name, "taskflow.tasks.completed");
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TaskFlowTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add,
        };
        ActivitySource.AddActivityListener(listener);

        await UseCase().RunAsync(first.Id);

        Assert.Equal(1, completed.GetMeasurementSnapshot().EvaluateAsCounter());
        var span = Assert.Single(spans, s => s.OperationName == "CompleteTask");
        Assert.Equal(first.Id, span.GetTagItem("taskflow.task.id"));
    }
}
