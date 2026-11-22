using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 20 (F4): secrets never go into the settings files in git. This test reads every
// appsettings*.json and fails if a setting whose NAME sounds secret has a value. (A real team adds a
// secret scanner in CI too, such as gitleaks or GitHub's secret scanning, for every file and commit.)
[Trait("Category", "Security")]
public partial class NoSecretsInRepoTests
{
    [GeneratedRegex("password|secret|apikey|api_key|token|connectionstring.*password", RegexOptions.IgnoreCase)]
    private static partial Regex SecretName();

    private static string RepoRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    public static TheoryData<string> SettingsFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot(), "TaskFlow"), "appsettings*.json"))
        {
            data.Add(Path.GetFileName(file));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(SettingsFiles))]
    public void SettingsFiles_HoldNoSecrets(string file)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "TaskFlow", file)));
        var found = new List<string>();
        Walk(json.RootElement, "", found);
        Assert.Empty(found);
    }

    private static void Walk(JsonElement element, string path, List<string> found)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in element.EnumerateObject()) Walk(p.Value, path.Length == 0 ? p.Name : $"{path}:{p.Name}", found);
        }
        else if (element.ValueKind == JsonValueKind.String && SecretName().IsMatch(path) && element.GetString() is { Length: > 0 })
        {
            found.Add(path);
        }
    }
}
