using Ch08.Orders.After;

namespace Ch08.Orders.Tests;

// Test doubles: tiny classes that keep the small interfaces' promises, instantly,
// and remember what was asked of them.

public class FakeGateway : IPaymentGateway
{
    public bool Decline { get; set; }
    public List<(string Card, decimal Amount)> Charges { get; } = new();

    public bool Charge(string card, decimal amount)
    {
        if (Decline)
        {
            return false;
        }
        Charges.Add((card, amount));
        return true;
    }
}

public class RecordingNotifier : INotifier
{
    public List<(string Name, decimal Total)> Sent { get; } = new();

    public void OrderPlaced(Customer customer, decimal total) => Sent.Add((customer.Name, total));
}
