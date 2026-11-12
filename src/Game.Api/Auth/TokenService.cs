namespace Game.Api.Auth;

using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

/// <summary>Issues the Api's own JWTs (HS256 with a symmetric key from configuration).</summary>
public class TokenService(IConfiguration configuration)
{
    public const string Issuer = "game-api";

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public string CreateToken(Guid userId)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Issuer,
            Expires = DateTime.UtcNow.Add(Lifetime),
            Subject = new ClaimsIdentity([new Claim(ClaimsPrincipalExtensions.UserIdClaim, userId.ToString())]),
            SigningCredentials = new SigningCredentials(GetSigningKey(configuration), SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    // REQ17: signing key from configuration
    public static SymmetricSecurityKey GetSigningKey(IConfiguration configuration) =>
        new(Encoding.UTF8.GetBytes(configuration["Auth:SigningKey"]
            ?? throw new InvalidOperationException("Auth:SigningKey is not configured.")));
}
