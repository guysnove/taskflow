using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Data;

// Course 2, Chapter 15. One transaction around the whole request (Chapter 14): on SQLite it starts with
// BEGIN IMMEDIATE, so a second request with the same key waits, then finds the saved answer.
public class EfIdempotencyStore(TaskFlowDbContext db, ICurrentUser user, TimeProvider time) : IIdempotencyStore
{
    public async Task<IIdempotencyScope> BeginAsync() =>
        new Scope(db, user.Id, time, await db.Database.BeginTransactionAsync());

    private sealed class Scope(TaskFlowDbContext db, int userId, TimeProvider time, IDbContextTransaction transaction) : IIdempotencyScope
    {
        public Task<IdempotencyRecord?> FindAsync(string key) =>
            db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId && r.Key == key);

        public async Task SaveAndCommitAsync(IdempotencyRecord record)
        {
            record.UserId = userId;
            record.CreatedAt = time.GetUtcNow();
            db.IdempotencyRecords.Add(record);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public ValueTask DisposeAsync() => transaction.DisposeAsync();    // without a commit: rolled back
    }
}
