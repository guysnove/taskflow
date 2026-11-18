namespace Ch04.Algorithms;

// Chapter 4: a directed graph as an adjacency list. An edge a -> b means "a points to b".
public class Graph
{
    private readonly Dictionary<string, List<string>> _edges = new();

    public IEnumerable<string> Nodes => _edges.Keys;
    public IReadOnlyList<string> Next(string node) => _edges.TryGetValue(node, out var list) ? list : [];

    public void AddNode(string node) => _edges.TryAdd(node, new List<string>());

    public void AddEdge(string from, string to)
    {
        AddNode(from); AddNode(to);
        _edges[from].Add(to);
    }

    // Breadth-first search: explore level by level with a queue. Finds a path with the fewest edges.
    public List<string>? ShortestPath(string start, string goal)
    {
        var cameFrom = new Dictionary<string, string?> { [start] = null };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            string node = queue.Dequeue();
            if (node == goal) return Rebuild(cameFrom, goal);
            foreach (string next in Next(node))
            {
                if (cameFrom.ContainsKey(next)) continue;      // already seen
                cameFrom[next] = node;
                queue.Enqueue(next);
            }
        }
        return null;                                         // unreachable
    }

    private static List<string> Rebuild(Dictionary<string, string?> cameFrom, string goal)
    {
        var path = new List<string>();
        for (string? n = goal; n is not null; n = cameFrom[n]) path.Add(n);
        path.Reverse();
        return path;
    }

    // Depth-first search with three colours finds a cycle, if there is one.
    //   white: not visited yet; grey: on the current path; black: finished.
    // An edge into a GREY node closes a loop.
    public List<string>? FindCycle()
    {
        var colour = _edges.Keys.ToDictionary(n => n, _ => 0);   // 0 white, 1 grey, 2 black
        var path = new List<string>();

        List<string>? Visit(string node)
        {
            colour[node] = 1; path.Add(node);
            foreach (string next in Next(node))
            {
                if (colour[next] == 1)                                 // back to the current path
                    return [.. path[path.IndexOf(next)..], next];
                if (colour[next] == 0 && Visit(next) is { } cycle) return cycle;
            }
            colour[node] = 2; path.RemoveAt(path.Count - 1);
            return null;
        }

        foreach (string node in _edges.Keys)
            if (colour[node] == 0 && Visit(node) is { } found) return found;
        return null;
    }

    // Kahn's algorithm: an order where every node comes after everything that points to it.
    // Returns null when there's a cycle (no such order exists).
    public List<string>? TopologicalOrder()
    {
        var incoming = _edges.Keys.ToDictionary(n => n, _ => 0);
        foreach (var targets in _edges.Values) foreach (string t in targets) incoming[t]++;
        var ready = new Queue<string>(incoming.Where(p => p.Value == 0).Select(p => p.Key));
        var order = new List<string>();
        while (ready.Count > 0)
        {
            string n = ready.Dequeue();
            order.Add(n);
            foreach (string t in Next(n))
                if (--incoming[t] == 0) ready.Enqueue(t);
        }
        return order.Count == _edges.Count ? order : null;
    }
}
