namespace TaskFlow.Middleware;

// Chapter 27: security headers on every response. Each one tells the browser to be stricter.
public class SecurityHeadersMiddleware
{
    // Content-Security-Policy: where this page may load things from. 'self' = TaskFlow's own address.
    // No inline <script>, no onclick="…", no scripts from other sites.
    public const string Policy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] = Policy;
        headers["X-Content-Type-Options"] = "nosniff";                    // don't guess file types
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";   // don't leak full addresses to other sites
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";  // TaskFlow needs none of these
        return _next(context);
    }
}
