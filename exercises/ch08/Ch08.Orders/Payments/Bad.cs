namespace Ch08.Orders.Payments.Bad;

// Liskov Substitution, BROKEN. Every PaymentMethod promises it can Pay and Refund.
// GiftCard inherits the promise, then breaks it.
public abstract class PaymentMethod
{
    public List<string> History { get; } = new();
    public abstract void Pay(decimal amount);
    public abstract void Refund(decimal amount);
}

public class CardPayment : PaymentMethod
{
    public override void Pay(decimal amount) => History.Add($"card paid {amount:0.00}");
    public override void Refund(decimal amount) => History.Add($"card refunded {amount:0.00}");
}

public class GiftCardPayment : PaymentMethod
{
    public override void Pay(decimal amount) => History.Add($"gift card paid {amount:0.00}");

    // A gift card can't give money back... so this "PaymentMethod" can't do what PaymentMethod promises.
    public override void Refund(decimal amount) =>
        throw new NotSupportedException("Gift card payments can't be refunded.");
}

public static class Refunds
{
    // Written against PaymentMethod, trusting its promise. Correct code: until a GiftCardPayment arrives.
    public static void RefundAll(IEnumerable<PaymentMethod> payments, decimal amount)
    {
        foreach (var p in payments)
        {
            p.Refund(amount);
        }
    }
}
