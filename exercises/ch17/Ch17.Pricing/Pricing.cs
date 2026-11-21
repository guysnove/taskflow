namespace Ch17.Pricing;

public record OrderLine(string Sku, decimal UnitPrice, int Quantity);
public record Order(string CustomerId, List<OrderLine> Lines, string? Coupon = null);
public record Coupon(string Code, decimal Percent, DateOnly Expires);
public record Quote(decimal Subtotal, decimal DiscountPercent, decimal Total);

// The things the calculator needs from the outside world: each one an interface,
// so a test can give it a stand-in (a "test double").
public interface ICouponStore { Task<Coupon?> FindAsync(string code); }
public interface IMemberDirectory { Task<bool> IsMemberAsync(string customerId); }
public interface IReceiptSender { Task SendAsync(string customerId, decimal total); }

// The business rules of a small shop:
//   - bulk: 10% off a line when you buy 10 or more of it
//   - a coupon: its percentage, if it hasn't expired (valid ON its expiry date)
//   - members: 5% more
//   - coupon + member together: at most 25% off the order
//   - money is rounded to cents, halves away from zero
public class PriceCalculator(ICouponStore coupons, IMemberDirectory members, TimeProvider time)
{
    public const decimal MaxOrderDiscount = 25m;

    public async Task<Quote> QuoteAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Lines.Count == 0) throw new ArgumentException("An order needs at least one line.", nameof(order));

        decimal subtotal = order.Lines.Sum(l => l.UnitPrice * l.Quantity * (l.Quantity >= 10 ? 0.9m : 1m));

        decimal percent = 0;
        if (order.Coupon is string code && await coupons.FindAsync(code) is Coupon coupon
            && DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime) <= coupon.Expires)
        {
            percent += coupon.Percent;
        }
        if (await members.IsMemberAsync(order.CustomerId))
        {
            percent += 5;
        }
        percent = Math.Min(percent, MaxOrderDiscount);

        decimal total = Math.Round(subtotal * (100 - percent) / 100, 2, MidpointRounding.AwayFromZero);
        return new Quote(Math.Round(subtotal, 2, MidpointRounding.AwayFromZero), percent, total);
    }
}

// Checkout: price the order, then send a receipt. The receipt is an OUTPUT we can't see in the
// return value — that's where a spy or a mock helps.
public class Checkout(PriceCalculator calculator, IReceiptSender receipts)
{
    public async Task<decimal> PayAsync(Order order)
    {
        var quote = await calculator.QuoteAsync(order);
        await receipts.SendAsync(order.CustomerId, quote.Total);
        return quote.Total;
    }
}
