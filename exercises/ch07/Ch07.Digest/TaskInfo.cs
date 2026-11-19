namespace Ch07.Digest;

// The parts of a TaskFlow task that a digest needs.
public record TaskInfo(int Id, int UserId, string Title, string Priority, DateOnly? Due, bool Done, int StepsDone, int StepsTotal);
