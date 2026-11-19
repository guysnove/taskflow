namespace Ch11.Kwic.Hiding;

public static class KwicB
{
    public static List<string> Run(IEnumerable<string> lines)
    {
        ILineStorage storage = new PackedLineStorage();
        Input.Read(lines, storage);
        var shifts = new CircularShifter(storage);
        return Output.Lines(shifts, new Alphabetizer(shifts));
    }
}
