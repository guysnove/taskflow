using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TaskFlow.Common;
using TaskFlow.Data;

namespace TaskFlow;

// Course 2, Chapter 29: OBSERVABILITY. Logs (Chapter 18, JSON in production), plus metrics and traces through OpenTelemetry,
// the vendor-neutral standard: the same code can send them to Prometheus, Grafana, Jaeger, Azure Monitor, Datadog…
// Where they go is configuration, not code:
//   OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317   send traces and metrics to an OpenTelemetry collector (production)
//   Telemetry__ConsoleTraces=true                     print each finished span to the console (learning, debugging)
// Without either, TaskFlow still measures; dotnet-counters can read the metrics of a running process directly.
public static class TelemetryServices
{
    public static IServiceCollection AddTaskFlowTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        string version = typeof(TelemetryServices).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
        var otel = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("taskflow", serviceVersion: version))
            .WithTracing(tracing =>
            {
                tracing.AddSource(TaskFlowTelemetry.Name)
                       .AddAspNetCoreInstrumentation(options => options.Filter = context => IsWorthTracing(context.Request.Path));
                if (configuration.GetValue<bool>("Telemetry:ConsoleTraces")) tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics => metrics
                .AddMeter(TaskFlowTelemetry.Name)
                .AddAspNetCoreInstrumentation());                       // http.server.request.duration, and more
        if (!string.IsNullOrEmpty(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])) otel.UseOtlpExporter();

        // Health: /health stays "is it alive?" (the app and its database); /health/details adds checks a person should look at.
        services.AddHealthChecks().AddCheck<OutboxHealthCheck>("email-outbox", tags: [DetailsTag]);
        return services;
    }

    public const string DetailsTag = "details";

    // Health probes and static files would be most of the traces, and tell us nothing.
    private static bool IsWorthTracing(PathString path) =>
        !path.StartsWithSegments("/health") && !path.StartsWithSegments("/lib") && !path.StartsWithSegments("/css")
        && !path.StartsWithSegments("/js") && !path.StartsWithSegments("/favicon.ico");

    // /health/details: every check, as JSON — status, a description, and the numbers (never exception messages).
    public static Task WriteDetailsAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var body = new {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => new { status = e.Value.Status.ToString(), e.Value.Description, e.Value.Data }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public static HealthCheckOptions LiveOnly => new() { Predicate = check => !check.Tags.Contains(DetailsTag) };
    public static HealthCheckOptions Details => new() { ResponseWriter = WriteDetailsAsync };
}
