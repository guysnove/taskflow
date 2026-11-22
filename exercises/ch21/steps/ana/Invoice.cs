namespace Ch21.Invoices;

// Chapter 21: a tiny invoice, shared by a small team (Ana and Ben) through one Git repository.
public record InvoiceLine(string Description, decimal UnitPrice, int Quantity)
{
    public decimal Amount => UnitPrice * Quantity;
}

public class Invoice
{
    public List<InvoiceLine> Lines { get; } = [];
    public decimal DiscountPercent { get; set; }        // e.g. 10 for 10% off the lines

    // What the customer pays: the lines, plus VAT, rounded to pennies.
    public decimal Total()
    {
        decimal net = Lines.Sum(line => line.Amount);
        net -= net * DiscountPercent / 100;
        decimal vat = net * 0.20m;
        return Math.Round(net + vat, 2);
    }
}
