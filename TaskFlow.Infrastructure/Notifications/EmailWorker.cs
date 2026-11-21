using Microsoft.Extensions.Options;

namespace TaskFlow.Notifications;

// Course 2, Chapter 16: sends queued emails, one at a time, in the background.
//   - waits for work without blocking a thread (await foreach over the channel)
//   - gives each email at most SendTimeout (all attempts included), then gives up and logs it
//   - one failing email never stops the worker
//   - when the app stops, stoppingToken is cancelled: the current send is cancelled, and the
//     emails still waiting are counted in the log (they are lost: TD-13)
public sealed class EmailWorker(OutgoingEmails queue, IEmailSender sender, IOptions<EmailOptions> options,
                                ILogger<EmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var email in queue.Reader.ReadAllAsync(stoppingToken))
            {
                await SendOneAsync(email, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            if (queue.Waiting > 0)
            {
                logger.LogWarning("Stopping: {Count} queued email(s) were not sent", queue.Waiting);
            }
        }
    }

    private async Task SendOneAsync(EmailMessage email, CancellationToken stoppingToken)
    {
        // A token that is cancelled by EITHER the timeout OR the app stopping.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(options.Value.SendTimeout);
        try
        {
            await sender.SendAsync(email.To, email.Subject, email.Body, timeout.Token);
            logger.LogInformation("Email sent to {To}", email.To);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError("Email to {To} timed out after {Seconds} s; given up", email.To, options.Value.SendTimeout.TotalSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Email to {To} failed after every attempt; given up", email.To);
        }
    }
}
