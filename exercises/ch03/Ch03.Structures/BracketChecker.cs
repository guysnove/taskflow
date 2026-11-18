namespace Ch03.Structures;

// Chapter 3: a classic use of a stack: are the brackets balanced?
public static class BracketChecker
{
    public static bool IsBalanced(string text)
    {
        var open = new Stack<char>();
        foreach (char c in text)
        {
            if (c is '(' or '[' or '{') open.Push(c);
            else if (c is ')' or ']' or '}')
            {
                if (open.Count == 0) return false;                 // a closer with nothing open
                char expected = c switch { ')' => '(', ']' => '[', _ => '{' };
                if (open.Pop() != expected) return false;          // wrong kind: ( ]
            }
        }
        return open.Count == 0;                                    // anything left open?
    }
}
