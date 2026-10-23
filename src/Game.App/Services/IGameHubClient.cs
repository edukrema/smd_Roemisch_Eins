namespace Game.App.Services;

/// <summary>
/// Receives signals from the server. The events arrive on the UI thread.
/// The connection only exists while a lobby list or a game is being watched.
/// </summary>
public interface IGameHubClient
{
    event Action<Guid>? GameChanged;

    event Action? LobbiesChanged;

    Task WatchGameAsync(Guid gameId);

    Task UnwatchGameAsync();

    Task WatchLobbiesAsync();

    Task UnwatchLobbiesAsync();
}
