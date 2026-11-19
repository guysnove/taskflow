using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Middleware;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.Settings;

// 1. The builder: configuration, logging and the list of services.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Chapter 26: a JSON API next to the pages.
// JavaScript sends the anti-forgery token in this header (forms send it as a hidden field).
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
// A machine-readable description of the API, at /openapi/v1.json.
builder.Services.AddOpenApi();

// The database: SQLite, in the file named by ConnectionStrings:TaskFlow.
builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskFlow")));

// Storage: since Chapter 23, the database. Scoped, like the DbContext it uses.
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
builder.Services.AddScoped<IProjectRepository, EfProjectRepository>();   // Chapter 24
builder.Services.AddScoped<IStepRepository, EfStepRepository>();         // Chapter 30
builder.Services.AddScoped<IDependencyRepository, EfDependencyRepository>();   // Course 2, Chapter 4

// Course 2, Chapter 8: the rules for a new task, checked in this order.
// A new rule: write the class, add one line here. Nothing else changes.
builder.Services.AddScoped<INewTaskRule, DueNotInPastRule>();
builder.Services.AddScoped<INewTaskRule, DueWithinFiveYearsRule>();   // FR-TASK-3
builder.Services.AddScoped<INewTaskRule, ProjectExistsRule>();
builder.Services.AddScoped<INewTaskRule, DuplicateTitleRule>();
builder.Services.AddScoped<NewTaskChecker>();
builder.Services.AddScoped<ProjectExistsRule>();   // Course 2, Chapter 9: the edit form uses it on its own

// Chapter 25: accounts. Who is signed in is remembered in an encrypted cookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TaskFlow.SignIn";
        options.LoginPath = "/account/signin";          // where [Authorize] sends visitors
        options.AccessDeniedPath = "/account/denied";   // signed in, but not allowed
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;               // using the site renews the 14 days
    });

// Secure by default: every page needs a signed-in user, unless it says [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AccountService>();

// Chapter 27: rate limiting. Too many requests in a short time are answered with 429.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Signing in and registering: 5 tries per minute from one IP address.
    // Chapter 30: the number comes from settings (default 5), so the tests can raise it.
    int signInLimit = builder.Configuration.GetValue("RateLimits:SignInPerMinute", 5);
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

// The clock: ask for TimeProvider instead of calling DateTime.Today directly.
builder.Services.AddSingleton(TimeProvider.System);

// TaskFlow's settings, from the "TaskFlow" section, checked when the app starts.
builder.Services.AddOptions<TaskFlowOptions>()
    .Bind(builder.Configuration.GetSection(TaskFlowOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Chapter 29: behind a reverse proxy (nginx, a cloud load balancer…), the proxy talks to TaskFlow.
// These headers tell TaskFlow the real client's address and whether the client used HTTPS.
// Only proxies on this machine are trusted by default (127.0.0.1/8 and ::1).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Chapter 29: the keys that encrypt sign-in and anti-forgery cookies.
// Without a fixed folder, a server or container that loses its files loses the keys,
// and every user is signed out. Set DataProtection:KeysPath in production.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("TaskFlow");
string? keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// A simple "am I alive?" endpoint for hosting platforms (Chapter 29).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskFlowDbContext>();       // "can I reach the database?"

// 2. Build the app: after this line, the list of services is fixed.
var app = builder.Build();

// Before the first request: create or update the database, and add sample tasks if empty.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
    var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    await SampleData.PrepareAsync(db, time, addSamples: app.Environment.IsDevelopment());   // Chapter 29: samples only in Development
}

// 3. The pipeline: middleware, in order.
app.UseForwardedHeaders();   // Chapter 29: first, so everything after sees the real client and scheme
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();   // Chapter 27: CSP, nosniff, Referrer-Policy…

// Error status codes without a body (like NotFound()) get a friendly page.
app.UseStatusCodePagesWithReExecute("/status/{0}");

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();   // reads the cookie: who is this?        (Chapter 25)
app.UseAuthorization();    // may they use the chosen endpoint?
app.UseRateLimiter();      // Chapter 27: after routing, so each endpoint's policy is known

// 4. The endpoints: what can answer a request.
// CSS, JavaScript and images: everyone may download them, or the sign-in page has no styles.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHealthChecks("/health").AllowAnonymous();   // hosting platforms don't sign in

// Development only: list the routes of every controller action.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();             // Chapter 26: /openapi/v1.json
    app.MapGet("/debug/routes", (EndpointDataSource source) =>
        string.Join("\n", source.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .Select(e => e.RoutePattern.RawText)
            .Distinct()
            .Order()));
}

// 5. Start listening for requests.
app.Run();
