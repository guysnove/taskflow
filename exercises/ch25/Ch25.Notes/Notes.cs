using Npgsql;

namespace Ch25.Notes;

public record Note(int Id, string Text, DateTime CreatedUtc)
{
    public const int MaxLength = 200;
    /// <summary>Returns what's wrong with the text, or null if it's fine.</summary>
    public static string? Validate(string? text) =>
        string.IsNullOrWhiteSpace(text) ? "A note needs some text."
        : text.Trim().Length > MaxLength ? $"A note can have at most {MaxLength} characters."
        : null;
}

/// <summary>Notes in PostgreSQL, with plain SQL (no ORM): the point of this chapter is where the database runs.</summary>
public sealed class NoteStore(string connectionString)
{
    private readonly NpgsqlDataSource _db = NpgsqlDataSource.Create(connectionString);

    public async Task PrepareAsync(ILogger log, int attempts = 10)
    {
        for (int i = 1; ; i++)
        {
            try
            {
                await using var cmd = _db.CreateCommand(
                    "CREATE TABLE IF NOT EXISTS notes (id serial PRIMARY KEY, text varchar(200) NOT NULL, created_utc timestamptz NOT NULL DEFAULT now())");
                await cmd.ExecuteNonQueryAsync();
                log.LogInformation("Database ready (attempt {Attempt})", i);
                return;
            }
            catch (Exception e) when (e is NpgsqlException or System.Net.Sockets.SocketException && i < attempts)
            {
                log.LogWarning("Database not ready (attempt {Attempt} of {Attempts}): {Message}", i, attempts, e.Message);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public async Task<bool> CanConnectAsync()
    {
        try { await using var cmd = _db.CreateCommand("SELECT 1"); await cmd.ExecuteScalarAsync(); return true; }
        catch (NpgsqlException) { return false; }
    }

    public async Task<List<Note>> AllAsync()
    {
        await using var cmd = _db.CreateCommand("SELECT id, text, created_utc FROM notes ORDER BY id");
        await using var r = await cmd.ExecuteReaderAsync();
        var notes = new List<Note>();
        while (await r.ReadAsync()) notes.Add(new Note(r.GetInt32(0), r.GetString(1), r.GetDateTime(2)));
        return notes;
    }

    public async Task<Note> AddAsync(string text)
    {
        await using var cmd = _db.CreateCommand("INSERT INTO notes (text) VALUES ($1) RETURNING id, text, created_utc");
        cmd.Parameters.AddWithValue(text);
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return new Note(r.GetInt32(0), r.GetString(1), r.GetDateTime(2));
    }
}
