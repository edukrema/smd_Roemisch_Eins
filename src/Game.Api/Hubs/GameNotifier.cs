namespace Game.Api.Hubs;

using Game.Contracts;
using Microsoft.AspNetCore.SignalR;

// REQ10: server push via SignalR
public class GameNotifier(IHubContext<GameHub> hubContext) : IGameNotifier
{
    public async Task GameChangedAsync(Guid gameId)
    {
        await hubContext.Clients.Group(GameHub.GameGroup(gameId)).SendAsync(GameHubContract.GameChanged, gameId);

        // Almost every change (player count, status, deletion) also affects the lobby list.
        await hubContext.Clients.Group(GameHub.LobbyListGroup).SendAsync(GameHubContract.LobbiesChanged);
    }
}
