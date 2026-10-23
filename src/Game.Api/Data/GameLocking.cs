namespace Game.Api.Data;

using Game.Api.Errors;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>Pessimistic locking for all changes to a game (ADR-0005).</summary>
public static class GameLocking
{
    /// <summary>Locks the game, applies the change and saves everything in one transaction.</summary>
    public static async Task UpdateGameAsync(this GameDbContext db, Guid gameId, Action<Game> change)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        var game = await db.LoadGameForUpdateAsync(gameId);
        change(game);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    /// <summary>
    /// Locks the game row with SELECT ... FOR UPDATE until the end of the transaction.
    /// Concurrent actions on the same game therefore wait and see the new state afterwards.
    /// </summary>
    public static async Task<Game> LoadGameForUpdateAsync(this GameDbContext db, Guid gameId)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A game may only be locked inside a transaction.");
        }

        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Games" WHERE "Id" = {gameId} FOR UPDATE""");

        return await db.Games
                   .Include(g => g.Participants)
                   .ThenInclude(p => p.Attributes)
                   .SingleOrDefaultAsync(g => g.Id == gameId)
               ?? throw new NotFoundException("The game was not found.");
    }
}
