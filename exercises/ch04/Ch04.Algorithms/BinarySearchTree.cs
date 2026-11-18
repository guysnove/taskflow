namespace Ch04.Algorithms;

// Chapter 4: a binary search tree. Smaller keys go left, larger keys go right.
public class BinarySearchTree
{
    private sealed class Node(int key)
    {
        public int Key = key;
        public Node? Left, Right;
    }

    private Node? _root;
    public int Count { get; private set; }

    public bool Add(int key)                       // O(height)
    {
        if (_root is null) { _root = new Node(key); Count++; return true; }
        var current = _root;
        while (true)
        {
            if (key == current.Key) return false;  // no duplicates
            if (key < current.Key)
            {
                if (current.Left is null) { current.Left = new Node(key); Count++; return true; }
                current = current.Left;
            }
            else
            {
                if (current.Right is null) { current.Right = new Node(key); Count++; return true; }
                current = current.Right;
            }
        }
    }

    public bool Contains(int key)                  // O(height): one path from the root
    {
        var current = _root;
        while (current is not null)
        {
            if (key == current.Key) return true;
            current = key < current.Key ? current.Left : current.Right;
        }
        return false;
    }

    // In-order traversal (left, node, right) visits the keys in sorted order.
    public IEnumerable<int> InOrder() => InOrder(_root);
    private static IEnumerable<int> InOrder(Node? n)
    {
        if (n is null) yield break;
        foreach (int k in InOrder(n.Left)) yield return k;
        yield return n.Key;
        foreach (int k in InOrder(n.Right)) yield return k;
    }

    public int Height => HeightOf(_root);          // edges on the longest root-to-leaf path + 1
    private static int HeightOf(Node? n) => n is null ? 0 : 1 + Math.Max(HeightOf(n.Left), HeightOf(n.Right));
}
