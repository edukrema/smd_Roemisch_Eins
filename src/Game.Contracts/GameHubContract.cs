namespace Game.Contracts;

/// <summary>
/// Names for the SignalR hub. The hub only sends signals; the client reloads the data via REST.
/// </summary>
public static class GameHubContract
{
    public const string Path = "/hubs/game";

    /// <summary>Server to client, argument: the GameId.</summary>
    public const string GameChanged = "GameChanged";

    /// <summary>Server to client, no argument.</summary>
    public const string LobbiesChanged = "LobbiesChanged";

    public const string JoinGame = "JoinGame";
    public const string LeaveGame = "LeaveGame";
    public const string JoinLobbyList = "JoinLobbyList";
    public const string LeaveLobbyList = "LeaveLobbyList";
}
