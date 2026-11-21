using Ch14.Data;
using Microsoft.Data.Sqlite;

namespace Ch14.Data.Tests;

public class DataTests
{
    private static string Db(string name) => Path.Combine(Path.GetTempPath(), $"ch14-test-{name}-{Guid.NewGuid():N}.db");

    [Fact]
    public void Normalised_OneUpdate_ChangesTheFactEverywhere()
    {
        using var c = Sql.Open(Db("normal"));
        c.Exec(Normalisation.Normal);
        c.Exec("UPDATE Customers SET Email = 'ana@new-mail.com' WHERE Id = 1");
        Assert.Equal(["ana@new-mail.com"], c.Rows("SELECT DISTINCT c.Email FROM Orders o JOIN Customers c ON c.Id = o.CustomerId"));
    }

    [Theory]
    [InlineData("INSERT INTO Orders (CustomerId, Product, Qty) VALUES (1, 'Pen', 0)", "CHECK")]
    [InlineData("INSERT INTO Orders (CustomerId, Product, Qty) VALUES (99, 'Pen', 1)", "FOREIGN KEY")]
    [InlineData("INSERT INTO Customers (Name, Email) VALUES ('Ana 2', 'ana@example.com')", "UNIQUE")]
    public void Constraints_RefuseBadData(string sql, string expected)
    {
        using var c = Sql.Open(Db("constraints"));
        c.Exec(Normalisation.Normal);
        Assert.Contains(expected, Assert.Throws<SqliteException>(() => c.Exec(sql)).Message);
    }

    [Theory]
    [InlineData(false, 70)]          // money vanished
    [InlineData(true, 100)]          // rolled back
    public void ACrashHalfway_ThroughATransfer(bool useTransaction, long expectedTotal)
    {
        using var c = Sql.Open(Db("bank"));
        c.Exec(Bank.Schema);
        Assert.Throws<IOException>(() => Bank.Transfer(c, "Ana", "Ben", 30, crashHalfway: true, useTransaction));
        Assert.Equal(expectedTotal, Bank.Total(c));
    }

    [Fact]
    public void LostUpdate_ThenOptimistic_ThenAtomic()
    {
        string path = Db("shop");
        Stock.Reset(path);
        Assert.Equal(6, Stock.TwoSales(path, checkedVersion: false, 3, 4).Final);      // a sale was lost
        Stock.Reset(path);
        var (final, error, _) = Stock.TwoSales(path, checkedVersion: true, 3, 4);
        Assert.Equal(7, final);                                                        // the second sale was refused…
        Assert.NotNull(error);                                                         // …and the caller knows
        Stock.Reset(path);
        Assert.Equal(3, Stock.TwoAtomicSales(path, 3, 4));                             // both counted
    }
}
