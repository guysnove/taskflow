using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Tests.Integration;

// AccountService with its REAL store (EfAccountStore) and SQLite "in memory": a real database that
// lives only as long as the connection. Course 2, Chapter 17: moved from Unit/ to Integration/ —
// it tests two pieces together, plus the migrations. The pure unit tests are in Unit/SignInRulesTests.
[Trait("Req", "FR-ACC-1")]
public class AccountServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TaskFlowDbContext _db;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 8, 9, 0, 0, TimeSpan.Zero));
    private readonly AccountService _accounts;

    public AccountServiceTests()
    {
        _connection.Open();
        _db = new TaskFlowDbContext(new DbContextOptionsBuilder<TaskFlowDbContext>().UseSqlite(_connection).Options);
        _db.Database.Migrate();                             // the same migrations as the real app
        _accounts = new AccountService(new EfAccountStore(_db), new PasswordHasher<User>(), _clock, NullLogger<AccountService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private Task<User?> Register(string email, string name = "Test") =>
        _accounts.RegisterAsync(new RegisterInput { Email = email, DisplayName = name, Password = "long-enough-1", ConfirmPassword = "long-enough-1" });

    [Fact]
    public async Task FirstUser_IsAdmin_SecondIsNot()
    {
        var ana = await Register("ana@example.com", "Ana");
        var ben = await Register("ben@example.com", "Ben");

        Assert.True(ana!.IsAdmin);
        Assert.False(ben!.IsAdmin);
    }

    [Fact]
    public async Task Password_IsNeverStoredAsTyped()
    {
        var ana = await Register("ana@example.com");
        Assert.NotEqual("long-enough-1", ana!.PasswordHash);
        Assert.DoesNotContain("long-enough-1", ana.PasswordHash);
    }

    [Fact]
    public async Task SameEmail_InOtherCapitals_IsRefused()
    {
        await Register("ana@example.com");
        var again = await Register("ANA@Example.com");
        Assert.Null(again);                                 // null: the email is taken
    }

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount_ForFiveMinutes()
    {
        await Register("ana@example.com");
        for (int i = 0; i < AccountService.MaxFailedSignIns; i++)
        {
            await _accounts.CheckAsync("ana@example.com", "wrong-" + i);
        }

        // Locked: even the right password is refused now.
        var (locked, _) = await _accounts.CheckAsync("ana@example.com", "long-enough-1");
        Assert.Equal(SignInCheck.LockedOut, locked);

        // The fake clock lets the test "wait" five minutes in no time at all.
        _clock.Advance(AccountService.LockoutTime + TimeSpan.FromSeconds(1));
        var (after, user) = await _accounts.CheckAsync("ana@example.com", "long-enough-1");
        Assert.Equal(SignInCheck.Success, after);
        Assert.NotNull(user);
    }
}
