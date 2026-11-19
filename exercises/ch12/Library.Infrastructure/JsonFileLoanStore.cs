using System.Text.Json;
using Library.Application;
using Library.Domain;

namespace Library.Infrastructure;

// A second ADAPTER for the same port: loans kept in a JSON file. The use cases don't know or care.
public sealed class JsonFileLoanStore(string path) : ILoanStore
{
    private sealed record Row(string Book, string Member, DateOnly LentOn, DateOnly? ReturnedOn);

    private List<Loan>? _loans;

    public async Task<List<Loan>> AllAsync() => (_loans ??= await LoadAsync()).ToList();

    public async Task AddAsync(Loan loan) => (_loans ??= await LoadAsync()).Add(loan);

    public async Task SaveAsync()
    {
        var rows = (_loans ?? []).Select(l => new Row(l.BookTitle, l.Member, l.LentOn, l.ReturnedOn));
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<List<Loan>> LoadAsync()
    {
        if (!File.Exists(path)) return [];
        var rows = JsonSerializer.Deserialize<List<Row>>(await File.ReadAllTextAsync(path)) ?? [];
        return rows.Select(r =>
        {
            var loan = new Loan(r.Book, r.Member, r.LentOn);
            if (r.ReturnedOn is DateOnly returned) loan.Return(returned);
            return loan;
        }).ToList();
    }
}
