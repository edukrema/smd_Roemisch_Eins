namespace Game.Api.Auth;

using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public const string UserIdClaim = "sub";

    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(UserIdClaim), out var id)
            ? id
            : throw new UnauthorizedAccessException("The token does not contain a valid user ID.");
}
