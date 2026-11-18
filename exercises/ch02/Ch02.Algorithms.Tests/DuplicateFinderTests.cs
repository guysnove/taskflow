using Ch02.Algorithms;

namespace Ch02.Algorithms.Tests;

public class DuplicateFinderTests
{
    private static readonly List<Customer> Sample =
    [
        new(1, "Ana Silva",   "ana@example.com"),
        new(2, "Ben Okafor",  "ben@example.com"),
        new(3, "ANA SILVA",   " Ana@Example.com "),   // same person: case and spaces differ
        new(4, "Chloe Martin", "chloe@example.com"),
        new(5, "Ben O.",      "BEN@example.com"),      // same email, different name
        new(6, "No Email",    ""),                     // missing email
        new(7, "Also None",   "  "),                   // blank email
    ];

    [Theory]
    [InlineData("ana@example.com", "ana@example.com")]
    [InlineData("  Ana@Example.COM ", "ana@example.com")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizeEmail_TrimsAndLowercases(string? input, string expected)
    {
        Assert.Equal(expected, DuplicateFinder.NormalizeEmail(input));
    }

    [Fact]
    public void FindPairs_FindsEachDuplicatePairOnce()
    {
        var pairs = DuplicateFinder.FindPairs(Sample);

        Assert.Equal(2, pairs.Count);
        Assert.Contains(pairs, p => p.Item1.Id == 1 && p.Item2.Id == 3);
        Assert.Contains(pairs, p => p.Item1.Id == 2 && p.Item2.Id == 5);
    }

    [Fact]
    public void FindPairs_BlankEmailsAreNotDuplicates()
    {
        var pairs = DuplicateFinder.FindPairs(Sample);
        Assert.DoesNotContain(pairs, p => p.Item1.Id == 6 || p.Item2.Id == 6);
    }

    [Fact]
    public void FindGroups_AgreesWithFindPairs()
    {
        var groups = DuplicateFinder.FindGroups(Sample);

        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, g => g.Select(c => c.Id).SequenceEqual([1, 3]));
        Assert.Contains(groups, g => g.Select(c => c.Id).SequenceEqual([2, 5]));
    }

    [Fact]
    public void EmptyAndSingleLists_HaveNoDuplicates()
    {
        Assert.Empty(DuplicateFinder.FindPairs([]));
        Assert.Empty(DuplicateFinder.FindGroups([]));
        Assert.Empty(DuplicateFinder.FindPairs([new Customer(1, "Solo", "solo@example.com")]));
    }

    [Fact]
    public void ThreeCopies_GiveThreePairs_ButOneGroup()
    {
        List<Customer> three = [new(1, "A", "a@x.com"), new(2, "A", "A@x.com"), new(3, "A", "a@X.com")];

        Assert.Equal(3, DuplicateFinder.FindPairs(three).Count);   // (1,2) (1,3) (2,3)
        Assert.Single(DuplicateFinder.FindGroups(three));
    }
}
