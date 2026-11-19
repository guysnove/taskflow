using Library.Application;
using Library.Domain;

namespace Library.Infrastructure;

// An ADAPTER: implements the port with a list in memory (handy for tests and demos).
public sealed class InMemoryLoanStore : ILoanStore
{
    private readonly List<Loan> _loans = new();

    public Task<List<Loan>> AllAsync() => Task.FromResult(_loans.ToList());

    public Task AddAsync(Loan loan)
    {
        _loans.Add(loan);
        return Task.CompletedTask;
    }

    public Task SaveAsync() => Task.CompletedTask;
}
