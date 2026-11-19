using Microsoft.EntityFrameworkCore;

namespace Ch10.Patterns.Repository;

public sealed class Row
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
}

public sealed class Db(DbContextOptions<Db> options) : DbContext(options)
{
    public DbSet<Row> Rows => Set<Row>();
}

// UNNECESSARY ABSTRACTION: a "generic repository" over EF Core. It looks tidy, but GetAll returns
// IEnumerable, so every filter written afterwards runs in C#, after the WHOLE table was loaded.
public interface IRepository<T>
{
    IEnumerable<T> GetAll();
    void Add(T item);
}

public sealed class GenericRepository<T>(Db db) : IRepository<T> where T : class
{
    public IEnumerable<T> GetAll() => db.Set<T>().ToList();
    public void Add(T item)
    {
        db.Set<T>().Add(item);
        db.SaveChanges();
    }
}

// A REPOSITORY with intention-revealing methods: each one is a query the database can run well.
public sealed class RowRepository(Db db)
{
    public List<Row> Open(int take) => db.Rows.Where(r => !r.Done).OrderBy(r => r.Id).Take(take).ToList();
}
