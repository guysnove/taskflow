namespace TaskFlow.Models;

// Course 2, Chapter 4: "Task can't start until BlockedBy is done."
// Each row is one edge of a graph: BlockedBy -> Task ("do BlockedBy first").
public class TaskDependency
{
    public int TaskId { get; set; }
    public TaskItem? Task { get; set; }

    public int BlockedById { get; set; }
    public TaskItem? BlockedBy { get; set; }
}
