using Ch06.Requirements;

namespace Ch06.Requirements.Tests;

public class RequirementLintTests
{
    [Fact]
    public void VagueWords_AreFlagged()
    {
        var words = RequirementLint.Check("Notifications should be instant and easy to use.").Select(f => f.Word.ToLower()).ToList();
        Assert.Equal(["instant", "easy", "should"], words);
    }

    [Fact]
    public void AMeasurableRequirement_HasNoFlags() =>
        Assert.Empty(RequirementLint.Check("An assignment notification appears within 1 minute, 95% of the time."));

    [Fact]
    public void WholeWordsOnly_NotPartsOfWords() =>
        Assert.Empty(RequirementLint.Check("The breakfast menu is shown."));   // "fast" inside "breakfast" is fine
}
