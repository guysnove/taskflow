using System.Security.Claims;

namespace TaskFlow.Services;

// Who is signed in, for the current request (Chapter 25).
public interface ICurrentUser
{
    bool IsSignedIn { get; }
    int Id { get; }          // throws if nobody is signed in: a bug we want to hear about
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsSignedIn => Principal?.Identity?.IsAuthenticated == true;

    // The user's id was stored in the sign-in cookie as the NameIdentifier claim.
    public int Id => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out int id)
        ? id
        : throw new InvalidOperationException("No user is signed in.");
}
