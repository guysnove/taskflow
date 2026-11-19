namespace Ch07.Digest.Before;

// Independent exercise B: a colleague's CSV importer for tasks. It works — mostly.
// Format: title,priority,due,steps   (due: yyyy-MM-dd or empty; steps: "a|b|c" or empty)
// The first line is a header. Bad lines are skipped and counted.
public class CsvImport
{
    public int Bad;

    public List<(string, string, DateOnly?, List<string>)> Parse(string text)
    {
        var r = new List<(string, string, DateOnly?, List<string>)>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        Bad = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (i == 0) continue;
            var x = lines[i];
            if (x.Trim() == "") continue;
            var p = x.Split(',');
            if (p.Length != 4)
            {
                Bad++;
                continue;
            }
            var t = p[0].Trim();
            if (t.Length < 3 || t.Length > 80)
            {
                Bad++;
                continue;
            }
            string pr;
            if (p[1].Trim().ToLower() == "high") pr = "High";
            else if (p[1].Trim() == "Medium") pr = "Medium";
            else pr = "Low";
            DateOnly? d = null;
            if (p[2].Trim() != "")
            {
                DateOnly dd;
                if (DateOnly.TryParseExact(p[2].Trim(), "yyyy-MM-dd", out dd))
                {
                    d = dd;
                }
                else
                {
                    Bad++;
                    continue;
                }
            }
            var s = new List<string>();
            if (p[3].Trim() != "")
            {
                foreach (var q in p[3].Split('|'))
                {
                    if (q.Trim() != "") s.Add(q.Trim());
                }
            }
            r.Add((t, pr, d, s));
        }
        return r;
    }
}
