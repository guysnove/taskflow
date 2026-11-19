namespace TaskFlow.Common;

// Course 2, Chapter 11: moved here from Services. Data needed it too, which made Data and
// Services depend on each other (a cycle). Common is for tiny helpers that every module may use,
// and that depend on nothing in TaskFlow.
public static class TimeProviderExtensions
{
    // Today's date, according to the TimeProvider (a fake one in tests: Chapter 28).
    public static DateOnly Today(this TimeProvider time) =>
        DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
