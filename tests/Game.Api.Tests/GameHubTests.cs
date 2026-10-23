namespace Game.Api.Tests;

using Game.Api.Tests.Infrastructure;
using Game.Contracts;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

public class GameHubTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task GameChanged_IsSentToGroupWhenPlayerJoins()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);
        await using var connection = await ConnectAsync(gameMaster.Id);
        var signal = new TaskCompletionSource<Guid>();
        connection.On<Guid>(GameHubContract.GameChanged, id => signal.TrySetResult(id));
        await connection.InvokeAsync(GameHubContract.JoinGame, gameId);

        await PostAsync(await CreateUserAsync("player"), $"/games/{gameId}/join");

        Assert.Equal(gameId, await signal.Task.WaitAsync(SignalTimeout));
    }

    [Fact]
    public async Task LobbiesChanged_IsSentToLobbyListWhenGameIsCreated()
    {
        var player = await CreateUserAsync("player");
        await using var connection = await ConnectAsync(player.Id);
        var signal = new TaskCompletionSource();
        connection.On(GameHubContract.LobbiesChanged, () => signal.TrySetResult());
        await connection.InvokeAsync(GameHubContract.JoinLobbyList);

        await CreateGameAsync(await CreateUserAsync("master"));

        await signal.Task.WaitAsync(SignalTimeout);
    }

    [Fact]
    public async Task JoinGame_AsOutsider_IsRejected()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var outsider = await CreateUserAsync("outsider");
        await using var connection = await ConnectAsync(outsider.Id);

        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync(GameHubContract.JoinGame, gameId));
    }

    [Fact]
    public async Task Connect_WithoutLogin_Fails()
    {
        await using var connection = CreateConnection(userId: null);

        await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
    }

    private async Task<HubConnection> ConnectAsync(Guid userId)
    {
        var connection = CreateConnection(userId);
        await connection.StartAsync();
        return connection;
    }

    private HubConnection CreateConnection(Guid? userId) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, GameHubContract.Path), options =>
            {
                // The TestServer only supports long polling; WebSockets need a real server.
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (userId is not null)
                {
                    options.Headers[TestAuthHandler.UserHeader] = userId.ToString()!;
                }
            })
            .Build();
}
