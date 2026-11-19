// Course 2, Chapter 6: a traceability report.
// Which requirements (docs/requirements/*.md) have tests ([Trait("Req", "…")] in TaskFlow.Tests), and which don't yet?
//   dotnet run --project tools/Traceability            (from the repository root)
using System.Text.RegularExpressions;

string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

// 1. Requirements: table rows whose first cell is an ID such as FR-DUP-1 or NFR-SEC-TEAM-1.
var idPattern = new Regex(@"^\|\s*((?:FR|NFR)-[A-Z]+(?:-[A-Z]+)?-\d+)\s*\|\s*([^|]+)\|");
// An ID can appear again later (in a traceability table); the FIRST row that mentions it defines it.
var requirements = new List<(string Id, string Text, string File, bool WontDo)>();
var seen = new HashSet<string>();
foreach (string file in Directory.GetFiles(Path.Combine(root, "docs", "requirements"), "*.md").Order())
    foreach (string line in File.ReadLines(file))
    {
        Match m = idPattern.Match(line);
        if (m.Success && seen.Add(m.Groups[1].Value))
            requirements.Add((m.Groups[1].Value, m.Groups[2].Value.Trim(), Path.GetFileName(file), line.Contains("| Won't")));
    }

// 2. Tests: each [Trait("Req", "X")] applies to the class or method declared just below it.
var traitPattern = new Regex(@"\[Trait\(""Req"",\s*""([^""]+)""\)\]");
var declPattern = new Regex(@"(?:class\s+(\w+))|(?:(?:Task|void)\s+(\w+)\s*\()");
var testsFor = new Dictionary<string, List<string>>();
var classesWithAnyTrait = new HashSet<string>();
var allTestClasses = new HashSet<string>();
foreach (string file in Directory.GetFiles(Path.Combine(root, "TaskFlow.Tests"), "*.cs", SearchOption.AllDirectories))
{
    if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
    string[] lines = File.ReadAllLines(file);
    string? currentClass = null;
    for (int i = 0; i < lines.Length; i++)
    {
        Match cls = Regex.Match(lines[i], @"public (?:sealed )?class (\w+)");
        if (cls.Success) currentClass = cls.Groups[1].Value;
        if (lines[i].Contains("[Fact") || lines[i].Contains("[Theory")) allTestClasses.Add(currentClass ?? file);
        Match trait = traitPattern.Match(lines[i]);
        if (!trait.Success) continue;
        for (int j = i + 1; j < Math.Min(i + 6, lines.Length); j++)
        {
            Match decl = declPattern.Match(lines[j]);
            if (!decl.Success) continue;
            string name = decl.Groups[1].Success ? decl.Groups[1].Value + " (all tests)" : $"{currentClass}.{decl.Groups[2].Value}";
            classesWithAnyTrait.Add(decl.Groups[1].Success ? decl.Groups[1].Value : currentClass ?? "");
            if (!testsFor.TryGetValue(trait.Groups[1].Value, out var list)) testsFor[trait.Groups[1].Value] = list = [];
            list.Add(name);
            break;
        }
    }
}

// 3. The report.
int covered = 0, wont = 0;
foreach (var group in requirements.GroupBy(r => r.File))
{
    Console.WriteLine($"{group.Key}");
    foreach (var (id, text, _, wontDo) in group)
    {
        bool has = testsFor.TryGetValue(id, out var tests);
        if (has) covered++;
        if (wontDo) wont++;
        string status = has ? string.Join(", ", tests!) : wontDo ? "out of scope (Won't)" : "no tests yet";
        Console.WriteLine($"  {(has ? "✓" : wontDo ? "–" : "·")} {id,-15} {status}");
    }
}
Console.WriteLine();
Console.WriteLine($"{covered} of {requirements.Count - wont} requirements in scope have at least one test ({wont} out of scope).");
var unknown = testsFor.Keys.Except(requirements.Select(r => r.Id)).ToList();
if (unknown.Count > 0) Console.WriteLine($"Tests point at requirements that don't exist: {string.Join(", ", unknown)}");
var orphans = allTestClasses.Except(classesWithAnyTrait).Order().ToList();
if (orphans.Count > 0) Console.WriteLine($"Test classes not linked to any requirement: {string.Join(", ", orphans)}");
