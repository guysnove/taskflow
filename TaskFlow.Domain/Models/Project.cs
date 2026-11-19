namespace TaskFlow.Models;

// A project groups tasks (Chapter 24). One project has many tasks.
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    // Chapter 25: who owns this project. Null only for projects from before users existed.
    public int? UserId { get; set; }
    public User? User { get; set; }

    // Navigation property: this project's tasks.
    public List<TaskItem> Tasks { get; set; } = new();
}
