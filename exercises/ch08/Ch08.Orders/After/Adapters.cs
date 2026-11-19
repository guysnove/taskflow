using Ch08.Orders.Before;

namespace Ch08.Orders.After;

// The "details": real implementations of the small interfaces.

public class InMemoryStock : IStock
{
    private readonly Dictionary<string, int> _counts;

    // Each InMemoryStock has its own counts: nothing static, nothing shared.
    public InMemoryStock(Dictionary<string, int> counts) => _counts = new(counts);

    public int Available(string sku) => _counts.TryGetValue(sku, out int n) ? n : -1;

    public void Take(string sku, int quantity) => _counts[sku] -= quantity;
}

public class InMemoryOrderStore : IOrderStore
{
    public List<(int Number, Order Order, decimal Total)> Saved { get; } = new();

    public int Save(Order order, decimal total)
    {
        int number = Saved.Count + 1;
        Saved.Add((number, order, total));
        return number;
    }
}

// An adapter: makes the old CardGateway fit the new interface. Composition: it HAS a gateway.
public class CardGatewayAdapter : IPaymentGateway
{
    private readonly CardGateway _gateway = new();

    public bool Charge(string card, decimal amount) => _gateway.Charge(card, amount);
}

public class EmailNotifier : INotifier
{
    private readonly SmtpMailer _mailer = new();

    public void OrderPlaced(Customer customer, decimal total) =>
        _mailer.Send(customer.Email, "Your order", $"Thank you, {customer.Name}! You paid {total:0.00}.");
}

public class SmsNotifier : INotifier
{
    public void OrderPlaced(Customer customer, decimal total) =>
        Console.WriteLine($"  [sms] {customer.Name}: order confirmed, {total:0.00}");
}

// Composition instead of inheritance (see Inheritance/Notifiers.cs for the other way).
// LoggingNotifier wraps ANY notifier and adds a log line: one class, not one per kind.
public class LoggingNotifier : INotifier
{
    private readonly INotifier _inner;
    private readonly TextWriter _log;

    public LoggingNotifier(INotifier inner, TextWriter log)
    {
        _inner = inner;
        _log = log;
    }

    public void OrderPlaced(Customer customer, decimal total)
    {
        _log.WriteLine($"  [log] notifying customer {customer.Id} with {_inner.GetType().Name}");
        _inner.OrderPlaced(customer, total);
    }
}

// Several notifiers that look like one: email AND sms.
public class AllNotifiers : INotifier
{
    private readonly INotifier[] _notifiers;

    public AllNotifiers(params INotifier[] notifiers) => _notifiers = notifiers;

    public void OrderPlaced(Customer customer, decimal total)
    {
        foreach (var n in _notifiers)
        {
            n.OrderPlaced(customer, total);
        }
    }
}
