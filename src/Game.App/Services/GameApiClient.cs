namespace Game.App.Services;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Game.Contracts;

public class GameApiClient(HttpClient httpClient, IAuthService authService) : IGameApiClient
{
    public Task<MeDto> GetMeAsync() => SendAsync<MeDto>(HttpMethod.Get, "me");

    public Task<MeDto> ChangeUsernameAsync(string username) =>
        SendAsync<MeDto>(HttpMethod.Put, "me/username", new ChangeUsernameRequest(username));

    public Task<List<GameSummaryDto>> GetOpenLobbiesAsync() => SendAsync<List<GameSummaryDto>>(HttpMethod.Get, "games");

    public Task<GameDto> CreateGameAsync() => SendAsync<GameDto>(HttpMethod.Post, "games");

    public Task<GameDto> GetGameAsync(Guid gameId) => SendAsync<GameDto>(HttpMethod.Get, $"games/{gameId}");

    public Task JoinAsync(Guid gameId) => SendAsync(HttpMethod.Post, $"games/{gameId}/join");

    public Task LeaveAsync(Guid gameId) => SendAsync(HttpMethod.Post, $"games/{gameId}/leave");

    public Task StartAsync(Guid gameId) => SendAsync(HttpMethod.Post, $"games/{gameId}/start");

    public Task EndAsync(Guid gameId) => SendAsync(HttpMethod.Post, $"games/{gameId}/end");

    public Task DeleteAsync(Guid gameId) => SendAsync(HttpMethod.Delete, $"games/{gameId}");

    public Task UpdateSettingsAsync(Guid gameId, int maxPlayers) =>
        SendAsync(HttpMethod.Put, $"games/{gameId}/settings", new UpdateSettingsRequest(maxPlayers));

    public Task ReplaceAttributesAsync(Guid gameId, Guid userId, IReadOnlyList<AttributeDto> attributes) =>
        SendAsync(HttpMethod.Put, $"games/{gameId}/participants/{userId}/attributes", attributes);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendRequestAsync(method, path, body);
        return await response.Content.ReadFromJsonAsync<T>()
               ?? throw new ApiException(response.StatusCode, "Empty response from the server.");
    }

    private async Task SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendRequestAsync(method, path, body);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string path, object? body)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var token = await authService.GetAccessTokenAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                throw await ApiException.FromResponseAsync(response);
            }
        }

        return response;
    }
}
