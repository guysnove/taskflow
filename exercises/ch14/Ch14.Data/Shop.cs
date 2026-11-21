using Microsoft.EntityFrameworkCore;

namespace Ch14.Data;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Stock { get; set; }
    public int Version { get; set; }               // used as a concurrency token when checked = true
}

// EF Core context, with no concurrency check.
public class ShopDb(string path) : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public List<string> Log { get; } = new();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={path}")
               .LogTo(s => { if (s.Contains("UPDATE")) Log.Add(s); }, Microsoft.Extensions.Logging.LogLevel.Information);

}

// The same, but Version is a concurrency token: UPDATE … WHERE Id = @id AND Version = @original.
// (A separate class, because EF Core builds the model once per context TYPE and caches it.)
public class CheckedShopDb(string path) : ShopDb(path)
{
    protected override void OnModelCreating(ModelBuilder model) =>
        model.Entity<Product>().Property(p => p.Version).IsConcurrencyToken();
}

public static class Stock
{
    public static string Path(string name) => System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ch14-{name}.db");

    public static void Reset(string path)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(path);
        using var db = new ShopDb(path);
        db.Database.EnsureCreated();
        db.Products.Add(new Product { Name = "Desk lamp", Stock = 10 });
        db.SaveChanges();
    }

    // Two people each load the product, sell some, and save. Read–modify–write.
    public static (int Final, string? Error, string Sql) TwoSales(string path, bool checkedVersion, int first, int second)
    {
        using var a = checkedVersion ? new CheckedShopDb(path) : new ShopDb(path);
        using var b = checkedVersion ? new CheckedShopDb(path) : new ShopDb(path);
        var pa = a.Products.Single();                  // both read Stock = 10
        var pb = b.Products.Single();
        pa.Stock -= first; pa.Version++;
        a.SaveChanges();                               // saves 10 - first
        pb.Stock -= second; pb.Version++;
        string? error = null;
        try { b.SaveChanges(); }                       // saves 10 - second: overwriting the first sale?
        catch (DbUpdateConcurrencyException ex) { error = ex.Message; }
        using var check = new ShopDb(path);
        string sql = b.Log.LastOrDefault() ?? "";
        sql = System.Text.RegularExpressions.Regex.Replace(sql[sql.IndexOf("UPDATE", StringComparison.Ordinal)..], @"\s+", " ");
        return (check.Products.Single().Stock, error, sql);
    }

    // The database does the arithmetic: no read–modify–write in C#.
    public static int TwoAtomicSales(string path, int first, int second)
    {
        using var a = new ShopDb(path);
        using var b = new ShopDb(path);
        a.Products.Where(p => p.Id == 1).ExecuteUpdate(s => s.SetProperty(p => p.Stock, p => p.Stock - first));
        b.Products.Where(p => p.Id == 1).ExecuteUpdate(s => s.SetProperty(p => p.Stock, p => p.Stock - second));
        using var check = new ShopDb(path);
        return check.Products.Single().Stock;
    }
}
