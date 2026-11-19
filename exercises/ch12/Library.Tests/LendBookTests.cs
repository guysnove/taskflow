using Library.Application;
using Library.Infrastructure;
using Microsoft.Extensions.Time.Testing;

namespace Library.Tests;

// Use cases tested with the in-memory adapter and a fake clock: no files, no waiting 21 days.
public class LendBookTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly InMemoryLoanStore _store = new();
    private LendBook Lend() => new(_store, _clock);

    [Fact]
    public async Task ABook_CanBeLent_AndIsDueIn21Days()
    {
        var loan = await Lend().RunAsync("Clean Code", "ana");
        Assert.Equal(new DateOnly(2026, 11, 22), loan.DueOn);
    }

    [Fact]
    public async Task TheSameBook_CannotBeLentTwice()
    {
        await Lend().RunAsync("Clean Code", "ana");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Lend().RunAsync("clean code", "ben"));
    }

    [Fact]
    public async Task AMember_WithAnOverdueBook_CannotBorrow()
    {
        await Lend().RunAsync("Clean Code", "ana");
        _clock.Advance(TimeSpan.FromDays(22));
        Assert.Single(await new OverdueReport(_store, _clock).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Lend().RunAsync("Refactoring", "ana"));
    }

    [Fact]
    public async Task TheJsonAdapter_KeepsLoans_BetweenRuns()
    {
        string path = Path.Combine(Path.GetTempPath(), $"loans-{Guid.NewGuid():N}.json");
        try
        {
            await new LendBook(new JsonFileLoanStore(path), _clock).RunAsync("Clean Code", "ana");
            var again = new JsonFileLoanStore(path);                         // a fresh "run"
            Assert.Single(await again.AllAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => new LendBook(again, _clock).RunAsync("Clean Code", "ben"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
