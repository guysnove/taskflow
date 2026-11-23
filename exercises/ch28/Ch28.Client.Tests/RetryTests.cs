using System.Net;
using Ch28.Client;

public class RetryTests
{
    private sealed class NoJitter : Random { public override double NextDouble() => 0; }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(3, 400)]
    public void Backoff_Doubles(int failedAttempt, double ms) => Assert.Equal(ms, Retry.Delay(failedAttempt, new NoJitter()).TotalMilliseconds);

    [Fact]
    public void Only_TemporaryFailures_AreRetried()
    {
        Assert.True(Retry.IsTransient(new TaskCanceledException()));
        Assert.True(Retry.IsTransient(new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable)));
        Assert.False(Retry.IsTransient(new HttpRequestException("400", null, HttpStatusCode.BadRequest)));
    }

    [Fact]
    public async Task SucceedsOnTheThirdAttempt()
    {
        int calls = 0;
        int result = await Retry.WithBackoffAsync(async () =>
        {
            await Task.Yield();
            if (++calls < 3) throw new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable);
            return 42;
        }, order: 1, log: _ => { }, random: new NoJitter());
        Assert.Equal((42, 3), (result, calls));
    }

    [Fact]
    public async Task GivesUp_AfterTheLastAttempt()
    {
        int calls = 0;
        await Assert.ThrowsAsync<HttpRequestException>(() => Retry.WithBackoffAsync<int>(() =>
        {
            calls++;
            throw new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable);
        }, order: 1, log: _ => { }, attempts: 4, random: new NoJitter()));
        Assert.Equal(4, calls);
    }
}
