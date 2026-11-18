using System.Net;
using System.Text.RegularExpressions;

namespace TaskFlow.Tests.Integration;

// Helpers that do what a browser does: read the anti-forgery token from a page, post forms, keep cookies.
public static class Browser
{
    public static async Task<string> TokenFromAsync(HttpClient client, string path)
    {
        string html = await client.GetStringAsync(path);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, $"No anti-forgery token on {path}");
        return match.Groups[1].Value;
    }

    // Registers through the real form. The client keeps the sign-in cookie afterwards.
    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string name, string password = "long-enough-1")
    {
        string token = await TokenFromAsync(client, "/account/register");
        return await client.PostAsync("/account/register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["DisplayName"] = name,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["__RequestVerificationToken"] = token,
        }));
    }
}
