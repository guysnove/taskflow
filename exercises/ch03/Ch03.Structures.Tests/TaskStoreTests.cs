using Ch03.Structures.Search;

namespace Ch03.Structures.Tests;

public class TaskStoreTests
{
    private static readonly TaskRecord[] Tasks =
    [
        new(1, "Fix login bug", false),
        new(2, "Write report", false),
        new(3, "Fix the report's typos!", true),
        new(7, "Login page: new design", false),
    ];

    public static TheoryData<string> Stores => ["list", "indexed"];
    private static ITaskStore Make(string kind) =>
        kind == "list" ? new ListTaskStore(Tasks) : new IndexedTaskStore(Tasks);

    [Theory]
    [MemberData(nameof(Stores))]
    public void FindById_FindsOrReturnsNull(string kind)
    {
        var store = Make(kind);
        Assert.Equal("Write report", store.FindById(2)?.Title);
        Assert.Null(store.FindById(5));                       // ids have gaps
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public void SearchWord_MatchesWholeWords_IgnoringCase(string kind)
    {
        var store = Make(kind);
        Assert.Equal([1, 7], store.SearchWord("LOGIN").Select(t => t.Id));
        Assert.Equal([1, 3], store.SearchWord(" fix ").Select(t => t.Id));
        Assert.Empty(store.SearchWord("log"));                // part of a word: no
        Assert.Empty(store.SearchWord("missing"));
    }

    [Theory]
    [MemberData(nameof(Stores))]
    public void SearchResult_CannotChangeTheStore(string kind)
    {
        var store = Make(kind);
        store.SearchWord("login").Clear();                    // a caller "tidies up" its result
        Assert.Equal(2, store.SearchWord("login").Count);     // the store must be unaffected
    }

    [Fact]
    public void BothStores_AgreeOnGeneratedData()
    {
        string[] words = ["fix", "write", "report", "login", "bug", "call", "plan", "review"];
        var rnd = new Random(7);
        var tasks = Enumerable.Range(1, 500).Select(i =>
            new TaskRecord(i, $"{words[rnd.Next(8)]} {words[rnd.Next(8)]} {i}", rnd.Next(2) == 0)).ToList();
        var list = new ListTaskStore(tasks); var indexed = new IndexedTaskStore(tasks);

        foreach (var w in words)
            Assert.Equal(list.SearchWord(w).Select(t => t.Id), indexed.SearchWord(w).Select(t => t.Id));
    }
}
