using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.Tests.Unit.Fakes;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 17: moving AccountServiceTests to Integration/ dropped AccountService's unit coverage
// from 84% to 59% — the registration rules had only been tested together with the database.
// Here they are as unit tests.
[Trait("Req", "FR-ACC-1")]
public class RegistrationRulesTests
{
    private readonly FakeAccountStore _store = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 21, 9, 0, 0, TimeSpan.Zero));

    private AccountService Accounts(IAccountStore? store = null) =>
        new(store ?? _store, new PasswordHasher<User>(), _clock, NullLogger<AccountService>.Instance);

    private static RegisterInput Input(string email, string name = "Ana") =>
        new() { Email = email, DisplayName = name, Password = "long-enough-1", ConfirmPassword = "long-enough-1" };

    [Fact]
    public async Task TheFirstAccount_IsTheAdmin_TheSecondIsNot()
    {
        var ana = await Accounts().RegisterAsync(Input("ana@example.com"));
        var ben = await Accounts().RegisterAsync(Input("ben@example.com", "Ben"));

        Assert.True(ana!.IsAdmin);
        Assert.False(ben!.IsAdmin);
    }

    [Theory]
    [InlineData("ana@example.com")]
    [InlineData("ANA@Example.com")]                 // other capitals
    [InlineData("  ana@example.com  ")]             // spaces around it
    public async Task AnEmailThatIsTaken_IsRefused(string again)
    {
        await Accounts().RegisterAsync(Input("ana@example.com"));

        Assert.Null(await Accounts().RegisterAsync(Input(again)));
        Assert.Single(_store.Users);
    }

    [Fact]
    public async Task NamesAndEmails_AreTrimmed_AndTheDateIsToday()
    {
        var ana = await Accounts().RegisterAsync(Input("  ana@example.com ", "  Ana  "));

        Assert.Equal("ana@example.com", ana!.Email);
        Assert.Equal("Ana", ana.DisplayName);
        Assert.Equal(new DateOnly(2026, 11, 21), ana.CreatedOn);
    }

    [Fact]
    public async Task TheSameEmail_AtTheSameMoment_IsRefusedByTheStore()
    {
        // A STUB built with NSubstitute: the email looked free, but the database's unique index refuses it.
        var store = Substitute.For<IAccountStore>();
        store.EmailTakenAsync("ana@example.com").Returns(false);
        store.AnyAsync().Returns(true);
        store.AddAsync(Arg.Any<User>()).Returns(false);

        Assert.Null(await Accounts(store).RegisterAsync(Input("ana@example.com")));
        await store.DidNotReceive().AdoptOrphansAsync(Arg.Any<int>());
    }
}
