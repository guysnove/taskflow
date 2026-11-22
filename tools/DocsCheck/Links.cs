using System.Text;
using System.Text.RegularExpressions;

namespace TaskFlow.Tools.DocsCheck;

// Chapter 23: documentation rots quietly. This finds one kind of rot automatically: links inside the
// repository's Markdown that point at a file, or a heading, that no longer exists.

public record Problem(string File, int Line, string Target, string Reason)
{
    public override string ToString() => $"{File}:{Line}: {Target} — {Reason}";
}

public static partial class Links
{
    [GeneratedRegex(@"(?<!!)\[[^\]]*\]\(([^)\s]+)(?:\s+""[^""]*"")?\)")]   // [text](target "title"), not images
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$")]
    private static partial Regex Heading();

    // GitHub's heading anchors: lower case; drop punctuation except '-' and '_'; spaces become '-'.
    public static string Slug(string heading)
    {
        var text = Regex.Replace(heading, @"`|\*\*|\*|_(?=\w)|(?<=\w)_", "");      // inline code/emphasis markers
        text = Regex.Replace(text, @"\[([^\]]*)\]\([^)]*\)", "$1");                // a link inside a heading: its text
        var sb = new StringBuilder();
        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_') sb.Append(c);
            else if (c == ' ') sb.Append('-');
        }
        return sb.ToString();
    }

    public static HashSet<string> Anchors(string markdown)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var seen = new Dictionary<string, int>();
        bool inFence = false;
        foreach (var line in markdown.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { inFence = !inFence; continue; }
            if (inFence) continue;
            var m = Heading().Match(line);
            if (!m.Success) continue;
            var slug = Slug(m.Groups[1].Value);
            int n = seen.GetValueOrDefault(slug);
            anchors.Add(n == 0 ? slug : $"{slug}-{n}");                                  // duplicates: -1, -2, …
            seen[slug] = n + 1;
        }
        return anchors;
    }

    public static IEnumerable<Problem> Check(string root)
    {
        root = Path.GetFullPath(root);
        foreach (var file in MarkdownFiles(root))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            var lines = File.ReadAllLines(file);
            bool inFence = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal)) { inFence = !inFence; continue; }
                if (inFence) continue;
                foreach (Match m in MarkdownLink().Matches(StripInlineCode(lines[i])))
                {
                    string target = m.Groups[1].Value;
                    if (IsExternal(target)) continue;
                    var problem = CheckOne(root, file, target);
                    if (problem is not null) yield return new Problem(rel, i + 1, target, problem);
                }
            }
        }
    }

    private static string? CheckOne(string root, string file, string target)
    {
        string path = target, anchor = "";
        int hash = target.IndexOf('#');
        if (hash >= 0) { path = target[..hash]; anchor = target[(hash + 1)..]; }
        string full = path.Length == 0 ? file
            : path.StartsWith('/') ? Path.Combine(root, path.TrimStart('/'))
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, Uri.UnescapeDataString(path)));
        if (!File.Exists(full) && !Directory.Exists(full)) return "no such file";
        if (anchor.Length > 0 && full.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && File.Exists(full)
            && !Anchors(File.ReadAllText(full)).Contains(anchor))
            return $"no heading #{anchor} in {Path.GetFileName(full)}";
        return null;
    }

    private static bool IsExternal(string t) => t.Contains("://", StringComparison.Ordinal) || t.StartsWith("mailto:", StringComparison.Ordinal);
    private static string StripInlineCode(string line) => Regex.Replace(line, "`[^`]*`", "");

    // A folder below the root with a ".docscheck-ignore" file is skipped, with everything under it
    // (e.g. exercises/ch23/sample, which is broken on purpose).
    private static IEnumerable<string> MarkdownFiles(string root) =>
        Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "node_modules" or ".git" or "TestResults" or "StrykerOutput"))
            .Where(f => !IsIgnored(root, Path.GetDirectoryName(f)!))
            .Order(StringComparer.Ordinal);

    private static bool IsIgnored(string root, string dir)
    {
        for (var d = new DirectoryInfo(dir); d is not null && d.FullName.Length > root.Length; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, ".docscheck-ignore"))) return true;
        }
        return false;
    }
}
