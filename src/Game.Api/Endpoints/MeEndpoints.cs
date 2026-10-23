namespace Game.Api.Endpoints;

using Game.Api.Auth;
using Game.Api.Data;
using Game.Contracts;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

public class MeEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/me");
        me.MapGet("/", GetMe);
        me.MapPut("/username", ChangeUsername);
    }

    private static async Task<MeDto> GetMe(GameDbContext db, ICurrentUser currentUser)
    {
        var profile = await db.GetOrCreateProfileAsync(currentUser.Id);
        return new MeDto(profile.Id, profile.Username);
    }

    private static async Task<MeDto> ChangeUsername(ChangeUsernameRequest request, GameDbContext db, ICurrentUser currentUser)
    {
        var profile = await db.GetOrCreateProfileAsync(currentUser.Id);
        profile.ChangeUsername(request.Username);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            throw new DomainException("The username is already taken.");
        }

        return new MeDto(profile.Id, profile.Username);
    }
}
