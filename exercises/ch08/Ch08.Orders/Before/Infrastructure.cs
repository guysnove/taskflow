namespace Ch08.Orders.Before;

// Stand-ins for the outside world. In a real shop these would talk to a payment
// company and a mail server over the network: slow, and with real effects.

public class CardGateway
{
    public bool Charge(string card, decimal amount)
    {
        Thread.Sleep(1500);                    // a network call to the payment company
        if (card.StartsWith("4000", StringComparison.Ordinal))
        {
            Console.WriteLine($"  [gateway] DECLINED card ending {card[^4..]}");
            return false;
        }
        Console.WriteLine($"  [gateway] CHARGED {amount:0.00} to card ending {card[^4..]}");
        return true;
    }
}

public class SmtpMailer
{
    public void Send(string to, string subject, string body)
    {
        Console.WriteLine($"  [smtp] to {to}: \"{subject}\" - {body}");
    }
}
