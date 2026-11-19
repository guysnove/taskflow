using System.Security.Claims;
using TaskFlow.Services;

namespace TaskFlow.Security;

// The web's answer to ICurrentUser: the signed-in user of the current request.
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
