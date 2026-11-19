namespace Ch10.Patterns.Observer;

// Problem: when a task is completed, other parts of the program want to react
// (notify someone, update statistics, write an audit line), and the task list shouldn't know them.

// 1. A C# EVENT: the language's built-in observer.
public sealed class TaskList
{
    public event EventHandler<Item>? Completed;

    public void Complete(Item item) => Completed?.Invoke(this, item with { Done = true });
}

// 2. An OBSERVER INTERFACE, with the observers given to the subject (as TaskFlow does, through DI).
public interface ICompletionObserver
{
    void OnCompleted(Item item);
}

public sealed class TaskListWithObservers(IEnumerable<ICompletionObserver> observers)
{
    public void Complete(Item item)
    {
        foreach (var o in observers)
        {
            o.OnCompleted(item with { Done = true });
        }
    }
}

// A typical observer, usable with either version.
public sealed class Statistics : ICompletionObserver
{
    public int CompletedCount { get; private set; }
    public void OnCompleted(Item item) => CompletedCount++;
    public void Handle(object? sender, Item item) => OnCompleted(item);
}
