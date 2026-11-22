namespace Ch22.Planning.Tests;

public class PrioritiseTests
{
    private static Story S(string id, int points, int value, MoSCoW p) => new(id, id, points, value, p);

    [Fact]
    public void MustComesFirst_ThenTheBestValuePerPoint_AndWontIsLeftOut()
    {
        var order = Backlog.Prioritise([S("c1", 1, 9, MoSCoW.Could), S("m1", 8, 8, MoSCoW.Must), S("m2", 2, 6, MoSCoW.Must), S("w1", 1, 10, MoSCoW.Wont)]);

        Assert.Equal(["m2", "m1", "c1"], order.Select(s => s.Id));      // m2: 3.0 per point beats m1: 1.0
    }
}

public class OrderedTests
{
    [Fact]
    public void Ordered_KeepsTheProductOwnersOrder_WithoutWont() =>
        Assert.Equal(["b", "a"], Backlog.Ordered([new("b", "b", 8, 1, MoSCoW.Could), new("x", "x", 1, 9, MoSCoW.Wont), new("a", "a", 1, 9, MoSCoW.Must)]).Select(s => s.Id));
}

public class PlanTests
{
    private static Story S(string id, int points) => new(id, id, points, 5, MoSCoW.Must);

    [Fact]
    public void NoSprint_GoesAboveCapacity_AndSmallerStoriesFillTheGap()
    {
        var sprints = Backlog.Plan([S("a", 8), S("b", 8), S("c", 5), S("d", 3)], capacity: 20, maxSprints: 5);

        Assert.Equal(["a", "b", "d"], sprints[0].Stories.Select(s => s.Id));   // c (5) doesn't fit after 16; d (3) does
        Assert.Equal(["c"], sprints[1].Stories.Select(s => s.Id));
        Assert.All(sprints, sp => Assert.True(sp.Points <= 20));
    }

    [Fact]
    public void AStoryBiggerThanASprint_MustBeSplit() =>
        Assert.Contains("split it", Assert.Throws<InvalidOperationException>(() => Backlog.Plan([S("epic", 40)], 20, 5)).Message);
}

public class ForecastTests
{
    [Fact]
    public void AlwaysTheSameVelocity_GivesOneAnswer() =>
        Assert.Equal((3, 3), Velocity.Forecast([10, 10, 10], remaining: 30));

    [Fact]
    public void MoreConfidence_NeverMeansFewerSprints()
    {
        var (p50, p85) = Velocity.Forecast([6, 14, 9, 12, 4], remaining: 60);
        Assert.True(p85 >= p50);
    }

    [Fact]
    public void SameDataAndSeed_SameForecast() =>
        Assert.Equal(Velocity.Forecast([6, 14, 9], 50), Velocity.Forecast([6, 14, 9], 50));
}

public class FlowTests
{
    [Fact]
    public void CycleTime_CountsBothTheStartAndTheEndDay() =>
        Assert.Equal(3, new Card("x", new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4)).CycleDays);

    [Fact]
    public void Percentile_85_OfTenValues_IsTheNinthSmallest() =>
        Assert.Equal(9, Flow.Percentile([10, 1, 2, 3, 4, 5, 6, 7, 8, 9], 0.85));

    [Fact]
    public void AverageWip_CountsCardsInProgressEachDay()
    {
        var d = new DateOnly(2026, 11, 2);
        Card[] cards = [new("a", d, d.AddDays(1)), new("b", d.AddDays(1), null)];   // day 1: a; day 2: a, b
        Assert.Equal(1.5, Flow.AverageWip(cards, d, d.AddDays(1)));
    }
}
