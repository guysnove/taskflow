using System.Runtime.CompilerServices;

namespace TaskFlow.Architecture;

// Course 2, Chapter 11: docs/design/modules.md says which module may use which.
// These tests keep the code and the design document honest with each other.
public class ModuleRulesTests
{
    // The design, as data: each module, and the modules it is ALLOWED to use.
    public static readonly Dictionary<string, string[]> Allowed = new()
    {
        // Web: turns HTTP into calls, and results into pages or JSON. Never touches the database directly.
        ["Controllers"] = ["Models", "Services", "UseCases", "Notifications", "Settings", "Common"],
        ["Api"] = ["Models", "Services", "UseCases", "Notifications", "Common"],
        ["ViewComponents"] = ["Models", "Services", "Notifications", "Settings", "Common"],
        ["TagHelpers"] = ["Models", "Common"],
        ["Middleware"] = ["Common"],
        ["Security"] = ["Services", "Common"],                         // Course 2, Chapter 12: CurrentUser
        // Application: the rules and the features. Course 2, Chapter 12: no longer allowed to use Data.
        ["Services"] = ["Models", "Settings", "Common"],
        ["Notifications"] = ["Models", "Services", "Settings", "Common"],
        ["UseCases"] = ["Models", "Services", "Notifications", "Common"],          // Chapter 12: what the app does
        // Infrastructure (Data): implements the Application's ports, so it uses them (Chapter 12: the arrow flipped).
        ["Data"] = ["Models", "Services", "Notifications", "Common"],
        ["Caching"] = ["Models", "Services", "Common"],                              // Course 2, Chapter 27: caching decorators
        // The core: depends on nothing.
        ["Models"] = ["Common"],
        ["Settings"] = [],
        ["Common"] = [],
    };

    // Course 2, Chapter 12: the modules now live in four projects; read them all.
    public static readonly string[] Projects = ["TaskFlow", "TaskFlow.Application", "TaskFlow.Domain", "TaskFlow.Infrastructure"];

    public static string Root([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static ModuleMap Map() => ModuleMap.Read(Projects.Select(p => Path.Combine(Root(), p)).ToArray());

    [Fact]
    public void EveryModule_UsesOnlyWhatTheDesignAllows()
    {
        var violations = Map().Uses
            .SelectMany(m => m.Value.Keys.Where(to => !Allowed.GetValueOrDefault(m.Key, []).Contains(to))
                                         .Select(to => $"{m.Key} -> {to} ({m.Value[to]} files)"))
            .ToList();
        Assert.True(violations.Count == 0, "Not allowed by docs/design/modules.md:\n  " + string.Join("\n  ", violations));
    }

    [Fact]
    public void EveryModule_IsInTheDesign()
    {
        var unknown = Map().Uses.Keys.Where(m => !Allowed.ContainsKey(m)).ToList();
        Assert.True(unknown.Count == 0, "Modules missing from the design: " + string.Join(", ", unknown));
    }

    [Fact]
    public void ThereAreNoCycles()
    {
        var cycles = Map().Cycles();
        Assert.True(cycles.Count == 0, "Cycles between modules:\n  " + string.Join("\n  ", cycles));
    }
}
