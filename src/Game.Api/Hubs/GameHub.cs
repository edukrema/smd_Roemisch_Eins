namespace Game.Api.Hubs;

using Game.Api.Auth;
using Game.Api.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

/// <summary>Server-to-client signals only. Clients can only join and leave groups.</summary>
public class GameHub(GameDbContext db) : Hub
{
    public const string LobbyListGroup = "lobby-list";

    public static string GameGroup(Guid gameId) => $"game-{gameId}";

    public async Task JoinGame(Guid gameId)
    {
        var userId = Context.User!.GetUserId();
        var isMember = await db.Games.AnyAsync(g =>
            g.Id == gameId && (g.GameMasterId == userId || g.Participants.Any(p => p.UserId == userId)));

        if (!isMember)
        {
            throw new HubException("Only the game master and the players receive signals for this game.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GameGroup(gameId));
    }

    public Task LeaveGame(Guid gameId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GameGroup(gameId));

    public Task JoinLobbyList() => Groups.AddToGroupAsync(Context.ConnectionId, LobbyListGroup);

    public Task LeaveLobbyList() => Groups.RemoveFromGroupAsync(Context.ConnectionId, LobbyListGroup);
}
