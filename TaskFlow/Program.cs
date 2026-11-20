using Microsoft.AspNetCore.Mvc.Controllers;
using TaskFlow;
using TaskFlow.Data;
using TaskFlow.Middleware;
using TaskFlow.Notifications;
using TaskFlow.Settings;

// 1. The builder: configuration, logging and the list of services.
var builder = WebApplication.CreateBuilder(args);

// Course 2, Chapter 13: check the whole service graph when the app starts, in EVERY environment.
// (By default only Development does.) A lifetime mistake now stops the app at startup,
// instead of leaking data between users in production.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Each layer registers its own classes. This file, the composition root, is the only place
// that knows every layer (Course 2, Chapters 12–13).
builder.Services.AddTaskFlowWeb(builder.Configuration);
builder.Services.AddTaskFlowApplication();
builder.Services.AddTaskFlowInfrastructure(builder.Configuration);
builder.Services.AddOptions<EmailOptions>().Bind(builder.Configuration.GetSection(EmailOptions.Section))
    .ValidateDataAnnotations().ValidateOnStart();

// The clock: ask for TimeProvider instead of calling DateTime.Today directly.
builder.Services.AddSingleton(TimeProvider.System);

// TaskFlow's settings, from the "TaskFlow" section, checked when the app starts.
builder.Services.AddOptions<TaskFlowOptions>()
    .Bind(builder.Configuration.GetSection(TaskFlowOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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
