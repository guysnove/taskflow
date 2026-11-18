using TaskFlow.Services;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 4: cycle detection for task dependencies.
// Edges are (BlockedById, TaskId): "BlockedById must be done before TaskId".
public class DependencyGraphTests
{
    // 1 Design -> 2 Build -> 3 Test -> 4 Deploy, and 2 Build -> 5 Docs -> 4 Deploy
    private static readonly List<(int, int)> Release = [(1, 2), (2, 3), (3, 4), (2, 5), (5, 4)];

    [Fact]
    public void NewEdge_WithoutALoop_IsFine()
    {
        Assert.Null(DependencyGraph.CycleIfAdded(Release, taskId: 4, blockedById: 1));   // deploy waits for design: fine
        Assert.Null(DependencyGraph.CycleIfAdded([], taskId: 1, blockedById: 2));        // empty graph
    }

    [Fact]
    public void ATaskWaitingForItself_IsALoop()
    {
        Assert.Equal([3, 3], DependencyGraph.CycleIfAdded(Release, taskId: 3, blockedById: 3));
    }

    [Fact]
    public void TwoTasksWaitingForEachOther_IsALoop()
    {
        // Build already blocks Test. "Build waits for Test" closes the loop.
        Assert.Equal([3, 2, 3], DependencyGraph.CycleIfAdded(Release, taskId: 2, blockedById: 3));
    }

    [Fact]
    public void ALongerLoop_IsFound_ShortestFirst()
    {
        // Design waits for Deploy: Deploy -> Design -> Build -> Test -> Deploy (or via Docs: the same length).
        var cycle = DependencyGraph.CycleIfAdded(Release, taskId: 1, blockedById: 4)!;
        Assert.Equal(5, cycle.Count);
        Assert.Equal(4, cycle[0]);
        Assert.Equal(4, cycle[^1]);
        Assert.Equal([4, 1, 2], cycle.Take(3));
    }

    [Fact]
    public void ADiamond_IsNotALoop()
    {
        // Two paths to the same task (Build -> Test -> Deploy and Build -> Docs -> Deploy) are fine.
        Assert.Null(DependencyGraph.CycleIfAdded(Release, taskId: 5, blockedById: 3));   // Docs also waits for Test
    }
}
