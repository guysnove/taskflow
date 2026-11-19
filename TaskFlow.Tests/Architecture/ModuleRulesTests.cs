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
        ["Controllers"] = ["Models", "Services", "Notifications", "Settings", "Common"],
        ["Api"] = ["Models", "Services", "Notifications", "Common"],
        ["ViewComponents"] = ["Models", "Services", "Notifications", "Settings", "Common"],
        ["TagHelpers"] = ["Models", "Common"],
        ["Middleware"] = ["Common"],
        // Application: the rules and the features.
        ["Services"] = ["Models", "Data", "Settings", "Common"],
        ["Notifications"] = ["Models", "Data", "Services", "Settings", "Common"],
        // Data access: knows the models, nothing above it.
        ["Data"] = ["Models", "Common"],
        // The core: depends on nothing.
        ["Models"] = ["Common"],
        ["Settings"] = [],
        ["Common"] = [],
    };

    private static ModuleMap Map([CallerFilePath] string here = "") =>
        ModuleMap.Read(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "TaskFlow")));

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
