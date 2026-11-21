using TaskFlow.Models;

namespace TaskFlow.Services;

// Course 2, Chapter 15: where answers to idempotent requests are kept (for the signed-in user).
public interface IIdempotencyStore
{
    // Starts a unit of work: the request's own changes and its saved answer are committed together,
    // and two requests with the same key take turns.
    Task<IIdempotencyScope> BeginAsync();
}

public interface IIdempotencyScope : IAsyncDisposable
{
    Task<IdempotencyRecord?> FindAsync(string key);
    Task SaveAndCommitAsync(IdempotencyRecord record);      // not called: everything is rolled back
}
