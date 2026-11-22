using TaskFlow.Tools.DocsCheck;

// Course 2, Chapter 23: dotnet run --project tools/DocsCheck -- .
// Checks every relative link in every Markdown file under <folder>. Exit code 1 if any is broken.
string root = args.Length > 0 ? args[0] : ".";
var problems = Links.Check(root).ToList();
foreach (var p in problems) Console.WriteLine(p);
Console.WriteLine(problems.Count == 0 ? "All relative links OK." : $"{problems.Count} broken link(s).");
return problems.Count == 0 ? 0 : 1;
