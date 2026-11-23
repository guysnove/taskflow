// Chapter 28 → 29: an "orders" service that calls a "stock" service. Run both, place orders, and watch the three signals:
//   LOGS     structured JSON lines, each with the trace id;
//   METRICS  dotnet-counters monitor -n Ch29.Orders --counters Ch29.Orders
//   TRACES   printed by OpenTelemetry's console exporter: ONE trace id across BOTH services.
//   dotnet run --project Ch29.Stock      then      dotnet run --project Ch29.Orders
using System.Diagnostics;
using Ch29.Orders;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5091");
builder.Logging.ClearProviders().AddJsonConsole(o => o.IncludeScopes = true);     // structured logs, with the trace id in the scopes
builder.Logging.AddFilter("Microsoft", LogLevel.Warning).AddFilter("System.Net.Http", LogLevel.Warning);

builder.Services.AddSingleton<OrderMetrics>();
builder.Services.AddHttpClient("stock", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["StockUrl"] ?? "http://127.0.0.1:5092");
    c.Timeout = TimeSpan.FromSeconds(2);                                          // Chapter 28: always a timeout
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("orders"))
    .WithTracing(t => t.AddSource(OrderMetrics.Name)
                       .AddAspNetCoreInstrumentation()                             // a span for each request we receive
                       .AddHttpClientInstrumentation()                             // a span for each request we SEND (and the traceparent header)
                       .AddConsoleExporter());

var app = builder.Build();

app.MapPost("/orders", async (OrderRequest order, IHttpClientFactory http, OrderMetrics metrics, ILogger<Program> log) =>
{
    using var activity = OrderMetrics.Traces.StartActivity("PlaceOrder");          // our own span, inside the request's
    activity?.SetTag("order.item", order.Item);
    var started = Stopwatch.GetTimestamp();

    log.LogInformation("Placing an order for {Quantity} x {Item}", order.Quantity, order.Item);   // a TEMPLATE: Quantity and Item become fields
    var stock = await http.CreateClient("stock").GetFromJsonAsync<StockLevel>($"/stock/{order.Item}");
    if (stock!.Available < order.Quantity)
    {
        log.LogWarning("Not enough {Item}: wanted {Quantity}, {Available} available", order.Item, order.Quantity, stock.Available);
        metrics.Rejected(order.Item);
        return Results.Conflict(new { error = "not enough stock", traceId = Activity.Current?.TraceId.ToString() });
    }
    metrics.Placed(order.Item, Stopwatch.GetElapsedTime(started));
    log.LogInformation("Order placed: {Quantity} x {Item}", order.Quantity, order.Item);
    return Results.Ok(new { order.Item, order.Quantity, traceId = Activity.Current?.TraceId.ToString() });
});

app.Run();
