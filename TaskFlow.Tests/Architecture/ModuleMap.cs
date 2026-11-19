using System.Text.RegularExpressions;

namespace TaskFlow.Architecture;

// Course 2, Chapter 11: TaskFlow's modules (its top-level folders / namespaces) and which ones
// each depends on, read from the source code's "using TaskFlow.X;" lines.
// A simple, honest approximation: fully-qualified names (TaskFlow.X.Y without a using) aren't seen.
public sealed class ModuleMap
{
    public SortedDictionary<string, SortedDictionary<string, int>> Uses { get; } = new(StringComparer.Ordinal);

    // Course 2, Chapter 12: one or more project folders; a module is a top-level folder in any of them.
    public static ModuleMap Read(params string[] projectFolders)
    {
        var map = new ModuleMap();
        foreach (string folder in projectFolders)
        {
            map.Add(folder);
        }
        return map;
    }

    private void Add(string projectFolder)
    {
        var map = this;
        foreach (string file in Directory.EnumerateFiles(projectFolder, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(projectFolder, file);
            string[] parts = rel.Split(Path.DirectorySeparatorChar);
            if (parts.Length < 2 || parts[0] is "bin" or "obj" or "Migrations")
            {
                continue;                                   // Program.cs and generated code aren't a module
            }
            string from = parts[0];
            var row = map.Uses.TryGetValue(from, out var r) ? r : map.Uses[from] = new(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"^\s*(?:global\s+)?using\s+TaskFlow\.(\w+)\s*;", RegexOptions.Multiline))
            {
                string to = m.Groups[1].Value;
                if (to != from)
                {
                    row[to] = row.GetValueOrDefault(to) + 1;
                }
            }
        }
    }

    // Every cycle, as a path: Data -> Services -> Data.
    public List<string> Cycles()
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string start in Uses.Keys)
        {
            Walk(start, [start], found);
        }
        return found.ToList();
    }

    private void Walk(string at, List<string> path, SortedSet<string> found)
    {
        if (!Uses.TryGetValue(at, out var next)) return;
        foreach (string to in next.Keys)
        {
            if (to == path[0])
            {
                // Report each cycle once, starting from its alphabetically first module.
                var cycle = path.ToList();
                int first = cycle.IndexOf(cycle.Min(StringComparer.Ordinal)!);
                var rotated = cycle.Skip(first).Concat(cycle.Take(first)).ToList();
                found.Add(string.Join(" -> ", rotated.Append(rotated[0])));
            }
            else if (!path.Contains(to) && path.Count < 8)
            {
                Walk(to, [.. path, to], found);
            }
        }
    }
}
