using System.Threading.Channels;

namespace Ch28.Notify;

// One EVENT: "something happened". It carries its own Id, so a consumer can recognise it if it arrives twice.
public record TaskFinished(string Id, int TaskId, string NotifyEmail);

// A tiny in-memory message queue that behaves like real ones (RabbitMQ, Azure Service Bus, Amazon SQS…) do:
//  - AT-LEAST-ONCE delivery: if the consumer fails, the message is delivered again, later;
//  - and sometimes AGAIN after success: every LostAckEvery-th "done" (acknowledgement) is lost on the way back,
//    so the queue thinks the message wasn't handled and redelivers it;
//  - after MaxDeliveries failures it goes to a DEAD-LETTER list for a person to look at.
public sealed class AtLeastOnceQueue(int lostAckEvery, int maxDeliveries = 4)
{
    private readonly Channel<(TaskFinished Message, int Delivery)> _channel = Channel.CreateUnbounded<(TaskFinished, int)>();
    private int _acks;
    public int Deliveries { get; private set; }
    public List<TaskFinished> DeadLetters { get; } = [];

    public void Publish(TaskFinished message) => _channel.Writer.TryWrite((message, 1));

    // Deliver every message to the handler until the queue is empty.
    public async Task ConsumeAllAsync(Func<TaskFinished, int, Task> handler, Action<string> log)
    {
        while (_channel.Reader.TryRead(out var item))
        {
            var (message, delivery) = item;
            Deliveries++;
            try
            {
                await handler(message, delivery);
                if (lostAckEvery > 0 && ++_acks % lostAckEvery == 0)
                {
                    log($"  queue: the 'done' for {message.Id} was lost; delivering it again");
                    _channel.Writer.TryWrite((message, delivery + 1));
                }
            }
            catch (Exception e) when (delivery < maxDeliveries)
            {
                log($"  queue: {message.Id} failed ({e.Message}); delivering it again later");
                _channel.Writer.TryWrite((message, delivery + 1));
            }
            catch (Exception e)
            {
                log($"  queue: {message.Id} failed {delivery} times ({e.Message}); moved to the dead-letter list");
                DeadLetters.Add(message);
            }
        }
    }
}

// The fake mail server: it records what was sent. FailFirstFor: one task whose first send fails (a temporary error).
public sealed class MailServer(int failFirstFor)
{
    private bool _failed;
    public List<string> Sent { get; } = [];
    public Task SendAsync(string to, int taskId)
    {
        if (taskId == failFirstFor && !_failed) { _failed = true; throw new IOException("mail server busy"); }
        Sent.Add($"{to}: task {taskId} is finished");
        return Task.CompletedTask;
    }
}

// The CONSUMER. Naive: send an email for every delivery. Idempotent: remember which event Ids were handled (an "inbox";
// in a real app, a database table with a unique Id, written in the same transaction as the work), and skip repeats.
public sealed class NotificationHandler(MailServer mail, bool idempotent, Action<string> log)
{
    private readonly HashSet<string> _handled = [];

    public async Task HandleAsync(TaskFinished e, int delivery)
    {
        if (idempotent && _handled.Contains(e.Id))
        {
            log($"{e.Id} (delivery {delivery}): already handled, duplicate ignored");
            return;
        }
        await mail.SendAsync(e.NotifyEmail, e.TaskId);
        if (idempotent) _handled.Add(e.Id);
        log($"{e.Id} (delivery {delivery}): email sent");
    }
}
