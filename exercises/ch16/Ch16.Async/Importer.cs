namespace Ch16.Async;

// Error classification: an order importer meets three kinds of problem.
//   1. Bad input       — expected; report it, skip the line, carry on.          (a FAILURE of the data)
//   2. A transient fault — the store hiccupped; try again, a few times.          (a FAILURE of the world)
//   3. A bug           — our own code is wrong; don't hide it: let it crash loudly. (a BUG)
public record OrderLine(int Number, string Product, int Quantity);

public interface IOrderStore
{
    Task SaveAsync(OrderLine line, CancellationToken cancellationToken = default);
}

// A store that fails now and then, like a real network or database.
public sealed class FlakyStore(int failEvery) : IOrderStore
{
    private int _calls;
    public List<OrderLine> Saved { get; } = [];

    public Task SaveAsync(OrderLine line, CancellationToken cancellationToken = default)
    {
        if (++_calls % failEvery == 0)
        {
            throw new IOException($"store timed out (call {_calls})");          // transient: worth a retry
        }
        Saved.Add(line);
        return Task.CompletedTask;
    }
}

public sealed class Importer(IOrderStore store, Action<string> report)
{
    public async Task<int> ImportAsync(IEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        int imported = 0, lineNumber = 0;
        foreach (string text in lines)
        {
            lineNumber++;
            if (!TryParse(text, out var line, out string? problem))
            {
                report($"line {lineNumber}: skipped — {problem}");                // 1. bad input: expected
                continue;
            }
            await SaveWithRetryAsync(line!, cancellationToken);                // 2. transient faults: retried
            imported++;
        }
        return imported;
    }

    private static bool TryParse(string text, out OrderLine? line, out string? problem)
    {
        line = null; problem = null;
        string[] parts = text.Split(',');
        if (parts.Length != 3) { problem = "expected 3 fields"; return false; }
        if (!int.TryParse(parts[0], out int number)) { problem = $"\"{parts[0]}\" is not an order number"; return false; }
        if (!int.TryParse(parts[2], out int quantity) || quantity <= 0) { problem = $"\"{parts[2]}\" is not a quantity"; return false; }
        line = new OrderLine(number, parts[1].Trim(), quantity);
        return true;
    }

    private async Task SaveWithRetryAsync(OrderLine line, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await store.SaveAsync(line, cancellationToken);
                return;
            }
            catch (IOException ex) when (attempt < 3)                          // only what's worth retrying
            {
                report($"order {line.Number}: {ex.Message} — retrying ({attempt} of 3)");
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);   // back off a little more each time
            }
        }
    }
}
