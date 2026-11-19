using Microsoft.AspNetCore.Identity;
using TaskFlow.Common;
using TaskFlow.Models;

namespace TaskFlow.Services;

public enum SignInCheck { Success, Failed, LockedOut }

// Course 2, Chapter 11: moved from AdminController, so the Accounts module owns its own queries.
public record UserSummary(int Id, string Email, string DisplayName, DateOnly CreatedOn,
                          bool IsAdmin, int Tasks);

// Registering and checking passwords (Chapter 25). Signing in with a cookie is the controller's job.
public class AccountService
{
    public const int MaxFailedSignIns = 5;
    public static readonly TimeSpan LockoutTime = TimeSpan.FromMinutes(5);

    private readonly IAccountStore _store;               // Course 2, Chapter 12: a port, not the DbContext
    private readonly IPasswordHasher<User> _hasher;
    private readonly TimeProvider _time;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IAccountStore store, IPasswordHasher<User> hasher, TimeProvider time,
                          ILogger<AccountService> logger)
    {
        _store = store;
        _hasher = hasher;
        _time = time;
        _logger = logger;
    }

    public Task<bool> EmailTakenAsync(string email) => _store.EmailTakenAsync(email.Trim());

    // Creates the account. Returns null if the email address is already used.
    public async Task<User?> RegisterAsync(RegisterInput input)
    {
        if (await EmailTakenAsync(input.Email!))
        {
            return null;
        }
        bool first = !await _store.AnyAsync();
        var user = new User
        {
            Email = input.Email!.Trim(),
            DisplayName = input.DisplayName!.Trim(),
            CreatedOn = _time.Today(),
            IsAdmin = first,                               // the very first account runs the site
        };
        user.PasswordHash = _hasher.HashPassword(user, input.Password!);   // salted, slow hash
        if (!await _store.AddAsync(user))
        {
            return null;                                   // the unique index: same email, same moment
        }

        if (first)
        {
            // Tasks and projects from before accounts existed belong to nobody.
            // Give them to the first user.
            var (tasks, projects) = await _store.AdoptOrphansAsync(user.Id);
            _logger.LogInformation("First user {UserId} adopted {Tasks} tasks and {Projects} projects",
                                   user.Id, tasks, projects);
        }
        _logger.LogInformation("User {UserId} registered", user.Id);   // the id, not the email address
        return user;
    }

    // Checks an email and password. Same answer for "no such email" and "wrong password".
    public async Task<(SignInCheck Result, User? User)> CheckAsync(string email, string password)
    {
        var user = await _store.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            _hasher.HashPassword(new User(), password);    // take about as long as a real check
            return (SignInCheck.Failed, null);
        }

        var now = _time.GetUtcNow();
        if (user.LockedUntil is DateTimeOffset until && until > now)
        {
            return (SignInCheck.LockedOut, null);
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedSignIns++;
            if (user.FailedSignIns >= MaxFailedSignIns)
            {
                user.LockedUntil = now + LockoutTime;
                user.FailedSignIns = 0;
                _logger.LogWarning("User {UserId} locked out after {Max} wrong passwords",
                                   user.Id, MaxFailedSignIns);
            }
            await _store.SaveAsync();
            return (SignInCheck.Failed, null);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);   // upgrade old hashes quietly
        }
        user.FailedSignIns = 0;
        user.LockedUntil = null;
        await _store.SaveAsync();
        return (SignInCheck.Success, user);
    }

    // Course 2, Chapter 11: the Accounts module answers questions about accounts, so controllers
    // don't need the database (docs/design/modules.md).

    // Every account, with its number of tasks (the admin page).
    public Task<List<UserSummary>> SummariesAsync() => _store.SummariesAsync();

    // FR-NOTIF-4: does this user want notifications by email?
    public Task<bool> EmailNotificationsAsync(int userId) => _store.EmailNotificationsAsync(userId);

    public Task SetEmailNotificationsAsync(int userId, bool enabled) => _store.SetEmailNotificationsAsync(userId, enabled);
}
