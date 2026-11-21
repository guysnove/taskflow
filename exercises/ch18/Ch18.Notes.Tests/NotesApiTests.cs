using System.Net;
using System.Net.Http.Json;
using Ch18.Notes;
using Microsoft.EntityFrameworkCore;

namespace Ch18.Notes.Tests;

// Integration tests: one HTTP request goes through the whole app — routing, sign-in, validation,
// EF Core, SQLite — and we check the answer AND what was stored.
// IClassFixture: ONE app and ONE database for all the tests in this class (starting an app takes time).
// So every test uses its own user: tests share the database, never each other's data.
public class NotesApiTests(TestSignInFactory app) : IClassFixture<TestSignInFactory>
{
    private static string NewUser() => $"ana-{Guid.NewGuid():N}"[..12];

    [Fact]
    public async Task Post_Returns201_AndTheNoteIsInTheDatabase()
    {
        // Arrange
        var ana = NewUser();
        var client = app.ClientFor(ana);

        // Act
        var response = await client.PostAsJsonAsync("/api/notes", new { text = "  Buy milk  " });

        // Assert: the HTTP answer...
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<NoteDto>();
        Assert.Equal("Buy milk", created!.Text);
        Assert.Equal($"/api/notes/{created.Id}", response.Headers.Location!.AbsolutePath);

        // ...and what the database really holds
        var row = await app.DbAsync(db => db.Notes.SingleAsync(n => n.Id == created.Id));
        Assert.Equal(ana, row.Owner);
        Assert.Equal("Buy milk", row.Text);                                       // trimmed
        Assert.Equal(new DateTime(2026, 11, 23, 9, 0, 0), row.CreatedUtc);        // the fake clock
    }

    [Fact]
    public async Task TheLocationHeader_LeadsToTheSameNote()
    {
        var client = app.ClientFor(NewUser());
        var response = await client.PostAsJsonAsync("/api/notes", new { text = "Call the bank" });

        var again = await client.GetFromJsonAsync<NoteDto>(response.Headers.Location);

        Assert.Equal("Call the bank", again!.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EmptyText_Is400_AndNothingIsStored(string? text)
    {
        var ben = NewUser();

        var response = await app.ClientFor(ben).PostAsJsonAsync("/api/notes", new { text });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Please write something.", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await app.DbAsync(db => db.Notes.CountAsync(n => n.Owner == ben)));
    }

    [Fact]
    public async Task NotSignedIn_Is401()
    {
        var stranger = app.CreateClient();                      // no X-Test-User header

        var response = await stranger.PostAsJsonAsync("/api/notes", new { text = "Hello" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SomeoneElsesNote_IsNotFound()
    {
        var mine = await app.ClientFor(NewUser()).PostAsJsonAsync("/api/notes", new { text = "Private" });

        var response = await app.ClientFor(NewUser()).GetAsync(mine.Headers.Location);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
