using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 25: the Dockerfile lists TaskFlow's projects by hand (so restore can be cached). Add a project to
// the solution and forget the Dockerfile, and only "docker build" finds out, in CI or later. These tests find out first.
public class DockerfileTests
{
    private static string RepoRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static readonly string Dockerfile = File.ReadAllText(Path.Combine(RepoRoot(), "Deploy", "Dockerfile"));

    /// <summary>TaskFlow and every project it depends on, directly or not.</summary>
    public static TheoryData<string> ShippedProjects()
    {
        var found = new SortedSet<string>();
        var todo = new Queue<string>(["TaskFlow"]);
        while (todo.Count > 0)
        {
            string name = todo.Dequeue();
            if (!found.Add(name)) continue;
            var csproj = XDocument.Load(Path.Combine(RepoRoot(), name, name + ".csproj"));
            foreach (var reference in csproj.Descendants("ProjectReference"))
            {
                todo.Enqueue(Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/')));
            }
        }
        var data = new TheoryData<string>();
        foreach (string name in found) data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(ShippedProjects))]
    public void EveryShippedProject_IsRestoredWithItsLockFile_AndCopied(string project)
    {
        Assert.Contains($"COPY {project}/{project}.csproj {project}/packages.lock.json {project}/", Dockerfile);
        Assert.Contains($"COPY {project}/ {project}/", Dockerfile);
    }

    [Fact]
    public void RestoreUsesLockedMode() => Assert.Contains("dotnet restore TaskFlow/TaskFlow.csproj --locked-mode", Dockerfile);

    [Fact]
    public void RunsAsTheNonRootUser() => Assert.Contains("USER $APP_UID", Dockerfile);
}
