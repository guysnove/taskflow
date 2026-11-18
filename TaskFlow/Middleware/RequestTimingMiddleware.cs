using System.Diagnostics;

namespace TaskFlow.Middleware;

// Logs every request: method, path, status code and the time it took.
// (Chapter 18 wrote this inline in Program.cs; here it's a class of its own.)
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;                        // the rest of the pipeline
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        await _next(context);                                       // everything after this middleware
        _logger.LogInformation("{Method} {Path}{Query} -> {Status} in {Ms} ms",
            context.Request.Method, context.Request.Path, context.Request.QueryString,
            context.Response.StatusCode, timer.ElapsedMilliseconds);
    }
}
