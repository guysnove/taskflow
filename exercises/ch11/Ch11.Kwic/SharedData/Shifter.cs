namespace Ch11.Kwic.SharedData;

// Module 2: Circular shift. A shift = (line, index of the character where its first word starts).
public static class Shifter
{
    public static void Build()
    {
        for (int line = 0; line < Store.LineStart.Count; line++)
        {
            for (int i = Store.LineStart[line]; i < Store.LineEnd(line); i++)
            {
                if (i == Store.LineStart[line] || Store.Chars[i - 1] == ' ')
                {
                    Store.Shifts.Add((line, i));
                }
            }
        }
    }
}
