namespace Ch11.Kwic.SharedData;

// The master control: runs the steps in order.
public static class KwicA
{
    public static List<string> Run(IEnumerable<string> lines)
    {
        Store.Clear();
        Input.Read(lines);
        Shifter.Build();
        Alphabetizer.Sort();
        return Output.Lines();
    }
}
