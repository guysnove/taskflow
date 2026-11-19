using Ch11.Kwic.Encapsulation;

namespace Ch11.Kwic.Tests;

public class EncapsulationTests
{
    [Fact]
    public void Leaky_OneForgottenLine_AndTheCountIsWrong()
    {
        var p = new LeakyProject();
        p.Tasks.Add("Write copy"); p.OpenCount++;
        p.Tasks.Add("Get logo");                         // a second caller forgot OpenCount++
        p.Done.Add("Write copy"); p.OpenCount--;
        Assert.Equal(0, p.OpenCount);                    // the object says: nothing left to do
        Assert.Single(p.Tasks.Except(p.Done));           // the truth: "Get logo" is still open
    }

    [Fact]
    public void Encapsulated_TheCountCannotBeWrong()
    {
        var p = new Project();
        p.Add("Write copy");
        p.Add("Get logo");
        p.Complete("Write copy");
        Assert.Equal(1, p.OpenCount);
        Assert.Throws<InvalidOperationException>(() => p.Complete("Unknown"));
        Assert.Throws<InvalidOperationException>(() => p.Add("Get logo"));
    }
}
