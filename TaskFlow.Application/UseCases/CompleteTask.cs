using TaskFlow.Common;
using TaskFlow.Notifications;
using TaskFlow.Services;

namespace TaskFlow.UseCases;

// Course 2, Chapter 12: a USE CASE. "Complete a task" is one thing the application does, whichever
// door the request came through: the form (TasksController) or the API (TasksApiController).
// Before, both controllers repeated these steps; now they each make one call.
public class CompleteTask(ITaskRepository tasks, TaskEvents events, ILogger<CompleteTask> logger, TaskFlowMetrics metrics)
{
    // False when the task doesn't exist, or isn't the current user's.
    public async Task<bool> RunAsync(int taskId)
    {
        // Course 2, Chapter 29: a span for this use case. Its children (the observers' work) and its log lines share its trace id.
        using var activity = TaskFlowTelemetry.Traces.StartActivity("CompleteTask");
        activity?.SetTag("taskflow.task.id", taskId);
        if (!await tasks.MarkDoneAsync(taskId))
        {
            return false;
        }
        logger.LogInformation("Task {TaskId} marked done", taskId);
        await events.TaskCompletedAsync(taskId);             // whoever is listening (Chapter 10)
        metrics.TaskCompleted();
        return true;
    }
}
