// Chapter 27: a page-view counter, to see what "stateless" means when there are several copies of an app.
//   Store=memory  each copy keeps its own count in its own memory (STATEFUL: copies disagree)
//   Store=redis   every copy uses one shared Redis server (STATELESS: any copy can answer any request)
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
string store = builder.Configuration["Store"] ?? "memory";
IConnectionMultiplexer? redis = store == "redis"
    ? await ConnectionMultiplexer.ConnectAsync(builder.Configuration["Redis"] ?? "localhost:6379")
    : null;
var app = builder.Build();

long local = 0;   // only this copy of the app can see this
app.MapGet("/", async () =>
{
    long count = redis is null
        ? Interlocked.Increment(ref local)
        : await redis.GetDatabase().StringIncrementAsync("views");   // atomic, shared by every copy
    return new { instance = Environment.MachineName, store, count };
});
app.MapGet("/health", () => "Healthy");
app.Run();
