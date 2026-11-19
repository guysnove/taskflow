namespace Ch08.Orders.Before;

// Chapter 8, BEFORE: one class does everything an order needs.
// It works. But count its reasons to change: the checks, the prices, the discounts,
// the payment company, the stock, the saving, the email. Seven.
public class OrderProcessor
{
    // Shared by every OrderProcessor in the program (static): and by every test.
    public static Dictionary<string, int> Stock = new() { ["MUG"] = 10, ["TEE"] = 5, ["CAP"] = 0 };
    public static List<string> SavedOrders = new();

    public string Process(Order order)
    {
        // 1. Check the order.
        if (order.Lines.Count == 0)
        {
            return "ERROR: the order is empty";
        }
        foreach (var line in order.Lines)
        {
            if (line.Quantity <= 0)
            {
                return $"ERROR: the quantity of {line.Sku} must be at least 1";
            }
            if (!Stock.ContainsKey(line.Sku))
            {
                return $"ERROR: there is no product {line.Sku}";
            }
            if (Stock[line.Sku] < line.Quantity)
            {
                return $"ERROR: only {Stock[line.Sku]} {line.Sku} left";
            }
        }

        // 2. Work out the price, with every discount written in here.
        decimal total = 0;
        foreach (var line in order.Lines)
        {
            total += line.Quantity * line.UnitPrice;
        }
        if (order.Customer.Level == CustomerLevel.Gold)
        {
            total = total * 0.90m;             // Gold customers: 10% off
        }
        else if (order.Customer.Level == CustomerLevel.Staff)
        {
            total = total * 0.70m;             // staff: 30% off
        }
        if (order.Coupon == "SAVE5" && total >= 20)
        {
            total = total - 5;
        }
        var now = DateTime.Now;                // Black Friday: 20% off, says the real clock
        if (now.Month == 11 && now.DayOfWeek == DayOfWeek.Friday && now.Day >= 23 && now.Day <= 29)
        {
            total = total * 0.80m;
        }
        total = Math.Round(total, 2);

        // 3. Take the money: always the real payment company.
        var gateway = new CardGateway();
        if (!gateway.Charge(order.Card, total))
        {
            return "ERROR: the card was declined";
        }

        // 4. Take the products off the shelf.
        foreach (var line in order.Lines)
        {
            Stock[line.Sku] -= line.Quantity;
        }

        // 5. Save the order.
        SavedOrders.Add($"{order.Customer.Name}: {total:0.00}");

        // 6. Email the customer: always by SMTP.
        var mailer = new SmtpMailer();
        mailer.Send(order.Customer.Email, "Your order",
                    $"Thank you, {order.Customer.Name}! You paid {total:0.00}.");

        return $"OK: charged {total:0.00}";
    }
}
