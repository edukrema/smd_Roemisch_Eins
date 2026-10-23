namespace Game.Api.Auth;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id => httpContextAccessor.HttpContext!.User.GetUserId();
}
