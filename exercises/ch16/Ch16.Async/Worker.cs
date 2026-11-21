using System.Threading.Channels;

namespace Ch16.Async;

// Reliable-ish background processing: requests put jobs in a queue and return at once;
// one worker takes them out and processes them. Stopping cancels the worker cleanly.
public sealed class ResizeWorker(Action<string> report)
{
    private readonly Channel<string> _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(100) { SingleReader = true });

    public bool Enqueue(string photo) => _queue.Writer.TryWrite(photo);

    public int Waiting => _queue.Reader.Count;

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (string photo in _queue.Reader.ReadAllAsync(stoppingToken))   // waits without blocking a thread
            {
                try
                {
                    if (photo.EndsWith(".bad", StringComparison.Ordinal)) throw new InvalidDataException($"{photo} is not an image");
                    await Task.Delay(150, stoppingToken);                                 // "resize"
                    report($"resized {photo}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    report($"FAILED {photo}: {ex.Message} — skipped, the worker goes on");   // one bad job never stops the worker
                }
            }
        }
        catch (OperationCanceledException)
        {
            report($"stopping: {Waiting} photo(s) still waiting");
        }
    }
}
