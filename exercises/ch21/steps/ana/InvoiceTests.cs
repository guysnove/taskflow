namespace Ch21.Invoices.Tests;

public class InvoiceTests
{
    [Fact]
    public void AnEmptyInvoice_CostsNothing() => Assert.Equal(0m, new Invoice().Total());

    [Fact]
    public void TheTotal_IsTheLinesPlusTwentyPercentVat()
    {
        var invoice = new Invoice();
        invoice.Lines.Add(new InvoiceLine("Pens", 1.50m, 4));
        invoice.Lines.Add(new InvoiceLine("Paper", 4.00m, 1));

        Assert.Equal(12.00m, invoice.Total());          // (6.00 + 4.00) * 1.2
    }

    [Fact]
    public void ADiscount_ComesOffBeforeVat()
    {
        var invoice = new Invoice { DiscountPercent = 10 };
        invoice.Lines.Add(new InvoiceLine("Desk", 100.00m, 1));

        Assert.Equal(108.00m, invoice.Total());         // (100 - 10) * 1.2
    }
}
