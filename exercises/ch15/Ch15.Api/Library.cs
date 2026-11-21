namespace Ch15.Api;

// The resource: a book in a small library. Stored in memory, so every run starts the same.
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public List<string> Authors { get; set; } = [];
    public int Year { get; set; }
    public int Copies { get; set; }
}

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string Member { get; set; } = "";
    public DateOnly Due { get; set; }
}

// Thread-safe: many requests can arrive at the same moment, so they take turns (lock).
public class Library
{
    private readonly Lock _lock = new();
    private readonly List<Book> _books =
    [
        new() { Id = 1, Title = "Clean Code", Authors = ["Robert C. Martin"], Year = 2008, Copies = 2 },
        new() { Id = 2, Title = "Refactoring", Authors = ["Martin Fowler"], Year = 2018, Copies = 1 },
        new() { Id = 3, Title = "Domain-Driven Design", Authors = ["Eric Evans"], Year = 2003, Copies = 1 },
        new() { Id = 4, Title = "Designing Data-Intensive Applications", Authors = ["Martin Kleppmann"], Year = 2017, Copies = 3 },
        new() { Id = 5, Title = "The Pragmatic Programmer", Authors = ["David Thomas", "Andrew Hunt"], Year = 2019, Copies = 2 },
        new() { Id = 6, Title = "Accelerate", Authors = ["Nicole Forsgren", "Jez Humble", "Gene Kim"], Year = 2018, Copies = 1 },
        new() { Id = 7, Title = "Working Effectively with Legacy Code", Authors = ["Michael Feathers"], Year = 2004, Copies = 1 },
        new() { Id = 8, Title = "Release It!", Authors = ["Michael T. Nygard"], Year = 2018, Copies = 0 },
    ];
    private readonly List<Loan> _loans = [];
    private int _nextBookId = 9, _nextLoanId = 1;

    // Filter, then a total order (Id), then one page.
    public (List<Book> Items, int Total) Find(string? author, bool? available, int page, int pageSize)
    {
        lock (_lock)
        {
            var matching = _books.Where(b => author is null || b.Authors.Any(a => a.Contains(author, StringComparison.OrdinalIgnoreCase)))
                                 .Where(b => available is null || (b.Copies > 0) == available)
                                 .OrderBy(b => b.Id)
                                 .ToList();
            return (matching.Skip((page - 1) * pageSize).Take(pageSize).ToList(), matching.Count);
        }
    }

    public Book? Get(int id) { lock (_lock) return _books.FirstOrDefault(b => b.Id == id); }

    public Book Add(string title, List<string> authors, int year, int copies)
    {
        lock (_lock)
        {
            var book = new Book { Id = _nextBookId++, Title = title, Authors = authors, Year = year, Copies = copies };
            _books.Add(book);
            return book;
        }
    }

    public bool Remove(int id) { lock (_lock) return _books.RemoveAll(b => b.Id == id) == 1; }

    public Loan? Borrow(int bookId, string member, DateOnly today)
    {
        lock (_lock)
        {
            var book = _books.FirstOrDefault(b => b.Id == bookId);
            if (book is null || book.Copies == 0) return null;
            book.Copies--;
            var loan = new Loan { Id = _nextLoanId++, BookId = bookId, Member = member, Due = today.AddDays(21) };
            _loans.Add(loan);
            return loan;
        }
    }

    public int LoanCount { get { lock (_lock) return _loans.Count; } }
}
