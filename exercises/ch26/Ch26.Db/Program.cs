// Chapter 26: the N+1 problem, measured. "500 orders with their customer's name", three ways:
//   N+1   one query for the orders, then one query PER ORDER for its customer (501 queries)
//   IN    one query for the orders, one for all their customers (2 queries)
//   JOIN  one query that returns both (1 query)
// Run against SQLite (a file, inside this process) and PostgreSQL (a server: here, in a container):
//   dotnet run -c Release --project Ch26.Db -- sqlite
//   dotnet run -c Release --project Ch26.Db -- postgres "Host=localhost;Port=55433;Username=postgres;Password=…;Database=shop"
using System.Data.Common;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Npgsql;

string kind = args.FirstOrDefault() ?? "sqlite";
await using DbConnection db = kind == "postgres" ? new NpgsqlConnection(args[1]) : new SqliteConnection("Data Source=ch26-shop.db");
await db.OpenAsync();
int queries = 0;

async Task Exec(string sql) { await using var c = db.CreateCommand(); c.CommandText = sql; await c.ExecuteNonQueryAsync(); }
async Task<List<object[]>> Rows(string sql)
{
    queries++;
    await using var c = db.CreateCommand(); c.CommandText = sql;
    await using var r = await c.ExecuteReaderAsync();
    var rows = new List<object[]>();
    while (await r.ReadAsync()) { var row = new object[r.FieldCount]; r.GetValues(row); rows.Add(row); }
    return rows;
}

// The same data every time: 200 customers, 2,000 orders.
await Exec("DROP TABLE IF EXISTS orders; DROP TABLE IF EXISTS customers;");
await Exec("CREATE TABLE customers (id int PRIMARY KEY, name varchar(50) NOT NULL);");
await Exec("CREATE TABLE orders (id int PRIMARY KEY, customer_id int NOT NULL REFERENCES customers(id), total int NOT NULL);");
await Exec("INSERT INTO customers (id, name) VALUES " + string.Join(",", Enumerable.Range(1, 200).Select(i => $"({i}, 'Customer {i}')")) + ";");
await Exec("INSERT INTO orders (id, customer_id, total) VALUES " + string.Join(",", Enumerable.Range(1, 2000).Select(i => $"({i}, {i % 200 + 1}, {i * 7 % 500})")) + ";");

async Task<int> NPlusOne()
{
    var orders = await Rows("SELECT id, customer_id, total FROM orders ORDER BY id LIMIT 500");
    int named = 0;
    foreach (var o in orders)
        if ((await Rows($"SELECT name FROM customers WHERE id = {o[1]}")).Count == 1) named++;
    return named;
}
async Task<int> In()
{
    var orders = await Rows("SELECT id, customer_id, total FROM orders ORDER BY id LIMIT 500");
    var ids = string.Join(",", orders.Select(o => o[1]).Distinct());
    var names = (await Rows($"SELECT id, name FROM customers WHERE id IN ({ids})")).ToDictionary(r => Convert.ToInt32(r[0]), r => (string)r[1]);
    return orders.Count(o => names.ContainsKey(Convert.ToInt32(o[1])));
}
async Task<int> Join() => (await Rows("SELECT o.id, o.total, c.name FROM orders o JOIN customers c ON c.id = o.customer_id ORDER BY o.id LIMIT 500")).Count;

Console.WriteLine($"{(kind == "postgres" ? "PostgreSQL (server, in a container)" : "SQLite (a file, in this process)")}: 500 orders with their customer's name, median of 7 runs after 2 warm-ups");
foreach (var (name, run) in new (string, Func<Task<int>>)[] { ("N+1 ", NPlusOne), ("IN  ", In), ("JOIN", Join) })
{
    for (int i = 0; i < 2; i++) await run();                     // warm-up: connection, query plans, caches
    var times = new List<double>(); int rows = 0;
    for (int i = 0; i < 7; i++)
    {
        queries = 0; var sw = Stopwatch.StartNew(); rows = await run(); times.Add(sw.Elapsed.TotalMilliseconds);
    }
    times.Sort();
    Console.WriteLine($"  {name}  {queries,4} queries   {times[3],7:F2} ms   ({rows} orders named)");
}
