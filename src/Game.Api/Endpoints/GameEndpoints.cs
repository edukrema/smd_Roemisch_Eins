namespace Game.Api.Endpoints;

using Game.Api.Auth;
using Game.Api.Data;
using Game.Api.Errors;
using Game.Api.Hubs;
using Game.Contracts;
using Game.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

public class GameEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/games");
        games.MapGet("/", GetOpenLobbies);
        games.MapPost("/", CreateGame);
        games.MapGet("/{id:guid}", GetGame);
        games.MapDelete("/{id:guid}", DeleteGame);
        games.MapPost("/{id:guid}/join", Join);
        games.MapPost("/{id:guid}/leave", Leave);
        games.MapPost("/{id:guid}/start", Start);
        games.MapPost("/{id:guid}/end", End);
        games.MapPut("/{id:guid}/settings", UpdateSettings);
        games.MapPut("/{id:guid}/participants/{userId:guid}/attributes", ReplaceAttributes);
    }

    /// <summary>Open lobbies in which the user is neither game master nor player.</summary>
    private static async Task<List<GameSummaryDto>> GetOpenLobbies(GameDbContext db, ICurrentUser currentUser)
    {
        var userId = currentUser.Id;
        return await db.Games
            .Where(g => g.Status == GameStatus.Lobby
                        && g.GameMasterId != userId
                        && !g.Participants.Any(p => p.UserId == userId))
            .ToSummaries(db)
            .ToListAsync();
    }

    private static async Task<Created<GameDto>> CreateGame(
        GameDbContext db, ICurrentUser currentUser, IGameNotifier notifier, ILogger<GameEndpoints> logger)
    {
        var gameMaster = await db.GetOrCreateProfileAsync(currentUser.Id);
        var game = Game.Create(gameMaster.Id, Game.DefaultMaxPlayers);
        db.Games.Add(game);
        await db.SaveChangesAsync();
        await notifier.GameChangedAsync(game.Id);

        logger.LogInformation("Game {GameId} created by {UserId}", game.Id, gameMaster.Id);
        return TypedResults.Created($"/games/{game.Id}", await GameMapping.ToDtoAsync(db, game));
    }

    private static async Task<GameDto> GetGame(Guid id, GameDbContext db, ICurrentUser currentUser)
    {
        var game = await db.Games
                       .AsNoTracking()
                       .Include(g => g.Participants)
                       .ThenInclude(p => p.Attributes)
                       .SingleOrDefaultAsync(g => g.Id == id)
                   ?? throw new NotFoundException("The game was not found.");

        if (!game.IsGameMaster(currentUser.Id) && !game.IsParticipant(currentUser.Id))
        {
            throw new ForbiddenException("Only the game master and the players can see this game.");
        }

        return await GameMapping.ToDtoAsync(db, game);
    }

    private static async Task<NoContent> DeleteGame(
        Guid id, GameDbContext db, ICurrentUser currentUser, IGameNotifier notifier, ILogger<GameEndpoints> logger)
    {
        await db.UpdateGameAsync(id, game =>
        {
            EnsureGameMaster(game, currentUser.Id);
            game.EnsureCanBeDeleted();
            db.Games.Remove(game);
        });
        await notifier.GameChangedAsync(id);

        logger.LogInformation("Game {GameId} deleted", id);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Join(
        Guid id, GameDbContext db, ICurrentUser currentUser, IGameNotifier notifier, ILogger<GameEndpoints> logger)
    {
        var user = await db.GetOrCreateProfileAsync(currentUser.Id);
        await db.UpdateGameAsync(id, game => game.Join(user));
        await notifier.GameChangedAsync(id);

        logger.LogInformation("User {UserId} joined game {GameId}", user.Id, id);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Leave(
        Guid id, GameDbContext db, ICurrentUser currentUser, IGameNotifier notifier, ILogger<GameEndpoints> logger)
    {
        await db.UpdateGameAsync(id, game => game.Leave(currentUser.Id));
        await notifier.GameChangedAsync(id);

        logger.LogInformation("User {UserId} left game {GameId}", currentUser.Id, id);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Start(
        Guid id, GameDbContext db, ICurrentUser currentUser, IGameNotifier notifier, ILogger<GameEndpoints> logger)
    {
        await db.UpdateGameAsync(id, game =>
        {
            EnsureGameMaster(game, currentUser.Id);
            game.Start();
        });
        await notifier.GameChangedAsync(id);

        logger.LogInformation("Game {GameId} started", id);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> End(
        Guid id, GameDbContext db, ICurrentUser currentUser, IGameNotifier notifier, ILogger<GameEndpoints> logger)
    {
        await db.UpdateGameAsync(id, game =>
        {
            EnsureGameMaster(game, currentUser.Id);
            game.End();
        });
        await notifier.GameChangedAsync(id);

        logger.LogInformation("Game {GameId} ended", id);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UpdateSettings(
        Guid id, UpdateSettingsRequest request, GameDbContext db, ICurrentUser currentUser, IGameNotifier notifier)
    {
        await db.UpdateGameAsync(id, game =>
        {
            EnsureGameMaster(game, currentUser.Id);
            game.UpdateSettings(request.MaxPlayers);
        });
        await notifier.GameChangedAsync(id);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ReplaceAttributes(
        Guid id, Guid userId, List<AttributeDto?> attributes, GameDbContext db, ICurrentUser currentUser,
        IGameNotifier notifier)
    {
        if (attributes.Contains(null))
        {
            throw new ValidationException("The attribute list must not contain empty entries.");
        }

        await db.UpdateGameAsync(id, game =>
        {
            EnsureGameMaster(game, currentUser.Id);
            game.ReplaceAttributes(userId, attributes.Select(a => (a!.Key, a.Value)).ToList());
        });
        await notifier.GameChangedAsync(id);

        return TypedResults.NoContent();
    }

    private static void EnsureGameMaster(Game game, Guid userId)
    {
        if (!game.IsGameMaster(userId))
        {
            throw new ForbiddenException("Only the game master may perform this action.");
        }
    }
}
