using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Notifications;

// Settings, from the "Email" section. In Development, PickupDirectory writes each email as an
// .eml file instead of sending it: real messages you can open, and no mail server needed.
public class EmailOptions
{
    public const string Section = "Email";
    [Required] public string From { get; set; } = "taskflow@example.com";
    [Required] public string SiteUrl { get; set; } = "https://localhost:5001";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? PickupDirectory { get; set; }
    [Range(1, 10)] public int Attempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}

// What the rest of TaskFlow needs from email: one method. TaskFlow's own interface.
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body);
}
