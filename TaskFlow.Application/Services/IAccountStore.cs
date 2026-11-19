using TaskFlow.Models;

namespace TaskFlow.Services;

// Course 2, Chapter 12: a PORT. How AccountService stores and finds accounts. The rules
// (first account is admin, lockout, password hashing) stay in AccountService; only storage moved out.
public interface IAccountStore
{
    Task<bool> EmailTakenAsync(string email);
    Task<bool> AnyAsync();
    Task<bool> AddAsync(User user);                    // false: the email was taken at the same moment
    Task<(int Tasks, int Projects)> AdoptOrphansAsync(int userId);
    Task<User?> FindByEmailAsync(string email);
    Task SaveAsync();                                  // saves changes to accounts found above
    Task<List<UserSummary>> SummariesAsync();
    Task<bool> EmailNotificationsAsync(int userId);
    Task SetEmailNotificationsAsync(int userId, bool enabled);
}
