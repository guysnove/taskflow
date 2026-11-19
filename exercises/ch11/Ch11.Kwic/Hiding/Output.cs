namespace Ch11.Kwic.Hiding;

public static class Output
{
    public static List<string> Lines(CircularShifter shifts, Alphabetizer order) =>
        Enumerable.Range(0, shifts.Count)
                  .Select(i => order.Ith(i))
                  .Select(s => string.Join(' ', Enumerable.Range(0, shifts.WordCount(s)).Select(w => shifts.Word(s, w))))
                  .ToList();
}
