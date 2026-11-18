namespace TaskFlow.Services;

public static class TimeProviderExtensions
{
    // Today's date, according to the TimeProvider (a fake one in tests: Chapter 28).
    public static DateOnly Today(this TimeProvider time) =>
        DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
