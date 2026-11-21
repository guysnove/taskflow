using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.Tests.Unit.Fakes;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 17: the sign-in rules, as true unit tests — no database, no web server.
//   FakeAccountStore: a fake (Chapter 12's port makes this possible)
//   IPasswordHasher:  an NSubstitute mock, so each test decides what the hasher says
//   FakeTimeProvider: a clock the test controls
[Trait("Req", "FR-ACC-1")]
public class SignInRulesTests
{
    private readonly FakeAccountStore _store = new();
    private readonly IPasswordHasher<User> _hasher = Substitute.For<IPasswordHasher<User>>();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 21, 9, 0, 0, TimeSpan.Zero));
    private readonly User _ana = new() { Email = "ana@example.com", DisplayName = "Ana", PasswordHash = "HASH" };

    public SignInRulesTests()
    {
        _store.Users.Add(_ana);
        _hasher.VerifyHashedPassword(_ana, "HASH", "right").Returns(PasswordVerificationResult.Success);
        _hasher.VerifyHashedPassword(_ana, "HASH", Arg.Is<string>(p => p != "right")).Returns(PasswordVerificationResult.Failed);
    }

    private AccountService Accounts() => new(_store, _hasher, _clock, NullLogger<AccountService>.Instance);

    private async Task WrongPasswordsAsync(int times)
    {
        for (int i = 0; i < times; i++) await Accounts().CheckAsync("ana@example.com", "wrong");
    }

    [Theory]
    [InlineData(4, SignInCheck.Success)]      // one below the limit: still not locked
    [InlineData(5, SignInCheck.LockedOut)]    // the fifth wrong password locks the account
    public async Task WrongPasswords_LockTheAccountAtTheFifth(int wrong, SignInCheck thenRightPassword)
    {
        await WrongPasswordsAsync(wrong);

        var (result, _) = await Accounts().CheckAsync("ana@example.com", "right");

        Assert.Equal(thenRightPassword, result);
    }

    [Theory]
    [InlineData(-1, SignInCheck.LockedOut)]   // one second before the lock ends
    [InlineData(0, SignInCheck.Success)]      // exactly when it ends: open again
    public async Task TheLock_EndsAfterFiveMinutes(int secondsAfterTheEnd, SignInCheck expected)
    {
        await WrongPasswordsAsync(AccountService.MaxFailedSignIns);
        _clock.Advance(AccountService.LockoutTime + TimeSpan.FromSeconds(secondsAfterTheEnd));

        var (result, _) = await Accounts().CheckAsync("ana@example.com", "right");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ARightPassword_ResetsTheCount()
    {
        await WrongPasswordsAsync(4);
        await Accounts().CheckAsync("ana@example.com", "right");
        await WrongPasswordsAsync(4);                               // 4 + 4, with a success between

        var (result, _) = await Accounts().CheckAsync("ana@example.com", "right");

        Assert.Equal(SignInCheck.Success, result);
        Assert.Equal(0, _ana.FailedSignIns);
    }

    [Fact]
    public async Task AnUnknownEmail_LooksExactlyLikeAWrongPassword_AndTakesAsLong()
    {
        var (unknown, user) = await Accounts().CheckAsync("nobody@example.com", "whatever");

        Assert.Equal(SignInCheck.Failed, unknown);
        Assert.Null(user);
        _hasher.Received(1).HashPassword(Arg.Any<User>(), "whatever");   // the slow hash still ran (no timing hint)
    }

    [Fact]
    public async Task AnOldHash_IsUpgraded_WhenTheUserSignsIn()
    {
        _hasher.VerifyHashedPassword(_ana, "HASH", "right").Returns(PasswordVerificationResult.SuccessRehashNeeded);
        _hasher.HashPassword(_ana, "right").Returns("NEW-HASH");

        var (result, _) = await Accounts().CheckAsync("ana@example.com", "right");

        Assert.Equal(SignInCheck.Success, result);
        Assert.Equal("NEW-HASH", _ana.PasswordHash);
        Assert.True(_store.Saves > 0);
    }
}
