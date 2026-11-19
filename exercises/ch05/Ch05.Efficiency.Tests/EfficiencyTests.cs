using Ch05.Efficiency;

namespace Ch05.Efficiency.Tests;

public class DedupeTests
{
    [Fact]
    public void BothVersions_KeepTheFirstOfEach_InOrder()
    {
        string[] input = ["ana@x.com", "ben@x.com", "ana@x.com", "cy@x.com", "ben@x.com"];
        Assert.Equal(["ana@x.com", "ben@x.com", "cy@x.com"], Dedupe.WithList(input));
        Assert.Equal(Dedupe.WithList(input), Dedupe.WithSet(input));
    }

    [Fact]
    public void EmptyInput_GivesAnEmptyList() => Assert.Empty(Dedupe.WithSet([]));
}

public class StepCounterTests
{
    [Theory]
    [InlineData(1_024, 10)]
    [InlineData(1_000_000, 19)]
    [InlineData(1, 0)]
    public void Logarithmic_CountsHalvings(int n, long expected) => Assert.Equal(expected, StepCounter.Logarithmic(n));

    [Fact]
    public void Quadratic_IsNSquared() => Assert.Equal(1_000_000, StepCounter.Quadratic(1_000));

    [Fact]
    public void Triangular_IsHalfOfQuadratic_Roughly() => Assert.Equal(1_000 * 999 / 2, StepCounter.Triangular(1_000));

    [Fact]
    public void DoublingN_DoublesLinear_AndQuadruplesQuadratic()
    {
        Assert.Equal(2, StepCounter.Linear(2_000) / StepCounter.Linear(1_000));
        Assert.Equal(4, StepCounter.Quadratic(2_000) / StepCounter.Quadratic(1_000));
    }

    [Fact]
    public void Exponential_DoublesWithEachExtraItem() => Assert.Equal(2 * StepCounter.Exponential(10), StepCounter.Exponential(11));
}

public class SearchesTests
{
    private static readonly int[] Items = [42, 7, 19, 3, 88, 61];

    [Theory]
    [InlineData(19, true)]
    [InlineData(42, true)]
    [InlineData(61, true)]
    [InlineData(5, false)]
    public void AllSixAgree(int target, bool expected)
    {
        int[] sorted = Items.Order().ToArray();
        Assert.Equal(expected, Searches.Loop(Items, target));
        Assert.Equal(expected, Searches.LinqAny(Items, target));
        Assert.Equal(expected, Searches.ListContains([.. Items], target));
        Assert.Equal(expected, Searches.Binary(sorted, target));
        Assert.Equal(expected, Searches.SortThenBinary(Items, target));
        Assert.Equal(expected, Searches.Set([.. Items], target));
    }

    [Fact]
    public void Binary_OnUnsortedData_CanGiveTheWrongAnswer() =>
        Assert.False(Searches.Binary(Items, 42));        // 42 IS there, but the data isn't sorted

    [Fact]
    public void SortThenBinary_DoesNotChangeTheCallersArray()
    {
        int[] items = [3, 1, 2];
        Searches.SortThenBinary(items, 2);
        Assert.Equal([3, 1, 2], items);
    }

    [Fact]
    public void EmptyCollections_FindNothing()
    {
        Assert.False(Searches.Loop([], 1));
        Assert.False(Searches.Binary([], 1));
        Assert.False(Searches.Set([], 1));
    }
}

public class SnippetsTests
{
    [Fact] public void MinAndMax() => Assert.Equal((3, 88), Snippets.MinAndMax([42, 7, 3, 88]));
    [Fact] public void DuplicatePair_Found() => Assert.True(Snippets.HasDuplicatePair([1, 2, 3, 2]));
    [Fact] public void DuplicatePair_NotFound() => Assert.False(Snippets.HasDuplicatePair([1, 2, 3]));
    [Fact] public void CommonCount_CountsMatches() => Assert.Equal(2, Snippets.CommonCount(["a", "b", "c"], ["b", "c", "d"]));
    [Fact] public void CountDoublings_IsLogarithmic() => Assert.Equal(10, Snippets.CountDoublings(1_024));
    [Fact] public void JoinSlowAndFast_Agree() => Assert.Equal(Snippets.JoinSlow(["a", "b"]), Snippets.JoinFast(["a", "b"]));

    [Fact]
    public void CountByWeekday_CountsEachDay()
    {
        DateOnly monday = new(2026, 11, 16);
        var counts = Snippets.CountByWeekday([monday, monday.AddDays(1), monday.AddDays(7)]);
        Assert.Equal(2, counts[(int)DayOfWeek.Monday]);
        Assert.Equal(1, counts[(int)DayOfWeek.Tuesday]);
    }
}

public class SpaceAndCasesTests
{
    [Fact]
    public void ReverseInPlace_AndReversedCopy_Agree()
    {
        int[] a = [1, 2, 3, 4, 5];
        int[] copy = Space.ReversedCopy(a);
        Space.ReverseInPlace(a);
        Assert.Equal([5, 4, 3, 2, 1], a);
        Assert.Equal(a, copy);
    }

    [Fact] public void SumRecursive_AddsEverything() => Assert.Equal(15, Space.SumRecursive([1, 2, 3, 4, 5]));

    [Fact]
    public void LinearSearch_BestAndWorstCase()
    {
        int[] items = [10, 20, 30, 40];
        Assert.Equal(1, Cases.ComparisonsToFind(items, 10));   // best: the first item
        Assert.Equal(4, Cases.ComparisonsToFind(items, 99));   // worst: not there at all
    }

    [Fact]
    public void InsertionSort_SortedInputShiftsNothing_ReversedShiftsEveryPair()
    {
        Assert.Equal(0, Cases.InsertionSortShifts([1, 2, 3, 4, 5]));
        Assert.Equal(10, Cases.InsertionSortShifts([5, 4, 3, 2, 1]));   // 5 × 4 / 2
    }
}

public class PracticeTests
{
    [Fact] public void A_AddsThenSubtractsHalves() => Assert.Equal(5, Practice.A([4, 6]));     // (4 + 6) - (2 + 3)
    [Fact] public void B_CountsDivisionsByThree() => Assert.Equal(4, Practice.B(81));
    [Fact] public void D_CountsInversions() => Assert.Equal(3, Practice.D([3, 2, 1]));
    [Fact] public void E_CountsSetBits() => Assert.Equal(3, Practice.E([1, 6]));
    [Fact] public void F_DoublesWithEachLevel() => Assert.Equal(1024, Practice.F(10));
}
