using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Ch18.Notes.Tests;

// A MISTAKE, on purpose (Category=Flaky, left out of the normal test task).
// The factory uses ONE fixed database file and never deletes it, and the test assumes it starts
// with an empty database. It passes the first time... run it again.
public class SharedFileFactory : TestSignInFactory
{
    protected override string DbFile => Path.Combine(Path.GetTempPath(), "notes-shared-test.db");
    protected override bool KeepDatabase => true;
}

[Trait("Category", "Flaky")]
public class FlakyTests(SharedFileFactory app) : IClassFixture<SharedFileFactory>
{
    [Fact]
    public async Task Bad_AssumesAnEmptyDatabase()
    {
        var created = await (await app.ClientFor("ana").PostAsJsonAsync("/api/notes", new { text = "Hello" }))
            .Content.ReadFromJsonAsync<NoteDto>();

        Assert.Equal(1, created!.Id);                                              // "the first note gets id 1"
        Assert.Equal(1, await app.DbAsync(db => db.Notes.CountAsync()));         // "there is one note"
    }
}
