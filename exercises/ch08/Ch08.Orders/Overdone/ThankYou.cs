namespace Ch08.Orders.Overdone;

// Over-engineering: "SOLID" applied to something that will never change.
// Five types and a factory to build one sentence. Every name sounds sensible; together they're noise.
public interface IThankYouMessageFormatter
{
    string Format(IThankYouMessageContext context);
}

public interface IThankYouMessageContext
{
    string CustomerName { get; }
}

public interface IThankYouMessageFormatterFactory
{
    IThankYouMessageFormatter Create();
}

public sealed class DefaultThankYouMessageContext(string customerName) : IThankYouMessageContext
{
    public string CustomerName { get; } = customerName;
}

public sealed class DefaultThankYouMessageFormatter : IThankYouMessageFormatter
{
    public string Format(IThankYouMessageContext context) => $"Thank you, {context.CustomerName}!";
}

public sealed class DefaultThankYouMessageFormatterFactory : IThankYouMessageFormatterFactory
{
    public IThankYouMessageFormatter Create() => new DefaultThankYouMessageFormatter();
}

// What it needed: one line. If a second kind of message ever appears, THEN add an interface.
public static class ThankYou
{
    public static string For(string customerName) => $"Thank you, {customerName}!";
}
