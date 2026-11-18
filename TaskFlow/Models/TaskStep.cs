namespace TaskFlow.Models;

// Chapter 30 (the capstone): one step of a task's checklist, like "Buy boxes".
// One task has many steps; a step belongs to exactly one task.
public class TaskStep
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public bool Done { get; set; }
    public int Position { get; set; }           // 1, 2, 3… the order the steps were added

    public int TaskItemId { get; set; }         // foreign key: required (int, not int?)
    public TaskItem? TaskItem { get; set; }     // navigation property: the task it belongs to
}
