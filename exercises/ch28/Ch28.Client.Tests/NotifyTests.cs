using Ch28.Notify;

namespace Ch28.Client.Tests;

public class NotifyTests
{
    private static (MailServer, AtLeastOnceQueue) Run(bool idempotent)
    {
        var queue = new AtLeastOnceQueue(lostAckEvery: 3);
        var mail = new MailServer(failFirstFor: 104);
        var handler = new NotificationHandler(mail, idempotent, _ => { });
        for (int i = 1; i <= 8; i++) queue.Publish(new TaskFinished($"evt-{i}", 100 + i, $"user{i}@example.com"));
        queue.ConsumeAllAsync(handler.HandleAsync, _ => { }).GetAwaiter().GetResult();
        return (mail, queue);
    }

    [Fact]
    public void ANaiveConsumer_SendsDuplicates()
    {
        var (mail, queue) = Run(idempotent: false);
        Assert.Equal(12, queue.Deliveries);
        Assert.Equal(11, mail.Sent.Count);
        Assert.Equal(8, mail.Sent.Distinct().Count());
    }

    [Fact]
    public void AnIdempotentConsumer_SendsEachEmailOnce_DespiteRedelivery()
    {
        var (mail, queue) = Run(idempotent: true);
        Assert.Equal(12, queue.Deliveries);
        Assert.Equal(8, mail.Sent.Count);
        Assert.Equal(8, mail.Sent.Distinct().Count());
    }

    [Fact]
    public async Task AMessageThatAlwaysFails_EndsInTheDeadLetterList()
    {
        var queue = new AtLeastOnceQueue(lostAckEvery: 0, maxDeliveries: 4);
        int calls = 0;
        queue.Publish(new TaskFinished("evt-1", 101, "a@example.com"));
        await queue.ConsumeAllAsync((_, _) => { calls++; throw new IOException("down"); }, _ => { });
        Assert.Equal(4, calls);
        Assert.Single(queue.DeadLetters);
    }
}
