namespace Ch11.Kwic.Tests;

public class KwicTests
{
    public static readonly string[] Titles =
    [
        "Software Engineering Economics",
        "The Mythical Man Month",
        "On the Criteria To Be Used in Decomposing Systems into Modules",
        "  Clean   Code  ",
        "",
    ];

    [Fact]
    public void BothDesigns_GiveTheSameIndex()
    {
        var a = SharedData.KwicA.Run(Titles);
        var b = Hiding.KwicB.Run(Titles);
        Assert.Equal(a, b);
        Assert.Equal(3 + 4 + 11 + 2, a.Count);           // one shift per word
    }

    [Fact]
    public void ASmallExample()
    {
        Assert.Equal(["a b c", "b c a", "c a b"], Hiding.KwicB.Run(["b c a"]));
    }
}
