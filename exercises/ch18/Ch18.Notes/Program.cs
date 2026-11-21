using System.Security.Claims;
using System.Text.Encodings.Web;
using Ch18.Notes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

// A tiny notes app: a web page for people, a JSON API for programs, one SQLite database.
// Chapter 18 tests it from the outside: through HTTP, down to the database, and in a real browser.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NotesDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Notes") ?? "Data Source=notes.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        // A program calling the API gets 401, not a redirect to a sign-in page.
        o.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 401;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotesDb>().Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

static string Who(ClaimsPrincipal user) => user.Identity!.Name!;
static string Page(string body) =>
    $"<!doctype html><html><head><meta charset=\"utf-8\"><title>Notes</title></head><body>{body}</body></html>";

// ---- the web pages (forms without anti-forgery tokens, to keep the demo short; TaskFlow uses them)

app.MapGet("/", async (HttpContext ctx, NotesDb db) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
        return Results.Content(Page("<h1>Notes</h1><p><a href=\"/login\">Sign in</a></p>"), "text/html");
    var me = Who(ctx.User);
    var notes = await db.Notes.Where(n => n.Owner == me).OrderBy(n => n.Id).ToListAsync();
    var h = HtmlEncoder.Default;
    var items = string.Concat(notes.Select(n => $"<li>{h.Encode(n.Text)}</li>"));
    var error = ctx.Request.Query["error"].ToString();
    return Results.Content(Page($"""
        <h1>{h.Encode(me)}'s notes</h1>
        <form method="post" action="/notes"><input name="text" aria-label="New note"> <button>Add</button></form>
        {(error.Length > 0 ? $"<p role=\"alert\">{h.Encode(error)}</p>" : "")}
        <ul id="notes">{items}</ul>
        """), "text/html");
});

app.MapGet("/login", () => Results.Content(Page("""
    <h1>Sign in</h1>
    <form method="post" action="/login"><input name="name" aria-label="Your name"> <button>Sign in</button></form>
    """), "text/html"));

app.MapPost("/login", async (HttpContext ctx) =>
{
    var name = (await ctx.Request.ReadFormAsync())["name"].ToString().Trim();
    if (name.Length == 0) return Results.Redirect("/login");
    var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(new ClaimsPrincipal(identity));
    return Results.Redirect("/");
});

app.MapPost("/notes", async (HttpContext ctx, NotesDb db, TimeProvider time) =>
{
    var (text, error) = NoteRules.Check((await ctx.Request.ReadFormAsync())["text"]);
    if (error is not null) return Results.Redirect("/?error=" + Uri.EscapeDataString(error));
    db.Notes.Add(new Note { Owner = Who(ctx.User), Text = text!, CreatedUtc = time.GetUtcNow().UtcDateTime });
    await db.SaveChangesAsync();
    return Results.Redirect("/");
}).RequireAuthorization();

// ---- the JSON API

var api = app.MapGroup("/api/notes").RequireAuthorization();

api.MapGet("", async (ClaimsPrincipal user, NotesDb db) =>
{
    var me = Who(user);
    return (await db.Notes.Where(n => n.Owner == me).OrderBy(n => n.Id).ToListAsync()).Select(NoteDto.From);
});

api.MapGet("{id:int}", async (int id, ClaimsPrincipal user, NotesDb db) =>
{
    var me = Who(user);
    return await db.Notes.SingleOrDefaultAsync(n => n.Id == id && n.Owner == me) is Note n
        ? Results.Ok(NoteDto.From(n))
        : Results.NotFound();                                  // someone else's note: "not found", not "forbidden"
}).WithName("GetNote");

api.MapPost("", async (NewNote input, ClaimsPrincipal user, NotesDb db, TimeProvider time) =>
{
    var (text, error) = NoteRules.Check(input.Text);
    if (error is not null)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["text"] = [error] });
    var note = new Note { Owner = Who(user), Text = text!, CreatedUtc = time.GetUtcNow().UtcDateTime };
    db.Notes.Add(note);
    await db.SaveChangesAsync();
    return Results.CreatedAtRoute("GetNote", new { id = note.Id }, NoteDto.From(note));
});

app.Run();

// So the tests can say WebApplicationFactory<Program>.
public partial class Program;
