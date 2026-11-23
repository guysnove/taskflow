// Chapter 28: a small "stock" service that misbehaves ON PURPOSE, the way real services do over a network.
// Every request gets a number. Every FailEvery-th request answers 503 (doing nothing); every SlowEvery-th request
// DOES THE WORK, then takes SlowSeconds to answer — so an impatient client times out although the reservation was made.
//   dotnet run --project Ch28.Stock -- --FailEvery 4 --SlowEvery 5 --SlowSeconds 3
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5080");
var app = builder.Build();
int failEvery = app.Configuration.GetValue("FailEvery", 0), slowEvery = app.Configuration.GetValue("SlowEvery", 0);
var slow = TimeSpan.FromSeconds(app.Configuration.GetValue("SlowSeconds", 3.0));
bool down = false;

int requests = 0;
var reservations = new ConcurrentBag<Reservation>();
var answered = new ConcurrentDictionary<string, Reservation>();      // Idempotency-Key -> the answer already given

app.MapPost("/reservations", async (ReservationRequest r, HttpRequest http) =>
{
    int n = Interlocked.Increment(ref requests);
    if (down || (failEvery > 0 && n % failEvery == 0)) return Results.StatusCode(503);          // "Service Unavailable"
    string? key = http.Headers["Idempotency-Key"];
    Reservation result;
    if (key is not null)
    {
        // The same key again: the same answer, and no second reservation.
        result = answered.GetOrAdd(key, _ => Reserve(r));
    }
    else result = Reserve(r);
    if (slowEvery > 0 && n % slowEvery == 0) await Task.Delay(slow);                            // done, but slow to answer
    return Results.Ok(result);
});
app.MapGet("/reservations/count", () => new { reservations = reservations.Count, requests });
app.MapPost("/admin/reset", () => { reservations.Clear(); answered.Clear(); requests = 0; return Results.Ok(); });
app.MapPost("/admin/down", (bool on) => { down = on; return Results.Ok(new { down }); });
app.Run();

Reservation Reserve(ReservationRequest r)
{
    var reservation = new Reservation(Guid.NewGuid().ToString("N")[..8], r.Item, r.Quantity);
    reservations.Add(reservation);
    return reservation;
}

record ReservationRequest(string Item, int Quantity);
record Reservation(string Id, string Item, int Quantity);
