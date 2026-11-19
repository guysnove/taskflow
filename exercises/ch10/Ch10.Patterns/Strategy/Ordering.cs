namespace Ch10.Patterns.Strategy;

// Problem: the task list can be ordered in several ways, and more will come.
// Three implementations of the same idea, from heaviest to lightest.

// 1. The classic STRATEGY: an interface, one class per algorithm.
public interface IOrdering
{
    string Name { get; }
    IEnumerable<Item> Apply(IEnumerable<Item> items);
}

public sealed class ByDue : IOrdering
{
    public string Name => "due";
    public IEnumerable<Item> Apply(IEnumerable<Item> items) =>
        items.OrderBy(i => i.Done).ThenBy(i => i.Due is null).ThenBy(i => i.Due);
}

public sealed class ByPriority : IOrdering
{
    public string Name => "priority";
    public IEnumerable<Item> Apply(IEnumerable<Item> items) =>
        items.OrderBy(i => i.Done).ThenBy(i => i.Priority switch { "High" => 0, "Medium" => 1, _ => 2 });
}

// 2. A strategy as a DELEGATE: the same idea, no classes. Fine when the algorithm is one expression.
public static class Orderings
{
    public static readonly IReadOnlyDictionary<string, Func<IEnumerable<Item>, IEnumerable<Item>>> ByName =
        new Dictionary<string, Func<IEnumerable<Item>, IEnumerable<Item>>>
        {
            ["due"] = items => new ByDue().Apply(items),
            ["title"] = items => items.OrderBy(i => i.Done).ThenBy(i => i.Title, StringComparer.Ordinal),
        };
}

// 3. No pattern at all: a switch. Perfectly good while there are two or three cases that
// never grow, and only one place chooses between them.
public static class OrderingSwitch
{
    public static IEnumerable<Item> Apply(string name, IEnumerable<Item> items) => name switch
    {
        "priority" => new ByPriority().Apply(items),
        "title" => items.OrderBy(i => i.Done).ThenBy(i => i.Title, StringComparer.Ordinal),
        _ => new ByDue().Apply(items),
    };
}
