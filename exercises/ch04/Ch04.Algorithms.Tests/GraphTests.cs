using Ch04.Algorithms;

namespace Ch04.Algorithms.Tests;

public class GraphTests
{
    // "a -> b" means: a must be done before b.
    private static Graph Release()
    {
        var g = new Graph();
        g.AddEdge("design", "build");
        g.AddEdge("build", "test");
        g.AddEdge("test", "deploy");
        g.AddEdge("build", "docs");
        g.AddEdge("docs", "deploy");
        return g;
    }

    [Fact]
    public void Bfs_FindsAPathWithTheFewestEdges()
    {
        var g = Release();
        g.AddEdge("design", "deploy");                             // a shortcut
        Assert.Equal(["design", "deploy"], g.ShortestPath("design", "deploy"));
        Assert.Null(g.ShortestPath("deploy", "design"));          // edges have a direction
    }

    [Fact]
    public void NoCycle_TopologicalOrderRespectsEveryEdge()
    {
        var g = Release();
        Assert.Null(g.FindCycle());
        var order = g.TopologicalOrder()!;
        foreach (var from in g.Nodes)
            foreach (var to in g.Next(from))
                Assert.True(order.IndexOf(from) < order.IndexOf(to), $"{from} must come before {to}");
    }

    [Fact]
    public void Cycle_IsFound_AndThereIsNoOrder()
    {
        var g = Release();
        g.AddEdge("deploy", "build");                              // build -> test -> deploy -> build
        var cycle = g.FindCycle()!;
        Assert.Equal(cycle[0], cycle[^1]);                         // starts and ends at the same node
        Assert.Contains("deploy", cycle);
        Assert.Null(g.TopologicalOrder());
    }

    [Fact]
    public void SelfLoop_IsACycle()
    {
        var g = new Graph();
        g.AddEdge("a", "a");
        Assert.Equal(["a", "a"], g.FindCycle());
    }
}
