// Chapter 29: the "stock" service. Every 3rd request is slow (300 ms): find it in the trace.
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5092");
builder.Logging.ClearProviders().AddJsonConsole(o => o.IncludeScopes = true);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("stock"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddConsoleExporter());     // continues the caller's trace (traceparent)

var app = builder.Build();
var levels = new Dictionary<string, int> { ["chair"] = 5, ["desk"] = 0, ["lamp"] = 12 };
int requests = 0;

app.MapGet("/stock/{item}", async (string item, ILogger<Program> log) =>
{
    if (Interlocked.Increment(ref requests) % 3 == 0) await Task.Delay(300);       // a slow moment, on purpose
    int available = levels.GetValueOrDefault(item);
    log.LogInformation("Stock checked for {Item}: {Available}", item, available);
    return new { item, available };
});

app.Run();
