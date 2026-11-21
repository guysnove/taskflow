namespace Ch16.Async;

public static class Jobs
{
    // A long job in steps. It checks the token between steps (cooperative cancellation),
    // and its finally block always runs — cancelled or not.
    public static async Task<int> LongJobAsync(int steps, Action<string> report, CancellationToken cancellationToken)
    {
        int done = 0;
        try
        {
            for (int step = 1; step <= steps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(200, cancellationToken);                       // "work" that can also be cancelled
                done = step;
                report($"step {step} done");
            }
            return done;
        }
        finally
        {
            report($"cleanup: {done} of {steps} steps were done");
        }
    }

    // A slow service: answers after the given time, unless cancelled first.
    public static async Task<string> SlowServiceAsync(TimeSpan takes, Action<string> report, CancellationToken cancellationToken = default)
    {
        await Task.Delay(takes, cancellationToken);
        report($"slow service: finished its work after {takes.TotalSeconds:0.0} s");
        return "the answer";
    }
}
