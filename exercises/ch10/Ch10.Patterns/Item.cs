namespace Ch10.Patterns;

// A small task record, shared by the examples.
public sealed record Item(int Id, string Title, string Priority, DateOnly? Due, bool Done = false);
