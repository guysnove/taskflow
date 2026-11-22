"""Chapter 21: six more commits on main, by Ana and Ben, one of which (no one noticed) breaks rounding.
Run inside a clone: python3 ../../steps/history.py   (used by the video, and by exercise Independent A)."""
import os, subprocess, re

def commit(author, email, message, edits):
    for path, old, new in edits:
        text = open(path).read(); assert old in text, (path, old); open(path, "w").write(text.replace(old, new, 1))
    env = dict(os.environ, GIT_AUTHOR_NAME=author, GIT_AUTHOR_EMAIL=email, GIT_COMMITTER_NAME=author, GIT_COMMITTER_EMAIL=email)
    subprocess.run(["git", "commit", "-qam", message], env=env, check=True)

I = "Ch21.Invoices/Invoice.cs"
commit("Ben Okafor", "ben@example.com", "Add a reference number to invoices",
       [(I, "    public List<InvoiceLine> Lines { get; } = [];\n", "    public List<InvoiceLine> Lines { get; } = [];\n    public string Number { get; set; } = \"\";             // e.g. INV-2026-0042\n")])
commit("Ana Lopez", "ana@example.com", "Explain why VAT is added after the discount",
       [(I, "        net -= net * DiscountPercent / 100;\n", "        net -= net * DiscountPercent / 100;          // discount first: VAT is charged on what is paid\n")])
commit("Ben Okafor", "ben@example.com", "Round each line to pennies, like the printed invoice",
       [(I, "    public decimal Amount => UnitPrice * Quantity;", "    public decimal Amount => Math.Round(UnitPrice * Quantity, 2);")])
commit("Ana Lopez", "ana@example.com", "Add LineCount for the invoice summary",
       [(I, "    // What the customer pays", "    public int LineCount => Lines.Count;\n\n    // What the customer pays")])
commit("Ben Okafor", "ben@example.com", "Name the VAT calculation",
       [(I, "        decimal vat = net * VatRate;\n        return Math.Round(net + vat, 2);", "        return Math.Round(net + Vat(net), 2);"),
        (I, "    // What the customer pays", "    private decimal Vat(decimal net) => net * VatRate;\n\n    // What the customer pays")])
commit("Ana Lopez", "ana@example.com", "Allow a note on each invoice",
       [(I, "    public int LineCount => Lines.Count;", "    public int LineCount => Lines.Count;\n    public string? Note { get; set; }")])
