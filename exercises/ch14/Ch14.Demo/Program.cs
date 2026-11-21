// Chapter 14 demo: relational data, for real, in SQLite.
//   dotnet run -- anomaly | constraints | index | transaction | isolation | lostupdate
using System.Diagnostics;
using Ch14.Data;
using Microsoft.Data.Sqlite;

string Db(string name) => Path.Combine(Path.GetTempPath(), $"ch14-{name}.db");

switch (args.FirstOrDefault())
{
    case "anomaly":
        {
            using var flat = Sql.Open(Db("flat"));
            flat.Exec(Normalisation.Flat);
            flat.Exec("UPDATE Orders SET CustomerEmail = 'ana@new-mail.com' WHERE Id = 1");     // Ana tells us her new email…
            Console.WriteLine("Flat table — Ana's email(s) after one UPDATE:");
            foreach (var r in flat.Rows("SELECT DISTINCT Customer, CustomerEmail FROM Orders")) Console.WriteLine("  " + r);

            using var normal = Sql.Open(Db("normal"));
            normal.Exec(Normalisation.Normal);
            normal.Exec("UPDATE Customers SET Email = 'ana@new-mail.com' WHERE Id = 1");          // one fact, one place
            Console.WriteLine("Normalised — Ana's email(s) after one UPDATE:");
            foreach (var r in normal.Rows("SELECT DISTINCT c.Name, c.Email FROM Orders o JOIN Customers c ON c.Id = o.CustomerId")) Console.WriteLine("  " + r);
            break;
        }
    case "constraints":
        {
            using var c = Sql.Open(Db("normal"));
            c.Exec(Normalisation.Normal);
            Console.WriteLine("NOT NULL : " + c.Try("INSERT INTO Customers (Name, Email) VALUES (NULL, 'x@example.com')"));
            Console.WriteLine("UNIQUE   : " + c.Try("INSERT INTO Customers (Name, Email) VALUES ('Ana 2', 'ana@example.com')"));
            Console.WriteLine("CHECK    : " + c.Try("INSERT INTO Orders (CustomerId, Product, Qty) VALUES (1, 'Pen', 0)"));
            Console.WriteLine("FOREIGN  : " + c.Try("INSERT INTO Orders (CustomerId, Product, Qty) VALUES (99, 'Pen', 1)"));
            Console.WriteLine("DELETE   : " + c.Try("DELETE FROM Customers WHERE Id = 1"));
            Console.WriteLine("Valid    : " + c.Try("INSERT INTO Orders (CustomerId, Product, Qty) VALUES (1, 'Pen', 3)"));
            break;
        }
    case "index":
        {
            using var c = Sql.Open(Db("index"));
            c.Exec("CREATE TABLE Tasks (Id INTEGER PRIMARY KEY, UserId INTEGER NOT NULL, Due TEXT, Done INTEGER NOT NULL)");
            using (var tx = c.BeginTransaction())
            {
                using var cmd = c.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO Tasks (UserId, Due, Done) VALUES ($u, $d, $done)";
                var u = cmd.Parameters.Add("$u", SqliteType.Integer); var d = cmd.Parameters.Add("$d", SqliteType.Text); var done = cmd.Parameters.Add("$done", SqliteType.Integer);
                var rnd = new Random(14);
                for (int i = 0; i < 200_000; i++)
                {
                    u.Value = rnd.Next(1, 2001); d.Value = new DateOnly(2026, 1, 1).AddDays(rnd.Next(0, 730)).ToString("yyyy-MM-dd"); done.Value = rnd.Next(0, 2);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            const string Q = "SELECT COUNT(*) FROM Tasks WHERE UserId = 42 AND Done = 0 AND Due < '2026-11-19'";
            void Measure(string label)
            {
                Console.WriteLine($"{label}:");
                foreach (var r in c.Rows("EXPLAIN QUERY PLAN " + Q)) Console.WriteLine("  plan: " + r.Split(" | ").Last());
                c.Scalar(Q);                                                    // warm up
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 50; i++) c.Scalar(Q);
                Console.WriteLine($"  answer: {c.Scalar(Q)}   time: {sw.Elapsed.TotalMilliseconds / 50:0.000} ms per query");
            }
            Console.WriteLine("200,000 tasks, 2,000 users.");
            Measure("No index");
            c.Exec("CREATE INDEX IX_Tasks_UserId_Due ON Tasks (UserId, Due)");
            Measure("With an index on (UserId, Due)");
            break;
        }
    case "transaction":
        {
            foreach (bool useTx in new[] { false, true })
            {
                using var c = Sql.Open(Db("bank"));
                c.Exec(Bank.Schema);
                try { Bank.Transfer(c, "Ana", "Ben", 30, crashHalfway: true, useTransaction: useTx); }
                catch (IOException ex) { Console.Write($"{(useTx ? "With" : "Without")} a transaction: \"{ex.Message}\" → "); }
                Console.WriteLine($"{string.Join(", ", c.Rows("SELECT Name, Balance FROM Accounts"))}; total {Bank.Total(c)}");
            }
            break;
        }
    case "isolation":
        {
            using var setup = Sql.Open(Db("iso"));
            setup.Exec("PRAGMA journal_mode = WAL;");
            setup.Exec(Bank.Schema);
            using var reader = Sql.Open(Db("iso"), fresh: false);
            using var writer = Sql.Open(Db("iso"), fresh: false);

            using var readTx = reader.BeginTransaction(deferred: true);
            Console.WriteLine($"Reader, in a transaction, sees Ana = {reader.Scalar("SELECT Balance FROM Accounts WHERE Name='Ana'", readTx)}");
            writer.Exec("UPDATE Accounts SET Balance = 70 WHERE Name = 'Ana'");                 // committed at once
            Console.WriteLine("Writer committed Ana = 70.");
            Console.WriteLine($"Reader, same transaction, sees Ana = {reader.Scalar("SELECT Balance FROM Accounts WHERE Name='Ana'", readTx)}   (a snapshot)");
            readTx.Commit();
            Console.WriteLine($"Reader, new transaction, sees Ana = {reader.Scalar("SELECT Balance FROM Accounts WHERE Name='Ana'")}");

            using var first = writer.BeginTransaction();                                         // BEGIN IMMEDIATE: takes the write lock
            writer.Exec("UPDATE Accounts SET Balance = 60 WHERE Name = 'Ana'", first);
            reader.DefaultTimeout = 1;
            var sw = Stopwatch.StartNew();
            try { reader.Exec("UPDATE Accounts SET Balance = 50 WHERE Name = 'Ana'"); }
            catch (SqliteException ex) { Console.WriteLine($"A second writer, after waiting {sw.Elapsed.TotalSeconds:0.0} s: {ex.Message}"); }
            first.Commit();
            break;
        }
    case "lostupdate":
        {
            var path = Stock.Path("shop");
            Stock.Reset(path);
            var (final, _, sql1) = Stock.TwoSales(path, checkedVersion: false, first: 3, second: 4);
            Console.WriteLine($"No check:   10 lamps, sold 3 and 4 → stock {final}   (should be 3)");
            Console.WriteLine($"  SQL: {sql1}");

            Stock.Reset(path);
            var (final2, error, sql2) = Stock.TwoSales(path, checkedVersion: true, first: 3, second: 4);
            Console.WriteLine($"Optimistic: second save refused → stock {final2}");
            Console.WriteLine($"  SQL: {sql2}");
            Console.WriteLine($"  {error![..error!.IndexOf(';')]}");

            Stock.Reset(path);
            Console.WriteLine($"Atomic:     UPDATE … SET Stock = Stock - n, twice → stock {Stock.TwoAtomicSales(path, 3, 4)}");
            break;
        }
    default:
        Console.WriteLine("Usage: anomaly | constraints | index | transaction | isolation | lostupdate");
        break;
}
