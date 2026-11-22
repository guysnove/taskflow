// Chapter 24: the smallest web app worth a pipeline. A greeting, a version, and a health check.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();

// For the chapter's rollback demo only: FailOnStart=true makes this "release" crash at startup,
// like a release with a broken setting would.
if (app.Configuration.GetValue<bool>("FailOnStart"))
    throw new InvalidOperationException("FailOnStart is set: this release refuses to start (rollback demo).");

string version = typeof(Program).Assembly
    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion ?? "dev";
string greeting = app.Configuration["Greeting"] ?? "Hello";   // set per environment: Greeting=… or appsettings

app.MapGet("/", () => $"{greeting} from Ch24.Hello {version}");
app.MapGet("/version", () => version);
app.MapHealthChecks("/health");
app.Run();

public partial class Program;   // lets the tests start the app
