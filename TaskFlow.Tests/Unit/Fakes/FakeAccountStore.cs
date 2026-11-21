using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Unit.Fakes;

// Course 2, Chapter 17: a FAKE — a real, working IAccountStore that keeps accounts in a list.
// Thanks to the port from Chapter 12, AccountService's rules can be tested with no database at all.
public sealed class FakeAccountStore : IAccountStore
{
    public List<User> Users { get; } = [];
    public int Saves { get; private set; }                       // a little spying, too

    public Task<bool> EmailTakenAsync(string email) =>
        Task.FromResult(Users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> AnyAsync() => Task.FromResult(Users.Count > 0);

    public Task<bool> AddAsync(User user)
    {
        user.Id = Users.Count + 1;
        Users.Add(user);
        return Task.FromResult(true);
    }

    public Task<(int Tasks, int Projects)> AdoptOrphansAsync(int userId) => Task.FromResult((0, 0));

    public Task<User?> FindByEmailAsync(string email) =>
        Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task SaveAsync()
    {
        Saves++;
        return Task.CompletedTask;
    }

    public Task<List<UserSummary>> SummariesAsync() => Task.FromResult(new List<UserSummary>());
    public Task<bool> EmailNotificationsAsync(int userId) => Task.FromResult(false);
    public Task SetEmailNotificationsAsync(int userId, bool enabled) => Task.CompletedTask;
}
