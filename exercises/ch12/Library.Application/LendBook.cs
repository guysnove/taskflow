using Library.Domain;

namespace Library.Application;

// A USE CASE: one thing the application does. It applies the rules and uses the port.
public sealed class LendBook(ILoanStore store, TimeProvider time)
{
    public const int MaxLoansPerMember = 3;

    public async Task<Loan> RunAsync(string bookTitle, string member)
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().Date);
        var loans = await store.AllAsync();
        if (loans.Any(l => l.ReturnedOn is null && l.BookTitle.Equals(bookTitle.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"\"{bookTitle}\" is already lent out.");
        if (loans.Count(l => l.ReturnedOn is null && l.Member == member.Trim()) >= MaxLoansPerMember)
            throw new InvalidOperationException($"{member} already has {MaxLoansPerMember} books.");
        if (loans.Any(l => l.Member == member.Trim() && l.IsOverdue(today)))
            throw new InvalidOperationException($"{member} has an overdue book.");

        var loan = new Loan(bookTitle, member, today);
        await store.AddAsync(loan);
        await store.SaveAsync();
        return loan;
    }
}
