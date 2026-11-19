// Course 2, Chapter 11: print TaskFlow's module dependency matrix and any cycles.
//   dotnet run --project tools/Modules            (from the repository root)
using TaskFlow.Architecture;

// Course 2, Chapter 12: TaskFlow is now four projects; read them all (or the folders given).
string[] roots = args.Length > 0 ? args : ["TaskFlow", "TaskFlow.Application", "TaskFlow.Domain", "TaskFlow.Infrastructure"];
var map = ModuleMap.Read(roots.Select(Path.GetFullPath).ToArray());
var modules = map.Uses.Keys.Union(map.Uses.Values.SelectMany(v => v.Keys)).Order(StringComparer.Ordinal).ToList();

Console.WriteLine("Files in the row's module with \"using TaskFlow.<column>;\"\n");
Console.Write("".PadRight(15));
foreach (var m in modules) Console.Write(m[..Math.Min(5, m.Length)].PadLeft(6));
Console.WriteLine();
foreach (var from in modules)
{
    Console.Write(from.PadRight(15));
    foreach (var to in modules)
    {
        int n = map.Uses.TryGetValue(from, out var row) ? row.GetValueOrDefault(to) : 0;
        Console.Write((from == to ? "·" : n == 0 ? "" : n.ToString(System.Globalization.CultureInfo.InvariantCulture)).PadLeft(6));
    }
    Console.WriteLine();
}
var cycles = map.Cycles();
Console.WriteLine(cycles.Count == 0 ? "\nNo cycles." : "\nCycles:\n  " + string.Join("\n  ", cycles));
