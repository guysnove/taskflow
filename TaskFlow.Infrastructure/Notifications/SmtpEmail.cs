using System.Net.Mail;
using Microsoft.Extensions.Options;
using TaskFlow.Common;

namespace TaskFlow.Notifications;

// ADAPTER: makes .NET's SmtpClient fit IEmailSender. If TaskFlow moves to another library or an
// email service's HTTP API, only this class changes. (Microsoft no longer recommends SmtpClient for
// new code, which is exactly the kind of change an adapter keeps out of the rest of the app.)
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var o = options.Value;
        using var client = new SmtpClient();
        if (!string.IsNullOrEmpty(o.PickupDirectory))
        {
            Directory.CreateDirectory(o.PickupDirectory);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = Path.GetFullPath(o.PickupDirectory);
        }
        else
        {
            client.Host = o.Host ?? throw new InvalidOperationException("Email:Host or Email:PickupDirectory must be set.");
            client.Port = o.Port;
            client.EnableSsl = true;
            if (!string.IsNullOrEmpty(o.Username))
            {
                client.Credentials = new System.Net.NetworkCredential(o.Username, o.Password);   // Course 2, Ch. 20
            }
        }
        using var mail = new MailMessage(o.From, recipient, subject, body);
        await client.SendMailAsync(mail, cancellationToken);
    }
}

// DECORATOR: adds retries and logging to ANY IEmailSender (NFR-NOTIF-2), without changing it.
public sealed class RetryingEmailSender(IEmailSender inner, IOptions<EmailOptions> options,
                                        ILogger<RetryingEmailSender> logger, TimeProvider time) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        int attempts = options.Value.Attempts;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await inner.SendAsync(recipient, subject, body, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < attempts && ex is SmtpException or IOException)
            {
                logger.LogWarning(ex, "Email to {To} failed (attempt {Attempt} of {Attempts}); retrying", LogSafe.Email(recipient), attempt, attempts);
                await Task.Delay(options.Value.RetryDelay, time, cancellationToken);   // Chapter 16: stops waiting when cancelled
            }
        }
    }
}
