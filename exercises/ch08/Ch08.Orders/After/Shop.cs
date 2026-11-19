namespace Ch08.Orders.After;

// The "composition root": the one place that chooses the real parts and plugs them together.
// (Chapter 13 hands this job to the dependency-injection container, as TaskFlow's Program.cs does.)
public static class Shop
{
    public static OrderService Create(TimeProvider time, TextWriter log)
    {
        var stock = new InMemoryStock(new() { ["MUG"] = 10, ["TEE"] = 5, ["CAP"] = 0 });
        IDiscountRule[] rules = [new LevelDiscount(), new CouponDiscount(), new BlackFridayDiscount()];
        INotifier notifier = new LoggingNotifier(new AllNotifiers(new EmailNotifier(), new SmsNotifier()), log);
        return new OrderService(new OrderValidator(stock), new PriceCalculator(rules, time),
                                new CardGatewayAdapter(), stock, new InMemoryOrderStore(), notifier);
    }
}
