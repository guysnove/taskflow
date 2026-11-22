using System.Text.Encodings.Web;
using Microsoft.Data.Sqlite;

// Chapter 20: a tiny notes service with FOUR classic vulnerabilities, each next to its fix.
//   /bad/...   written the way it's too often written: exploitable (the tests prove it)
//   /good/...  the same feature, done safely
// NEVER deploy the /bad endpoints. They exist so you can see an attack work, then see it fail.
//
// "Who is this?" comes from an X-User header to keep the demo short. That is NOT authentication:
// anyone can send any header. TaskFlow uses a signed cookie (Course 1, Chapter 25).
var builder = WebApplication.CreateBuilder(args);
string connection = builder.Configuration.GetConnectionString("Notes") ?? "Data Source=notes.db";
builder.Services.AddSingleton(new NotesDb(connection));
var app = builder.Build();
app.Services.GetRequiredService<NotesDb>().CreateAndSeed();

static string Who(HttpContext ctx) => ctx.Request.Headers["X-User"].ToString();
static IResult Html(string body) => Results.Content($"<!doctype html><html><body>{body}</body></html>", "text/html");

// ---- 1. SQL injection ------------------------------------------------------------------------
// BAD: the search words are pasted INTO the SQL text. A quote ends the string; the rest is SQL.
app.MapGet("/bad/search", (HttpContext ctx, string q, NotesDb db) =>
{
    string sql = $"SELECT owner, text FROM notes WHERE owner = '{Who(ctx)}' AND text LIKE '%{q}%'";
    return Results.Ok(new { sql, rows = db.Query(sql) });
});

// GOOD: parameters. The words are sent to SQLite as DATA, separately from the SQL text.
app.MapGet("/good/search", (HttpContext ctx, string q, NotesDb db) =>
{
    const string sql = "SELECT owner, text FROM notes WHERE owner = $owner AND text LIKE '%' || $q || '%'";
    return Results.Ok(new { sql, rows = db.Query(sql, ("$owner", Who(ctx)), ("$q", q)) });
});

// ---- 2. Cross-site scripting (XSS) -------------------------------------------------------------
// BAD: the name is pasted into the HTML. <script> in the name becomes a script in the page.
app.MapGet("/bad/hello", (string name) => Html($"<p>Hello, {name}!</p>"));

// GOOD: encode for HTML. <script> becomes &lt;script&gt;: text on the screen, not code.
// (Razor does this for you with @name. Plus a Content-Security-Policy as a second line of defence.)
app.MapGet("/good/hello", (HttpContext ctx, string name) =>
{
    ctx.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'";
    return Html($"<p>Hello, {HtmlEncoder.Default.Encode(name)}!</p>");
});

// ---- 3. Broken access control (IDOR: insecure direct object reference) ----------------------------
// BAD: any note, by its number. Ben asks for /bad/notes/1 and reads Ana's note.
app.MapGet("/bad/notes/{id:int}", (int id, NotesDb db) =>
    db.Query("SELECT owner, text FROM notes WHERE id = $id", ("$id", id)) is [var row] ? Results.Ok(row) : Results.NotFound());

// GOOD: only YOUR notes. Someone else's is "not found" — not even "forbidden", which would confirm it exists.
app.MapGet("/good/notes/{id:int}", (HttpContext ctx, int id, NotesDb db) =>
    db.Query("SELECT owner, text FROM notes WHERE id = $id AND owner = $owner", ("$id", id), ("$owner", Who(ctx)))
        is [var row] ? Results.Ok(row) : Results.NotFound());

// ---- 4. Open redirect --------------------------------------------------------------------------
// BAD: "after signing in, go to ?to=...". A phishing link can send people to a look-alike site.
app.MapGet("/bad/go", (string to) => Results.Redirect(to));

// GOOD: only addresses on this site ("/tasks", not "https://evil.example" or "//evil.example").
app.MapGet("/good/go", (string to) => Results.LocalRedirect(IsLocal(to) ? to : "/"));

app.Run();

static bool IsLocal(string url) =>
    url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal) && !url.StartsWith("/\\", StringComparison.Ordinal);

public partial class Program;

// Plain ADO.NET, so you can see exactly which SQL reaches the database.
public sealed class NotesDb(string connectionString)
{
    public void CreateAndSeed()
    {
        Query("CREATE TABLE IF NOT EXISTS notes (id INTEGER PRIMARY KEY, owner TEXT NOT NULL, text TEXT NOT NULL)");
        if (Query("SELECT COUNT(*) AS n FROM notes")[0]["n"] is long n && n > 0) return;
        foreach (var (owner, text) in new[] { ("ana", "Buy milk"), ("ana", "Dentist on Friday"),
                                              ("ben", "Bank PIN hint: dog's birthday"), ("ben", "Surprise party for Ana") })
        {
            Query("INSERT INTO notes (owner, text) VALUES ($o, $t)", ("$o", owner), ("$t", text));
        }
    }

    public List<Dictionary<string, object?>> Query(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        using var reader = command.ExecuteReader();
        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }
}
