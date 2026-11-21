using System.Net;

namespace Ch18.Notes.Tests;

// The REAL sign-in: a form post, a cookie, and the cookie sent back on the next request —
// what a browser does. Slower to write than the test scheme, so a few tests like this
// prove sign-in works, and the rest use the shortcut.
public class SignInTests(NotesFactory app) : IClassFixture<NotesFactory>
{
    [Fact]
    public async Task SignIn_ThenAddANoteThroughTheForm_ShowsItOnThePage()
    {
        // The test client keeps cookies, like a browser. Redirects are followed for us.
        var browser = app.CreateClient();
        var name = $"cleo-{Guid.NewGuid():N}"[..13];

        var signIn = await browser.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = name }));
        var add = await browser.PostAsync("/notes", new FormUrlEncodedContent(new Dictionary<string, string> { ["text"] = "Water the plants" }));

        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);         // after following the redirect to "/"
        string page = await add.Content.ReadAsStringAsync();
        Assert.Contains($"{name}'s notes", page);
        Assert.Contains("<li>Water the plants</li>", page);
    }

    [Fact]
    public async Task WithoutSigningIn_TheFormRedirectsToTheSignInPage()
    {
        var browser = app.CreateClient(new() { AllowAutoRedirect = false });

        var response = await browser.PostAsync("/notes", new FormUrlEncodedContent(new Dictionary<string, string> { ["text"] = "x" }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location!.AbsolutePath);
    }
}
