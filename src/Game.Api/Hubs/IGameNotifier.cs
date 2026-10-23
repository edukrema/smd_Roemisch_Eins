namespace Game.Api.Hubs;

public interface IGameNotifier
{
    /// <summary>Reports a change to a game. Call only after a successful commit.</summary>
    Task GameChangedAsync(Guid gameId);
}
