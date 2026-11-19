// The COMPOSITION ROOT: the only place that knows every layer, and picks the adapters.
//   dotnet run -- lend "Clean Code" ana        dotnet run -- overdue
using Library.Application;
using Library.Infrastructure;

ILoanStore store = new JsonFileLoanStore("loans.json");     // swap for new InMemoryLoanStore(): one line
var time = TimeProvider.System;

switch (args.FirstOrDefault())
{
    case "lend":
        try
        {
            var loan = await new LendBook(store, time).RunAsync(args[1], args[2]);
            Console.WriteLine($"Lent \"{loan.BookTitle}\" to {loan.Member}, due {loan.DueOn:yyyy-MM-dd}.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Refused: {ex.Message}");
        }
        break;
    case "overdue":
        var overdue = await new OverdueReport(store, time).RunAsync();
        Console.WriteLine(overdue.Count == 0 ? "Nothing overdue." : string.Join(Environment.NewLine, overdue));
        break;
    default:
        Console.WriteLine("Usage: lend <book> <member> | overdue");
        break;
}
