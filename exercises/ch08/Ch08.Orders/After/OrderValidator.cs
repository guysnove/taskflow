namespace Ch08.Orders.After;

// One job: say what's wrong with an order. It reads the stock, it doesn't change it.
public class OrderValidator
{
    private readonly IStock _stock;

    public OrderValidator(IStock stock) => _stock = stock;

    public List<string> Problems(Order order)
    {
        var problems = new List<string>();
        if (order.Lines.Count == 0)
        {
            problems.Add("the order is empty");
        }
        foreach (var line in order.Lines)
        {
            int available = _stock.Available(line.Sku);
            if (line.Quantity <= 0)
            {
                problems.Add($"the quantity of {line.Sku} must be at least 1");
            }
            else if (available < 0)
            {
                problems.Add($"there is no product {line.Sku}");
            }
            else if (available < line.Quantity)
            {
                problems.Add($"only {available} {line.Sku} left");
            }
        }
        return problems;                           // every problem, not just the first
    }
}
