using Microsoft.Extensions.DependencyInjection;

namespace Ch10.Patterns.Factory;

// Problem: create the right sender for a name that arrives at run time ("email", "sms"…),
// e.g. from a user's settings.
public interface ISender
{
    string Send(string to, string text);
}

public sealed class EmailSender : ISender
{
    public string Send(string to, string text) => $"email to {to}: {text}";
}

public sealed class SmsSender : ISender
{
    public string Send(string to, string text) => $"sms to {to}: {text}";
}

// 1. A SIMPLE FACTORY: one method that knows every type. Adding one means editing it.
public static class SenderFactory
{
    public static ISender Create(string name) => name switch
    {
        "email" => new EmailSender(),
        "sms" => new SmsSender(),
        _ => throw new ArgumentException($"No sender called '{name}'.", nameof(name)),
    };
}

// 2. KEYED SERVICES (.NET 8+): the DI container is the factory. Each type registers itself under a key.
public static class KeyedSenders
{
    public static ServiceProvider Build() => new ServiceCollection()
        .AddKeyedSingleton<ISender, EmailSender>("email")
        .AddKeyedSingleton<ISender, SmsSender>("sms")
        .BuildServiceProvider();
}
