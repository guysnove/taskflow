namespace Ch08.Orders.Payments.Good;

// Liskov kept, with Interface Segregation: two small promises instead of one big one.
// A class only promises what it can really do.
public interface IPayment
{
    void Pay(decimal amount);
}

public interface IRefundable
{
    void Refund(decimal amount);
}

public class CardPayment : IPayment, IRefundable
{
    public List<string> History { get; } = new();
    public void Pay(decimal amount) => History.Add($"card paid {amount:0.00}");
    public void Refund(decimal amount) => History.Add($"card refunded {amount:0.00}");
}

public class GiftCardPayment : IPayment          // no Refund method at all: nothing to break
{
    public List<string> History { get; } = new();
    public void Pay(decimal amount) => History.Add($"gift card paid {amount:0.00}");
}

public static class Refunds
{
    // Asks only for things that CAN refund. Passing a gift card is now a compile error, not a crash.
    public static void RefundAll(IEnumerable<IRefundable> payments, decimal amount)
    {
        foreach (var p in payments)
        {
            p.Refund(amount);
        }
    }

    // For a mixed list: pick out the refundable ones, and say which ones aren't.
    public static List<IPayment> RefundWherePossible(IEnumerable<IPayment> payments, decimal amount)
    {
        var notRefunded = new List<IPayment>();
        foreach (var p in payments)
        {
            if (p is IRefundable r)
            {
                r.Refund(amount);
            }
            else
            {
                notRefunded.Add(p);
            }
        }
        return notRefunded;
    }
}
