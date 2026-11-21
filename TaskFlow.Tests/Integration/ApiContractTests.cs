using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Tests.Integration;

// Course 2, Chapter 15: the API's contract, as programs see it. The OpenAPI document is turned into a
// short, readable summary — every endpoint, its parameters, body and responses, and every schema —
// and compared with Approved/ApiContract.approved.txt. Any change fails the test until a person approves it:
// additions are usually safe; removals and renames break the programs that use the API.
public class ApiContractTests : IClassFixture<TaskFlowFactory>
{
    private readonly TaskFlowFactory _factory;

    public ApiContractTests(TaskFlowFactory factory) => _factory = factory;

    [Fact]
    [Trait("Req", "NFR-API-1")]
    public async Task TheApiContract_ChangesOnlyWhenApproved()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var document = JsonNode.Parse(await client.GetStringAsync("/openapi/v1.json"))!;

        Approve(Summarise(document));
    }

    private static string Summarise(JsonNode document)
    {
        var text = new StringBuilder();
        foreach (var (path, item) in document["paths"]!.AsObject().Where(p => p.Key.StartsWith("/api/", StringComparison.Ordinal)).OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            foreach (var (method, operation) in item!.AsObject())
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"{method.ToUpperInvariant()} {path}");
                foreach (var parameter in operation!["parameters"]?.AsArray() ?? new JsonArray())
                {
                    string required = parameter!["required"]?.GetValue<bool>() == true ? "" : " (optional)";
                    text.AppendLine(CultureInfo.InvariantCulture, $"  {parameter["in"]} {parameter["name"]}: {TypeOf(parameter["schema"])}{required}");
                }
                if (operation["requestBody"]?["content"]?["application/json"]?["schema"] is JsonNode body)
                {
                    text.AppendLine(CultureInfo.InvariantCulture, $"  body: {TypeOf(body)}");
                }
                foreach (var (code, response) in operation["responses"]!.AsObject())
                {
                    var schema = response!["content"]?.AsObject().FirstOrDefault().Value?["schema"];
                    text.AppendLine(CultureInfo.InvariantCulture, $"  {code}{(schema is null ? "" : " " + TypeOf(schema))}");
                }
            }
        }
        foreach (var (name, schema) in (document["components"]?["schemas"]?.AsObject() ?? new JsonObject()).OrderBy(s => s.Key, StringComparer.Ordinal))
        {
            var required = schema!["required"]?.AsArray().Select(r => r!.GetValue<string>()).ToHashSet() ?? [];
            text.AppendLine(CultureInfo.InvariantCulture, $"schema {name}");
            foreach (var (property, type) in schema["properties"]?.AsObject() ?? new JsonObject())
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"  {property}: {TypeOf(type)}{(required.Contains(property) ? "" : " (optional)")}");
            }
        }
        return text.ToString();
    }

    // "TaskDto", "TaskDto[]", "int32?", "string", …  ("?" = may be null)
    private static string TypeOf(JsonNode? schema)
    {
        if (schema is null) return "?";
        if (schema["$ref"] is JsonNode reference) return reference.GetValue<string>().Split('/')[^1];
        string nullable = schema["type"] is JsonArray types && types.Any(t => t!.GetValue<string>() == "null") ? "?" : "";
        if (schema["items"] is JsonNode items) return TypeOf(items) + "[]" + nullable;
        string type = schema["format"]?.GetValue<string>()
                      ?? (schema["type"] is JsonArray all ? all.Select(t => t!.GetValue<string>()).First(t => t != "null") : schema["type"]?.GetValue<string>())
                      ?? "object";
        return type + nullable;
    }

    private static void Approve(string received, [CallerFilePath] string here = "")
    {
        string folder = Path.Combine(Path.GetDirectoryName(here)!, "Approved");
        string approvedFile = Path.Combine(folder, "ApiContract.approved.txt");
        string receivedFile = Path.Combine(folder, "ApiContract.received.txt");
        string approved = File.Exists(approvedFile) ? File.ReadAllText(approvedFile) : "";
        if (received == approved)
        {
            File.Delete(receivedFile);
            return;
        }
        File.WriteAllText(receivedFile, received);
        Assert.Fail($"The API contract changed (or was never approved). Compare {Path.GetFileName(receivedFile)} with {Path.GetFileName(approvedFile)}.");
    }
}
