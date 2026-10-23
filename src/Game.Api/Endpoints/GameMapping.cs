namespace Game.Api.Endpoints;

using Game.Api.Data;
using Game.Contracts;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>Maps entities to contracts. Entities never leave the Api.</summary>
public static class GameMapping
{
    public static GameStatusDto ToDto(GameStatus status) => status switch
    {
        GameStatus.Lobby => GameStatusDto.Lobby,
        GameStatus.Running => GameStatusDto.Running,
        GameStatus.Finished => GameStatusDto.Finished,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static async Task<GameDto> ToDtoAsync(GameDbContext db, Game game)
    {
        var userIds = game.Participants.Select(p => p.UserId).Append(game.GameMasterId).ToList();
        var names = await db.UserProfiles
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username ?? "");

        var participants = game.Participants
            .OrderBy(p => p.JoinedAt)
            .Select(p => new ParticipantDto(p.UserId, names[p.UserId], ToDto(p.Attributes)))
            .ToList();

        return new GameDto(game.Id, game.GameMasterId, names[game.GameMasterId], ToDto(game.Status), game.MaxPlayers, participants);
    }

    public static IQueryable<GameSummaryDto> ToSummaries(this IQueryable<Game> games, GameDbContext db) =>
        from game in games
        join gameMaster in db.UserProfiles on game.GameMasterId equals gameMaster.Id
        orderby game.CreatedAt descending
        select new GameSummaryDto(
            game.Id,
            gameMaster.Username ?? "",
            ToDto(game.Status),
            game.Participants.Count,
            game.MaxPlayers);

    private static List<AttributeDto> ToDto(IEnumerable<ParticipantAttribute> attributes) =>
        attributes.OrderBy(a => a.Key).Select(a => new AttributeDto(a.Key, a.Value)).ToList();
}
