using Microsoft.EntityFrameworkCore;

namespace Ch18.Notes;

// One note, owned by one person.
public class Note
{
    public int Id { get; set; }
    public string Owner { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}

public class NotesDb(DbContextOptions<NotesDb> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

// What a client sends to create a note.
public record NewNote(string? Text);

// What the API sends back.
public record NoteDto(int Id, string Text, DateTime CreatedUtc)
{
    public static NoteDto From(Note n) => new(n.Id, n.Text, n.CreatedUtc);
}

public static class NoteRules
{
    public const int MaxLength = 200;

    // Returns the cleaned text, or an error message.
    public static (string? Text, string? Error) Check(string? text)
    {
        var t = text?.Trim();
        if (string.IsNullOrEmpty(t)) return (null, "Please write something.");
        if (t.Length > MaxLength) return (null, $"A note can be at most {MaxLength} characters.");
        return (t, null);
    }
}
