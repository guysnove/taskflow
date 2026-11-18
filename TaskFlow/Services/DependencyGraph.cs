namespace TaskFlow.Services;

// Course 2, Chapter 4: dependencies form a directed graph. Edges point from a blocker to the
// task it blocks. A cycle (A waits for B, B waits for A) means nothing can ever start.
public static class DependencyGraph
{
    // If we add "taskId is blocked by blockedById", is there a cycle? Returns it, or null.
    // The new edge is blockedById -> taskId. It closes a loop exactly when taskId can already
    // reach blockedById. Breadth-first search from taskId finds the shortest such path.
    public static List<int>? CycleIfAdded(IEnumerable<(int BlockedById, int TaskId)> edges, int taskId, int blockedById)
    {
        if (taskId == blockedById) return [blockedById, taskId];           // a task can't wait for itself

        var next = new Dictionary<int, List<int>>();                         // adjacency list
        foreach (var (from, to) in edges)
        {
            if (!next.TryGetValue(from, out var list)) next[from] = list = new List<int>();
            list.Add(to);
        }

        var cameFrom = new Dictionary<int, int> { [taskId] = taskId };
        var queue = new Queue<int>([taskId]);
        while (queue.Count > 0)
        {
            int node = queue.Dequeue();
            if (node == blockedById)
            {
                var path = new List<int>();                                  // taskId ... blockedById
                for (int n = node; n != taskId; n = cameFrom[n]) path.Add(n);
                path.Add(taskId);
                path.Reverse();
                return [blockedById, .. path];                               // blockedById -> taskId -> ... -> blockedById
            }
            foreach (int t in next.GetValueOrDefault(node, []))
            {
                if (cameFrom.TryAdd(t, node)) queue.Enqueue(t);
            }
        }
        return null;
    }
}
