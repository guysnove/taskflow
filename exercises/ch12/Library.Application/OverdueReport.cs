namespace Library.Application;

// A second USE CASE: which loans are overdue today?
public sealed class OverdueReport(ILoanStore store, TimeProvider time)
{
    public async Task<List<string>> RunAsync()
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().Date);
        return (await store.AllAsync())
            .Where(l => l.IsOverdue(today))
            .Select(l => $"{l.BookTitle} — {l.Member}, due {l.DueOn:yyyy-MM-dd}")
            .ToList();
    }
}
