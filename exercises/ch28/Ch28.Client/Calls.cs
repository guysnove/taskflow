using System.Net.Http.Json;

namespace Ch28.Client;

public record Reservation(string Id, string Item, int Quantity);

public static class Calls
{
    // One request, with a deadline. A timeout throws TaskCanceledException; a 5xx throws HttpRequestException.
    public static async Task<Reservation> ReserveAsync(HttpClient http, string? key, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/reservations") { Content = JsonContent.Create(new { item = "chair", quantity = 1 }) };
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        using var response = await http.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Reservation>(cts.Token))!;
    }
}

public static class Retry
{
    // Try up to `attempts` times. Wait 100 ms, 200 ms, 400 ms… (exponential backoff) plus up to 50% random jitter between
    // tries. Retry only failures that may be temporary: a timeout, a connection error, a 5xx — never a 400 (it won't change).
    public static async Task<T> WithBackoffAsync<T>(Func<Task<T>> call, int order, Action<string> log, int attempts = 4, Random? random = null)
    {
        random ??= new Random(order);                                                 // repeatable jitter for the demo
        for (int attempt = 1; ; attempt++)
        {
            try { return await call(); }
            catch (Exception e) when (attempt < attempts && IsTransient(e))
            {
                var wait = Delay(attempt, random);
                log($"order {order,2}: attempt {attempt} failed ({Describe(e)}); retrying in {wait.TotalMilliseconds:F0} ms");
                await Task.Delay(wait);
            }
        }
    }

    public static TimeSpan Delay(int failedAttempt, Random random) =>
        TimeSpan.FromMilliseconds(100 * Math.Pow(2, failedAttempt - 1) * (1 + 0.5 * random.NextDouble()));

    public static bool IsTransient(Exception e) => e switch
    {
        TaskCanceledException => true,                                                // our timeout
        HttpRequestException { StatusCode: null } => true,                            // no answer at all (connection refused…)
        HttpRequestException { StatusCode: var s } => (int)s! >= 500 || (int)s == 429,
        _ => false,
    };

    private static string Describe(Exception e) => e is TaskCanceledException ? "timeout" : e is HttpRequestException h && h.StatusCode is not null ? ((int)h.StatusCode).ToString() : e.GetType().Name;
}
