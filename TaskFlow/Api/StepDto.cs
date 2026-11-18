using TaskFlow.Models;

namespace TaskFlow.Api;

// What the API sends for a checklist step (Chapter 30). No TaskItem, no user: just the step.
public record StepDto(int Id, string Text, bool Done, int Position)
{
    public static StepDto From(TaskStep step) => new(step.Id, step.Text, step.Done, step.Position);
}
