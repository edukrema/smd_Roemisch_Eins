namespace Game.App.Services;

using Game.Contracts;

public interface IGameApiClient
{
    Task<MeDto> GetMeAsync();

    Task<MeDto> ChangeUsernameAsync(string username);

    Task<List<GameSummaryDto>> GetOpenLobbiesAsync();

    Task<GameDto> CreateGameAsync();

    Task<GameDto> GetGameAsync(Guid gameId);

    Task JoinAsync(Guid gameId);

    Task LeaveAsync(Guid gameId);

    Task StartAsync(Guid gameId);

    Task EndAsync(Guid gameId);

    Task DeleteAsync(Guid gameId);

    Task UpdateSettingsAsync(Guid gameId, int maxPlayers);

    Task ReplaceAttributesAsync(Guid gameId, Guid userId, IReadOnlyList<AttributeDto> attributes);
}
