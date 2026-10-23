namespace Game.Api.Auth;

public interface ICurrentUser
{
    /// <summary>The Keycloak ID (sub claim) of the logged-in user.</summary>
    Guid Id { get; }
}
