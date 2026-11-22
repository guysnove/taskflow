// Chapter 25: a tiny notes service with a real database server (PostgreSQL), made to run in containers.
// Every setting comes from configuration, so the same image runs anywhere: only the environment changes.
using Ch25.Notes;

var builder = WebApplication.CreateBuilder(args);
string connection = builder.Configuration.GetConnectionString("Notes")
    ?? throw new InvalidOperationException("ConnectionStrings:Notes is not set (in a container: ConnectionStrings__Notes=…)");
builder.Services.AddSingleton(new NoteStore(connection));
var app = builder.Build();

// The database may start more slowly than we do: wait for it (a few tries), then make sure the table exists.
await app.Services.GetRequiredService<NoteStore>().PrepareAsync(app.Logger);

app.MapGet("/", (IConfiguration config) => new
{
    service = "Ch25.Notes",
    host = Environment.MachineName,                                // in a container: the container's id
    environment = app.Environment.EnvironmentName,
    greeting = config["Greeting"] ?? "Hello",
});
app.MapGet("/notes", async (NoteStore store) => await store.AllAsync());
app.MapPost("/notes", async (NewNote note, NoteStore store) =>
{
    string? problem = Note.Validate(note.Text);
    if (problem is not null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["text"] = [problem] });
    Note saved = await store.AddAsync(note.Text.Trim());
    return Results.Created($"/notes/{saved.Id}", saved);
});
app.MapGet("/health", async (NoteStore store) =>
    await store.CanConnectAsync() ? Results.Text("Healthy") : Results.Text("Unhealthy: no database", statusCode: 503));
app.Run();

public record NewNote(string Text);
public partial class Program;   // lets the tests start the app
