namespace Game.Api.Endpoints;

using Game.Api.Auth;
using Game.Api.Data;
using Game.Contracts;
using Game.Domain;
using Microsoft.EntityFrameworkCore;

public class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", Login).AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Signs in and returns a JWT; an unknown username registers a new account.")
            .ProducesProblems(400, 409);
    }

    /// <summary>Signs in; an unknown username registers a new account on the fly.</summary>
    private static async Task<LoginResponse> Login(LoginRequest request, GameDbContext db, TokenService tokens)
    {
        var username = (request.Username ?? "").Trim();
        if (username.Length == 0 || string.IsNullOrEmpty(request.Password))
        {
            throw new ValidationException("Username and password are required.");
        }

        var account = await db.UserAccounts.SingleOrDefaultAsync(a => a.Username == username)
                      ?? await RegisterAsync(db, username, request.Password);
        if (!account.VerifyPassword(request.Password))
        {
            throw new DomainException("Wrong password.");
        }

        return new LoginResponse(tokens.CreateToken(account.Id));
    }

    /// <summary>Creates the account together with its profile, so the login name is the username right away.</summary>
    private static async Task<UserAccount> RegisterAsync(GameDbContext db, string username, string password)
    {
        var account = UserAccount.Create(username, password);
        var profile = new UserProfile(account.Id);
        profile.ChangeUsername(username);

        db.UserAccounts.Add(account);
        db.UserProfiles.Add(profile);

        try
        {
            await db.SaveChangesAsync();
            return account;
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            throw new DomainException("The username is already taken.");
        }
    }
}
