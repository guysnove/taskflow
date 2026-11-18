namespace TaskFlow.Models;

// A person who can sign in to TaskFlow (Chapter 25).
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";             // unique, ignoring capitals
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";      // never the password itself
    public bool IsAdmin { get; set; }
    public DateOnly CreatedOn { get; set; }

    // Slows down password guessing: after 5 wrong passwords, sign-in is locked for a while.
    public int FailedSignIns { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }

    public List<TaskItem> Tasks { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}
