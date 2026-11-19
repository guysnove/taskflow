namespace Ch07.Digest.Before;

// A colleague's prototype of the "daily summary" email (FR-NOTIF-5). It works.
// Chapter 7 refactors it — without changing what it produces.
public class DigestService
{
    public string Make(List<TaskInfo> l, int uid, DateTime d, bool b)
    {
        string s = "";
        var l2 = new List<TaskInfo>();
        foreach (var t in l)
        {
            if (t.UserId == uid)
            {
                if (t.Done == false || b == true)
                {
                    l2.Add(t);
                }
            }
        }
        if (l2.Count == 0)
        {
            return "Nothing to do. Enjoy your day!";
        }
        // sort by priority
        l2.Sort((x, y) =>
        {
            int a = x.Priority == "High" ? 0 : x.Priority == "Medium" ? 1 : 2;
            int c = y.Priority == "High" ? 0 : y.Priority == "Medium" ? 1 : 2;
            if (a != c) return a - c;
            return string.Compare(x.Title, y.Title);
        });
        s = s + "Good morning! Here is your TaskFlow summary for " + d.ToString("dddd d MMMM") + ".\n";
        var od = new List<TaskInfo>();
        foreach (var t in l2)
        {
            if (t.Due != null && !t.Done)
            {
                if (t.Due.Value < DateOnly.FromDateTime(d))
                {
                    od.Add(t);
                }
            }
        }
        if (od.Count > 0)
        {
            s = s + "\nOVERDUE (" + od.Count + ")\n";
            foreach (var t in od)
            {
                var days = DateOnly.FromDateTime(d).DayNumber - t.Due!.Value.DayNumber;
                s = s + "  ! " + t.Title + " (" + t.Priority.ToLower() + ", " + days + (days == 1 ? " day" : " days") + " late)";
                if (t.StepsTotal > 0)
                {
                    s = s + " [" + t.StepsDone + "/" + t.StepsTotal + "]";
                }
                s = s + "\n";
            }
        }
        var ds = new List<TaskInfo>();
        foreach (var t in l2)
        {
            if (t.Due != null && !t.Done)
            {
                if (t.Due.Value >= DateOnly.FromDateTime(d) && t.Due.Value <= DateOnly.FromDateTime(DateTime.Now).AddDays(2))
                {
                    ds.Add(t);
                }
            }
        }
        if (ds.Count > 0)
        {
            s = s + "\nDUE SOON (" + ds.Count + ")\n";
            foreach (var t in ds)
            {
                var days = t.Due!.Value.DayNumber - DateOnly.FromDateTime(d).DayNumber;
                s = s + "  - " + t.Title + " (" + t.Priority.ToLower() + ", " + (days == 0 ? "today" : days == 1 ? "tomorrow" : "in " + days + " days") + ")";
                if (t.StepsTotal > 0)
                {
                    s = s + " [" + t.StepsDone + "/" + t.StepsTotal + "]";
                }
                s = s + "\n";
            }
        }
        int n = 0;
        foreach (var t in l2)
        {
            if (!od.Contains(t) && !ds.Contains(t) && !t.Done) n++;
        }
        if (n > 0)
        {
            s = s + "\n" + n + " other open " + (n == 1 ? "task" : "tasks") + ".\n";
        }
        if (b)
        {
            int dn = 0;
            foreach (var t in l2) if (t.Done) dn++;
            if (dn > 0) s = s + dn + " done.\n";
        }
        return s;
    }
}
