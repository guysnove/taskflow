namespace Library.Domain;

// DOMAIN: the business ideas and their rules. No database, no web, no packages.
public sealed class Loan
{
    public const int MaxDays = 21;

    public Loan(string bookTitle, string member, DateOnly lentOn)
    {
        if (string.IsNullOrWhiteSpace(bookTitle)) throw new ArgumentException("A loan needs a book.", nameof(bookTitle));
        if (string.IsNullOrWhiteSpace(member)) throw new ArgumentException("A loan needs a member.", nameof(member));
        BookTitle = bookTitle.Trim();
        Member = member.Trim();
        LentOn = lentOn;
    }

    public string BookTitle { get; }
    public string Member { get; }
    public DateOnly LentOn { get; }
    public DateOnly? ReturnedOn { get; private set; }

    public DateOnly DueOn => LentOn.AddDays(MaxDays);
    public bool IsOverdue(DateOnly today) => ReturnedOn is null && today > DueOn;

    public void Return(DateOnly today)
    {
        if (ReturnedOn is not null) throw new InvalidOperationException($"\"{BookTitle}\" was already returned.");
        ReturnedOn = today;
    }
}
