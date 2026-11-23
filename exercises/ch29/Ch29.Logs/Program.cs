// Chapter 29: the same events, logged three ways. Run it and compare what a program could search for.
//   dotnet run --project Ch29.Logs
using Microsoft.Extensions.Logging;

string user = "ana@example.com"; int orderId = 1042; decimal total = 59.90m;

Console.WriteLine("== 1. Console.WriteLine: text for a human, nothing for a machine");
Console.WriteLine($"Order {orderId} for {user} failed: payment declined ({total} EUR)");

using (var plain = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information)))
{
    Console.WriteLine("\n== 2. ILogger, simple console: a level and a category, still one string");
    var log = plain.CreateLogger("Checkout");
    log.LogWarning("Order {OrderId} failed: payment declined ({Total} EUR)", orderId, total);
    log.LogDebug("Card checks: {Checks}", "cvc, expiry");                 // Debug: below the minimum level, not written at all
}

using var json = LoggerFactory.Create(b => b.AddJsonConsole(o => { o.IncludeScopes = true; o.JsonWriterOptions = new() { Indented = false }; })
                                             .SetMinimumLevel(LogLevel.Information));
var jlog = json.CreateLogger("Checkout");
Console.WriteLine("\n== 3a. Structured (JSON), with string interpolation — DON'T: the values are lost inside the message");
jlog.LogWarning($"Order {orderId} failed: payment declined ({total} EUR)");
Console.WriteLine("\n== 3b. Structured (JSON), with a message TEMPLATE and a scope — DO: OrderId and Total are fields");
using (jlog.BeginScope(new Dictionary<string, object> { ["UserId"] = 7 }))  // an id, not the email address: no personal data in logs
{
    jlog.LogWarning("Order {OrderId} failed: payment declined ({Total} EUR)", orderId, total);
}
