namespace Ch08.Orders.Inheritance;

// Composition vs inheritance: the INHERITANCE way to add logging, and then "both".
// Every new feature multiplies the classes: 2 kinds x (plain, logged) x (single, both)...
public class EmailNotifier
{
    public virtual void OrderPlaced(Customer c, decimal total) => Console.WriteLine($"  [smtp] to {c.Email}");
}

public class SmsNotifier
{
    public virtual void OrderPlaced(Customer c, decimal total) => Console.WriteLine($"  [sms] {c.Name}");
}

public class LoggingEmailNotifier : EmailNotifier
{
    public override void OrderPlaced(Customer c, decimal total)
    {
        Console.WriteLine($"  [log] notifying customer {c.Id} by email");
        base.OrderPlaced(c, total);
    }
}

public class LoggingSmsNotifier : SmsNotifier          // the same logging code, copied
{
    public override void OrderPlaced(Customer c, decimal total)
    {
        Console.WriteLine($"  [log] notifying customer {c.Id} by sms");
        base.OrderPlaced(c, total);
    }
}

// "Email and SMS"? C# has no multiple inheritance: a class can't inherit both.
// So it inherits one and copies the other. And a logged version of that is class number 6.
public class EmailAndSmsNotifier : EmailNotifier
{
    public override void OrderPlaced(Customer c, decimal total)
    {
        base.OrderPlaced(c, total);
        Console.WriteLine($"  [sms] {c.Name}");             // copied from SmsNotifier
    }
}
