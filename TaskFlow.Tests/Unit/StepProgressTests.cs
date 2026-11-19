using TaskFlow.Models;

namespace TaskFlow.Tests.Unit;

// Chapter 30: written before StepProgress() existed (red), then made to pass (green).
[Trait("Req", "FR-STEP-1")]
public class StepProgressTests
{
    [Fact]
    public void StepProgress_NoSteps_IsNull()
    {
        var task = new TaskItem { Title = "Plan team lunch" };
        Assert.Null(task.StepProgress());           // nothing to show
    }

    [Fact]
    public void StepProgress_SomeDone_CountsThem()
    {
        var task = new TaskItem { Title = "Office move" };
        task.Steps.Add(new TaskStep { Text = "Buy boxes", Done = true });
        task.Steps.Add(new TaskStep { Text = "Pack books", Done = true });
        task.Steps.Add(new TaskStep { Text = "Label boxes" });

        Assert.Equal("2 of 3 steps done", task.StepProgress());
    }

    [Fact]
    public void StepProgress_NoneDone_SaysZero()
    {
        var task = new TaskItem { Title = "Office move" };
        task.Steps.Add(new TaskStep { Text = "Buy boxes" });
        Assert.Equal("0 of 1 steps done", task.StepProgress());
    }
}
