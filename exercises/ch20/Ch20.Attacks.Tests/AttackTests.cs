using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ch20.Attacks.Tests;

// Each vulnerability, twice: the attack WORKS against /bad (the test proves it), and FAILS against /good.
// Security tests are ordinary tests: an attacker's input, and the result you want.
public class AttackTests(NotesFactory app) : IClassFixture<NotesFactory>
{
    private static async Task<string[]> SearchAsync(HttpClient client, string path, string q)
    {
        var json = await client.GetFromJsonAsync<JsonElement>($"{path}?q={Uri.EscapeDataString(q)}");
        return json.GetProperty("rows").EnumerateArray().Select(r => r.GetProperty("text").GetString()!).ToArray();
    }

    // ---- 1. SQL injection
    private const string Injection = "' OR 1=1 --";       // ' ends the text; OR 1=1 is always true; -- comments out the rest

    [Fact]
    public async Task SqlInjection_Bad_ShowsEveryonesNotes()
    {
        var notes = await SearchAsync(app.As("ana"), "/bad/search", Injection);
        Assert.Contains("Bank PIN hint: dog's birthday", notes);           // Ben's note, read by Ana
    }

    [Fact]
    public async Task SqlInjection_Good_FindsNothing()
    {
        var notes = await SearchAsync(app.As("ana"), "/good/search", Injection);
        Assert.Empty(notes);                                                // just text that isn't in any note
    }

    // ---- 2. Cross-site scripting
    private const string Script = "<script>alert('stolen')</script>";

    [Fact]
    public async Task Xss_Bad_PutsTheScriptInThePage()
    {
        string html = await app.As("ana").GetStringAsync("/bad/hello?name=" + Uri.EscapeDataString(Script));
        Assert.Contains("<script>alert('stolen')</script>", html);
    }

    [Fact]
    public async Task Xss_Good_ShowsItAsText_AndForbidsInlineScripts()
    {
        var response = await app.As("ana").GetAsync("/good/hello?name=" + Uri.EscapeDataString(Script));
        string html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    // ---- 3. Broken access control (IDOR)
    [Fact]
    public async Task Idor_Bad_BenReadsAnasNote()
    {
        var response = await app.As("ben").GetAsync("/bad/notes/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"owner\":\"ana\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Idor_Good_BenGetsNotFound_AnaGetsHerNote()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await app.As("ben").GetAsync("/good/notes/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.As("ana").GetAsync("/good/notes/1")).StatusCode);
    }

    // ---- 4. Open redirect
    [Theory]
    [InlineData("https://evil.example/login")]
    [InlineData("//evil.example/login")]
    public async Task OpenRedirect_Bad_SendsYouAway(string to)
    {
        var response = await app.As("ana").GetAsync("/bad/go?to=" + Uri.EscapeDataString(to));
        Assert.Equal(to, response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("https://evil.example/login", "/")]
    [InlineData("//evil.example/login", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("/notes", "/notes")]
    public async Task OpenRedirect_Good_StaysOnThisSite(string to, string expected)
    {
        var response = await app.As("ana").GetAsync("/good/go?to=" + Uri.EscapeDataString(to));
        Assert.Equal(expected, response.Headers.Location!.OriginalString);
    }
}
