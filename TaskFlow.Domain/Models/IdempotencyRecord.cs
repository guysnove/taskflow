namespace TaskFlow.Models;

// Course 2, Chapter 15: the answer to a POST that carried an Idempotency-Key, kept so that
// a repeat of the same request gets the same answer instead of doing the work twice.
public class IdempotencyRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Key { get; set; } = "";            // chosen by the client; unique per user
    public string RequestHash { get; set; } = "";    // SHA-256 of the request: the same key must mean the same request
    public int StatusCode { get; set; }
    public string? Location { get; set; }
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
