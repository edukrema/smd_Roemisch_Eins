namespace Game.App.Services;

using Game.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

public class GameHubClient : IGameHubClient
{
    private readonly HubConnection _connection;
    private readonly IDispatcher _dispatcher;
    private Guid? _watchedGameId;
    private bool _watchingLobbies;

    public GameHubClient(IAuthService authService, IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _connection = new HubConnectionBuilder()
            .WithUrl(AppSettings.ApiBaseUrl + GameHubContract.Path,
                options =>
                {
                    options.AccessTokenProvider = authService.GetAccessTokenAsync;
                    options.HttpMessageHandlerFactory = _ => DevCertificate.CreateHandler();
                    options.WebSocketConfiguration = socket => socket.RemoteCertificateValidationCallback =
                        (_, certificate, _, errors) => DevCertificate.IsValid(new Uri(AppSettings.ApiBaseUrl).Host, certificate, errors);
                })
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

    // SignalR callbacks arrive on a background thread; subscribers touch the UI.
    private void RaiseGameChanged(Guid gameId) => _dispatcher.Dispatch(() => GameChanged?.Invoke(gameId));

    private void RaiseLobbiesChanged() => _dispatcher.Dispatch(() => LobbiesChanged?.Invoke());
}
