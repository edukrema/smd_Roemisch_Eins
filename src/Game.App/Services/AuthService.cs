namespace Game.App.Services;

using System.Net.Http.Json;
using Game.Contracts;

/// <summary>
/// Username/password login against the Api. The token is kept in memory only, so every
/// app instance has its own session — handy for testing with several users at once.
/// </summary>
public class AuthService(HttpClient httpClient) : IAuthService
{
    private string? _accessToken;

    public async Task LoginAsync(string username, string password)
    {
        using var response = await httpClient.PostAsJsonAsync("auth/login", new LoginRequest(username, password));
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiException.FromResponseAsync(response);
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _accessToken = login!.Token;
    }

    public Task<string?> GetAccessTokenAsync() => Task.FromResult(_accessToken);

    public Task LogoutAsync()
    {
        _accessToken = null;
        return Task.CompletedTask;
    }
}
