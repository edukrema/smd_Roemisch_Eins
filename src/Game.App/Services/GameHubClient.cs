namespace Game.App.Services;

using Game.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

public class GameHubClient : IGameHubClient
{
    private readonly HubConnection _connection;
    private Guid? _watchedGameId;
    private bool _watchingLobbies;

    public GameHubClient(IAuthService authService)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(AppSettings.ApiBaseUrl + GameHubContract.Path,
                options => options.AccessTokenProvider = authService.GetAccessTokenAsync)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<Guid>(GameHubContract.GameChanged, RaiseGameChanged);
        _connection.On(GameHubContract.LobbiesChanged, RaiseLobbiesChanged);
        _connection.Reconnected += OnReconnectedAsync;
    }

    public event Action<Guid>? GameChanged;

    public event Action? LobbiesChanged;

    public async Task WatchGameAsync(Guid gameId)
    {
        await EnsureConnectedAsync();
        await _connection.InvokeAsync(GameHubContract.JoinGame, gameId);
        _watchedGameId = gameId;
    }

    public async Task UnwatchGameAsync()
    {
        if (_watchedGameId is { } gameId && _connection.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync(GameHubContract.LeaveGame, gameId);
        }

        _watchedGameId = null;
        await StopIfUnusedAsync();
    }

    public async Task WatchLobbiesAsync()
    {
        await EnsureConnectedAsync();
        await _connection.InvokeAsync(GameHubContract.JoinLobbyList);
        _watchingLobbies = true;
    }

    public async Task UnwatchLobbiesAsync()
    {
        if (_watchingLobbies && _connection.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync(GameHubContract.LeaveLobbyList);
        }

        _watchingLobbies = false;
        await StopIfUnusedAsync();
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
        }
    }

    private async Task StopIfUnusedAsync()
    {
        if (_watchedGameId is null && !_watchingLobbies)
        {
            await _connection.StopAsync();
        }
    }

    /// <summary>
    /// After a connection loss the groups on the server are gone. Rejoin and trigger a
    /// reload because signals may have been missed in the meantime.
    /// </summary>
    private async Task OnReconnectedAsync(string? connectionId)
    {
        if (_watchedGameId is { } gameId)
        {
            await _connection.InvokeAsync(GameHubContract.JoinGame, gameId);
            RaiseGameChanged(gameId);
        }

        if (_watchingLobbies)
        {
            await _connection.InvokeAsync(GameHubContract.JoinLobbyList);
            RaiseLobbiesChanged();
        }
    }

    private void RaiseGameChanged(Guid gameId) => MainThread.BeginInvokeOnMainThread(() => GameChanged?.Invoke(gameId));

    private void RaiseLobbiesChanged() => MainThread.BeginInvokeOnMainThread(() => LobbiesChanged?.Invoke());
}
