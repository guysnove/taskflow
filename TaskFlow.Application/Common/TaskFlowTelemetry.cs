using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TaskFlow.Common;

// Course 2, Chapter 29: TaskFlow's own signals, besides its logs.
//  - TRACES: an ActivitySource. Code starts an activity (a "span") around a piece of work; spans started inside it, and every log
//    line written meanwhile, carry the same trace id. ASP.NET Core starts one span per request; ours nest inside it.
//  - METRICS: a Meter with a few instruments. Counting is cheap (no I/O); a collector (dotnet-counters, OpenTelemetry) reads them.
// Names follow OpenTelemetry's conventions: lower case, dots, the unit in the name or the instrument.
public static class TaskFlowTelemetry
{
    public const string Name = "TaskFlow";
    public static readonly ActivitySource Traces = new(Name);
}

public sealed class TaskFlowMetrics
{
    private readonly Counter<long> _tasksCreated;
    private readonly Counter<long> _tasksCompleted;
    private readonly Counter<long> _emails;
    private readonly Histogram<double> _emailSendDuration;
    private int _outboxWaiting;

    public TaskFlowMetrics(IMeterFactory meters)
    {
        var meter = meters.Create(TaskFlowTelemetry.Name);
        _tasksCreated = meter.CreateCounter<long>("taskflow.tasks.created", "{task}", "Tasks created, by the door they came through (web, api)");
        _tasksCompleted = meter.CreateCounter<long>("taskflow.tasks.completed", "{task}", "Tasks marked done");
        _emails = meter.CreateCounter<long>("taskflow.emails", "{email}", "Email send attempts, by outcome (sent, failed, dead)");
        _emailSendDuration = meter.CreateHistogram<double>("taskflow.email.send.duration", "s", "How long one send attempt took");
        meter.CreateObservableGauge("taskflow.email.outbox.waiting", () => Volatile.Read(ref _outboxWaiting), "{email}",
                                    "Emails in the outbox, not yet sent and not given up");
    }

    public void TaskCreated(string via) => _tasksCreated.Add(1, new KeyValuePair<string, object?>("taskflow.via", via));
    public void TaskCompleted() => _tasksCompleted.Add(1);

    // outcome: "sent", "failed" (will be retried) or "dead" (given up).
    public void EmailAttempt(string outcome, TimeSpan duration)
    {
        var tag = new KeyValuePair<string, object?>("taskflow.outcome", outcome);
        _emails.Add(1, tag);
        _emailSendDuration.Record(duration.TotalSeconds, tag);
    }

    public void OutboxWaiting(int count) => Volatile.Write(ref _outboxWaiting, count);
}
