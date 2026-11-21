using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Api;

// Course 2, Chapter 15: makes a POST safe to repeat. Put [ServiceFilter<IdempotencyFilter>] on an action.
//   no Idempotency-Key header      → runs as usual
//   a new key                      → runs; a 201 answer is saved with the key (in the same transaction)
//   a key seen before, same body   → the saved answer again, with Idempotent-Replayed: true; nothing runs
//   a key seen before, other body  → 422: the client reused a key for a different request
public sealed class IdempotencyFilter(IIdempotencyStore store) : IAsyncActionFilter
{
    public const string Header = "Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string? key = context.HttpContext.Request.Headers[Header];
        if (string.IsNullOrEmpty(key))
        {
            await next();
            return;
        }
        if (key.Length > 100)
        {
            context.Result = Problem(context.HttpContext, StatusCodes.Status400BadRequest,
                                     "The Idempotency-Key header is too long (100 characters at most).");
            return;
        }

        // What the request asked for, after model binding: the same key must always mean the same request.
        string request = context.HttpContext.Request.Path + " " + JsonSerializer.Serialize(context.ActionArguments, JsonSerializerOptions.Web);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request)));

        await using var scope = await store.BeginAsync();
        var saved = await scope.FindAsync(key);
        if (saved is not null)
        {
            context.Result = saved.RequestHash != hash
                ? Problem(context.HttpContext, StatusCodes.Status422UnprocessableEntity,
                          "This Idempotency-Key was already used for a different request.")
                : Replay(context.HttpContext, saved);
            return;                                            // the action doesn't run again
        }

        var executed = await next();
        if (executed is { Exception: null, Result: ObjectResult { StatusCode: StatusCodes.Status201Created } created })
        {
            string? location = created is CreatedAtActionResult at
                ? context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>()
                      .GetUriByAction(context.HttpContext, at.ActionName, at.ControllerName, at.RouteValues)
                : null;
            await scope.SaveAndCommitAsync(new IdempotencyRecord
            {
                Key = key, RequestHash = hash, StatusCode = StatusCodes.Status201Created, Location = location,
                Body = JsonSerializer.Serialize(created.Value, JsonSerializerOptions.Web),
            });
        }
    }

    private static ContentResult Replay(HttpContext http, IdempotencyRecord saved)
    {
        http.Response.Headers["Idempotent-Replayed"] = "true";
        if (saved.Location is not null) http.Response.Headers.Location = saved.Location;
        return new ContentResult
        {
            StatusCode = saved.StatusCode, Content = saved.Body, ContentType = "application/json; charset=utf-8",
        };
    }

    // The same shape as every other API error (type, title, status, traceId).
    private static ObjectResult Problem(HttpContext http, int status, string title) =>
        new(http.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory>()
                .CreateProblemDetails(http, status, title))
        { StatusCode = status, ContentTypes = { "application/problem+json" } };
}
