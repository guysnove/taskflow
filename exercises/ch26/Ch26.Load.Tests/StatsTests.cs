public class StatsTests
{
    private static readonly double[] Ten = [1, 2, 3, 4, 5, 6, 7, 8, 9, 100];   // sorted: one slow request

    [Fact]
    public void P50_IsTheMiddle() => Assert.Equal(5, Stats.Percentile(Ten, 50));

    [Fact]
    public void P90_IgnoresTheOneSlowRequest() => Assert.Equal(9, Stats.Percentile(Ten, 90));

    [Fact]
    public void P99_AndMax_SeeIt() => Assert.Equal(100, Stats.Percentile(Ten, 99));

    [Fact]
    public void NoMeasurements_IsZero() => Assert.Equal(0, Stats.Percentile([], 50));
}
