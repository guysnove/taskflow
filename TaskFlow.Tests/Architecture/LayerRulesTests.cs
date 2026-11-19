using System.Xml.Linq;

namespace TaskFlow.Architecture;

// Course 2, Chapter 12: the layers are now separate projects, and the compiler enforces their
// references. These tests make sure nobody adds a reference (or a package) that breaks the rule
// "dependencies point inwards": Web -> Infrastructure -> Application -> Domain.
public class LayerRulesTests
{
    public static readonly Dictionary<string, string[]> AllowedReferences = new()
    {
        ["TaskFlow.Domain"] = [],
        ["TaskFlow.Application"] = ["TaskFlow.Domain"],
        ["TaskFlow.Infrastructure"] = ["TaskFlow.Application", "TaskFlow.Domain"],
        ["TaskFlow"] = ["TaskFlow.Application", "TaskFlow.Infrastructure", "TaskFlow.Domain"],   // the composition root
    };

    private static XDocument Project(string name) =>
        XDocument.Load(Path.Combine(ModuleRulesTests.Root(), name, name + ".csproj"));

    private static IEnumerable<string> Includes(XDocument project, string item) =>
        project.Descendants(item).Select(e => (string?)e.Attribute("Include") ?? "");

    [Fact]
    public void ProjectReferences_PointInwards()
    {
        var wrong = AllowedReferences.Keys
            .SelectMany(p => Includes(Project(p), "ProjectReference")
                .Select(r => Path.GetFileNameWithoutExtension(r.Replace('\\', '/')))
                .Where(r => !AllowedReferences[p].Contains(r))
                .Select(r => $"{p} -> {r}"))
            .ToList();
        Assert.True(wrong.Count == 0, "References that point outwards:\n  " + string.Join("\n  ", wrong));
    }

    [Fact]
    public void Domain_HasNoPackages()
    {
        Assert.Empty(Includes(Project("TaskFlow.Domain"), "PackageReference"));
        Assert.Empty(Includes(Project("TaskFlow.Domain"), "FrameworkReference"));
    }

    [Fact]
    public void Application_KnowsNoDatabaseAndNoWeb()
    {
        var application = Project("TaskFlow.Application");
        Assert.DoesNotContain(Includes(application, "PackageReference"), p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(Includes(application, "FrameworkReference"), f => f == "Microsoft.AspNetCore.App");
    }
}
