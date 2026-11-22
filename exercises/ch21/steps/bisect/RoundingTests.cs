namespace Ch21.Invoices.Tests;

// Bug report #7: "3 items at 0.125 should cost 0.45 (0.375 + VAT), but the invoice says 0.43."
// Written AFTER the bug was reported, so older commits don't have it: bisect.sh copies it in.
public class RoundingTests
{
    [Fact]
    public void SmallPrices_AreRoundedOnlyOnce_AtTheEnd()
    {
        var invoice = new Invoice();
        invoice.Lines.Add(new InvoiceLine("Screw", 0.125m, 1));
        invoice.Lines.Add(new InvoiceLine("Nut", 0.125m, 1));
        invoice.Lines.Add(new InvoiceLine("Washer", 0.125m, 1));

        Assert.Equal(0.45m, invoice.Total());
    }
}
