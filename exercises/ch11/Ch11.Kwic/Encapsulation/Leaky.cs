namespace Ch11.Kwic.Encapsulation;

// NOT encapsulated: the data is public, and a rule ("OpenCount = tasks not done") lives in callers' heads.
public class LeakyProject
{
    public List<string> Tasks = new();
    public List<string> Done = new();
    public int OpenCount;                       // every caller must remember to update this
}
