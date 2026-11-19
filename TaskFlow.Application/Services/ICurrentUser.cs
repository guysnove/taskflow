namespace TaskFlow.Services;

// Who is signed in, for the current request (Chapter 25).
public interface ICurrentUser
{
    bool IsSignedIn { get; }
    int Id { get; }          // throws if nobody is signed in: a bug we want to hear about
}
