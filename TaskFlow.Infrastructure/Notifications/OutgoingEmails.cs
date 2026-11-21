using System.Threading.Channels;

namespace TaskFlow.Notifications;

// Course 2, Chapter 16: an in-memory queue of emails (named OutgoingEmails: the analyzers reserve "...Queue" for
// classes that ARE .NET queues — CA1711), shared by every request (a singleton).
// System.Threading.Channels: many writers (requests), one reader (EmailWorker), thread-safe, and async:
// the reader waits without blocking a thread. Bounded, so a flood can't use up the server's memory.
// NOT durable: emails still in the queue are lost if the app stops (TD-13; Chapter 28 makes it durable).
public sealed class OutgoingEmails : IOutgoingEmails
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(1000) { SingleReader = true, FullMode = BoundedChannelFullMode.DropWrite });

    public bool TryEnqueue(EmailMessage email) => _channel.Writer.TryWrite(email);

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public int Waiting => _channel.Reader.Count;
}
