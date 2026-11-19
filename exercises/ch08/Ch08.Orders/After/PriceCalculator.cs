namespace Ch08.Orders.After;

public sealed record Saving(string Rule, decimal Amount);
public sealed record Price(decimal Subtotal, IReadOnlyList<Saving> Savings, decimal Total);

// One job: the price. Today's date comes from a TimeProvider, so a test can choose the day.
public class PriceCalculator
{
    private readonly IReadOnlyList<IDiscountRule> _rules;
    private readonly TimeProvider _time;

    public PriceCalculator(IEnumerable<IDiscountRule> rules, TimeProvider time)
    {
        _rules = rules.ToList();
        _time = time;
    }

    public Price PriceOf(Order order)
    {
        decimal subtotal = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        decimal total = subtotal;
        var savings = new List<Saving>();
        foreach (var rule in _rules)               // in order: level, coupon, Black Friday
        {
            decimal after = rule.Apply(order, total, today);
            if (after != total)
            {
                savings.Add(new Saving(rule.Name, Math.Round(total - after, 2)));
            }
            total = after;
        }
        return new Price(subtotal, savings, Math.Round(total, 2));
    }
}
