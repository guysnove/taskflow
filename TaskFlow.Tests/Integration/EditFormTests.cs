using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 9: characterisation tests for the edit form, written BEFORE refactoring
// TasksController.Edit. Until now nothing tested it: not the save, not the conflict, not the project check.
public class EditFormTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public EditFormTests(TaskFlowFactory factory) => _factory = factory;

    private sealed record Dto(int Id, int Version);

    private async Task<(HttpClient Client, int Id)> SignedInWithATaskAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Browser.RegisterAsync(client, $"ana-{Guid.NewGuid():N}@example.com", "Ana");
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/tasks") { Content = JsonContent.Create(new { title = "Renew passport", priority = "Medium" }) };
        create.Headers.Add("X-CSRF-TOKEN", await Browser.TokenFromAsync(client, "/tasks/quick"));
        var created = await client.SendAsync(create);
        return (client, (await created.Content.ReadFromJsonAsync<Dto>(JsonSerializerOptions.Web))!.Id);
    }

    // Opens the edit form like a browser, and returns its token and version.
    private static async Task<(string Token, string Version)> OpenFormAsync(HttpClient client, int id)
    {
        string html = await client.GetStringAsync($"/tasks/{id}/edit");
        return (Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value,
                Regex.Match(html, "name=\"Version\" value=\"([0-9]+)\"").Groups[1].Value);
    }

    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, int id, string token, string version, string title, string? projectId = null)
    {
        var form = new Dictionary<string, string> { ["Title"] = title, ["Priority"] = "High", ["Version"] = version, ["__RequestVerificationToken"] = token };
        if (projectId is not null) form["ProjectId"] = projectId;
        return client.PostAsync($"/tasks/{id}/edit", new FormUrlEncodedContent(form));
    }

    [Fact]
    [Trait("Req", "FR-TASK-1")]
    public async Task Save_RedirectsToTheTask()
    {
        var (ana, id) = await SignedInWithATaskAsync();
        var (token, version) = await OpenFormAsync(ana, id);

        var response = await SaveAsync(ana, id, token, version, "Renew passport today");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/tasks/{id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    [Trait("Req", "NFR-CONC-1")]
    public async Task TwoTabs_SecondSave_ShowsTheConflict_AndTheSavedValues()
    {
        var (ana, id) = await SignedInWithATaskAsync();
        var (token, version) = await OpenFormAsync(ana, id);          // tab 1 and tab 2 open the same version
        await SaveAsync(ana, id, token, version, "Saved in tab one");

        var response = await SaveAsync(ana, id, token, version, "Saved in tab two");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Someone else saved this task while you were editing it.", html);
        Assert.Contains("Saved in tab one", html);                    // what is saved now
        Assert.Contains("value=\"Saved in tab two\"", html);           // the visitor's values, kept
        Assert.Contains($"name=\"Version\" value=\"{int.Parse(version, System.Globalization.CultureInfo.InvariantCulture) + 1}\"", html);
    }

    [Fact]
    [Trait("Req", "FR-OWN-1")]
    public async Task UnknownProject_ShowsTheFormAgain_WithAMessage()
    {
        var (ana, id) = await SignedInWithATaskAsync();
        var (token, version) = await OpenFormAsync(ana, id);

        var response = await SaveAsync(ana, id, token, version, "Renew passport", projectId: "999999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Please choose a project from the list.", await response.Content.ReadAsStringAsync());
    }
}
