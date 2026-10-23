namespace Game.App.Services;

public interface IAuthService
{
    /// <summary>Signs in with username and password; a new username registers an account.
    /// Throws ApiException when the Api rejects the login.</summary>
    Task LoginAsync(string username, string password);

    /// <summary>Access token of this instance's session or null if not signed in.</summary>
    Task<string?> GetAccessTokenAsync();

    Task LogoutAsync();
}
