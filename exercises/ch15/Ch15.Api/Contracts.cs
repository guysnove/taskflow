namespace Ch15.Api;

// DTOs: what the API promises, separate from the Book class it's stored as.
// Version 1 (2024): one "author" string. Version 2: a list, because books have several authors.
public record BookV1(int Id, string Title, string Author, int Year, bool Available)
{
    public static BookV1 From(Book b) => new(b.Id, b.Title, string.Join(", ", b.Authors), b.Year, b.Copies > 0);
}

public record BookV2(int Id, string Title, List<string> Authors, int Year, int CopiesAvailable)
{
    public static BookV2 From(Book b) => new(b.Id, b.Title, b.Authors, b.Year, b.Copies);
}

// v2 lists come in an envelope: the items, and where you are.
public record PageOf<T>(List<T> Items, int Page, int PageSize, int Total, string? Next);

// What a client sends to create a book. Everything nullable: we check it ourselves and say what's wrong.
public record NewBook(string? Title, List<string>? Authors, int? Year, int? Copies);

public record NewLoan(int BookId, string? Member);
