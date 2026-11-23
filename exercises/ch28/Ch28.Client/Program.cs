// Chapter 28: calling a service that fails sometimes. Each scenario places 10 orders (10 reservations), then asks the
// service how many reservations it really made.
//   dotnet run --project Ch28.Client -- naive | timeout | retry | idempotent
using System.Diagnostics;
using System.Net.Http.Json;
using Ch28.Client;

string scenario = args.FirstOrDefault() ?? "naive";
using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5080") };
await http.PostAsync("/admin/reset", null);
var log = (string s) => Console.WriteLine(s);
var clock = Stopwatch.StartNew();
int ok = 0, failed = 0;

for (int order = 1; order <= 10; order++)
{
    string key = $"order-{order}";                                 // one key per ORDER, reused by every retry of it
    try
    {
        var reservation = scenario switch
        {
            "naive" => await Calls.ReserveAsync(http, key: null, timeout: Timeout.InfiniteTimeSpan),
            "timeout" => await Calls.ReserveAsync(http, key: null, timeout: TimeSpan.FromSeconds(1)),
            "retry" => await Retry.WithBackoffAsync(() => Calls.ReserveAsync(http, null, TimeSpan.FromSeconds(1)), order, log),
            "idempotent" => await Retry.WithBackoffAsync(() => Calls.ReserveAsync(http, key, TimeSpan.FromSeconds(1)), order, log),
            _ => throw new ArgumentException("naive | timeout | retry | idempotent"),
        };
        ok++;
        Console.WriteLine($"order {order,2}: reserved {reservation.Id}");
    }
    catch (Exception e)
    {
        failed++;
        Console.WriteLine($"order {order,2}: FAILED ({e.GetType().Name}: {e.Message.Split('.')[0]})");
    }
}
var count = await http.GetFromJsonAsync<ServiceCount>("/reservations/count");
Console.WriteLine($"\n{scenario}: {ok} orders succeeded, {failed} failed, in {clock.Elapsed.TotalSeconds:F1} s. " +
                  $"The service made {count!.Reservations} reservations from {count.Requests} requests.");

record ServiceCount(int Reservations, int Requests);
