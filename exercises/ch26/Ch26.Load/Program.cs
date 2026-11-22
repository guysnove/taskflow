// Chapter 26: a small load tester. It sends GET requests from N simulated users for S seconds, after a warm-up,
// and reports throughput (requests per second) and latency percentiles. Optional: sign in to TaskFlow first.
//   dotnet run -c Release --project Ch26.Load -- http://localhost:5050/tasks --users 10 --seconds 15 --email ana@example.com --password …
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

var opt = Options.Parse(args);
var cookies = new CookieContainer();
using var http = new HttpClient(new SocketsHttpHandler { CookieContainer = cookies, MaxConnectionsPerServer = 1000, AllowAutoRedirect = false })
{ Timeout = TimeSpan.FromSeconds(30) };
var target = new Uri(opt.Url);
if (opt.Email is not null) await SignIn.As(http, new Uri(target, "/account/signin"), opt.Email, opt.Password ?? "");

// Warm-up: the first requests pay for JIT compilation, connection setup and empty caches. Don't measure them.
var warm = await Run(TimeSpan.FromSeconds(opt.Warmup));
var result = await Run(TimeSpan.FromSeconds(opt.Seconds));
result.Print(opt, warm.Count);

async Task<Measured> Run(TimeSpan duration)
{
    var times = new List<double>[opt.Users];
    int errors = 0; var codes = new Dictionary<int, int>();
    var stop = Stopwatch.StartNew();
    var users = Enumerable.Range(0, opt.Users).Select(async u =>
    {
        times[u] = new List<double>();
        while (stop.Elapsed < duration)
        {
            long start = Stopwatch.GetTimestamp();
            try
            {
                using var response = await http.GetAsync(target, HttpCompletionOption.ResponseContentRead);
                times[u].Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                lock (codes) codes[(int)response.StatusCode] = codes.GetValueOrDefault((int)response.StatusCode) + 1;
                if (!response.IsSuccessStatusCode) Interlocked.Increment(ref errors);
            }
            catch (Exception) { Interlocked.Increment(ref errors); }
        }
    });
    await Task.WhenAll(users);
    return new Measured(times.SelectMany(t => t).Order().ToArray(), errors, codes, stop.Elapsed);
}

record Measured(double[] Ms, int Errors, Dictionary<int, int> Codes, TimeSpan Elapsed)
{
    public int Count => Ms.Length;
    double P(double p) => Stats.Percentile(Ms, p);
    public void Print(Options o, int warmupRequests)
    {
        Console.WriteLine($"GET {new Uri(o.Url).PathAndQuery} · {o.Users} users · {o.Seconds} s (after {o.Warmup} s warm-up: {warmupRequests} requests)");
        Console.WriteLine($"requests: {Count}  ({Count / Elapsed.TotalSeconds:F1}/s)   status: {string.Join(", ", Codes.OrderBy(c => c.Key).Select(c => $"{c.Key}×{c.Value}"))}   errors: {Errors}");
        Console.WriteLine($"latency ms: p50 {P(50):F1}   p90 {P(90):F1}   p99 {P(99):F1}   max {(Count > 0 ? Ms[^1] : 0):F1}");
    }
}

record Options(string Url, int Users, int Seconds, int Warmup, string? Email, string? Password)
{
    public static Options Parse(string[] a)
    {
        string? Get(string name) { int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        if (a.Length == 0 || a[0].StartsWith("--")) throw new ArgumentException("usage: Ch26.Load <url> [--users 10] [--seconds 10] [--warmup 2] [--email e --password p]");
        return new(a[0], int.Parse(Get("--users") ?? "10"), int.Parse(Get("--seconds") ?? "10"), int.Parse(Get("--warmup") ?? "2"), Get("--email"), Get("--password"));
    }
}

static partial class SignIn
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex Token();

    public static async Task As(HttpClient http, Uri signIn, string email, string password)
    {
        string page = await http.GetStringAsync(signIn);
        string token = Token().Match(page).Groups[1].Value;
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = email, ["Password"] = password, ["__RequestVerificationToken"] = token });
        using var response = await http.PostAsync(signIn, form);
        if (response.StatusCode != HttpStatusCode.Redirect) throw new InvalidOperationException($"Sign-in failed: {(int)response.StatusCode}");
    }
}

/// <summary>Percentiles by the "nearest rank" method: p90 is the value 90% of the measurements are at or below.</summary>
public static class Stats
{
    public static double Percentile(double[] sorted, double p)
    {
        if (sorted.Length == 0) return 0;
        int rank = (int)Math.Ceiling(p / 100 * sorted.Length);          // 1-based
        return sorted[Math.Clamp(rank, 1, sorted.Length) - 1];
    }
}
