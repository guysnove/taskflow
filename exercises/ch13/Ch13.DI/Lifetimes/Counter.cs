namespace Ch13.DI.Lifetimes;

// Every object gets a short id when it's created, so we can SEE when the container makes a new one.
public abstract class Tracked
{
    private static int _next;
    public int Id { get; } = Interlocked.Increment(ref _next);
    public override string ToString() => $"{GetType().Name} #{Id}";
}

public sealed class TransientThing : Tracked { }
public sealed class ScopedThing : Tracked { }
public sealed class SingletonThing : Tracked { }

// A consumer that asks for all three, to show what one "request" (scope) sees.
public sealed class Consumer(TransientThing t, ScopedThing s, SingletonThing g)
{
    public string Describe() => $"{t}, {s}, {g}";
}
