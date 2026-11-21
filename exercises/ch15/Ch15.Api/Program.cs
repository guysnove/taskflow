using System.Collections.Concurrent;
using Ch15.Api;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Library>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();          // every error: application/problem+json (RFC 9457)
builder.Services.AddOpenApi();                 // the contract, at /openapi/v1.json
var app = builder.Build();

app.UseStatusCodePages();                      // even "no such address" gets a problem document
app.MapOpenApi();

// ---------------------------------------------------------------- how NOT to do it
// Verbs in the address, always 200, errors hidden in the body, the database class sent as it is.
var bad = app.MapGroup("/bad");
bad.MapGet("/getBook", (int id, Library library) =>
    library.Get(id) is Book book ? Results.Ok(book) : Results.Ok(new { error = "not found" }));
bad.MapPost("/deleteBook", (int id, Library library) => Results.Ok(new { success = library.Remove(id) }));

// ---------------------------------------------------------------- version 1
var v1 = app.MapGroup("/v1/books").WithTags("Books v1");

// GET /v1/books  (version 1 has no paging: every book. Too late to change that now — see v2.)
v1.MapGet("/", (Library library) => library.Find(null, null, 1, int.MaxValue).Items.Select(BookV1.From));

// GET /v1/books/3  → 200, or 404 with a problem document
v1.MapGet("/{id:int}", Results<Ok<BookV1>, NotFound> (int id, Library library) =>
    library.Get(id) is Book book ? TypedResults.Ok(BookV1.From(book)) : TypedResults.NotFound())
  .WithName("GetBookV1");

// POST /v1/books  → 201 Created + Location, or 400 with every problem at once
v1.MapPost("/", Results<Created<BookV1>, ValidationProblem> (NewBook input, Library library) =>
{
    var errors = Validate(input);
    if (errors.Count > 0)
    {
        return TypedResults.ValidationProblem(errors);
    }
    var book = library.Add(input.Title!.Trim(), input.Authors!, input.Year!.Value, input.Copies ?? 1);
    return TypedResults.Created($"/v1/books/{book.Id}", BookV1.From(book));
});

// DELETE /v1/books/3  → 204; again → 404
v1.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, Library library) =>
    library.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound());

// ---------------------------------------------------------------- version 2: authors as a list, and paging
var v2 = app.MapGroup("/v2/books").WithTags("Books v2");

// GET /v2/books?author=martin&available=true&page=1&pageSize=3
v2.MapGet("/", Results<Ok<PageOf<BookV2>>, ValidationProblem> (
    string? author, bool? available, int? page, int? pageSize, Library library) =>
{
    int p = page ?? 1, size = pageSize ?? 10;
    if (p < 1 || size is < 1 or > 50)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["pageSize"] = ["page starts at 1; pageSize is 1 to 50."],
        });
    }
    var (items, total) = library.Find(author, available, p, size);
    string? next = p * size < total ? NextPage(author, available, p + 1, size) : null;   // null: the last page
    return TypedResults.Ok(new PageOf<BookV2>(items.Select(BookV2.From).ToList(), p, size, total, next));
});

v2.MapGet("/{id:int}", Results<Ok<BookV2>, NotFound> (int id, Library library) =>
    library.Get(id) is Book book ? TypedResults.Ok(BookV2.From(book)) : TypedResults.NotFound());

// ---------------------------------------------------------------- idempotency: borrowing a book
// POST /v1/loans with an Idempotency-Key header: a repeat with the same key gets the same answer.
var answers = new ConcurrentDictionary<string, (string Request, Loan Loan)>();
var turns = new Lock();
app.MapPost("/v1/loans", Results<Created<Loan>, ProblemHttpResult> (
    NewLoan input, HttpRequest request, HttpResponse response, Library library, TimeProvider time) =>
{
    string? key = request.Headers["Idempotency-Key"];
    string fingerprint = $"{input.BookId}|{input.Member}";
    lock (turns)                                     // two requests with one key: they take turns
    {
        if (key is not null && answers.TryGetValue(key, out var saved))
        {
            if (saved.Request != fingerprint)
            {
                return TypedResults.Problem(statusCode: 422,
                    title: "This Idempotency-Key was already used for a different request.");
            }
            response.Headers["Idempotent-Replayed"] = "true";
            return TypedResults.Created($"/v1/loans/{saved.Loan.Id}", saved.Loan);
        }
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var loan = library.Borrow(input.BookId, input.Member ?? "", today);
        if (loan is null)
        {
            return TypedResults.Problem(statusCode: 409, title: "No copy of that book is available.");
        }
        if (key is not null) answers[key] = (fingerprint, loan);
        return TypedResults.Created($"/v1/loans/{loan.Id}", loan);
    }
});
app.MapGet("/v1/loans/count", (Library library) => new { loans = library.LoanCount });

app.Run();

static Dictionary<string, string[]> Validate(NewBook input)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(input.Title)) errors["title"] = ["A book needs a title."];
    if (input.Authors is null || input.Authors.Count == 0 || input.Authors.Any(string.IsNullOrWhiteSpace))
        errors["authors"] = ["List at least one author, with a name."];
    if (input.Year is null || input.Year < 1450 || input.Year > DateTime.UtcNow.Year)
        errors["year"] = ["The year must be between 1450 and this year."];
    if (input.Copies is < 0) errors["copies"] = ["Copies can't be negative."];
    return errors;
}

static string NextPage(string? author, bool? available, int page, int pageSize)
{
    var query = new List<string>();
    if (author is not null) query.Add($"author={Uri.EscapeDataString(author)}");
    if (available is not null) query.Add($"available={(available.Value ? "true" : "false")}");
    query.Add($"page={page}");
    query.Add($"pageSize={pageSize}");
    return "/v2/books?" + string.Join("&", query);
}

public partial class Program;   // so the tests can start the app
