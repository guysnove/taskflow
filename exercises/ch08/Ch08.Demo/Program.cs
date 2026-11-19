// Chapter 8 demo.
//   dotnet run -- before       three orders through the old OrderProcessor
//   dotnet run -- after        the same three orders through the SOLID OrderService
//   dotnet run -- blackfriday  the AFTER shop on Black Friday 2026, with a chosen clock
//   dotnet run -- refunds      Liskov: refund a card and a gift card (the Bad version crashes)
//   dotnet run -- framework    the case study: a .NET array, used as an IList<int>
using System.Diagnostics;
using Ch08.Orders;
using Ch08.Orders.After;

var ana = new Customer(1, "Ana", "ana@example.com", CustomerLevel.Gold);
var ben = new Customer(2, "Ben", "ben@example.com", CustomerLevel.Regular);
Order[] orders =
[
    new(ana, [new("MUG", 2, 8.50m), new("TEE", 1, 15m)], Coupon: "SAVE5"),
    new(ben, [new("CAP", 1, 12m)]),
    new(ben, [new("TEE", 2, 15m)], Card: "4000000000000002"),
];

string mode = args.FirstOrDefault() ?? "before";
var watch = Stopwatch.StartNew();
switch (mode)
{
    case "before":
        foreach (var order in orders)
        {
            Console.WriteLine($"{order.Customer.Name}'s order:");
            Console.WriteLine("  " + new Ch08.Orders.Before.OrderProcessor().Process(order));
        }
        break;

    case "after":
    case "blackfriday":
        TimeProvider time = mode == "after" ? TimeProvider.System : new FixedDay(new DateOnly(2026, 11, 27));
        var shop = Shop.Create(time, Console.Out);
        foreach (var order in orders)
        {
            Console.WriteLine($"{order.Customer.Name}'s order:");
            var result = shop.Place(order);
            Console.WriteLine(result.Ok ? $"  OK: order {result.OrderNumber}, charged {result.Total:0.00}"
                                        : "  ERROR: " + string.Join("; ", result.Problems));
        }
        break;

    case "refunds":
        Console.WriteLine("Good version: a card and a gift card, refund 10.00 where possible");
        var card2 = new Ch08.Orders.Payments.Good.CardPayment();
        var left = Ch08.Orders.Payments.Good.Refunds.RefundWherePossible([card2, new Ch08.Orders.Payments.Good.GiftCardPayment()], 10m);
        Console.WriteLine($"  {string.Join(", ", card2.History)}; not refundable: {string.Join(", ", left.Select(p => p.GetType().Name))}");
        Console.WriteLine("Bad version: the same, through PaymentMethod.Refund");
        var card = new Ch08.Orders.Payments.Bad.CardPayment();
        Ch08.Orders.Payments.Bad.Refunds.RefundAll([card, new Ch08.Orders.Payments.Bad.GiftCardPayment()], 10m);
        Console.WriteLine("  (never printed)");
        break;

    case "framework":
        IList<int> numbers = new[] { 1, 2, 3 };       // an array IS an IList<int>: it compiles
        numbers[0] = 10;
        Console.WriteLine($"numbers[0] = 10 works: {string.Join(", ", numbers)}");
        Console.WriteLine($"IsReadOnly says: {numbers.IsReadOnly}");
        IReadOnlyList<int> readOnly = numbers.AsReadOnly();   // the smaller promise (since .NET Framework 4.5)
        Console.WriteLine($"As an IReadOnlyList<int>: Count = {readOnly.Count}, no Add method to call");
        numbers.Add(4);                              // IList<int> promises Add...
        Console.WriteLine("(never printed)");
        break;
}
Console.WriteLine($"Took {watch.Elapsed.TotalSeconds:0.0} s");

// A clock stopped at 10:00 on one day: for the demo. (Tests use FakeTimeProvider.)
sealed class FixedDay(DateOnly day) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(day.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
