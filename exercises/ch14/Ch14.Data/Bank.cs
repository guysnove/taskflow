using Microsoft.Data.Sqlite;

namespace Ch14.Data;

// Transactions: move money between two accounts. The "crash" happens between the two UPDATEs.
public static class Bank
{
    public const string Schema = """
        CREATE TABLE Accounts (Name TEXT PRIMARY KEY, Balance INTEGER NOT NULL CHECK (Balance >= 0));
        INSERT INTO Accounts VALUES ('Ana', 100), ('Ben', 0);
        """;

    public static void Transfer(SqliteConnection c, string from, string to, int amount, bool crashHalfway, bool useTransaction)
    {
        using var tx = useTransaction ? c.BeginTransaction() : null;
        Move(c, tx, from, -amount);
        if (crashHalfway) throw new IOException("The server lost power.");      // simulated failure
        Move(c, tx, to, +amount);
        tx?.Commit();                                                          // all or nothing
    }

    private static void Move(SqliteConnection c, SqliteTransaction? tx, string name, int change)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE Accounts SET Balance = Balance + $change WHERE Name = $name";   // parameters, never string glue
        cmd.Parameters.AddWithValue("$change", change);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public static long Total(SqliteConnection c) => (long)c.Scalar("SELECT SUM(Balance) FROM Accounts")!;
}
