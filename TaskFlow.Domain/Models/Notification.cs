namespace TaskFlow.Models;

// Course 2, Chapter 10: one in-app notification (FR-NOTIF-3).
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string Kind { get; set; } = "";          // "DueSoon", "Unblocked"
    public string Key { get; set; } = "";           // unique per user: the same news is never stored twice
    public string Text { get; set; } = "";
    public string? Link { get; set; }               // a page of this site, e.g. /tasks/3
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }     // null: unread
}
