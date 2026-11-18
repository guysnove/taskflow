using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.Services;

public enum SignInCheck { Success, Failed, LockedOut }

// Registering and checking passwords (Chapter 25). Signing in with a cookie is the controller's job.
public class AccountService
{
    public const int MaxFailedSignIns = 5;
    public static readonly TimeSpan LockoutTime = TimeSpan.FromMinutes(5);

    private readonly TaskFlowDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly TimeProvider _time;
    private readonly ILogger<AccountService> _logger;

    public AccountService(TaskFlowDbContext db, IPasswordHasher<User> hasher, TimeProvider time,
                          ILogger<AccountService> logger)
    {
        _db = db;
        _hasher = hasher;
        _time = time;
        _logger = logger;
    }

    public Task<bool> EmailTakenAsync(string email) =>
        _db.Users.AnyAsync(u => u.Email == email.Trim());

    // Creates the account. Returns null if the email address is already used.
    public async Task<User?> RegisterAsync(RegisterInput input)
    {
        if (await EmailTakenAsync(input.Email!))
        {
            return null;
        }
        bool first = !await _db.Users.AnyAsync();
        var user = new User
        {
            Email = input.Email!.Trim(),
            DisplayName = input.DisplayName!.Trim(),
            CreatedOn = _time.Today(),
            IsAdmin = first,                               // the very first account runs the site
        };
        user.PasswordHash = _hasher.HashPassword(user, input.Password!);   // salted, slow hash
        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return null;                                   // the unique index: same email, same moment
        }

        if (first)
        {
            // Tasks and projects from before accounts existed belong to nobody.
            // Give them to the first user.
            int tasks = await _db.Tasks
                .Where(t => t.UserId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UserId, user.Id));
            int projects = await _db.Projects
                .Where(p => p.UserId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UserId, user.Id));
            _logger.LogInformation("First user {UserId} adopted {Tasks} tasks and {Projects} projects",
                                   user.Id, tasks, projects);
        }
        _logger.LogInformation("User {UserId} registered", user.Id);   // the id, not the email address
        return user;
    }

    // Checks an email and password. Same answer for "no such email" and "wrong password".
    public async Task<(SignInCheck Result, User? User)> CheckAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
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
            await _db.SaveChangesAsync();
            return (SignInCheck.Failed, null);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);   // upgrade old hashes quietly
        }
        user.FailedSignIns = 0;
        user.LockedUntil = null;
        await _db.SaveChangesAsync();
        return (SignInCheck.Success, user);
    }
}
