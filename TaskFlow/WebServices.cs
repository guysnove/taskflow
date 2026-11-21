using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using TaskFlow.Data;
using TaskFlow.Security;
using TaskFlow.Services;

namespace TaskFlow;

// Course 2, Chapter 13: the web layer registers its own services, like the other layers do
// (pays most of TD-3: Program.cs was ~170 lines). Moved from Program.cs unchanged.
public static class WebServices
{
    public static IServiceCollection AddTaskFlowWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews();

        // Chapter 26: a JSON API next to the pages.
        // JavaScript sends the anti-forgery token in this header (forms send it as a hidden field).
        services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        // A machine-readable description of the API, at /openapi/v1.json.
        services.AddOpenApi();
        // Course 2, Chapter 15: one shape for every API error (RFC 9457). UseStatusCodePages uses it too.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            // Not every status has a built-in title (429 doesn't): use HTTP's own name for it.
            context.ProblemDetails.Title ??= Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(context.ProblemDetails.Status ?? 500);
        });

        // Chapter 25: accounts. Who is signed in is remembered in an encrypted cookie.
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "TaskFlow.SignIn";
                options.LoginPath = "/account/signin";          // where [Authorize] sends visitors
                options.AccessDeniedPath = "/account/denied";   // signed in, but not allowed
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;               // using the site renews the 14 days
            });

        // Secure by default: every page needs a signed-in user, unless it says [AllowAnonymous].
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddHttpContextAccessor();
        services.AddScoped<TaskFlow.Api.IdempotencyFilter>();     // Course 2, Chapter 15
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Chapter 27: rate limiting. Too many requests in a short time are answered with 429.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Course 2, Chapter 15: tell the client when to try again (in seconds), as HTTP says.
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };

            // Signing in and registering: 5 tries per minute from one IP address.
            // Chapter 30: the number comes from settings (default 5), so the tests can raise it.
            int signInLimit = configuration.GetValue("RateLimits:SignInPerMinute", 5);
            options.AddPolicy("signin", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = signInLimit,
                    Window = TimeSpan.FromMinutes(1),
                }));

            // The API: 20 requests per 10 seconds for each signed-in user.
            // Chapter 30: the user's id, not Identity.Name — that's the display name, and two people can share one.
            options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromSeconds(10),
                }));
        });

        // Chapter 29: behind a reverse proxy (nginx, a cloud load balancer…), the proxy talks to TaskFlow.
        // These headers tell TaskFlow the real client's address and whether the client used HTTPS.
        // Only proxies on this machine are trusted by default (127.0.0.1/8 and ::1).
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        // Chapter 29: the keys that encrypt sign-in and anti-forgery cookies.
        // Without a fixed folder, a server or container that loses its files loses the keys,
        // and every user is signed out. Set DataProtection:KeysPath in production.
        var dataProtection = services.AddDataProtection().SetApplicationName("TaskFlow");
        string? keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrEmpty(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        // A simple "am I alive?" endpoint for hosting platforms (Chapter 29).
        services.AddHealthChecks()
            .AddDbContextCheck<TaskFlowDbContext>();       // "can I reach the database?"
        return services;
    }
}
