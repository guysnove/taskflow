namespace Ch08.Orders.After;

public sealed record OrderResult(bool Ok, IReadOnlyList<string> Problems, decimal Total = 0, int OrderNumber = 0)
{
    public static OrderResult Failed(params string[] problems) => new(false, problems);
}

// Chapter 8, AFTER: the coordinator. It decides the ORDER of the steps, and nothing else:
// check, price, charge, take the stock, save, tell the customer.
// Everything it needs comes in through the constructor (Dependency Inversion).
public class OrderService
{
    private readonly OrderValidator _validator;
    private readonly PriceCalculator _prices;
    private readonly IPaymentGateway _payments;
    private readonly IStock _stock;
    private readonly IOrderStore _orders;
    private readonly INotifier _notifier;

    public OrderService(OrderValidator validator, PriceCalculator prices, IPaymentGateway payments,
                        IStock stock, IOrderStore orders, INotifier notifier)
    {
        _validator = validator;
        _prices = prices;
        _payments = payments;
        _stock = stock;
        _orders = orders;
        _notifier = notifier;
    }

    public OrderResult Place(Order order)
    {
        var problems = _validator.Problems(order);
        if (problems.Count > 0)
        {
            return new OrderResult(false, problems);
        }

        var price = _prices.PriceOf(order);
        if (!_payments.Charge(order.Card, price.Total))
        {
            return OrderResult.Failed("the card was declined");
        }

        foreach (var line in order.Lines)
        {
            _stock.Take(line.Sku, line.Quantity);
        }
        int number = _orders.Save(order, price.Total);
        _notifier.OrderPlaced(order.Customer, price.Total);
        return new OrderResult(true, [], price.Total, number);
    }
}
